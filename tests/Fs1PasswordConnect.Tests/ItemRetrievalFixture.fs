namespace Fs1PasswordConnect.Tests

open System
open Fs1PasswordConnect
open Swensen.Unquote
open FSharpPlus
open FSharpPlus.Data
open FSharpPlus.Lens
open TechTalk.SpecFlow
open Fleece.SystemTextJson

[<Binding>]
type ItemRetrievalFixture() =
    let mutable receivedCalls = []
    let mutable items = Map.empty
    let mutable vaults = Map.empty
    let mutable responses = Map.empty
    let mutable token = ""
    let mutable host = ""
    let requestProcessor builder = async {
        let { Url = url; Headers = headers } = builder Request.Zero

        receivedCalls <- url::receivedCalls

        let expectedHeaders = [ "Authorization", $"Bearer {token}" ]
        headers =! expectedHeaders

        return
            match Map.tryFind url responses with
            | Some r -> { Body = r; StatusCode = 200 }
            | None -> failwith $"No response configured for url: {url}"
    }
    let fromSettings =
        ConnectClient.fromRequestProcessor requestProcessor
        >> ConnectClientFacade
    let mutable client =
        fromSettings { Host = ConnectHost ""; Token = ConnectToken "" }

    let mutable itemResult : Result<Item, _> = Error <| CriticalFailure (Exception("Never called"))
    let mutable itemInfoListResult : Result<ItemInfo list, _> = Error <| CriticalFailure (Exception("Never called"))
    let mutable vaultInfoListResult : Result<VaultInfo list, _> = Error <| CriticalFailure (Exception("Never called"))

    let [<Given>] ``the client is configured to use host '(.*)' and token '(.*)'`` (h : string) (t : string) =
        host <- h.TrimEnd('/')
        token <- t
        client <- fromSettings { Host = ConnectHost h; Token = ConnectToken t }
    let [<Given>] ``the server returns the following body for call to url '(.*)'`` (url : string) (body : string) =
        responses <- Map.add url body responses
    let [<Given>] ``item with ID '(.*)' in vault with ID '(.*)'`` (itemId : string) (vaultId : string) body =
        items <- Map.add itemId body items
        vaults <-
            match Map.tryFind vaultId vaults with
            | Some vault -> itemId::vault
            | None -> [itemId]
            |> Map.add vaultId <| vaults
        ``the server returns the following body for call to url '(.*)'`` $"{host}/v1/vaults/{vaultId}/items/{itemId}" body
    let [<When>] ``the user requests item with ID '(.*)' in vault with ID '(.*)'`` (itemId : string) (vaultId : string) =
        itemResult <- client.GetItem(VaultId vaultId, ItemId itemId) |> Async.RunSynchronously
    let [<When>] ``the user requests item with title '(.*)' in vault with ID '(.*)'`` (itemTitle : string) (vaultId : string) =
        itemResult <- client.GetItem(VaultId vaultId, ItemTitle itemTitle) |> Async.RunSynchronously
    let [<When>] ``the user requests item with ID '(.*)' in vault with title '(.*)'`` (itemId : string) (vaultTitle : string) =
        itemResult <- client.GetItem(VaultTitle vaultTitle, ItemId itemId) |> Async.RunSynchronously
    let [<When>] ``the user requests item with title '(.*)' in vault with title '(.*)'`` (itemTitle : string) (vaultTitle : string) =
        itemResult <- client.GetItem(VaultTitle vaultTitle, ItemTitle itemTitle) |> Async.RunSynchronously
    let [<Then>] ``the following url should be called '(.*)'`` (url : string) =
        List.contains url receivedCalls
    let [<Then>] ``the client should return item with ID '(.*)'`` (itemId : string) =
        match parseJson items.[itemId] with
        | Ok (expected : Item) -> itemResult =! Ok expected
        | Error x -> failwith $"The following error occured while trying to parse the expected response: {x}"
    let [<When>] ``the user requests all items in vault with ID '(.*)'`` (vaultId : string) =
        itemInfoListResult <- client.GetVaultItems(VaultId vaultId) |> Async.RunSynchronously
    let [<When>] ``the user requests all items in vault with title '(.*)'`` (vaultTitle : string) =
        itemInfoListResult <- client.GetVaultItems(VaultTitle vaultTitle) |> Async.RunSynchronously
    let [<Then>] ``the client should return all items in vault with ID '(.*)'`` (vaultId : string) =
        let url = $"{host}/v1/vaults/{vaultId}/items"
        match Map.tryFind url responses with
        | Some expected ->
            match parseJson expected with
            | Ok (expectedItems : ItemInfo list) ->
                itemInfoListResult =! Ok expectedItems
            | Error x -> failwith $"The following error occured while trying to parse the expected response: {x}"
        | None -> failwith $"No response configured for url: {url}"
    let [<When>] ``the user requests all vaults`` () =
        vaultInfoListResult <- client.GetVaults() |> Async.RunSynchronously
    let [<Then>] ``the client should return all vaults`` () =
        let url = $"{host}/v1/vaults"
        match Map.tryFind url responses with
        | Some expected ->
            match parseJson expected with
            | Ok (expectedVaults : VaultInfo list) ->
                vaultInfoListResult =! Ok expectedVaults
            | Error x -> failwith $"The following error occured while trying to parse the expected response: {x}"
        | None -> failwith $"No response configured for url: {url}"
    let [<Then>] ``the item should contain tag '(.*)'`` tag =
        test <@ (itemResult |> Result.get).ItemInfo.Tags |> List.contains (ItemTag tag) = true @>
