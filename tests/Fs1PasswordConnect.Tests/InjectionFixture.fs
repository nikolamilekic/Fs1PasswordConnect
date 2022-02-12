namespace Fs1PasswordConnect

open System
open Swensen.Unquote
open FSharpPlus
open FSharpPlus.Data
open FSharpPlus.Lens
open TechTalk.SpecFlow

[<Binding>]
type InjectionFixture() =
    let mutable vaults : VaultInfo list = []
    let mutable items : Item list = []
    let connectClient =
        let getVaults () : ConnectClientMonad<VaultInfo list> = vaults |> result
        let getVaultInfoByTitle (title : VaultTitle) : ConnectClientMonad<VaultInfo> =
            match vaults |> List.tryFind (fun v -> v.Title = title) with
            | Some v -> result v
            | None -> Error VaultNotFound |> ResultT.hoist
        let getVaultInfoById (id : VaultId) : ConnectClientMonad<VaultInfo> =
            match vaults |> List.tryFind (fun v -> v.Id = id) with
            | Some v -> result v
            | None -> Error VaultNotFound |> ResultT.hoist
        let getItemInfo (vaultId : VaultId) (itemTitle : ItemTitle) : ConnectClientMonad<ItemInfo> =
            let itemInfos = items |>> fun x -> x.ItemInfo
            match vaults |> List.tryFind (fun v -> v.Id = vaultId) with
            | Some _ ->
                match itemInfos |> Seq.tryFind (fun i -> i.Title = itemTitle && i.VaultId = vaultId) with
                | Some i -> result i
                | None -> Error ItemNotFound |> ResultT.hoist
            | None -> Error VaultNotFound |> ResultT.hoist
        let getItem (vaultId : VaultId) (itemId : ItemId) : ConnectClientMonad<Item> =
            match vaults |> List.tryFind (fun v -> v.Id = vaultId) with
            | Some _ ->
                match items |> List.tryFind (fun i -> i.ItemInfo.Id = itemId && i.ItemInfo.VaultId = vaultId) with
                | Some i -> result i
                | None -> Error ItemNotFound |> ResultT.hoist
            | None -> Error VaultNotFound |> ResultT.hoist
        let getVaultItems (vaultId : VaultId) : ConnectClientMonad<ItemInfo list> =
            match vaults |> List.tryFind (fun v -> v.Id = vaultId) with
            | Some _ ->
                items
                |> List.filter (fun i -> i.ItemInfo.VaultId = vaultId)
                |> List.map (fun x -> x.ItemInfo)
                |> result
            | None -> Error VaultNotFound |> ResultT.hoist

        {
            ConnectClientOperations.GetVaults = getVaults
            GetVaultInfoByTitle = getVaultInfoByTitle
            GetVaultInfoById = getVaultInfoById
            GetItemInfo = getItemInfo
            GetItem = getItem
            GetVaultItems = getVaultItems
            GetFile = fun _ -> Error (CriticalFailure (Exception("Not implemented in fixture"))) |> ResultT.hoist
        }
        |> ConnectClientFacade

    let mutable result : Result<_, ConnectError> = Ok ""

    let [<Given>] ``vault with id "(.*)" and title "(.*)"`` id title =
        let vault = {
            Id = VaultId id
            Title = VaultTitle title
            Version = VaultVersion 0
            CreatedAt = CreatedAt DateTimeOffset.Now
            UpdatedAt = UpdatedAt DateTimeOffset.Now
            ItemCount = ItemCount 0
        }
        vaults <- vault::vaults
    let [<Given>] ``item with id "(.*)" and title "(.*)" in vault "(.*)" with fields`` id title vault (table : Table) =
        let fields =
            table.Rows
            |> Seq.map (fun (row : TableRow) -> {
                Id = FieldId row.[0]
                Label = FieldLabel row.[1]
                Value = FieldValue row.[2]
            })
            |> Seq.toList
        let item = {
            ItemInfo = {
                Id = ItemId id
                Title = ItemTitle title
                VaultId = VaultId vault
                Tags = []
                Version = ItemVersion 0
                Urls = []
            }
            Fields = fields
            Files = []
        }
        items <- item::items
    let [<When>] ``the user runs inject with the following text`` text =
        result <- connectClient.Inject text |> Async.RunSynchronously
    let [<Then>] ``the result should be`` expected = result =! (Ok expected)
