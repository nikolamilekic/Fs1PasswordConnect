module Fs1PasswordConnect.ConnectClient

open System
open System.Text
open FSharpPlus.Data
open Fleece.SystemTextJson
open Fleece.SystemTextJson.Operators
open FSharp.Data
open FSharpPlus
open Milekic.YoLo

type VaultId = VaultId of string
type VaultTitle = VaultTitle of string
type VaultInfo = { Id : VaultId; Title : VaultTitle } with
    static member JsonObjCodec =
        fun i t -> { Id = (VaultId i); Title = (VaultTitle t) }
        |> withFields
        |> jfield "id" (fun { Id = (VaultId i) } -> i)
        |> jfield "name" (fun { Title = (VaultTitle t) } -> t)
type ItemId = ItemId of string
type ItemTitle = ItemTitle of string
let private vaultStubCodec =
    (function | JObject o -> VaultId <!> (o .@ "id")
              | x -> Decode.Fail.objExpected x),
    (fun (VaultId id) -> jobj [ "id" .= id ])
type ItemInfo = { Id : ItemId; Title : ItemTitle; VaultId : VaultId } with
    static member JsonObjCodec =
        fun i t v -> { Id = (ItemId i); Title = (ItemTitle t); VaultId = v }
        |> withFields
        |> jfield "id" (fun { ItemInfo.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (ItemTitle t) } -> t)
        |> jfieldWith vaultStubCodec "vault" (fun { VaultId = v } -> v)
type FieldId = FieldId of string
type FieldLabel = FieldLabel of string
type FieldValue = FieldValue of string
type Field = { Id : FieldId; Label : FieldLabel; Value : FieldValue } with
    static member JsonObjCodec =
        fun i l v -> { Id = (FieldId i); Label = (FieldLabel l); Value = (FieldValue v) }
        |> withFields
        |> jfield "id" (fun { Field.Id = (FieldId i) } -> i)
        |> jfield "label" (fun { Label = (FieldLabel l) } -> l)
        |> jfield "value" (fun { Value = (FieldValue v) } -> v)
type Item = {
    Id : ItemId
    Title : ItemTitle
    VaultId : VaultId
    Fields : Field list } with
    static member JsonObjCodec =
        fun i t v f -> {
            Id = (ItemId i)
            Title = (ItemTitle t)
            VaultId = v
            Fields = f
        }
        |> withFields
        |> jfield "id" (fun { Item.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (ItemTitle t) } -> t)
        |> jfieldWith vaultStubCodec "vault" (fun { Item.VaultId = v } -> v)
        |> jfield "fields" (fun { Fields = f } -> f)

type ConnectError =
    | CriticalFailure of exn
    | UnexpectedStatusCode of int
    | DecodeError of string
    | UnauthorizedAccess
    | MissingToken
    | VaultNotFound
    | ItemNotFound
    with
    override this.ToString() =
        match this with
        | CriticalFailure exn -> $"Critical failure: {exn.Message}"
        | UnexpectedStatusCode code -> $"Connect server return an unexpected status code: {code}"
        | DecodeError message -> $"Connect server return a result which could not be decoded due to the following error: {message}"
        | UnauthorizedAccess -> "Unauthorized access to Connect server"
        | MissingToken -> "Connect server token is missing"
        | VaultNotFound -> "Vault not found"
        | ItemNotFound -> "Item not found"

type ConnectHost = ConnectHost of string
type ConnectToken = ConnectToken of string
type ConnectClientSettings = { Host : ConnectHost; Token : ConnectToken }
type internal Request = { Url : string; Headers : (string * string) list }
type internal Response = { StatusCode : int; Body : string }

