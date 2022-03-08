module VndbReProxy.Api.HandlersV2

open System.IO
open System.Threading.Tasks
open FSharp.Control
open Giraffe
open Microsoft.AspNetCore.Http
open VndbReProxy.Api.Services
open VndbReProxy.Api.Services.Tags
open VndbReProxy.Api.Services.Traits
open VndbReProxy.Api.Utils
open VndbReProxy.Proto
open System
open System.Text
open System.Text.Json

module C = Connection
module Rq = Request
module Rs = Response

module Login =
    type LoginRequestDto = { username: string; password: string }
    type LoginResponseDto = { sessiontoken: string }

    let handler: HttpHandler =
        fun next ctx ->
            task {
                let! loginDto = ctx.BindJsonAsync<LoginRequestDto>()

                let loginCmd =
                    Rq.login C.defaultConf (Rq.CreateSession(loginDto.username, loginDto.password))

                use client = C.client C.Tls C.defaultConf
                use stream = C.stream C.Tls C.defaultConf client

                let! w = Rq.send stream loginCmd

                match w with
                | Rs.t.Session sessiontoken -> return! json { sessiontoken = sessiontoken } next ctx
                | _ -> return! returnResponse true w next ctx
            }

let private loginParamsFromHeaders (ctx: HttpContext) =
    let username = ctx.TryGetRequestHeader "username"
    let st = ctx.TryGetRequestHeader "sessiontoken"

    match username, st with
    | Some username, Some sessiontoken -> Rq.SessionToken(username, sessiontoken)
    | _ -> Rq.Anon

module Logout =
    let handler: HttpHandler =
        fun next ctx ->
            task {
                let lp = loginParamsFromHeaders ctx

                use client = C.client C.Tls C.defaultConf

                use stream = C.stream C.Tls C.defaultConf client

                let! w = Rq.login C.defaultConf lp |> Rq.send stream

                match w with
                | Rs.t.Ok ->
                    let! ans = Rq.send stream Rq.logout
                    return! returnResponse false ans next ctx
                | _ -> return! returnResponse true w next ctx
            }

module Vndb =
    let writeInnerData (writer: Utf8JsonWriter) =
        function
        | Rs.Raw json ->
            writer.WritePropertyName("data")
            writer.WriteRawValue(json)
        | Rs.Null -> writer.WriteNull("data")
        | Rs.String s -> writer.WriteString("data", s)

    type ResponseDto =
        { response: string
          data: Rs.InnerData
          cached: DateTimeOffset option option }

    let responseDtoToJson dto =
        use ms = new MemoryStream()

        let () =
            use writer = new Utf8JsonWriter(ms)
            writer.WriteStartObject()
            writer.WriteString("response", dto.response)
            writeInnerData writer dto.data

            dto.cached
            |> Option.iter
                (function
                | Some cached -> writer.WriteString("cached", cached)
                | None -> writer.WriteNull("cached"))

            writer.WriteEndObject()

        Encoding.UTF8.GetString(ms.ToArray())

    let responseToDto response cached =
        { response = Rs.toResponseName response
          data = Rs.toInnerData response
          cached = cached }

    let handler isSet : HttpHandler =
        fun next ctx ->
            task {
                let lp = loginParamsFromHeaders ctx

                use sr = new StreamReader(ctx.Request.Body)
                use client = C.client C.Tls C.defaultConf
                use stream = C.stream C.Tls C.defaultConf client

                let! loginResponse = Rq.login C.defaultConf lp |> Rq.send stream

                match loginResponse with
                | Response.t.Ok ->
                    let! body = sr.ReadToEndAsync()
                    let! ans = Request.send stream body

                    return!
                        jsonFromString
                            (responseToDto ans (if isSet then None else Some None)
                             |> responseDtoToJson)
                            next
                            ctx
                | _ -> return! returnResponse true loginResponse next ctx
            }

module TagsTraits =
    let count_offset s count offset =
        match count, offset with
        | _, Some offset when offset > Seq.length s -> Seq.empty
        | Some count, Some offset -> s |> Seq.skip offset |> Seq.truncate count
        | Some count, None -> s |> Seq.truncate count
        | None, Some offset -> s |> Seq.skip offset
        | None, None -> s

    let private get (count: int option) (offset: int option) (tService: IDumpService<int, _>) next ctx =
        task {
            let all = tService.TryGetAll()

            let! () =
                if Option.isNone all then
                    tService.Download()
                else
                    Task.FromResult()

            let all = tService.TryGetAll()

            match all with
            | Some all -> return! json (count_offset all count offset) next ctx
            | None -> return! emptyResponse StatusCodes.Status400BadRequest next ctx
        }

    let getTags count offset : HttpHandler =
        inject1<IDumpService<int, Tag>> ^ get count offset

    let getTraits count offset : HttpHandler =
        inject1<IDumpService<int, Trait>>
        ^ get count offset

    let private byIds ids (tagsService: IDumpService<int, _>) next ctx =
        task {
            let idHead = ids |> Array.head
            let idTail = ids |> Array.tail

            let! tagHead = tagsService.GetOrDownload idHead

            let tagTail =
                idTail
                |> Array.map tagsService.TryGet
                |> Array.choose id

            match tagHead with
            | Ok hd -> return! json (Array.append [| hd |] tagTail) next ctx
            | Error _ -> return! emptyResponse StatusCodes.Status400BadRequest next ctx
        }

    let byIdsTags ids : HttpHandler =
        inject1<IDumpService<int, Tag>> ^ byIds ids

    let byIdsTraits ids : HttpHandler =
        inject1<IDumpService<int, Trait>> ^ byIds ids
