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
      parents: int array }

type TraitsService(url) =
    inherit DumpService<int, Trait>(url, (fun a -> a.id))

open Microsoft.Extensions.DependencyInjection

type IServiceCollection with
    member this.AddTraits(url) =
        this.AddSingleton<IDumpService<int, Trait>, TraitsService>(fun _ -> TraitsService(url))
