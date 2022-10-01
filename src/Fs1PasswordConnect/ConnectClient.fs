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

let internal operationsFromRequestProcessor requestProcessor settings =
    let requestProcessor request = monad {
        try return! requestProcessor request |> lift
        with exn -> return! Error (CriticalFailure exn) |> hoist
    }

    let { Host = (ConnectHost host); Token = (ConnectToken token) } = settings
    let baseRequest =
        {
            Url = $"{host.TrimEnd('/')}/v1/"
            Headers = ("Authorization", $"Bearer {token}")::settings.AdditionalHeaders
            RequestStream = false
        }

    let request (endpoint : string) =
        {
            baseRequest with
                Url = baseRequest.Url + $"{endpoint.TrimStart('/')}"
        }
        |> requestProcessor

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
        {
            baseRequest with
                Url = $"{host.TrimEnd('/')}/{path.TrimStart('/')}"
                RequestStream = true
        }
        |> requestProcessor >>= function
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
    let requestProcessor
        {
            Url = url
            Headers = headers
            RequestStream = requestStream
        } = monad {

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

    operationsFromRequestProcessor requestProcessor settings

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

let fromSettings = operationsFromSettings >> cache >> ConnectClientFacade
let fromSettingsWithoutCache = operationsFromSettings >> ConnectClientFacade

/// Attempts to make a client instance from CONNECT_HOST and CONNECT_TOKEN
/// environment variables (OP_CONNECT_HOST and OP_CONNECT_TOKEN can be used as well).
/// Fails if either is not set.
let internal operationsFromEnvironmentVariables () = monad.strict {
    let getEnvironment x =
        Seq.map Environment.GetEnvironmentVariable x
        |> Seq.tryFind (fun x -> x <> null)
        |> Option.toResult

    let! host = getEnvironment [ "CONNECT_HOST"; "OP_CONNECT_HOST" ]
    let! token = getEnvironment [ "CONNECT_TOKEN"; "OP_CONNECT_TOKEN" ]
    let additionalHeaders =
        getEnvironment [ "CONNECT_ADDITIONAL_HEADERS"; "OP_CONNECT_ADDITIONAL_HEADERS" ]
        |>> fun ah ->
            let separators = [| Environment.NewLine; "\n" |]
            let lines = ah.Split(separators, StringSplitOptions.TrimEntries)
            [ for line in lines ->
                let index = line.IndexOf('=')
                if index < 0
                then line, ""
                else line.Substring(0, index), line.Substring(index + 1) ]
        |> Result.defaultValue []

    return
        {
            Host = ConnectHost host
            Token = ConnectToken token
            AdditionalHeaders = additionalHeaders
        }
        |> operationsFromSettings
}

/// Attempts to make a client instance from CONNECT_HOST and CONNECT_TOKEN
/// environment variables (OP_CONNECT_HOST and OP_CONNECT_TOKEN can be used as well).
/// Fails if either is not set.
let fromEnvironmentVariables =
    operationsFromEnvironmentVariables
    >> Result.map (cache >> ConnectClientFacade)

let fromEnvironmentVariablesWithoutCache =
    operationsFromEnvironmentVariables >> Result.map ConnectClientFacade
