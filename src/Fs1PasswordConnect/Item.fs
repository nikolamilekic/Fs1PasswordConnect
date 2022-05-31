namespace Fs1PasswordConnect

open Fleece
open FSharpPlus

type Item = {
    ItemInfo : ItemInfo
    Fields : Field list
    Files : File list
} with
    static member get_Codec () =
        let itemInfo =
            let c = ItemInfo.get_ObjCodec ()
            Codec.decode c <-> (fun { ItemInfo = x } -> Codec.encode c x)

        fun fields files info -> {
            ItemInfo = info
            Fields = (fields |> Option.defaultValue [])
            Files = (files |> Option.defaultValue [])
        }
        <!> jopt "fields" (fun { Fields = fs } -> Some fs)
        <*> jopt "files" (fun { Files = fs } -> Some fs)
        <*> itemInfo
        |> ofObjCodec
