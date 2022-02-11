namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece.SystemTextJson
open FSharpPlus

type ItemInfo = {
    Id : ItemId
    Title : ItemTitle
    VaultId : VaultId
    Tags : ItemTag list
} with
    static member JsonObjCodec =
        fun i t v ts -> {
            Id = (ItemId i)
            Title = (ItemTitle t)
            VaultId = v
            Tags = List.map ItemTag ts
        }
        |> withFields
        |> jfield "id" (fun { ItemInfo.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (ItemTitle t) } -> t)
        |> jfieldWith VaultInfo.VaultIdStubCodec "vault" (fun { ItemInfo.VaultId = v } -> v)
        |> jfield "tags" (fun { Tags = ts } -> ts |> List.map (fun (ItemTag t) -> t))
and ItemId = ItemId of string
and ItemTitle = ItemTitle of string
and ItemTag = ItemTag of string
