namespace VnListProxy.Proto

open System
open System.IO
open System.Net.Sockets
open System.Text
open System.Text.Json
open FSharp.Control.Tasks

[<AutoOpen>]
module Prelude =
    let inline (^) a b = a b

module Request =
    type t = T of string

module Response =
    type json = string

    type t =
        | Results of json
        | Error of json
        | Ok
        | Unknown of string

    let (|StartsWithOrdinal|_|) str (message: string) =
        if message.StartsWith(str, StringComparison.Ordinal) then
            Some ^ message.Substring(str.Length)
        else
            None

    let parse (message: string) =
        let trim (str: string) = str.Trim().TrimEnd(char 0x04)

        match message with
        | StartsWithOrdinal "results" j -> j |> trim |> Results
        | StartsWithOrdinal "error" j -> j |> trim |> Error
        | StartsWithOrdinal "ok" j -> j |> trim |> Results
        | _ -> Unknown ^ trim message

    let toResponseName =
        function
        | Results _ -> "results"
        | Error _ -> "error"
        | Ok _ -> "ok"
        | Unknown _ -> "unknown"

    let writeData (writer: Utf8JsonWriter) =
        function
        | Results json
        | Error json ->
            writer.WriteStartObject("data")
            writer.WriteRawValue(json)
            writer.WriteEndObject()
        | Ok -> ()
        | Unknown raw -> writer.WriteString("rawdata", raw)

    let toJson t =
        use ms = new MemoryStream()
        use writer = new Utf8JsonWriter(ms)
        writer.WriteString("response", toResponseName t)
        writeData writer t
        Encoding.UTF8.GetString(ms.ToArray())

    let readByte (stream: NetworkStream) =
        task {
            let m = Memory<byte>([| byte 0 |])
            let! _ = stream.ReadAsync(m).AsTask()
            return m.ToArray().[0]
        }

    let nextMsg stream =
        let rec nextMsgAux acc =
            task {
                let! b = readByte stream

                if b = byte 0x04 then
                    return b :: acc
                else
                    return! nextMsgAux (b :: acc)
            }

        task {
            let! read = nextMsgAux []
            let arr = read |> List.rev |> List.toArray
            let str = Encoding.UTF8.GetString(arr)
            return str
        }

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

    let sendLogin (stream: NetworkStream) conf login password =
        let a =
            $"login {{\"protocol\":1,\"client\":\"%s{conf.Client}\",\"clientver\":\"%s{conf.ClientVer}\",\"username\":\"%s{login}\",\"password\":\"%s{password}\"}}"

        let buf = Encoding.UTF8.GetBytes a

        task {
            let! _ = stream.WriteAsync(buf, 0, buf.Length)
            let! _ = stream.WriteAsync([| byte 0x04 |], 0, 1)
            let! ans = Response.nextMsg stream
            return ans
        }
