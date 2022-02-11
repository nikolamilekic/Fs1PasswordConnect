namespace Fs1PasswordConnect

open FSharpPlus.Data
open FSharpPlus
open Milekic.YoLo

type ConnectClientFacade internal (client : ConnectClientOperations) =
    member _.GetVaults () = client.GetVaults () |> ResultT.run
    member _.GetVaultInfo x = client.GetVaultInfoById x |> ResultT.run
    member _.GetVaultInfo x = client.GetVaultInfoByTitle x |> ResultT.run
    member _.GetItemInfo (x, y) = client.GetItemInfo x y |> ResultT.run
    member _.GetItem (x, y) = client.GetItem x y |> ResultT.run
    member _.GetVaultItems x = client.GetVaultItems x |> ResultT.run
    member _.GetItem(vaultTitle, itemId : ItemId) =
        client.GetVaultInfoByTitle vaultTitle
        >>= fun vaultInfo -> client.GetItem vaultInfo.Id itemId
        |> ResultT.run
    member _.GetItem(vaultId, itemTitle : ItemTitle) =
        client.GetItemInfo vaultId itemTitle
        >>= fun itemInfo -> client.GetItem vaultId itemInfo.Id
        |> ResultT.run
    member _.GetItem(vaultTitle, itemTitle : ItemTitle) =
        client.GetVaultInfoByTitle vaultTitle
        >>= fun vaultInfo ->
            client.GetItemInfo vaultInfo.Id itemTitle
            >>= fun itemInfo -> client.GetItem vaultInfo.Id itemInfo.Id
        |> ResultT.run
    member _.GetVaultItems(vaultTitle) =
        client.GetVaultInfoByTitle vaultTitle
        >>= fun vaultInfo -> client.GetVaultItems vaultInfo.Id
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
                match! this.GetVaultInfo (VaultTitle vault) with
                | Ok vaultInfo ->
                    //Assume item is a title
                    match! this.GetItemInfo (vaultInfo.Id, ItemTitle item) with
                    | Ok itemInfo -> return! getField vaultInfo.Id itemInfo.Id
                    | Error ItemNotFound -> return! getField vaultInfo.Id (ItemId item) //Maybe item is id
                    | Error e -> return Error e
                | _ ->
                    //Assume vault is id
                    match! this.GetItemInfo (VaultId vault, ItemTitle item) with
                    | Ok itemInfo -> return! getField (VaultId vault) itemInfo.Id
                    | Error ItemNotFound -> return! getField (VaultId vault) (ItemId item) //Maybe item is id
                    | Error e -> return Error e
            | _ -> return Ok template
        }

        inner template
and internal ConnectClientOperations = {
    GetVaults : unit -> ConnectClientMonad<VaultInfo list>
    GetVaultItems : VaultId -> ConnectClientMonad<ItemInfo list>
    GetVaultInfoById : VaultId -> ConnectClientMonad<VaultInfo>
    GetVaultInfoByTitle : VaultTitle -> ConnectClientMonad<VaultInfo>
    GetItemInfo : VaultId -> ItemTitle -> ConnectClientMonad<ItemInfo>
    GetItem : VaultId -> ItemId -> ConnectClientMonad<Item>
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
