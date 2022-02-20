# 1Password Connect API for F#

## Creating a Client
A client can be created using a settings object or from environment variables.

```f#
open Fs1PasswordConnect

let settings : ConnectClientSettings = {
    Host = ConnectHost "https://connect.mydomain.com"
    Token = ConnectToken "connecttoken"
}
let client : ConnectClientFacade = ConnectClient.fromSettings settings
```

The host and token can also be saved retrieved from the OP_CONNECT_HOST and OP_CONNECT_TOKEN environment variables. `fromEnvironmentVariables` is used in this case.

```f#
open Fs1PasswordConnect
let client : Result<ConnectClientFacade, unit> = ConnectClient.fromEnvironmentVariables ()
```

If `fromEnvironmentVariables` returns an error one of the two environment variables are not set.

`ConnectClientSettings` implements a Fleece json codec and can be easily serialized to and from json.

```f#
open Fleece.SystemTextJson
let json = toJson settings |> string
let deserialized : ParseResult<ConnectClientSettings> = fromJson json
```

### Cached Clients
Both the `fromSettings` `fromEnvironmentVariables` have cached variants (`fromSettingsCached` and `fromEnvironmentVariablesCached`) that cache all calls and will return cached results for same calls.

## Getting Items
Items can be retrieved using a combination of item title or id and vault title or id.

```f#
open Fs1PasswordConnect

let client : ConnectClientFacade = // ...
let item : Async<Result<Item, ConnectError>> = client.GetItem (VaultId "id", ItemId "id")
let item : Async<Result<Item, ConnectError>> = client.GetItem (VaultTitle "title", ItemId "id")
let item : Async<Result<Item, ConnectError>> = client.GetItem (VaultId "id", ItemTitle "title")
let item : Async<Result<Item, ConnectError>> = client.GetItem (VaultTitle "title", ItemTitle "title")
```

## Injecting Item Fields Into a Template

Injecting item fields into a template is supported using the `Inject` function. The function will replace all occurrences of `{{ op://<VaultIdOrTitle/<ItemIdOrTitle>/<FieldIdLabelFileIdOrFileName> }}` with the value of the field (or file contents if the field is a file).

```f#
open Fs1PasswordConnect

let client : ConnectClientFacade = // ...
let template = "{{ op://My Vault/My Item/password }}"
let myPassword : Async<Result<string, ConnectError>> = client.Inject template
```

## Other Operations
Getting a listing of all vaults (`GetVaults`), info about a specific vault (`GetVaultInfo`), a listing of all items in a vault (`GetVaultItems`), and file retrieval (`GetFile`) are also supported.
