namespace VndbReProxy.Prelude

module Utils =
    open System.Text

    [<Struct>]
    type LoginBuilder =
        val d: StringBuilder
        new(()) = { d = StringBuilder("login {", 256) }

        member inline this.Yield _ = ()

        [<CustomOperation("protocol")>]
        member inline this.Protocol(_, value: int) =
            this
                .d
                .Append("\"protocol\":")
                .Append(value)
                .Append(",")

        [<CustomOperation("client")>]
        member inline this.Client(_, value: string) =
            this
                .d
                .Append("\"client\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")

        [<CustomOperation("clientver")>]
        member inline this.ClientVer(_, value: string) =
            this
                .d
                .Append("\"clientver\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")

        [<CustomOperation("username")>]
        member inline this.Username(_, value: string) =
            this
                .d
                .Append("\"username\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")

        [<CustomOperation("password")>]
        member inline this.Password(_, value: string) =
            this
                .d
                .Append("\"password\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")


        [<CustomOperation("sessiontoken")>]
        member inline this.SessionToken(_, value: string) =
            this
                .d
                .Append("\"sessiontoken\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")

        [<CustomOperation("createsession")>]
        member inline this.CreateSession(_, value: bool) =
            this
                .d
                .Append("\"createsession\":")
                .Append("\"")
                .Append(value)
                .Append("\"")
                .Append(",")

        member inline this.Run _ =
            this.d.Remove(this.d.Length - 1, 1).Append("}")
            |> string

    let inline loginBuilder () = LoginBuilder(())
