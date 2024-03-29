﻿module VndbReProxy.Api.Services.Tags

open Microsoft.Extensions.Logging

type Tag =
    { id: int
      name: string
      description: string
      meta: bool
      searchable: bool
      applicable: bool
      vns: int
      cat: string
      aliases: string array
      parents: int array
      mutable root_id: int voption }

    interface ITagTrait<int> with
        member this.Id = this.id
        member this.Parents = this.parents

        member this.RootId
            with get () = this.root_id
            and set value = this.root_id <- value

        member this.Name = this.name

type TagsService(logger, url) =
    inherit DumpService<int, Tag>(logger, url)

open Microsoft.Extensions.DependencyInjection

type IServiceCollection with
    member this.AddTags(url) =
        this.AddSingleton<IDumpService<int, Tag>, TagsService> (fun s ->
            TagsService(
                s.GetService<ILogger<TagsService>>()
                |> box
                |> Unchecked.unbox,
                url
            ))
