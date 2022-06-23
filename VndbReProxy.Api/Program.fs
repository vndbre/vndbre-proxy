namespace VndbReProxy.Api

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open System

module Program =
    let CreateHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder
                    .UseStartup<Startup>()
                    .UseUrls("http://*:" + (Environment.GetEnvironmentVariable("PORT")))
                    |> ignore<IWebHostBuilder>)

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args).Build().Run()
        0
