open System
open VnListProxy.Proto

[<EntryPoint>]
let main argv =
    let conf =
        { VnListProxy.Proto.Connection.t.Host = "api.vndb.org"
          VnListProxy.Proto.Connection.t.Port = 19534
          VnListProxy.Proto.Connection.t.PortTls = 19535
          VnListProxy.Proto.Connection.t.Client = "vn-list"
          VnListProxy.Proto.Connection.t.ClientVer = "0.0.1" }

    use client = Connection.connect conf

    let password = Console.ReadLine()
    let stream = client.GetStream()

    let tsk =
        Connection.login stream conf "misterptits" password

    let ans =
        tsk |> Async.AwaitTask |> Async.RunSynchronously

    match ans with
    | Ok a ->
        printfn "%A" ^ Response.toJson a
        printfn "%A" ^ Response.toHttpCode a
        printfn "%A" ^ Response.toResponseName a
    | _ -> ()

    printfn "%A" ans

    0
