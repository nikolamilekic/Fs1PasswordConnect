module Fs1PasswordConnect.Tests.FacadeTests

open Expecto
open Swensen.Unquote
open Milekic.YoLo

open Fs1PasswordConnect

[<Tests>]
let isTemplateTests = testList "IsTemplate" [
    [
        let validTemplates = [
            "\"op://Vault 1/Item 1/username\"", ("Vault 1", "Item 1", None, "username", true)
            "\"op://Vault 1/Item 1/Configuration/username\"", ("Vault 1", "Item 1", Some "Configuration", "username", true)
            "op://Vault1/Item1/username", ("Vault1", "Item1", None, "username", false)
            "op://Vault1/Item1/Configuration/username", ("Vault1", "Item1", Some "Configuration", "username", false)
            "op://Vault_1/Item_1/Configuration/username", ("Vault_1", "Item_1", Some "Configuration", "username", false)
            "op://Vault-1/Item-1/Configuration/username", ("Vault-1", "Item-1", Some "Configuration", "username", false)
            " \"op://Vault 1/Item 1/username\"", ("Vault 1", "Item 1", None, "username", true)
            " \"op://Vault 1/Item 1/Configuration/username\"", ("Vault 1", "Item 1", Some "Configuration", "username", true)
            " op://Vault1/Item1/username", ("Vault1", "Item1", None, "username", false)
            " op://Vault1/Item1/Configuration/username", ("Vault1", "Item1", Some "Configuration", "username", false)
            " op://Vault_1/Item_1/Configuration/username", ("Vault_1", "Item_1", Some "Configuration", "username", false)
            " op://Vault-1/Item-1/Configuration/username", ("Vault-1", "Item-1", Some "Configuration", "username", false)
            "\"op://Vault 1/Item 1/username\" ", ("Vault 1", "Item 1", None, "username", true)
            "\"op://Vault 1/Item 1/Configuration/username\" ", ("Vault 1", "Item 1", Some "Configuration", "username", true)
            "op://Vault1/Item1/username ", ("Vault1", "Item1", None, "username", false)
            "op://Vault1/Item1/Configuration/username ", ("Vault1", "Item1", Some "Configuration", "username", false)
            "op://Vault_1/Item_1/Configuration/username ", ("Vault_1", "Item_1", Some "Configuration", "username", false)
            "op://Vault-1/Item-1/Configuration/username ", ("Vault-1", "Item-1", Some "Configuration", "username", false)
            " \"op://Vault 1/Item 1/username\" ", ("Vault 1", "Item 1", None, "username", true)
            " \"op://Vault 1/Item 1/Configuration/username\" ", ("Vault 1", "Item 1", Some "Configuration", "username", true)
            " op://Vault1/Item1/username ", ("Vault1", "Item1", None, "username", false)
            " op://Vault1/Item1/Configuration/username ", ("Vault1", "Item1", Some "Configuration", "username", false)
            " op://Vault_1/Item_1/Configuration/username ", ("Vault_1", "Item_1", Some "Configuration", "username", false)
            " op://Vault-1/Item-1/Configuration/username ", ("Vault-1", "Item-1", Some "Configuration", "username", false)
            " prefix op://Vault-1/Item-1/Configuration/username suffix ", ("Vault-1", "Item-1", Some "Configuration", "username", false)
        ]
        for t, expected in validTemplates do
        testCase t <| fun () ->
            match t with
            | ConnectClientFacade.InjectPattern actual -> actual =! expected
            | _ -> failtest "Expected a valid template but it wasn't"

    ]
    |> testList "Valid templates"

    [
        let invalidTemplates = [
            "op://Vault 1/Item 1/username" //Contains spaces which are not allowed unless quoted
            "op://Vault-1/Item-1" //Missing field or file id
            "op://Vault-1/Item-1/ " //Missing field or file id
            ""
            "op://Vault+1/Item1/username" //Invalid character
            "op://Vault!1/Item1/username" //Invalid character
            "op://Vault*1/Item1/username" //Invalid character
            "op://Vault?1/Item1/username" //Invalid character
        ]
        for t in invalidTemplates do
        testCase t <| fun () ->
            match t with
            | ConnectClientFacade.InjectPattern r -> failtest $"Expected an invalid template but it was valid {r}"
            | _ -> ()
    ]
    |> testList "Invalid templates"
]
