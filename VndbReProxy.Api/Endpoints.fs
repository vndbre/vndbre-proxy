module VndbReProxy.Api.Endpoints

open System
open System.IO
open FSharp.Control
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.QueryReader
open Microsoft.Extensions.Logging
open VndbReProxy.Api.Utils
open VndbReProxy.Proto

type private v1vndbHandler = V1vndbHandler

let v1vndbHandler (login: string option) (password: string option) : HttpHandler =
    inject1<ILogger<v1vndbHandler>>
    ^ fun logger next ctx ->
        logger.LogInformation("Started request handling")

        let lp =
            match login, password with
            | Some login, Some password ->
                logger.LogTrace("Using login and password")
                Some(login, password)
            | _ ->
                logger.LogTrace("Not using login and password")
                None

        task {
            let bodyStream = ctx.Request.Body
            use sr = new StreamReader(bodyStream)
            logger.LogTrace("Started body reading")

            let! body = sr.ReadToEndAsync()
            logger.LogTrace("Ended body reading")

            logger.LogTrace("Create connection (client)")

            use client =
                Connection.client Tls Connection.defaultConf

            logger.LogTrace("Create connection (stream)")

            use stream =
                Connection.stream Tls Connection.defaultConf client

            logger.LogTrace("Send login command")

            let! w =
                Request.login Connection.defaultConf lp
                |> Request.send stream

            match w with
            | Response.t.Ok ->
                let! ans = Request.send stream body
                logger.LogDebug($"{Response.toResponseName ans} Response")
                return! returnResponse false ans next ctx
            | _ ->
                logger.LogDebug("Auth failed")
                return! returnResponse true w next ctx
        }

let endpoints =
    [ POST
      =@> route "/api/v1/vndb"
          ^ Query.read ("login", "password", v1vndbHandler) ]
