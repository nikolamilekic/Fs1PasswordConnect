# 1Password Connect API for F#

## Creating a Client
A client can be created using a settings object or from environment variables.

```f#
open Fs1PasswordConnect

let settings : ConnectClientSettings = {
    Host = ConnectHost "https://connect.mydomain.com"
    Token = ConnectToken "connecttoken"
    AdditionalHeaders = []
}
let client : ConnectClientFacade = ConnectClient.fromSettings settings
```

The host and token can also be saved retrieved from the CONNECT_HOST, CONNECT_TOKEN, and CONNECT_ADDITIONAL_HEADERS environment variables (OP_CONNECT_HOST, OP_CONNECT_TOKEN and OP_CONNECT_ADDITIONAL_HEADERS can be used as well). `fromEnvironmentVariables` is used in this case.

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
Both the `fromSettings` `fromEnvironmentVariables` are cached by default. Variants without caching exist as well (`fromEnvironmentVariablesWithoutCache` and `fromSettingsWithoutCache`).

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

Injecting item fields into a template is supported using the `Inject` function. The function will replace all occurrences of `op://<VaultIdOrTitle/<ItemIdOrTitle>/[SectionIdOrLabel]/<FieldIdLabelFileIdOrFileName>` with the value of the field (or file contents if the field is a file).

Inject command works with the same kind of replacement string as the 1Password CLI.
Same requirements apply. If the replacement string contains spaces it must be surrounded by quotes ("). The replacement string can contain alphanumeric characters and the underscore and the hyphen (_, -).

```f#
open Fs1PasswordConnect

let client : ConnectClientFacade = // ...
let template = "op://MyVault/MyItem/password"
let myPassword : Async<Result<string, ConnectError>> = client.Inject template
```

## Injecting Secrets Into Environment Variables
```f#
monad.strict {
    let! client =
        ConnectClient.fromEnvironmentVariables()
        |> Result.mapError (fun _ -> printfn "Connect client not configured")

    printfn "Injecting secrets into environment variables using Connect"

    let ghActions = GitHubActions.detect ()
    for u, r in client.InjectIntoEnvironmentVariables() |> Async.RunSynchronously do
        match r with
        | Ok (s : string) ->
            printfn $"Updated environment variable {u}"
            if ghActions then
                //See https://github.com/actions/toolkit/blob/4fbc5c941a57249b19562015edbd72add14be93d/packages/core/src/command.ts#L23
                let escaped =
                    s
                        .Replace("%", "%25")
                        .Replace("\r", "%0D")
                        .Replace("\n", "%0A")
                printfn $"::add-mask::{escaped}"
        | Error e -> printfn $"Failed to update environment variable {u}: {e}"
    printfn "Secret injection done"

    return ()
}
```

## Other Operations
Getting a listing of all vaults (`GetVaults`), info about a specific vault (`GetVaultInfo`), a listing of all items in a vault (`GetVaultItems`), and file retrieval (`GetFile`) are also supported.
