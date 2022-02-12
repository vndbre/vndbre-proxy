module VndbReProxy.Api.Utils

open System.Net.Mime
open FSharp.Control
open global.Giraffe
open VndbReProxy.Giraffe.EndpointRouting
open VndbReProxy.Proto

let (=@>) a b = a [ b ]

let inject1<'T> (handler: _ -> HttpHandler) : HttpHandler =
    fun next ctx ->
        let service = ctx.GetService<'T>()
        handler service next ctx

let inject2<'T1, 'T2> (handler: _ -> _ -> HttpHandler) : HttpHandler =
    fun next ctx ->
        let s1 = ctx.GetService<'T1>()
        let s2 = ctx.GetService<'T2>()
        handler s1 s2 next ctx

let inject3<'T1, 'T2, 'T3> (handler: _ -> _ -> _ -> HttpHandler) : HttpHandler =
    fun next ctx ->
        let s1 = ctx.GetService<'T1>()
        let s2 = ctx.GetService<'T2>()
        let s3 = ctx.GetService<'T3>()
        handler s1 s2 s3 next ctx

let inject4<'T1, 'T2, 'T3, 'T4> (handler: _ -> _ -> _ -> _ -> HttpHandler) : HttpHandler =
    fun next ctx ->
        let s1 = ctx.GetService<'T1>()
        inject3<'T2, 'T3, 'T4> (handler s1) next ctx

let jsonFromString str : HttpHandler =
    fun _ ctx ->
        ctx.SetContentType MediaTypeNames.Application.Json
        ctx.WriteStringAsync str

let returnResponse isAuth (t: Response.t) : HttpHandler =
    fun next ctx ->
        ctx.SetStatusCode(Response.toHttpCode isAuth t)
        jsonFromString (Response.toJson t) next ctx

let emptyResponse code : HttpHandler =
    fun _next ctx ->
        ctx.SetStatusCode(code)
        ctx.WriteStringAsync("")

let routeArray<'T> path (handler: 'T array -> HttpHandler) =
    route
        path
        (fun next ctx ->
            task {
                let! a = ctx.BindJsonAsync<'T array>()
                return! (handler a) next ctx
            })
