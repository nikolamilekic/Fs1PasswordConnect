namespace Fs1PasswordConnect

open System
open NodaTime
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

    let mutable result : Result<obj, ConnectError> = Ok ""

    let [<Given>] ``vault with id "(.*)" and title "(.*)"`` id title =
        let now = SystemClock.Instance.GetCurrentInstant()
        let vault = {
            Id = VaultId id
            Title = VaultTitle title
            Version = VaultVersion 0
            CreatedAt = CreatedAt now
            UpdatedAt = UpdatedAt now
            ItemCount = ItemCount 0
        }
        vaults <- vault::vaults
    let [<Given>] ``item with id "(.*)" and title "(.*)" in vault "(.*)" with fields`` id title vault (table : Table) =
        let fields =
            table.Rows
            |> Seq.map (fun (row : TableRow) ->
                let section =
                    let sectionId = row[3]
                    let sectionLabel = row[4]
                    if sectionId <> "" && sectionLabel <> "" then
                        Some { Id = SectionId sectionId; Label = SectionLabel sectionLabel }
                    else None
                {
                    Id = FieldId row[0]
                    Label = FieldLabel row[1]
                    Value = FieldValue row[2]
                    Section = section
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
        result <- Async.RunSynchronously <| async {
            let! result = connectClient.Inject text
            return result |>> box
        }
    let [<Then>] ``the result should be`` expected = result =! (Ok expected)
    let [<Given>] ``environment variable '(.*)' is set to '(.*)'`` name value =
        Environment.SetEnvironmentVariable(name, value)
    let [<When>] ``the user runs inject into environment variables`` () =
        connectClient.InjectIntoEnvironmentVariables ()
        |> Async.RunSynchronously
        |> ignore
    let [<Then>] ``environment variable '(.*)' should be '(.*)'`` name value =
        Environment.GetEnvironmentVariable(name) =! value