type ConnectClient = {
    GetVaults : unit -> ConnectClientMonad<VaultInfo list>
    GetVaultId : VaultTitle -> ConnectClientMonad<VaultId>
    GetItemId : VaultId -> ItemTitle -> ConnectClientMonad<ItemId>
    GetItem : VaultId -> ItemId -> ConnectClientMonad<Item>
    GetItems : VaultId -> ConnectClientMonad<ItemInfo list>
}
and ConnectClientMonad<'a> = ResultT<Async<Result<'a, ConnectError>>>

let internal lift x : ConnectClientMonad<'a> = ResultT.lift x
let internal hoist x : ConnectClientMonad<'a> = ResultT.hoist x

let internal fromRequestProcessor requestProcessor settings =
    let { Host = (ConnectHost host); Token = (ConnectToken token) } = settings
    let headers = [ "Authorization", $"Bearer {token}" ]
    let makeRequest (endpoint : string) = {
        Url = $"{host.TrimEnd('/')}/v1/{endpoint.TrimStart('/')}"
        Headers = headers
    }
    let request (endpoint : string) = monad {
        try return! requestProcessor (makeRequest endpoint) |> lift
        with exn -> return! Error (CriticalFailure exn) |> hoist
    }

    let getVaults () =
        request "vaults" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (vaults : VaultInfo list) -> result vaults
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getVaultId (VaultTitle title) =
        request $"vaults/?filter=title eq \"{title}\"" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (x : VaultInfo::_) -> result x.Id
            | Ok [] -> Error VaultNotFound |> hoist
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getItemId (VaultId vaultId) (ItemTitle itemTitle) =
        request $"vaults/{vaultId}/items?filter=title eq \"{itemTitle}\"" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (x : ItemInfo::_) -> result x.Id
            | Ok [] -> Error ItemNotFound |> hoist
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getItem (VaultId vaultId) (ItemId itemId) =
        request $"vaults/{vaultId}/items/{itemId}" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (x : Item) -> result x
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 403 } -> Error UnauthorizedAccess |> hoist
        | { StatusCode = 404 } -> Error ItemNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getItems (VaultId vaultId) =
        request $"vaults/{vaultId}/items" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (xs : ItemInfo list) -> result xs
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist

    {
        GetVaults = getVaults
        GetVaultId = getVaultId
        GetItemId = getItemId
        GetItem = getItem
        GetItems = getItems
    }

let fromSettings settings =
    let requestProcessor { Url = url; Headers = headers } = monad {
        let! response = Http.AsyncRequest(url, headers = headers)
        let body =
            match response.Body with
            | Text text -> text
            | Binary binary -> Encoding.UTF8.GetString(binary)
        return { StatusCode = response.StatusCode; Body = body }
    }
    fromRequestProcessor requestProcessor settings

/// Attempts to make a client instance from OP_CONNECT_HOST and OP_CONNECT_TOKEN
/// environment variables Fails if either is not set.
let fromEnvironmentVariables () =
    let host = Environment.GetEnvironmentVariable("OP_CONNECT_HOST")
    let token = Environment.GetEnvironmentVariable("OP_CONNECT_TOKEN")
    if host <> null && token <> null
    then Ok <| fromSettings { Host = ConnectHost host; Token = ConnectToken token }
    else Error ()

let internal cacheConnectFunction (f : 'a -> ConnectClientMonad<'b>) =
    let cache = ref Map.empty
    let cached x : ConnectClientMonad<'b> = ResultT <| async {
        match Map.tryFind x cache.Value with
        | Some y -> return y
        | None ->
            let! result = f x |> ResultT.run
            atomicUpdateQueryResult cache (fun c -> (), Map.add x result c)
            return result
    }
    cached

let cache inner = {
    GetVaults = cacheConnectFunction inner.GetVaults
    GetVaultId = cacheConnectFunction inner.GetVaultId
    GetItemId = cacheConnectFunction (uncurry inner.GetItemId) |> curry
    GetItem = cacheConnectFunction (uncurry inner.GetItem) |> curry
    GetItems = cacheConnectFunction inner.GetItems
}

type ConnectClientFacade (client) =
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
