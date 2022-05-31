namespace Fs1PasswordConnect

open System
open Fleece
open FSharpPlus

type VaultInfo = {
    Id : VaultId
    Title : VaultTitle
    Version : VaultVersion
    CreatedAt : CreatedAt
    UpdatedAt : UpdatedAt
    ItemCount : ItemCount
} with
    static member get_Codec () =
        let vaultId =
            let c = VaultId.get_ObjCodec ()
            Codec.decode c <-> (fun { Id = x } -> Codec.encode c x)

        fun i t v c u ic -> {
            Id = i
            Title = (VaultTitle t)
            Version = VaultVersion (v |> Option.defaultValue 0)
            CreatedAt = CreatedAt (c |> Option.defaultValue DateTimeOffset.MinValue)
            UpdatedAt = UpdatedAt (u |> Option.defaultValue DateTimeOffset.MinValue)
            ItemCount = ItemCount (ic |> Option.defaultValue 0)
        }
        <!> vaultId
        <*> jreq "name" (fun { Title = (VaultTitle t) } -> Some t)
        <*> jopt "contentVersion" (fun { Version = VaultVersion v } -> Some v)
        <*> jopt "createdAt" (fun { CreatedAt = CreatedAt c } -> Some c)
        <*> jopt "updatedAt" (fun { UpdatedAt = UpdatedAt u } -> Some u)
        <*> jopt "items" (fun { ItemCount = ItemCount ic } -> Some ic)
        |> ofObjCodec
and VaultId =
    VaultId of string
    with
    static member get_ObjCodec () =
        VaultId <!> jreq "id" (fun (VaultId i) -> Some i)
    static member get_Codec () = VaultId.get_ObjCodec () |> ofObjCodec
and VaultTitle = VaultTitle of string
and VaultVersion = VaultVersion of int
and CreatedAt = CreatedAt of DateTimeOffset
and UpdatedAt = UpdatedAt of DateTimeOffset
and ItemCount = ItemCount of int
