module Fs1PasswordConnect.Tests.FacadeTests

open Expecto
open Swensen.Unquote

open Fs1PasswordConnect

[<Tests>]
let isTemplateTests = testList "IsTemplate" [
    let validTemplate = "{{ op://Vault 1/Item 1/username }}"
    let invalidTemplates = [
        "{{ op://Vault 1/Item 1 }}"
        "{{ op://Vault 1/Item 1/ }}" //Missing field or file id
        ""
        "{{op://Vault 1/Item 1/username}}"
        "{{op://Vault 1/Item 1/username }}"
        "{{ op://Vault 1/Item 1/username}}"
    ]
    testCase "Valid template" <| fun () ->
        <@ ConnectClientFacade.IsTemplate validTemplate = true @> |> test

    [
        for t in invalidTemplates do
        testCase t <| fun () ->
            <@ ConnectClientFacade.IsTemplate t = false @> |> test
    ]
    |> testList "Invalid templates"
]
