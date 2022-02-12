module VndbReProxy.Api.Endpoints

open global.Giraffe
open VndbReProxy.Giraffe.EndpointRouting
open Giraffe.QueryReader
open VndbReProxy.Api.Utils

let endpointsV1 =
    [ POST
      =@> route "/api/v1/vndb"
          ^ Query.read ("login", "password", HandlersV1.v1vndbHandler)
      POST
      =@> routeArray "/api/v1/tags" HandlersV1.tagsHandler
      POST
      =@> routeArray "/api/v1/traits" HandlersV1.traitsHandler ]

let endpointsV2 =
    [ POST
      =@> route "/api/v2/vndb" (HandlersV2.vndbHandler false)
      QUERY
      =@> route "/api/v2/vndb" (HandlersV2.vndbHandler true)
      POST
      =@> route "/api/v2/login" HandlersV2.loginHandler
      POST
      =@> route "/api/v2/logout" HandlersV2.logoutHandler
      POST
      =@> routeArray "/api/v2/tags" HandlersV1.tagsHandler
      POST
      =@> routeArray "/api/v2/traits" HandlersV1.traitsHandler ]

let endpoints = endpointsV1 @ endpointsV2
