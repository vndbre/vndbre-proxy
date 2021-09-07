module VndbReProxy.Api.Endpoints

open System
open System.IO
open FSharp.Control.Tasks
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.QueryReader
open Microsoft.Extensions.Logging
open VndbReProxy.Api.Utils

let v1vndbHandler (login: string option) (password: string option) : HttpHandler =
    inject1<ILogger<Undefined>>
    ^ fun logger _ ctx ->
        let login, password =
            match login, password with
            | Some login, Some password -> login, password
            | _ -> undefined

        task {
            let bodyStream = ctx.Request.Body
            use sr = new StreamReader(bodyStream)
            let! body = sr.ReadToEndAsync()
            logger.LogInformation(string DateTimeOffset.Now)

            return!
                $"%s{login} %s{password} %s{body}"
                |> ctx.WriteTextAsync
        }

let endpoints =
    [ POST
      =@> route "/api/v1/vndb"
          ^ Query.read ("login", "password", v1vndbHandler) ]
