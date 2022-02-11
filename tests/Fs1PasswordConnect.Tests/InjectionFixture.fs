namespace Fs1PasswordConnect

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
        let getVaultId (title : VaultTitle) : ConnectClientMonad<VaultId> =
            match vaults |> List.tryFind (fun v -> v.Title = title) with
            | Some v -> v.Id |> result
            | None -> Error VaultNotFound |> ResultT.hoist
        let getItemId (vaultId : VaultId) (itemTitle : ItemTitle) : ConnectClientMonad<ItemId> =
            let itemInfos = items |>> fun x -> x.ItemInfo
            match vaults |> List.tryFind (fun v -> v.Id = vaultId) with
            | Some _ ->
                match itemInfos |> Seq.tryFind (fun i -> i.Title = itemTitle && i.VaultId = vaultId) with
                | Some i -> result i.Id
                | None -> Error ItemNotFound |> ResultT.hoist
            | None -> Error VaultNotFound |> ResultT.hoist
        let getItem (vaultId : VaultId) (itemId : ItemId) : ConnectClientMonad<Item> =
            match vaults |> List.tryFind (fun v -> v.Id = vaultId) with
            | Some _ ->
                match items |> List.tryFind (fun i -> i.ItemInfo.Id = itemId && i.ItemInfo.VaultId = vaultId) with
                | Some i -> result i
                | None -> Error ItemNotFound |> ResultT.hoist
            | None -> Error VaultNotFound |> ResultT.hoist
        let getItems (vaultId : VaultId) : ConnectClientMonad<ItemInfo list> =
            match vaults |> List.tryFind (fun v -> v.Id = vaultId) with
            | Some _ ->
                items
                |> List.filter (fun i -> i.ItemInfo.VaultId = vaultId)
                |> List.map (fun x -> x.ItemInfo)
                |> result
            | None -> Error VaultNotFound |> ResultT.hoist

        {
            GetVaults = getVaults
            GetVaultId = getVaultId
            GetItemId = getItemId
            GetItem = getItem
            GetItems = getItems
        }
        |> ConnectClientFacade

    let mutable result : Result<_, ConnectError> = Ok ""

    let [<Given>] ``vault with id "(.*)" and title "(.*)"`` id title =
        let vault = { Id = VaultId id; Title = VaultTitle title; }
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
            }
            Fields = fields
        }
        items <- item::items
    let [<When>] ``the user runs inject with the following text`` text =
        result <- connectClient.Inject text |> Async.RunSynchronously
    let [<Then>] ``the result should be`` expected = result =! (Ok expected)
