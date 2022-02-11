namespace Fs1PasswordConnect

open System
open Fleece.SystemTextJson
open Fleece.SystemTextJson.Operators
open FSharpPlus

type VaultInfo = {
    Id : VaultId
    Title : VaultTitle
    Version : VaultVersion
    CreatedAt : CreatedAt
    UpdatedAt : UpdatedAt
    ItemCount : ItemCount
} with
    static member JsonObjCodec =
        fun i t v c u ic -> {
            Id = (VaultId i)
            Title = (VaultTitle t)
            Version = VaultVersion (v |> Option.defaultValue 0)
            CreatedAt = CreatedAt (c |> Option.defaultValue DateTimeOffset.MinValue)
            UpdatedAt = UpdatedAt (u |> Option.defaultValue DateTimeOffset.MinValue)
            ItemCount = ItemCount (ic |> Option.defaultValue 0)
        }
        |> withFields
        |> jfield "id" (fun { Id = (VaultId i) } -> i)
        |> jfield "name" (fun { Title = (VaultTitle t) } -> t)
        |> jfieldOpt "contentVersion" (fun { Version = VaultVersion v } -> Some v)
        |> jfieldOpt "createdAt" (fun { CreatedAt = CreatedAt c } -> Some c)
        |> jfieldOpt "updatedAt" (fun { UpdatedAt = UpdatedAt u } -> Some u)
        |> jfieldOpt "items" (fun { ItemCount = ItemCount ic } -> Some ic)
    static member internal VaultIdStubCodec =
        (function | JObject o -> VaultId <!> (o .@ "id")
                  | x -> Decode.Fail.objExpected x),
        (fun (VaultId id) -> jobj [ "id" .= id ])
and VaultId = VaultId of string
and VaultTitle = VaultTitle of string
and VaultVersion = VaultVersion of int
and CreatedAt = CreatedAt of DateTimeOffset
and UpdatedAt = UpdatedAt of DateTimeOffset
and ItemCount = ItemCount of int
