module VndbReProxy.Proto.Request

open System.IO
open System.Text
open System.Threading.Tasks
open FSharp.Control
open VndbReProxy.Prelude.Utils

type t = string

let login (conf: Connection.conf) lp : t =
    match lp with
    | Some (login, password') ->
        loginBuilder () {
            protocol 1
            client conf.Client
            clientver conf.ClientVer
            username login
            password password'
        }
    | None ->
        loginBuilder () {
            protocol 1
            client conf.Client
            clientver conf.ClientVer
        }

let private stopByteBuff = [| Proto.stopByte |]

let private write (stream: Stream) (a: t) =
    try
        task {
            let buf = Encoding.UTF8.GetBytes a

            do! stream.WriteAsync(buf, 0, buf.Length)
            do! stream.WriteAsync(stopByteBuff, 0, 1)
            return Ok()
        }
    with
    | _ -> Task.FromResult ^ Error ^ Response.error.SendError

let send (stream: Stream) (a: t) =
    task {
        match! write stream a with
        | Error err -> return Response.t.InternalError err
        | _ ->
            let! ans = Proto.nextMsg stream
            let ret = Response.parseResult ans

            return ret
    }
