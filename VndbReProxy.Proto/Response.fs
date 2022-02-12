module VndbReProxy.Proto.Response

open System
open System.IO
open System.Text
open System.Text.Json
open Microsoft.AspNetCore.Http

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
    | DbStats of json
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
    | StartsWithOrdinal "dbstats" j -> j |> trim |> DbStats
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
    | DbStats _ -> "dbstats"
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
    | DbStats _ -> StatusCodes.Status200OK
    | Unknown _ -> StatusCodes.Status501NotImplemented
    | InternalError _ -> StatusCodes.Status500InternalServerError

let writeData (writer: Utf8JsonWriter) =
    function
    | Results json
    | Error json
    | DbStats json ->
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
