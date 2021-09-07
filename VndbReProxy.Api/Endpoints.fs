module VndbReProxy.Api.Endpoints

open Giraffe
open Giraffe.EndpointRouting
open Giraffe.QueryReader

let private (=>) a b = a [ b ]

let handler1: HttpHandler =
    fun _ ctx -> ctx.WriteTextAsync "Hello World"

let handler2 (firstName: string, age: int) : HttpHandler =
    fun _ ctx ->
        sprintf "Hello %s, you are %i years old." firstName age
        |> ctx.WriteTextAsync

let handler3 (a: string, b: string, c: string, d: int) : HttpHandler =
    fun _ ctx ->
        sprintf "Hello %s %s %s %i" a b c d
        |> ctx.WriteTextAsync

let endpoints =
    [ GET => route "/" ^ Query.read ("to", text)
      GET => routef "/%s/%i" handler2
      GET => routef "/%s/%s/%s/%i" handler3
      subRoute
          "/sub"
          [
            // Not specifying a http verb means it will listen to all verbs
            route "/test" handler1 ] ]
