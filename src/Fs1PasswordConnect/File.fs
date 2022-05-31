namespace Fs1PasswordConnect

open FSharpPlus.Data
open Fleece
open FSharpPlus

type File = {
    Id : FileId
    Name : FileName
    Size : FileSize
    Path : FileContentPath
} with
    static member get_Codec () =
        fun i n s p -> {
            Id = FileId i
            Name = FileName n
            Size = FileSize (s |> Option.defaultValue 0)
            Path = FileContentPath p
        }
        <!> jreq "id" (fun { File.Id = (FileId i) } -> Some i)
        <*> jreq "name" (fun { Name = (FileName l) } -> Some l)
        <*> jopt "size" (fun { Size = (FileSize v) } -> Some v)
        <*> jreq "content_path" (fun { File.Path = (FileContentPath p) } -> Some p)
        |> ofObjCodec
and FileId = FileId of string
and FileName = FileName of string
and FileSize = FileSize of int
and FileContentPath = FileContentPath of string
