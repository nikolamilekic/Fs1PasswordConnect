namespace Fs1PasswordConnect

open System.IO
open FSharpPlus.Data
open FSharpPlus
open Fs1PasswordConnect
open Milekic.YoLo

type ConnectClientFacade internal (client : ConnectClientOperations) =
    member _.GetVaults () = client.GetVaults () |> ResultT.run
    member _.GetVaultInfo vaultId = client.GetVaultInfoById vaultId |> ResultT.run
    member _.GetVaultInfo vaultTitle = client.GetVaultInfoByTitle vaultTitle |> ResultT.run
    member _.GetItemInfo (vaultId, itemTitle) = client.GetItemInfo vaultId itemTitle |> ResultT.run
    member _.GetItem (vaultId, itemId) = client.GetItem vaultId itemId |> ResultT.run
    member _.GetVaultItems vaultId = client.GetVaultItems vaultId |> ResultT.run
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
    member _.GetFile(contentPath)  = client.GetFile contentPath |> ResultT.run
    member _.GetVaultItems(vaultTitle) =
        client.GetVaultInfoByTitle vaultTitle
        >>= fun vaultInfo -> client.GetVaultItems vaultInfo.Id
        |> ResultT.run

    /// Replaces all tokens of type {{ op://<VaultIdOrTitle/<ItemIdOrTitle>/<FieldIdLabelFileIdOrFileName> }} from template
    /// with the values of the corresponding fields
    /// and returns the resulting string together with a list of replacements that were made.
    member this.InjectAndReturnReplacements template =
        let rec inner replacements (template : string) : Async<Result<string * (string * string) list, ConnectError>> = async {
            match template with
            | Regex "{{ op://(.+)/(.+)/(.+) }}" [ vault; item; fieldOrFile ] ->
                let replacement = "{{ " + $"op://{vault}/{item}/{fieldOrFile}" + " }}"

                let getField item = async {
                    let field =
                        item.Fields
                        |> List.tryFind (fun { Id = FieldId id; Label = FieldLabel label } ->
                            id = fieldOrFile || label = fieldOrFile)
                    match field with
                    | Some { Value = FieldValue v } ->
                        return!
                            template.Replace(replacement, v)
                            |> inner ((replacement, v)::replacements)
                    | None -> return Error FieldNotFound
                }

                let getFile item = async {
                    let file =
                        item.Files
                        |> List.tryFind (fun { File.Id = FileId id; Name = FileName name } ->
                            id = fieldOrFile || name = fieldOrFile)
                    match file with
                    | Some { Path = cp } ->
                        match! this.GetFile cp with
                        | Ok stream ->
                            use reader = new StreamReader(stream)
                            let! v = reader.ReadToEndAsync() |> Async.AwaitTask
                            return!
                                template.Replace(replacement, v)
                                |> inner ((replacement, v)::replacements)
                        | Error e -> return Error e
                    | None -> return Error FieldNotFound
                }

                let getFieldOrFile (vaultId : VaultId) (itemId : ItemId) = async {
                    match! this.GetItem(vaultId, itemId) with
                    | Ok item ->
                        match! getField item with
                        | Ok x -> return Ok x
                        | Error _ -> return! getFile item
                    | Error e -> return Error e
                }

                //Assume vault is a title
                match! this.GetVaultInfo (VaultTitle vault) with
                | Ok vaultInfo ->
                    //Assume item is a title
                    match! this.GetItemInfo (vaultInfo.Id, ItemTitle item) with
                    | Ok itemInfo -> return! getFieldOrFile vaultInfo.Id itemInfo.Id
                    | Error ItemNotFound -> return! getFieldOrFile vaultInfo.Id (ItemId item) //Maybe item is id
                    | Error e -> return Error e
                | _ ->
                    //Assume vault is id
                    match! this.GetItemInfo (VaultId vault, ItemTitle item) with
                    | Ok itemInfo -> return! getFieldOrFile (VaultId vault) itemInfo.Id
                    | Error ItemNotFound -> return! getFieldOrFile (VaultId vault) (ItemId item) //Maybe item is id
                    | Error e -> return Error e
            | _ -> return Ok (template, replacements |> List.rev)
        }

        inner [] template

    /// Replaces all tokens of type {{ op://<VaultIdOrTitle/<ItemIdOrTitle>/<FieldIdLabelFileIdOrFileName> }} from template
    /// with the values of the corresponding fields.
    member this.Inject template : Async<Result<string, ConnectError>> = async {
        let! result = this.InjectAndReturnReplacements template
        return result |>> fst
    }

and internal ConnectClientOperations = {
    GetVaults : unit -> ConnectClientMonad<VaultInfo list>
    GetVaultItems : VaultId -> ConnectClientMonad<ItemInfo list>
    GetVaultInfoById : VaultId -> ConnectClientMonad<VaultInfo>
    GetVaultInfoByTitle : VaultTitle -> ConnectClientMonad<VaultInfo>
    GetItemInfo : VaultId -> ItemTitle -> ConnectClientMonad<ItemInfo>
    GetItem : VaultId -> ItemId -> ConnectClientMonad<Item>
    GetFile : FileContentPath -> ConnectClientMonad<Stream>
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
    | FileNotFound
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
        | FileNotFound -> "File not found"
