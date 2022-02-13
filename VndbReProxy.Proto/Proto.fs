module VndbReProxy.Proto.Proto

open System.IO
open System.Text
open FSharp.Control

type error =
    | ReceiveError
    | EncodeError

let stopByte = byte 0x04

let readByte buff (stream: Stream) =
    task {
        let! cnt = stream.ReadAsync(buff, 0, 1)

        if cnt = 1 then
            return ValueSome buff.[0]
        else
            return ValueNone
    }

let rec private nextMsgAux stream buff acc =
    task {
        let! b = readByte buff stream

        match b with
        | ValueSome bt when bt = stopByte -> return acc |> Some
        | ValueSome bt -> return! nextMsgAux stream buff (bt :: acc)
        | ValueNone -> return None
    }

let rec private nextMsgMut stream buff acc =
    task {
        let mutable ret = None
        let mutable acc = acc

        while Option.isNone ret do
            let! b = readByte buff stream

            match b with
            | ValueSome bt when bt = stopByte -> ret <- Some acc
            | ValueSome bt -> acc <- bt :: acc
            | ValueNone -> ret <- None

        return ret
    }

let nextMsg stream =
    task {
        let buff = [| byte 0 |]

        match! nextMsgMut stream buff [] with
        | Some read ->
            let arr = read |> List.rev |> List.toArray

            try
                return arr |> Encoding.UTF8.GetString |> Ok
            with
            | _ -> return Error EncodeError
        | None -> return Error ReceiveError
    }
