namespace VnListProxy.Proto

open System
open System.IO
open System.Net.Sockets
open System.Text
open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks

[<AutoOpen>]
module Prelude =
    let inline (^) a b = a b

    module Option_Operators =
        let inline (>>=) a b = Option.bind b a

        let inline (>>|) a b = Option.map b a

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

    let nextMsg stream =
        let rec nextMsgAux buff acc =
            task {
                let! b = readByte buff stream

                match b with
                | ValueSome bt when bt = stopByte -> return acc |> Some
                | ValueSome bt -> return! nextMsgAux buff (bt :: acc)
                | ValueNone -> return None
            }

        task {
            let buff = [| byte 0 |]

            match! nextMsgAux buff [] with
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

    let toHttpCode =
        function
        | Results _ -> Microsoft.AspNetCore.Http.StatusCodes.Status200OK
        | Error _ -> Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest
        | Ok _ -> Microsoft.AspNetCore.Http.StatusCodes.Status200OK
        | Unknown _ -> Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError
        | InternalError _ -> Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError

    let writeData (writer: Utf8JsonWriter) =
        function
        | Results json
        | Error json ->
            writer.WritePropertyName("data")
            writer.WriteRawValue(json)
        | Ok -> ()
        | Unknown raw -> writer.WriteString("rawdata", raw)
        | InternalError error -> writer.WriteString("error", string error)

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
    type t =
        { Host: string
          Port: int
          PortTls: int
          Client: string
          ClientVer: string }

    let connect conf =
        let a = new TcpClient(conf.Host, conf.Port)
        a

module Request =
    type t = string

    let login (conf: Connection.t) login password : t =
        $"login {{\"protocol\":1,\"client\":\"%s{conf.Client}\",\"clientver\":\"%s{conf.ClientVer}\",\"username\":\"%s{login}\",\"password\":\"%s{password}\"}}"

    let private write (stream: NetworkStream) (a: t) =
        try
            task {
                let buf = Encoding.UTF8.GetBytes a

                do! stream.WriteAsync(buf, 0, buf.Length)
                do! stream.WriteAsync([| Proto.stopByte |], 0, 1)
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
