open System
open VndbReProxy.Proto

[<EntryPoint>]
let main _ =
    use client =
        Connection.client Tls Connection.defaultConf

    let login =
        match Console.ReadLine() with
        | "" -> "misterptits"
        | s -> s

    let password = Console.ReadLine()

    use stream =
        Connection.stream Tls Connection.defaultConf client

    let requests =
        [ Request.login
            Connection.defaultConf
            (if password |> String.IsNullOrEmpty then
                 None
             else
                 Some(login, password))
          "get vn basic,anime (id = 17)"
          "get quote basic (id>=1) {\"results\":1}" ]

    let print a =
        printfn ""
        printfn "%A" ^ Response.toJson a
        printfn "%A" ^ Response.toHttpCode false a
        printfn "%A" ^ Response.toResponseName a
        printfn "%A" a
        printfn ""

    for req in requests do
        Request.send stream req
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> print

    0
