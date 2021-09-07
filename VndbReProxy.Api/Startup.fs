namespace VndbReProxy.Api

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting

type Startup(_configuration: IConfiguration) =
    member _.ConfigureServices(services: IServiceCollection) =
        [ services.AddRouting()
          services.AddAuthorization()
          services.AddLogging()
          services.AddGiraffe() ]
        |> List.iter ignore<IServiceCollection>

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage()
            |> ignore<IApplicationBuilder>

        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(fun endpoints ->
                endpoints.MapGiraffeEndpoints(Endpoints.endpoints)
                |> ignore<unit>)
        |> ignore<IApplicationBuilder>
