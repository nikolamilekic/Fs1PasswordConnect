namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece.SystemTextJson
open FSharpPlus

type ItemUrl = { Path : ItemUrlPath } with
    static member JsonObjCodec : (_ -> Result<_, _>) * _ =
        fun p -> { Path = (ItemUrlPath p) }
        |> withFields
        |> jfield "href" (fun { ItemUrl.Path = (ItemUrlPath p) } -> p)
and ItemUrlPath = ItemUrlPath of string

type ItemInfo = {
    Id : ItemId
    Title : ItemTitle
    VaultId : VaultId
    Tags : ItemTag list
    Urls : ItemUrl list
    Version : ItemVersion
} with
    static member JsonObjCodec =
        fun i t v ts urls version -> {
            Id = (ItemId i)
            Title = (ItemTitle t)
            VaultId = v
            Tags = List.map ItemTag (ts |> Option.defaultValue [])
            Urls = urls |> Option.defaultValue []
            Version = ItemVersion (version |> Option.defaultValue 0)
        }
        |> withFields
        |> jfield "id" (fun { ItemInfo.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (ItemTitle t) } -> t)
        |> jfieldWith VaultInfo.VaultIdStubCodec "vault" (fun { ItemInfo.VaultId = v } -> v)
        |> jfieldOpt "tags" (fun { Tags = ts } ->
            Some <| (ts |> List.map (fun (ItemTag t) -> t)))
        |> jfieldOpt "urls" (fun { Urls = urls } -> Some urls)
        |> jfieldOpt "version" (fun { Version = (ItemVersion v) } -> Some v)
and ItemId = ItemId of string
and ItemTitle = ItemTitle of string
and ItemTag = ItemTag of string
and ItemVersion = ItemVersion of int
