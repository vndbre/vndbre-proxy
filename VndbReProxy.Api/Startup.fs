namespace VndbReProxy.Api

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe.EndpointRouting

type Startup(configuration: IConfiguration) =
    member _.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member _.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services.AddControllers() |> ignore<IMvcBuilder>

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
