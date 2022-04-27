module VndbReProxy.Api.Endpoints

open Giraffe
open Giraffe.EndpointRouting
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
      =@> route "/api/v2/vndb-set" (HandlersV2.Vndb.handler true)
      POST
      =@> route "/api/v2/login" HandlersV2.Login.handler
      POST
      =@> route "/api/v2/logout" HandlersV2.Logout.handler
      POST
      =@> route "/api/v2/vndb" (HandlersV2.Vndb.handler false)
      POST
      =@> routeArray "/api/v2/tags" HandlersV2.TagsTraits.byIdsTags
      GET
      =@> route "/api/v2/tags"
          ^ Query.read ("count", "offset", "name", HandlersV2.TagsTraits.getTags)
      POST
      =@> routeArray "/api/v2/traits" HandlersV2.TagsTraits.byIdsTraits
      GET
      =@> route "/api/v2/traits"
          ^ Query.read ("count", "offset", "name", HandlersV2.TagsTraits.getTraits)
      GET
      =@> route "/openapi.yaml" (yamlFile "Requests/openapi.yaml") ]

let endpoints =
    [ GET
      =@> route "/" (redirectTo true "https://vndbre.netlify.app/") ]
    @ endpointsV1 @ endpointsV2
