namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece.SystemTextJson
open FSharpPlus

type Field = { Id : FieldId; Label : FieldLabel; Value : FieldValue } with
    static member JsonObjCodec =
        fun i l v -> {
            Id = (FieldId i)
            Label = (FieldLabel (l |> Option.defaultValue ""))
            Value = (FieldValue (v |> Option.defaultValue ""))
        }
        |> withFields
        |> jfield "id" (fun { Field.Id = (FieldId i) } -> i)
        |> jfieldOpt "label" (fun { Label = (FieldLabel l) } -> Some l)
        |> jfieldOpt "value" (fun { Value = (FieldValue v) } -> Some v)
and FieldId = FieldId of string
and FieldLabel = FieldLabel of string
and FieldValue = FieldValue of string
