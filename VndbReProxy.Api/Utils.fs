module VndbReProxy.Api.Utils

open Giraffe

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
