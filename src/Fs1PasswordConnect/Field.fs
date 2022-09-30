namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece
open FSharpPlus

type Field =
    {
        Id : FieldId
        Label : FieldLabel
        Value : FieldValue
        Section : Section option
    } with
    static member get_Codec () =
        fun i l v s -> {
            Id = (FieldId i)
            Label = (FieldLabel (l |> Option.defaultValue ""))
            Value = (FieldValue (v |> Option.defaultValue ""))
            Section = s
        }
        <!> jreq "id" (fun { Field.Id = (FieldId i) } -> Some i)
        <*> jopt "label" (fun { Field.Label = (FieldLabel l) } -> Some l)
        <*> jopt "value" (fun { Field.Value = (FieldValue v) } -> Some v)
        <*> jopt "section" (fun { Field.Section = s } -> s)
        |> ofObjCodec
and FieldId = FieldId of string
and FieldLabel = FieldLabel of string
and FieldValue = FieldValue of string
and SectionId = SectionId of string
and SectionLabel = SectionLabel of string
and Section =
    {
        Id : SectionId
        Label : SectionLabel
    }
    static member get_Codec () =
        fun i l -> {
            Id = (SectionId i)
            Label = (SectionLabel (l |> Option.defaultValue ""))
        }
        <!> jreq "id" (fun { Section.Id = (SectionId i) } -> Some i)
        <*> jopt "label" (fun { Section.Label = (SectionLabel l) } -> Some l)
        |> ofObjCodec
