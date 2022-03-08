namespace VndbReProxy.Api.Services

open System.Collections.Generic
open System.IO.Compression
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json

type IDumpService<'TKey, 'TValue> =
    abstract TryGet : 'TKey -> 'TValue option
    abstract GetOrDownload : 'TKey -> Task<Result<'TValue, exn>>
    abstract Download : unit -> Task<unit>
    abstract TryGetAll : unit -> 'TValue seq option

type DumpService<'TKey, 'TValue when 'TKey: equality>(url: string, idGetter: 'TValue -> 'TKey) =
    let data = Dictionary<'TKey, 'TValue>()

    let tryGet key =
        if data.ContainsKey key then
            Some data.[key]
        else
            None

    let download () =
        task {
            use client = new HttpClient()
            let! a = client.GetAsync(url)
            let stream = a.Content.ReadAsStream()

            use gzStream =
                new GZipStream(stream, CompressionMode.Decompress)

            let! items = JsonSerializer.DeserializeAsync<'TValue array>(gzStream)

            data.Clear()

            for i in items do
                data.[idGetter i] <- i
        }

    let getOrDownload key =
        match tryGet key with
        | Some a -> Ok a |> Task.FromResult
        | None ->
            task {
                do! download ()

                return
                    try
                        Ok data.[key]
                    with
                    | ex -> Error ex
            }

    let tryGetAll () =
        if data.Count = 0 then
            None
        else
            Some(data.Values |> Seq.cast)

    interface IDumpService<'TKey, 'TValue> with
        member this.TryGet(key) = tryGet key

        member this.GetOrDownload(key) = getOrDownload key
        member this.Download() = download ()
        member this.TryGetAll() = tryGetAll ()
