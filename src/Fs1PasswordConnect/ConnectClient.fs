namespace Fs1PasswordConnect

open Milekic.YoLo

type VaultDesignator =  VaultId of string | VaultTitle of string
type ItemDesignator = ItemId of string | ItemTitle of string

type ConnectClient() =
    abstract member ProcessRequest : url:string -> string
    default _.ProcessRequest url = url

    member this.GetVaultIdFromDesignator(vaultDesignator) =
        match vaultDesignator with
        | VaultId(id) -> id
        | VaultTitle title ->
            let response = this.ProcessRequest($"/vaults/?filter=title eq \"{title}\"")
            match response with
            | Regex "\"id\": \"(.*)\"" [ id ] -> id
            | _ -> ""
    member this.GetItemIdFromDesignator(vaultDesignator, itemDesignator) =
        let vaultId = this.GetVaultIdFromDesignator(vaultDesignator)
        match itemDesignator with
        | ItemId(id) -> id
        | ItemTitle title ->
            let response = this.ProcessRequest($"/vaults/{vaultId}/items?filter=title eq \"{title}\"")
            match response with
            | Regex "\"id\": \"(.*)\"" [ id ] -> id
            | _ -> ""
    member this.GetItem(vaultDesignator, itemDesignator) =
        let vaultId = this.GetVaultIdFromDesignator(vaultDesignator)
        let itemId = this.GetItemIdFromDesignator(VaultId vaultId, itemDesignator)
        this.ProcessRequest($"/vaults/{vaultId}/items/{itemId}")
    member this.GetItemsInVault(vaultDesignator) =
        let vaultId = this.GetVaultIdFromDesignator(vaultDesignator)
        this.ProcessRequest($"/vaults/{vaultId}/items")
