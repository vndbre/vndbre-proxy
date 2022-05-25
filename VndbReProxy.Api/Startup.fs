namespace VndbReProxy.Api

open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting

open VndbReProxy.Api.Services.Tags
open VndbReProxy.Api.Services.Traits

type Startup(_configuration: IConfiguration) =
    member _.ConfigureServices(services: IServiceCollection) =
        services.AddCors (fun options ->
            options.AddDefaultPolicy (fun builder ->
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                |> ignore<CorsPolicyBuilder>))
        |> ignore<IServiceCollection>

        services.AddRouting()
        |> ignore<IServiceCollection>

        //services.AddAuthorization()
        //|> ignore<IServiceCollection>

        services.AddLogging()
        |> ignore<IServiceCollection>

        services.AddResponseCaching()
        |> ignore<IServiceCollection>

        services.AddGiraffe()
        |> ignore<IServiceCollection>

        services.AddTags("https://dl.vndb.org/dump/vndb-tags-latest.json.gz")
        |> ignore<IServiceCollection>

        services.AddTraits("https://dl.vndb.org/dump/vndb-traits-latest.json.gz")
        |> ignore<IServiceCollection>

        services.AddSingleton(
            let jsonOptions = JsonSerializerOptions()
            jsonOptions.Converters.Add(JsonFSharpConverter())
            jsonOptions
        )
        |> ignore<IServiceCollection>

        services.AddSingleton<Json.ISerializer, SystemTextJson.Serializer>()
        |> ignore<IServiceCollection>

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        //if env.IsDevelopment() then
        app.UseDeveloperExceptionPage()
        |> ignore<IApplicationBuilder>

        app
            .UseSwaggerUI(fun c -> c.SwaggerEndpoint("/openapi.yaml", "VndbReProxy"))
            .UseHttpsRedirection()
            .UseRouting()
            .UseCors()
            //.UseAuthorization()
            .UseResponseCaching()
            .UseEndpoints(fun endpoints -> endpoints.MapGiraffeEndpoints(Endpoints.endpoints))
        |> ignore<IApplicationBuilder>
