namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece
open FSharpPlus

type ItemUrl = { Path : ItemUrlPath } with
    static member get_Codec () =
        fun p -> { Path = (ItemUrlPath p) }
        <!> jreq "href" (fun { ItemUrl.Path = (ItemUrlPath p) } -> Some p)
        |> ofObjCodec
and ItemUrlPath = ItemUrlPath of string

type ItemInfo = {
    Id : ItemId
    Title : ItemTitle
    VaultId : VaultId
    Tags : ItemTag list
    Urls : ItemUrl list
    Version : ItemVersion
} with
    static member get_ObjCodec () =
        fun i t v ts urls version -> {
            Id = (ItemId i)
            Title = (ItemTitle t)
            VaultId = v
            Tags = List.map ItemTag (ts |> Option.defaultValue [])
            Urls = urls |> Option.defaultValue []
            Version = ItemVersion (version |> Option.defaultValue 0)
        }
        <!> jreq "id" (fun { ItemInfo.Id = (ItemId i) } -> Some i)
        <*> jreq "title" (fun { ItemInfo.Title = (ItemTitle t) } -> Some t)
        <*> jreq "vault" (fun { ItemInfo.VaultId = v } -> Some v)
        <*> jopt "tags" (fun { Tags = ts } ->
            Some <| (ts |> List.map (fun (ItemTag t) -> t)))
        <*> jopt "urls" (fun { Urls = urls } -> Some urls)
        <*> jopt "version" (fun { ItemInfo.Version = (ItemVersion v) } -> Some v)
    static member get_Codec () = ofObjCodec (ItemInfo.get_ObjCodec ())
and ItemId = ItemId of string
and ItemTitle = ItemTitle of string
and ItemTag = ItemTag of string
and ItemVersion = ItemVersion of int
