module VndbReProxy.Api.Services.Traits

open Microsoft.Extensions.Logging

type Trait =
    { id: int
      name: string
      description: string
      meta: bool
      searchable: bool
      applicable: bool
      chars: int
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

type TraitsService(logger, url) =
    inherit DumpService<int, Trait>(logger, url)

open Microsoft.Extensions.DependencyInjection

type IServiceCollection with
    member this.AddTraits(url) =
        this.AddSingleton<IDumpService<int, Trait>, TraitsService> (fun s ->
            TraitsService(
                s.GetService<ILogger<TraitsService>>()
                |> box
                |> Unchecked.unbox,
                url
            ))
