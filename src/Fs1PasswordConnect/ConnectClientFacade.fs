namespace Fs1PasswordConnect

open FSharpPlus.Data
open FSharpPlus
open Milekic.YoLo

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
and internal ConnectClientOperations = {
    GetVaults : unit -> ConnectClientMonad<VaultInfo list>
    GetVaultId : VaultTitle -> ConnectClientMonad<VaultId>
    GetItemId : VaultId -> ItemTitle -> ConnectClientMonad<ItemId>
    GetItem : VaultId -> ItemId -> ConnectClientMonad<Item>
    GetItems : VaultId -> ConnectClientMonad<ItemInfo list>
}
and ConnectClientMonad<'a> = ResultT<Async<Result<'a, ConnectError>>>
and ConnectError =
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
        | UnexpectedStatusCode code -> $"Connect server returned an unexpected status code: {code}"
        | DecodeError message -> $"Connect server returned a result which could not be decoded due to the following error: {message}"
        | UnauthorizedAccess -> "Unauthorized access to Connect server"
        | MissingToken -> "Connect server token is missing"
        | VaultNotFound -> "Vault not found"
        | ItemNotFound -> "Item not found"
        | FieldNotFound -> "Field not found"
