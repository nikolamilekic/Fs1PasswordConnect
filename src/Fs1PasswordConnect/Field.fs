namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece
open FSharpPlus

type Field = { Id : FieldId; Label : FieldLabel; Value : FieldValue } with
    static member get_Codec () =
        fun i l v -> {
            Id = (FieldId i)
            Label = (FieldLabel (l |> Option.defaultValue ""))
            Value = (FieldValue (v |> Option.defaultValue ""))
        }
        <!> jreq "id" (fun { Field.Id = (FieldId i) } -> Some i)
        <*> jopt "label" (fun { Label = (FieldLabel l) } -> Some l)
        <*> jopt "value" (fun { Field.Value = (FieldValue v) } -> Some v)
        |> ofObjCodec
and FieldId = FieldId of string
and FieldLabel = FieldLabel of string
and FieldValue = FieldValue of string
