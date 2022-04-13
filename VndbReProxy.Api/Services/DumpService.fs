namespace VndbReProxy.Api.Services

open System.Collections.Concurrent
open System.IO.Compression
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json

type IDumpService<'TKey, 'TValue> =
    abstract TryGet : 'TKey -> 'TValue option
    abstract GetOrDownload : 'TKey -> Task<Result<'TValue, exn>>
    abstract Download : unit -> Task<unit>
    abstract TryGetAll : unit -> 'TValue seq option

type ITagTrait<'TId> =
    abstract Id : 'TId
    abstract Parents : 'TId array
    abstract RootId : 'TId voption with get, set
    abstract Name : string

type DumpService<'TKey, 'TValue when 'TKey: equality and 'TValue :> ITagTrait<'TKey>>(url: string) =
    let data = ConcurrentDictionary<'TKey, 'TValue>()



    let rec getAndSetRootId (tag_trait: 'TValue) =
        match tag_trait.RootId with
        | ValueSome root_id -> root_id
        | ValueNone ->
            match tag_trait.Parents with
            | [||] ->
                tag_trait.RootId <- ValueSome tag_trait.Id
                tag_trait.Id
            | parents ->
                let res = getAndSetRootId data.[parents.[0]]

                tag_trait.RootId <- ValueSome res
                res

    let tryGet key =
        if data.ContainsKey key then
            let tag_trait = data.[key]
            getAndSetRootId tag_trait |> ignore<'TKey>
            Some tag_trait
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
                data.[i.Id] <- i
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
            data.Values
            |> Seq.cast
            |> Seq.map
                (fun tag_trait ->
                    getAndSetRootId tag_trait |> ignore<'TKey>
                    tag_trait)
            |> Some

    interface IDumpService<'TKey, 'TValue> with
        member this.TryGet(key) = tryGet key

        member this.GetOrDownload(key) = getOrDownload key
        member this.Download() = download ()
        member this.TryGetAll() = tryGetAll ()
