namespace VndbReProxy.Proto

open System
open System.IO
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

module Proto =
    type error =
        | ReceiveError
        | EncodeError

    let stopByte = byte 0x04

    let readByte buff (stream: NetworkStream) =
        task {
            let! cnt = stream.ReadAsync(buff, 0, 1)

            if cnt = 1 then
                return ValueSome buff.[0]
            else
                return ValueNone
        }

    let rec private nextMsgAux stream buff acc =
        task {
            let! b = readByte buff stream

            match b with
            | ValueSome bt when bt = stopByte -> return acc |> Some
            | ValueSome bt -> return! nextMsgAux stream buff (bt :: acc)
            | ValueNone -> return None
        }

    let rec private nextMsgRef stream buff acc =
        task {
            let ret = ref None
            let acc = ref acc

            while Option.isNone !ret do
                let! b = readByte buff stream

                match b with
                | ValueSome bt when bt = stopByte -> ret := Some !acc
                | ValueSome bt -> acc := bt :: !acc
                | ValueNone -> ret := None

            return !ret
        }

    let nextMsg stream =
        task {
            let buff = [| byte 0 |]

            match! nextMsgRef stream buff [] with
            | Some read ->
                let arr = read |> List.rev |> List.toArray

                try
                    return arr |> Encoding.UTF8.GetString |> Ok
                with
                | _ -> return Error EncodeError
            | None -> return Error ReceiveError
        }

module Response =
    type json = string

    type raw = string

    type error =
        | ProtoError of Proto.error
        | ParseError
        | SendError
        | UnknownError

    type t =
        | Results of json
        | Error of json
        | Ok
        | Unknown of raw
        | InternalError of error

    let (|StartsWithOrdinal|_|) str (message: string) =
        if message.StartsWith(str, StringComparison.Ordinal) then
            Some ^ message.Substring(str.Length)
        else
            None

    let parse (message: string) =
        let trim (str: string) = str.Trim()

        match message with
        | StartsWithOrdinal "results" j -> j |> trim |> Results
        | StartsWithOrdinal "error" j -> j |> trim |> Error
        | StartsWithOrdinal "ok" _ -> Ok
        | _ -> Unknown ^ trim message

    let parseResult =
        function
        | Result.Ok message -> parse message
        | Result.Error error -> InternalError ^ ProtoError error

    let toResponseName =
        function
        | Results _ -> "results"
        | Error _ -> "error"
        | Ok _ -> "ok"
        | Unknown _ -> "unknown"
        | InternalError _ -> "internalerror"

    let toHttpCode isAuth =
        function
        | Results _ -> StatusCodes.Status200OK
        | Error _ ->
            if isAuth then
                StatusCodes.Status401Unauthorized
            else
                StatusCodes.Status400BadRequest
        | Ok _ -> StatusCodes.Status204NoContent
        | Unknown _ -> StatusCodes.Status501NotImplemented
        | InternalError _ -> StatusCodes.Status500InternalServerError

    let writeData (writer: Utf8JsonWriter) =
        function
        | Results json
        | Error json ->
            writer.WritePropertyName("data")
            writer.WriteRawValue(json)
        | Ok -> writer.WriteNull("data")
        | Unknown raw -> writer.WriteString("data", raw)
        | InternalError error -> writer.WriteString("data", string error)

    let toJson t =
        use ms = new MemoryStream()

        let () =
            use writer = new Utf8JsonWriter(ms)
            writer.WriteStartObject()
            writer.WriteString("response", toResponseName t)
            writeData writer t
            writer.WriteEndObject()

        Encoding.UTF8.GetString(ms.ToArray())

module Connection =
    type conf =
        { Host: string
          Port: int
          PortTls: int
          Client: string
          ClientVer: string }

    let defaultConf =
        { Host = "api.vndb.org"
          Port = 19534
          PortTls = 19535
          Client = "vn-list"
          ClientVer = "0.0.1" }

    let connect conf =
        let a = new TcpClient(conf.Host, conf.Port)
        a

module Request =
    type t = string

    let login (conf: Connection.conf) login password : t =
        $"login {{\"protocol\":1,\"client\":\"%s{conf.Client}\",\"clientver\":\"%s{conf.ClientVer}\",\"username\":\"%s{login}\",\"password\":\"%s{password}\"}}"

    let private stopByteBuff = [| Proto.stopByte |]

    let private write (stream: NetworkStream) (a: t) =
        try
            task {
                let buf = Encoding.UTF8.GetBytes a

                do! stream.WriteAsync(buf, 0, buf.Length)
                do! stream.WriteAsync(stopByteBuff, 0, 1)
                return Ok()
            }
        with
        | _ -> Task.FromResult ^ Error ^ Response.error.SendError

    let send (stream: NetworkStream) (a: t) =
        task {
            match! write stream a with
            | Error err -> return Response.t.InternalError err
            | _ ->
                let! ans = Proto.nextMsg stream
                let ret = Response.parseResult ans

                return ret
        }
