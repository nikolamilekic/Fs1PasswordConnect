﻿namespace Fs1PasswordConnect

open System.Collections.Generic
open Fleece.SystemTextJson
open FSharpPlus

type Item = {
    ItemInfo : ItemInfo
    Fields : Field list
    Files : File list
} with
    static member JsonObjCodec =
        fun fields files info -> {
            ItemInfo = info
            Fields = (fields |> Option.defaultValue [])
            Files = (files |> Option.defaultValue [])
        }
        |> withFields
        |> jfieldOpt "fields" (fun { Fields = fs } -> Some fs)
        |> jfieldOpt "files" (fun { Files = fs } -> Some fs)
        |> fun (decoder, encoder) ->
            (fun o -> decoder o >>= fun c -> fst ItemInfo.JsonObjCodec o |>> c),
            (fun item ->
                let fromItem = encoder item |> seq
                let fromItemInfo = snd ItemInfo.JsonObjCodec item.ItemInfo |> seq
                Dictionary(fromItem ++ fromItemInfo) :> IReadOnlyDictionary<_, _>)
