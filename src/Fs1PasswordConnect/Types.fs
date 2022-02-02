namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece.SystemTextJson
open Fleece.SystemTextJson.Operators
open FSharpPlus
open Milekic.YoLo

type VaultId = VaultId of string

module internal VaultId =
    let vaultStubCodec =
        (function | JObject o -> VaultId <!> (o .@ "id")
                  | x -> Decode.Fail.objExpected x),
        (fun (VaultId id) -> jobj [ "id" .= id ])
open VaultId

type VaultTitle = VaultTitle of string
type VaultInfo = { Id : VaultId; Title : VaultTitle } with
    static member JsonObjCodec =
        fun i t -> { Id = (VaultId i); Title = (VaultTitle t) }
        |> withFields
        |> jfield "id" (fun { Id = (VaultId i) } -> i)
        |> jfield "name" (fun { Title = (VaultTitle t) } -> t)
type ItemId = ItemId of string
type ItemTitle = ItemTitle of string
type ItemInfo = { Id : ItemId; Title : ItemTitle; VaultId : VaultId } with
    static member JsonObjCodec =
        fun i t v -> { Id = (ItemId i); Title = (ItemTitle t); VaultId = v }
        |> withFields
        |> jfield "id" (fun { ItemInfo.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (ItemTitle t) } -> t)
        |> jfieldWith vaultStubCodec "vault" (fun { VaultId = v } -> v)
type FieldId = FieldId of string
type FieldLabel = FieldLabel of string
type FieldValue = FieldValue of string
type Field = { Id : FieldId; Label : FieldLabel; Value : FieldValue } with
    static member JsonObjCodec =
        fun i l v -> {
            Id = (FieldId i)
            Label = (FieldLabel (l |> Option.defaultValue ""))
            Value = (FieldValue (v |> Option.defaultValue ""))
        }
        |> withFields
        |> jfield "id" (fun { Field.Id = (FieldId i) } -> i)
        |> jfieldOpt "label" (fun { Label = (FieldLabel l) } -> Some l)
        |> jfieldOpt "value" (fun { Value = (FieldValue v) } -> Some v)
type Item = {
    Id : ItemId
    Title : ItemTitle
    VaultId : VaultId
    Fields : Field list } with
    static member JsonObjCodec =
        fun i t v f -> {
            Id = (ItemId i)
            Title = (ItemTitle t)
            VaultId = v
            Fields = f
        }
        |> withFields
        |> jfield "id" (fun { Item.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (ItemTitle t) } -> t)
        |> jfieldWith vaultStubCodec "vault" (fun { Item.VaultId = v } -> v)
        |> jfield "fields" (fun { Fields = f } -> f)

type ConnectError =
    | CriticalFailure of exn
    | UnexpectedStatusCode of int
    | DecodeError of string
    | UnauthorizedAccess
    | MissingToken
    | VaultNotFound
    | ItemNotFound
    | FieldNotFound
    with
    override this.ToString() =
        match this with
        | CriticalFailure exn -> $"Critical failure: {exn.Message}"
        | UnexpectedStatusCode code -> $"Connect server return an unexpected status code: {code}"
        | DecodeError message -> $"Connect server return a result which could not be decoded due to the following error: {message}"
        | UnauthorizedAccess -> "Unauthorized access to Connect server"
        | MissingToken -> "Connect server token is missing"
        | VaultNotFound -> "Vault not found"
        | ItemNotFound -> "Item not found"
        | FieldNotFound -> "Field not found"

type ConnectHost = ConnectHost of string
type ConnectToken = ConnectToken of string
type ConnectClientSettings = { Host : ConnectHost; Token : ConnectToken } with
    static member JsonObjCodec =
        fun h t -> { Host = ConnectHost h; Token = ConnectToken t }
        |> withFields
        |> jfield "Host" (fun { Host = (ConnectHost h) } -> h)
        |> jfield "Token" (fun { Token = (ConnectToken t) } -> t)
type internal Request = { Url : string; Headers : (string * string) list }
type internal Response = { StatusCode : int; Body : string }

type internal ConnectClientOperations = {
    GetVaults : unit -> ConnectClientMonad<VaultInfo list>
    GetVaultId : VaultTitle -> ConnectClientMonad<VaultId>
    GetItemId : VaultId -> ItemTitle -> ConnectClientMonad<ItemId>
    GetItem : VaultId -> ItemId -> ConnectClientMonad<Item>
    GetItems : VaultId -> ConnectClientMonad<ItemInfo list>
}
and ConnectClientMonad<'a> = ResultT<Async<Result<'a, ConnectError>>>

type ConnectClientFacade internal (client : ConnectClientOperations) =
    member _.GetVaults () = client.GetVaults () |> ResultT.run
    member _.GetVaultId x = client.GetVaultId x |> ResultT.run
    member _.GetItemId (x, y) = client.GetItemId x y |> ResultT.run
    member _.GetItem (x, y) = client.GetItem x y |> ResultT.run
    member _.GetItems x = client.GetItems x |> ResultT.run
    member _.GetItem(vaultTitle, itemId : ItemId) =
        client.GetVaultId vaultTitle
        >>= fun vaultId -> client.GetItem vaultId itemId
        |> ResultT.run
    member _.GetItem(vaultId, itemTitle : ItemTitle) =
        client.GetItemId vaultId itemTitle >>= client.GetItem vaultId
        |> ResultT.run
    member _.GetItem(vaultTitle, itemTitle : ItemTitle) =
        client.GetVaultId vaultTitle
        >>= fun vaultId -> client.GetItemId vaultId itemTitle >>= client.GetItem vaultId
        |> ResultT.run
    member _.GetItems(vaultTitle) =
        client.GetVaultId vaultTitle >>= client.GetItems
        |> ResultT.run

    /// Replaces all tokens of type {{ op://<VaultIdOrTitle/<ItemIdOrTitle>/<FieldIdOrLabel> }} from template
    /// with the values of the corresponding fields.
    member this.Inject template =
        let rec inner (template : string) : Async<Result<string, ConnectError>> = async {
            match template with
            | Regex "{{ op://(.+)/(.+)/(.+) }}" [ vault; item; field ] ->
                let replacement = "{{ " + $"op://{vault}/{item}/{field}" + " }}"

                let getField (vaultId : VaultId) (itemId : ItemId) = async {
                    match! this.GetItem(vaultId, itemId) with
                    | Ok item ->
                        let field =
                            item.Fields
                            |> List.tryFind (fun { Id = FieldId id; Label = FieldLabel label } ->
                                id = field || label = field)
                        match field with
                        | Some { Value = FieldValue v } ->
                            return! (template.Replace(replacement, v) |> inner)
                        | None -> return Error FieldNotFound
                    | Error e -> return Error e
                }

                //Assume vault is a title
                match! this.GetVaultId (VaultTitle vault) with
                | Ok vaultId ->
                    //Assume item is a title
                    match! this.GetItemId (vaultId, ItemTitle item) with
                    | Ok itemId -> return! getField vaultId itemId
                    | Error ItemNotFound -> return! getField vaultId (ItemId item) //Maybe item is id
                    | Error e -> return Error e
                | _ ->
                    //Assume vault is id
                    match! this.GetItemId (VaultId vault, ItemTitle item) with
                    | Ok itemId -> return! getField (VaultId vault) itemId
                    | Error ItemNotFound -> return! getField (VaultId vault) (ItemId item) //Maybe item is id
                    | Error e -> return Error e
            | _ -> return Ok template
        }

        inner template
