module VndbReProxy.Api.Services.Tags

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
      parents: int array }

type TagsService(url) =
    inherit DumpService<int, Tag>(url, (fun a -> a.id))

open Microsoft.Extensions.DependencyInjection

type IServiceCollection with
    member this.AddTags(url) =
        this.AddSingleton<IDumpService<int, Tag>, TagsService>(fun _ -> TagsService(url))
