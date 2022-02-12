namespace Fs1PasswordConnect

open System.IO
open Fleece.SystemTextJson

type ConnectClientSettings = { Host : ConnectHost; Token : ConnectToken } with
    static member JsonObjCodec =
        fun h t -> { Host = ConnectHost h; Token = ConnectToken t }
        |> withFields
        |> jfield "Host" (fun { Host = (ConnectHost h) } -> h)
        |> jfield "Token" (fun { Token = (ConnectToken t) } -> t)
and ConnectHost = ConnectHost of string
and ConnectToken = ConnectToken of string

type internal Request = {
    Url : string
    Headers : (string * string) list
    RequestStream : bool
} with
    static member Zero =  { Url = ""; Headers = []; RequestStream = false }
type internal Response = {
    StatusCode : int
    Body : string
    Stream : Stream option
}
