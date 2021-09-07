module VndbReProxy.Api.Endpoints

open System.IO
open FSharp.Control.Tasks
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.QueryReader

let private (=>) a b = a [ b ]

let v1vndbHandler (login: string option) (password: string option) : HttpHandler =
    fun _ ctx ->
        let login, password =
            match login, password with
            | Some login, Some password -> login, password
            | _ -> undefined

        task {
            let bodyStream = ctx.Request.Body
            use sr = new StreamReader(bodyStream)
            let! body = sr.ReadToEndAsync()

            return!
                $"%s{login} %s{password} %s{body}"
                |> ctx.WriteTextAsync
        }

let endpoints =
    [ POST
      => route "/api/v1/vndb"
         ^ Query.read ("login", "password", v1vndbHandler) ]
