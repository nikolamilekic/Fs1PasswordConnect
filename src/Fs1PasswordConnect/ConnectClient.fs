module Fs1PasswordConnect.ConnectClient

open System
open System.Text
open FSharpPlus.Data
open Fleece.SystemTextJson
open Fleece.SystemTextJson.Operators
open FSharp.Data
open FSharpPlus

type VaultId = VaultId of string
type VaultTitle = VaultTitle of string
type VaultInfo = { Id : VaultId; Title : VaultTitle } with
    static member JsonObjCodec =
        fun i t -> { Id = (VaultId i); Title = (VaultTitle t) }
        |> withFields
        |> jfield "id" (fun { Id = (VaultId i) } -> i)
        |> jfield "name" (fun { Title = (VaultTitle t) } -> t)
type ItemId = ItemId of string
type Title = Title of string
let private vaultStubCodec =
    (function | JObject o -> VaultId <!> (o .@ "id")
              | x -> Decode.Fail.objExpected x),
    (fun (VaultId id) -> jobj [ "id" .= id ])
type ItemInfo = { Id : ItemId; Title : Title; VaultId : VaultId } with
    static member JsonObjCodec =
        fun i t v -> { Id = (ItemId i); Title = (Title t); VaultId = v }
        |> withFields
        |> jfield "id" (fun { ItemInfo.Id = (ItemId i) } -> i)
        |> jfield "title" (fun { Title = (Title t) } -> t)
        |> jfieldWith vaultStubCodec "vault" (fun { VaultId = v } -> v)
type FieldId = FieldId of string
type Label = Label of string
type FieldValue = FieldValue of string
type Field = { Id : FieldId; Label : Label; Value : FieldValue } with
    static member JsonObjCodec =
        fun i l v -> { Id = (FieldId i); Label = (Label l); Value = (FieldValue v) }
        |> withFields
        |> jfield "id" (fun { Field.Id = (FieldId i) } -> i)
        |> jfield "label" (fun { Label = (Label l) } -> l)
        |> jfield "value" (fun { Value = (FieldValue v) } -> v)
type Item = {
    ItemId : ItemId
    ItemTitle : Title
    VaultId : VaultId
    Fields : Field list } with
    static member JsonObjCodec =
        fun i t v f -> {
            ItemId = (ItemId i)
            ItemTitle = (Title t)
            VaultId = v
            Fields = f
        }
        |> withFields
        |> jfield "id" (fun { Item.ItemId = (ItemId i) } -> i)
        |> jfield "title" (fun { ItemTitle = (Title t) } -> t)
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

type ConnectHost = ConnectHost of string
type ConnectToken = ConnectToken of string
type ConnectClientSettings = { Host : ConnectHost; Token : ConnectToken }
type internal Request = { Url : string; Headers : (string * string) list }
type internal Response = { StatusCode : int; Body : string }

type ConnectClient internal (requestProcessor, settings) =
    let { Host = (ConnectHost host); Token = (ConnectToken token) } = settings
    let headers = [ "Authorization", $"Bearer {token}" ]
    let makeRequest (endpoint : string) = {
        Url = $"{host.TrimEnd('/')}/v1/{endpoint.TrimStart('/')}"
        Headers = headers
    }
    let lift : (Async<_> -> ResultT<Async<Result<_, ConnectError>>>) = ResultT.lift
    let hoist : (Result<_, ConnectError> -> ResultT<Async<_>>) = ResultT.hoist
    let request (endpoint : string) = monad {
        try return! requestProcessor (makeRequest endpoint) |> lift
        with exn -> return! Error (CriticalFailure exn) |> hoist
    }

    static member Make settings =
        let processRequest { Url = url; Headers = headers } = monad {
            let! response = Http.AsyncRequest(url, headers = headers)
            let body =
                match response.Body with
                | Text text -> text
                | Binary binary -> Encoding.UTF8.GetString(binary)
            return { StatusCode = response.StatusCode; Body = body }
        }
        ConnectClient(processRequest, settings)

    /// Attempts to make a client instance from OP_CONNECT_HOST and OP_CONNECT_TOKEN
    /// environment variables Fails if either is not set.
    static member MakeFromEnvironment() =
        let host = Environment.GetEnvironmentVariable("OP_CONNECT_HOST")
        let token = Environment.GetEnvironmentVariable("OP_CONNECT_TOKEN")
        if host <> null && token <> null
        then Ok <| ConnectClient.Make { Host = ConnectHost host; Token = ConnectToken token }
        else Error ()
    member _.GetVaults() = request "vaults" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (vaults : VaultInfo list) -> result vaults
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    member _.GetVaultId(VaultTitle title) =
        request $"vaults/?filter=title eq \"{title}\"" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (x : VaultInfo::_) -> result x.Id
            | Ok [] -> Error VaultNotFound |> hoist
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    member _.GetItemId(VaultId vaultId, Title itemTitle) =
        request $"vaults/{vaultId}/items?filter=title eq \"{itemTitle}\"" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (x : ItemInfo::_) -> result x.Id
            | Ok [] -> Error ItemNotFound |> hoist
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    member _.GetItem(VaultId vaultId, ItemId itemId) =
        request $"vaults/{vaultId}/items/{itemId}" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (x : Item) -> result x
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 403 } -> Error UnauthorizedAccess |> hoist
        | { StatusCode = 404 } -> Error ItemNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    member this.GetItem(vaultTitle, itemId : ItemId) =
        this.GetVaultId vaultTitle
        >>= fun vaultId -> this.GetItem(vaultId, itemId)
    member this.GetItem(vaultId, itemTitle) =
        this.GetItemId(vaultId, itemTitle)
        >>= fun itemId -> this.GetItem(vaultId, itemId)
    member this.GetItem(vaultTitle, itemTitle : Title) =
        this.GetVaultId vaultTitle
        >>= fun vaultId -> this.GetItem(vaultId, itemTitle)
    member _.GetItems(VaultId vaultId) =
        request $"vaults/{vaultId}/items" >>= function
        | ({ StatusCode = 200; Body = response } : Response) ->
            match parseJson response with
            | Ok (xs : ItemInfo list) -> result xs
            | Error error -> Error (DecodeError (error.ToString())) |> hoist
        | { StatusCode = 401 } -> Error MissingToken |> hoist
        | { StatusCode = 404 } -> Error VaultNotFound |> hoist
        | { StatusCode = c } -> Error (UnexpectedStatusCode c) |> hoist
    member x.GetItems(vaultTitle) = x.GetVaultId vaultTitle >>= x.GetItems
