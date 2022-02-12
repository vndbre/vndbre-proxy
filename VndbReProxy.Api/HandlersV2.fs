module VndbReProxy.Api.HandlersV2

open System.IO
open FSharp.Control
open global.Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open VndbReProxy.Api.Services
open VndbReProxy.Api.Services.Tags
open VndbReProxy.Api.Services.Traits
open VndbReProxy.Api.Utils
open VndbReProxy.Proto

type LoginDto = { username: string; password: string }

let loginHandler : HttpHandler = undefined

let logoutHandler : HttpHandler = undefined

let vndbHandler isQuery : HttpHandler = undefined
