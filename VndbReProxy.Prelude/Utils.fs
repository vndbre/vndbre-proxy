namespace VndbReProxy.Prelude

module Utils =
    open System.Text

    [<Struct>]
    type LoginBuilder =
        val d: StringBuilder
        new(()) = { d = StringBuilder("login {", 256) }

        member this.Yield _ = ()

        [<CustomOperation("protocol")>]
        member this.Protocol(_, value: int) =
            this
                .d
                .Append("\"protocol\":")
                .Append(value)
                .Append(",")

        [<CustomOperation("client")>]
        member this.Client(_, value: string) =
            this
                .d
                .Append("\"client\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")

        member this.Run _ =
            this.d.Remove(this.d.Length - 1, 1).Append("}")
            |> string

    let inline loginBuilder () = LoginBuilder(())

    let a () = loginBuilder () { protocol 1
                                 client "ars" }
