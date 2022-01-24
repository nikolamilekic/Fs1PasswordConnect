namespace Fs1PasswordConnect.Tests

open System
open System.IO
open System.Threading
open System.Text
open Swensen.Unquote
open FSharpPlus
open FSharpPlus.Lens
open TechTalk.SpecFlow
open TechTalk.SpecFlow.Assist

open Fs1PasswordConnect

[<Binding>]
type ItemRetrievalFixture() =
    let mutable result = ""
    let mutable receivedEndpointCalls = []
    let mutable items = Map.empty
    let mutable vaults = Map.empty
    let mutable responses = Map.empty
    let client = { new ConnectClient() with
        member _.ProcessRequest url =
            receivedEndpointCalls <- url::receivedEndpointCalls
            match Map.tryFind url responses with
            | Some r -> r
            | None -> failwith $"No response configured for url: {url}"
    }
    let [<Given>] ``the server returns the following body for call to url '(.*)'`` (url : string) (body : string) =
        responses <- Map.add (if url.StartsWith "/" then url else "/" + url) body responses
    let [<Given>] ``item with ID '(.*)' in vault with ID '(.*)'`` (itemId : string) (vaultId : string) body =
        items <- Map.add itemId body items
        vaults <-
            match Map.tryFind vaultId vaults with
            | Some vault -> itemId::vault
            | None -> [itemId]
            |> Map.add vaultId <| vaults
        ``the server returns the following body for call to url '(.*)'`` $"vaults/{vaultId}/items/{itemId}" body
    let [<Given>] ``the client is configured to use host '(.*)' and token '(.*)'`` (host : string) (token : string) = ()
    let [<When>] ``the user requests item with ID '(.*)' in vault with ID '(.*)'`` (itemId : string) (vaultId : string) =
        result <- client.GetItem(VaultId vaultId, ItemId itemId)
    let [<When>] ``the user requests item with title '(.*)' in vault with ID '(.*)'`` (itemTitle : string) (vaultId : string) =
        result <- client.GetItem(VaultId vaultId, ItemTitle itemTitle)
    let [<When>] ``the user requests item with ID '(.*)' in vault with title '(.*)'`` (itemId : string) (vaultTitle : string) =
        result <- client.GetItem(VaultTitle vaultTitle, ItemId itemId)
    let [<When>] ``the user requests item with title '(.*)' in vault with title '(.*)'`` (itemTitle : string) (vaultTitle : string) =
        result <- client.GetItem(VaultTitle vaultTitle, ItemTitle itemTitle)
    let [<Then>] ``the following server endpoint should be called '(.*)'`` (url : string) =
        List.contains url receivedEndpointCalls
    let [<Then>] ``the client should return item with ID '(.*)'`` (itemId : string) =
        result =! items.[itemId]
    let [<When>] ``the user requests all items in vault with ID '(.*)'`` (vaultId : string) =
        result <- client.GetItemsInVault(VaultId vaultId)
    let [<When>] ``the user requests all items in vault with title '(.*)'`` (vaultTitle : string) =
        result <- client.GetItemsInVault(VaultTitle vaultTitle)
    let [<Then>] ``the client should return all items in vault with ID '(.*)'`` (vaultId : string) =
        let url = $"vaults/{vaultId}/items"
        match Map.tryFind ("/" + url) responses with
        | Some expected ->
            result =! expected
        | None -> failwith $"No response configured for url: {url}"

