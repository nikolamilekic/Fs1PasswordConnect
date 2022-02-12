namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece.SystemTextJson
open FSharpPlus

type File = {
    Id : FileId
    Name : FileName
    Size : FileSize
    Path : FileContentPath
} with
    static member JsonObjCodec =
        fun i n s p -> {
            Id = FileId i
            Name = FileName n
            Size = FileSize (s |> Option.defaultValue 0)
            Path = FileContentPath p
        }
        |> withFields
        |> jfield "id" (fun { File.Id = (FileId i) } -> i)
        |> jfield "name" (fun { Name = (FileName l) } -> l)
        |> jfieldOpt "size" (fun { Size = (FileSize v) } -> Some v)
        |> jfield "content_path" (fun { Path = (FileContentPath p) } -> p)
and FileId = FileId of string
and FileName = FileName of string
and FileSize = FileSize of int
and FileContentPath = FileContentPath of string
