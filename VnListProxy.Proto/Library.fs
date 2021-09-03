namespace VnListProxy.Proto

open System
open System.ComponentModel.DataAnnotations
open System.IO
open System.Text
open System.Text.Json

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

    let private trim (str: string) = str.Trim().TrimEnd(char 0x04)

    let parse (message: string) =
        match message with
        | StartsWithOrdinal "results" j -> j |> trim |> Results
        | StartsWithOrdinal "error" j -> j |> trim |> Error
        | StartsWithOrdinal "ok" j -> j |> trim |> Results
        | _ -> Unknown ^ trim message

    type dto =
        { [<Required>]
          response: string
          data: string }

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
