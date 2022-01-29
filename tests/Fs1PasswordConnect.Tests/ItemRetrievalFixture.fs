namespace Fs1PasswordConnect.Tests

open Fs1PasswordConnect
open Swensen.Unquote
open FSharpPlus
open FSharpPlus.Data
open FSharpPlus.Lens
open TechTalk.SpecFlow
open Fleece.SystemTextJson

open Fs1PasswordConnect.ConnectClient

[<Binding>]
type ItemRetrievalFixture() =
    let mutable result = box ""
    let mutable receivedCalls = []
    let mutable items = Map.empty
    let mutable vaults = Map.empty
    let mutable responses = Map.empty
    let mutable token = ""
    let mutable host = ""
    let requestProcessor { Url = url; Headers = headers } = async {
        receivedCalls <- url::receivedCalls

        let expectedHeaders = [ "Authorization", $"Bearer {token}" ]
        headers =! expectedHeaders

        return
            match Map.tryFind url responses with
            | Some r -> { Body = r; StatusCode = 200 }
            | None -> failwith $"No response configured for url: {url}"
    }
    let fromSettings = ConnectClient.fromRequestProcessor requestProcessor
    let mutable client =
        let c = fromSettings { Host = ConnectHost ""; Token = ConnectToken "" }
        ConnectClientFacade c
    let run (x : ResultT<Async<Result<_, ConnectError>>>) =
        match ResultT.run x |> Async.RunSynchronously with
        | Ok x -> result <- x
        | Error e -> result <- e.ToString()
    let [<Given>] ``the client is configured to use host '(.*)' and token '(.*)'`` (h : string) (t : string) =
        host <- h.TrimEnd('/')
        token <- t
        let c = fromSettings { Host = ConnectHost h; Token = ConnectToken t }
        client <- ConnectClientFacade c
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
        client.GetItem(VaultId vaultId, ItemId itemId) |> run
    let [<When>] ``the user requests item with title '(.*)' in vault with ID '(.*)'`` (itemTitle : string) (vaultId : string) =
        client.GetItem(VaultId vaultId, ItemTitle itemTitle) |> run
    let [<When>] ``the user requests item with ID '(.*)' in vault with title '(.*)'`` (itemId : string) (vaultTitle : string) =
        client.GetItem(VaultTitle vaultTitle, ItemId itemId) |> run
    let [<When>] ``the user requests item with title '(.*)' in vault with title '(.*)'`` (itemTitle : string) (vaultTitle : string) =
        client.GetItem(VaultTitle vaultTitle, ItemTitle itemTitle) |> run
    let [<Then>] ``the following url should be called '(.*)'`` (url : string) =
        List.contains url receivedCalls
    let [<Then>] ``the client should return item with ID '(.*)'`` (itemId : string) =
        match parseJson items.[itemId] with
        | Ok (expected : Item) -> result :?> Item =! expected
        | Error x -> failwith $"The following error occured while trying to parse the expected response: {x}"
    let [<When>] ``the user requests all items in vault with ID '(.*)'`` (vaultId : string) =
        client.GetItems(VaultId vaultId) |> run
    let [<When>] ``the user requests all items in vault with title '(.*)'`` (vaultTitle : string) =
        client.GetItems(VaultTitle vaultTitle) |> run
    let [<Then>] ``the client should return all items in vault with ID '(.*)'`` (vaultId : string) =
        let url = $"{host}/v1/vaults/{vaultId}/items"
        match Map.tryFind url responses with
        | Some expected ->
            match parseJson expected with
            | Ok (expectedItems : ItemInfo list) ->
                result :?> ItemInfo list =! expectedItems
            | Error x -> failwith $"The following error occured while trying to parse the expected response: {x}"
        | None -> failwith $"No response configured for url: {url}"
    let [<When>] ``the user requests all vaults`` () =
        client.GetVaults() |> run
    let [<Then>] ``the client should return all vaults`` () =
        let url = $"{host}/v1/vaults"
        match Map.tryFind url responses with
        | Some expected ->
            match parseJson expected with
            | Ok (expectedVaults : VaultInfo list) ->
                result :?> VaultInfo list =! expectedVaults
            | Error x -> failwith $"The following error occured while trying to parse the expected response: {x}"
        | None -> failwith $"No response configured for url: {url}"
