namespace Fs1PasswordConnect

open Fleece.SystemTextJson
open Fleece.SystemTextJson.Operators
open FSharpPlus

type VaultInfo = { Id : VaultId; Title : VaultTitle } with
    static member JsonObjCodec =
        fun i t -> { Id = (VaultId i); Title = (VaultTitle t) }
        |> withFields
        |> jfield "id" (fun { Id = (VaultId i) } -> i)
        |> jfield "name" (fun { Title = (VaultTitle t) } -> t)
    static member internal VaultIdStubCodec =
        (function | JObject o -> VaultId <!> (o .@ "id")
                  | x -> Decode.Fail.objExpected x),
        (fun (VaultId id) -> jobj [ "id" .= id ])
and VaultId = VaultId of string
and VaultTitle = VaultTitle of string
