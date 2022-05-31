[<RequireQualifiedAccess>]
module Fs1PasswordConnect.ConnectClient

open System
open System.Net
open System.Text
open FSharpPlus.Data
open Fleece.SystemTextJson
open FSharp.Data
open FSharpPlus
open Milekic.YoLo

let internal lift x : ConnectClientMonad<'a> = ResultT.lift x
let internal hoist x : ConnectClientMonad<'a> = ResultT.hoist x

let internal fromRequestProcessor requestProcessor settings =
    let { Host = (ConnectHost host); Token = (ConnectToken token) } = settings
    let requestWithBuilder requestBuilder = monad {
        try return! requestProcessor requestBuilder |> lift
        with exn -> return! Error (CriticalFailure exn) |> hoist
    }
    let request (endpoint : string) =
        let requestBuilder request = {
            request with
                Url = $"{host.TrimEnd('/')}/v1/{endpoint.TrimStart('/')}"
                Headers = [ "Authorization", $"Bearer {token}" ]
        }
        requestWithBuilder requestBuilder

    let getVaults () =
        request "vaults" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match ofJsonText response with
            | Ok (vaults : VaultInfo list) -> result vaults
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getVaultInfoByTitle (VaultTitle title) =
        request $"vaults?filter=title eq \"{title}\"" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match ofJsonText response with
            | Ok (x : VaultInfo::_) -> result x
            | Ok [] -> Error VaultNotFound |> hoist
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getVaultInfoById (VaultId id) =
        request $"vaults/{id}" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match ofJsonText response with
            | Ok (x : VaultInfo) -> result x
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 403 } -> Error UnauthorizedAccess |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getItemInfo (VaultId vaultId) (ItemTitle itemTitle) =
        request $"vaults/{vaultId}/items?filter=title eq \"{itemTitle}\"" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match ofJsonText response with
            | Ok (x : ItemInfo::_) -> result x
            | Ok [] -> Error ItemNotFound |> hoist
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getItem (VaultId vaultId) (ItemId itemId) =
        request $"vaults/{vaultId}/items/{itemId}" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match ofJsonText response with
            | Ok (x : Item) -> result x
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 403 } -> Error UnauthorizedAccess |> hoist
        | { StatusCode = 404 } -> Error ItemNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getVaultItems (VaultId vaultId) =
        request $"vaults/{vaultId}/items" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match ofJsonText response with
            | Ok (xs : ItemInfo list) -> result xs
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    let getFile (FileContentPath path) =
        let requestBuilder request = {
            request with
                Url = $"{host.TrimEnd('/')}/{path.TrimStart('/')}"
                Headers = [ "Authorization", $"Bearer {token}" ]
                RequestStream = true
        }
        requestWithBuilder requestBuilder >>= function
        | ({ StatusCode = 200; Stream = stream } : Response) ->
            result (stream |> Option.get)
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 403 } -> Error UnauthorizedAccess |> hoist
        | { StatusCode = 404 } -> Error FileNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist

    {
        GetVaults = getVaults
        GetVaultInfoByTitle = getVaultInfoByTitle
        GetVaultInfoById = getVaultInfoById
        GetItemInfo = getItemInfo
        GetItem = getItem
        GetVaultItems = getVaultItems
        GetFile = getFile
    }

let private operationsFromSettings settings =
    let requestProcessor requestBuilder = monad {
        let {
            Url = url
            Headers = headers
            RequestStream = requestStream
        } = requestBuilder Request.Zero

        if not requestStream then
            let! response = Http.AsyncRequest(url, headers = headers)
            let body =
                match response.Body with
                | Text text -> text
                | Binary binary -> Encoding.UTF8.GetString(binary)
            return { StatusCode = response.StatusCode; Body = body; Stream = None }
        else
            let! response = Http.AsyncRequestStream(url, headers = headers)
            return {
                StatusCode = response.StatusCode
                Body = ""
                Stream = Some response.ResponseStream
            }
    }

    fromRequestProcessor requestProcessor settings

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

let internal cache inner = {
    GetVaults = cacheConnectFunction inner.GetVaults
    GetVaultInfoByTitle = cacheConnectFunction inner.GetVaultInfoByTitle
    GetVaultInfoById = cacheConnectFunction inner.GetVaultInfoById
    GetItemInfo = cacheConnectFunction (uncurry inner.GetItemInfo) |> curry
    GetItem = cacheConnectFunction (uncurry inner.GetItem) |> curry
    GetVaultItems = cacheConnectFunction inner.GetVaultItems
    GetFile = inner.GetFile
}

let fromSettings = operationsFromSettings >> ConnectClientFacade
let fromSettingsCached = operationsFromSettings >> cache >> ConnectClientFacade

/// Attempts to make a client instance from OP_CONNECT_HOST and OP_CONNECT_TOKEN
/// environment variables Fails if either is not set.
let internal operationsFromEnvironmentVariables () =
    let host = Environment.GetEnvironmentVariable("OP_CONNECT_HOST")
    let token = Environment.GetEnvironmentVariable("OP_CONNECT_TOKEN")
    if host <> null && token <> null
    then Ok <| operationsFromSettings { Host = ConnectHost host; Token = ConnectToken token }
    else Error ()

/// Attempts to make a client instance from OP_CONNECT_HOST and OP_CONNECT_TOKEN
/// environment variables Fails if either is not set.
let fromEnvironmentVariables = operationsFromEnvironmentVariables >> Result.map ConnectClientFacade

/// Attempts to make a client instance from OP_CONNECT_HOST and OP_CONNECT_TOKEN
/// environment variables Fails if either is not set.
let fromEnvironmentVariablesCached =
    operationsFromEnvironmentVariables
    >> Result.map (cache >> ConnectClientFacade)
