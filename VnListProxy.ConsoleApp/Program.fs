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
        Request.login conf "misterptits" password
        |> Request.send stream

    let ans =
        tsk |> Async.AwaitTask |> Async.RunSynchronously

    printfn "%A" ^ Response.toJson ans
    printfn "%A" ^ Response.toHttpCode ans
    printfn "%A" ^ Response.toResponseName ans
    printfn "%A" ans

    0
