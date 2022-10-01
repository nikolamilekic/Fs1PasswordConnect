## New in 2.0 (Released 2022/10/01)
* Injection matching strings are now aligned with the official CLI. The same types of replacement strings are now used.
* Switched to NodaTime for date/time types
* Added InjectIntoEnvironmentVariables to allow for injecting secrets into environment variables
* Added the ability to assign additional headers for HTTP requests to connect

## New in 1.1 (Released 2022/07/15)
* Add `InjectAndReturnReplacements` and `IsTemplate` methods to `ConnectClientFacade`
* Add additional environment variable names. CONNECT_HOST and CONNECT_TOKEN can now be used in addition to OP_CONNECT_HOST and OP_CONNECT_TOKEN.
* Fix a bug that prevented multiple templates from being replaced when they are on the same line.
* Switch to Fleece 0.10

## New in 1.0 (Released 2022/02/20)
* 1.0 release

## New in 0.2 (Released 2022/02/12)
* Reading files
* GetVaultId / GetTitleId refactored to get all item/vault infos
* Added missing vault / item metadata

## New in 0.1 (Released 2022/02/02)
* Initial release
