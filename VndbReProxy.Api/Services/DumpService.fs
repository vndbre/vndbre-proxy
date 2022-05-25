namespace VndbReProxy.Api.Services

open System
open System.Collections.Concurrent
open System.IO.Compression
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json
open Microsoft.Extensions.Logging

type IDumpService<'TKey, 'TValue> =
    abstract DownloadIfOld: unit -> Task<unit>
    abstract TryGet: 'TKey -> 'TValue option
    abstract GetAll: unit -> 'TValue seq

type ITagTrait<'TId> =
    abstract Id: 'TId
    abstract Parents: 'TId array
    abstract RootId: 'TId voption with get, set
    abstract Name: string

type DumpService<'TKey, 'TValue when 'TKey: equality and 'TValue :> ITagTrait<'TKey>>(logger: ILogger<_>, url: string) =
    let data =
        ConcurrentDictionary<'TKey, 'TValue>()

    let mutable downloadTime = None

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
            logger.LogInformation("Started downloading")
            use client = new HttpClient()
            let! a = client.GetAsync(url)
            let stream = a.Content.ReadAsStream()

            use gzStream =
                new GZipStream(stream, CompressionMode.Decompress)

            let! items = JsonSerializer.DeserializeAsync<'TValue array>(gzStream)

            logger.LogInformation($"Downloaded {Array.length items} items instead of {data.Count}")
            data.Clear()

            for i in items do
                data.[i.Id] <- i

            downloadTime <- Some DateTimeOffset.Now
        }

    let getAll () =
        data.Values
        |> Seq.cast
        |> Seq.map (fun tag_trait ->
            getAndSetRootId tag_trait |> ignore<'TKey>
            tag_trait)

    let downloadIfOld () =
        task {
            match downloadTime with
            | None -> do! download ()
            | Some t when DateTimeOffset.Now - t > (TimeSpan.FromHours 23) -> do! download ()
            | Some _ -> ()
        }

    interface IDumpService<'TKey, 'TValue> with
        member this.DownloadIfOld() = downloadIfOld ()
        member this.TryGet(key) = tryGet key
        member this.GetAll() = getAll ()
