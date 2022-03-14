module VndbReProxy.Api.Services.Traits

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

type TraitsService(url) =
    inherit DumpService<int, Trait>(url)

open Microsoft.Extensions.DependencyInjection

type IServiceCollection with
    member this.AddTraits(url) =
        this.AddSingleton<IDumpService<int, Trait>, TraitsService>(fun _ -> TraitsService(url))
