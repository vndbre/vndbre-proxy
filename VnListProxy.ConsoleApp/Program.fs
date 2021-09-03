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
        Connection.sendLogin stream conf "misterptits" password

    let ans =
        tsk |> Async.AwaitTask |> Async.RunSynchronously

    printfn "%s" ans

    0
