#nowarn "0044"

namespace Fs1PasswordConnect

open System
open System.IO
open System.Collections
open FSharpPlus.Data
open FSharpPlus
open Fs1PasswordConnect
open Milekic.YoLo

module ConnectClientFacade =
    let (|InjectPattern|_|) s =
        match s with
        | Regex "\"op://([a-zA-Z0-9\-_\s]+)/([a-zA-Z0-9\-_\s]+)/([a-zA-Z0-9\-_\s]+)/([a-zA-Z0-9\-_\s]+)\"" [ vault; item; section; fieldOrFile ] ->
            Some (InjectPattern (vault, item, Some section, fieldOrFile, true))
        | Regex "\"op://([a-zA-Z0-9\-_\s]+)/([a-zA-Z0-9\-_\s]+)/([a-zA-Z0-9\-_\s]+)\"" [ vault; item; fieldOrFile ] ->
            Some (InjectPattern (vault, item, None, fieldOrFile, true))
        | Regex "\"op://([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)\"" [ vault; item; section; fieldOrFile ] ->
            Some (InjectPattern (vault, item, Some section, fieldOrFile, true))
        | Regex "\"op://([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)\"" [ vault; item; fieldOrFile ] ->
            Some (InjectPattern (vault, item, None, fieldOrFile, true))
        | Regex "op://([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)" [ vault; item; section; fieldOrFile ] ->
            Some (InjectPattern (vault, item, Some section, fieldOrFile, false))
        | Regex "op://([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)/([a-zA-Z0-9\-_]+)" [ vault; item; fieldOrFile ] ->
            Some (InjectPattern (vault, item, None, fieldOrFile, false))
        | _ -> None

open ConnectClientFacade

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

    /// Replaces all tokens of type op://<VaultIdOrTitle/<ItemIdOrTitle>/[SectionIdOrLabel]/<FieldIdLabelFileIdOrFileName> from template
    /// with the values of the corresponding fields.
    member this.Inject template : Async<Result<string, ConnectError>> =
        let rec inner (template : string) : Async<Result<string, ConnectError>> = async {
            match template with
            | InjectPattern (vault, item, section, fieldOrFile, quoted) ->
                let replacement =
                    match section, quoted with
                    | Some s, true ->
                        let containsSpaces =
                            seq { vault; item; s; fieldOrFile }
                            |> Seq.exists (fun s -> s.Contains " ")
                        if containsSpaces then
                            $"\"op://{vault}/{item}/{s}/{fieldOrFile}\""
                        else $"op://{vault}/{item}/{s}/{fieldOrFile}"
                    | Some s, false -> $"op://{vault}/{item}/{s}/{fieldOrFile}"
                    | None, true ->
                        let containsSpaces =
                            seq { vault; item; fieldOrFile }
                            |> Seq.exists (fun s -> s.Contains " ")
                        if containsSpaces then
                            $"\"op://{vault}/{item}/{fieldOrFile}\""
                        else $"op://{vault}/{item}/{fieldOrFile}"
                    | None, false -> $"op://{vault}/{item}/{fieldOrFile}"

                let getField item = async {
                    let field =
                        match section with
                        | Some s ->
                            item.Fields
                            |> List.tryFind (fun { Id = FieldId id; Label = FieldLabel label; Section = sectionInfo } ->
                                match sectionInfo with
                                | Some { Label = SectionLabel sectionLabel; Id = SectionId sectionId } ->
                                    (sectionLabel = s || sectionId = s) &&
                                    (id = fieldOrFile || label = fieldOrFile)
                                | None -> false)
                        | None ->
                            item.Fields
                            |> List.tryFind (fun { Id = FieldId id; Label = FieldLabel label; Section = sectionInfo } ->
                                match sectionInfo with
                                | None -> (id = fieldOrFile || label = fieldOrFile)
                                | Some _ -> false)
                            |> Option.orElseWith (fun () ->
                                item.Fields
                                |> List.tryFind (fun { Id = FieldId id; Label = FieldLabel label } ->
                                    id = fieldOrFile || label = fieldOrFile))
                    match field with
                    | Some { Value = FieldValue v } ->
                        return! template.Replace(replacement, v) |> inner
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
                            return! template.Replace(replacement, v) |> inner
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
            | _ -> return Ok template
        }

        inner template

    member this.InjectIntoEnvironmentVariables () : Async<(string * Result<string, ConnectError>) list> =
        Environment.GetEnvironmentVariables ()
        |> Seq.cast<DictionaryEntry>
        |> Seq.map (fun kvp -> async {
            let value = kvp.Value.ToString()
            match! this.Inject value with
            | Ok i when i = value -> return None
            | Ok i as x ->
                let var = kvp.Key.ToString()
                Environment.SetEnvironmentVariable(var, i)
                return Some (var, x)
            | x -> return Some (kvp.Key.ToString(), x)
        })
        |> Seq.sequence
        |> Async.map (Seq.choose id >> Seq.toList)

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
    | [<Obsolete("Critical exceptions are raised instead of being wrapped, so that more information is available")>] CriticalFailure of exn
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
        | CriticalFailure exn -> $"Critical failure: {exn}"
        | UnexpectedStatusCode code -> $"Connect server returned an unexpected status code: {code}"
        | DecodeError message -> $"Connect server returned a result which could not be decoded due to the following error: {message}"
        | UnauthorizedAccess -> "Unauthorized access to Connect server"
        | MissingToken -> "Connect server token is missing"
        | VaultNotFound -> "Vault not found"
        | ItemNotFound -> "Item not found"
        | FieldNotFound -> "Field not found"
        | FileNotFound -> "File not found"
