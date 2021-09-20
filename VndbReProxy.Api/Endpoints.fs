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

let v1vndbHandler (login: string option) (password: string option) : HttpHandler =
    inject1<ILogger<Undefined>>
    ^ fun logger next ctx ->
        let login, password =
            match login, password with
            | Some login, Some password -> login, password
            | _ -> undefined

        task {
            let bodyStream = ctx.Request.Body
            use sr = new StreamReader(bodyStream)
            let! body = sr.ReadToEndAsync()
            logger.LogInformation(string DateTimeOffset.Now)

            use conn =
                Connection.connect Connection.defaultConf

            let lq =
                Request.login Connection.defaultConf login password

            let s = conn.GetStream()
            let! w = Request.send s lq

            match w with
            | Response.t.Ok ->
                let! ans = Request.send s body
                return! returnResponse false ans next ctx
            | _ -> return! returnResponse true w next ctx
        }

let endpoints =
    [ POST
      =@> route "/api/v1/vndb"
          ^ Query.read ("login", "password", v1vndbHandler) ]
