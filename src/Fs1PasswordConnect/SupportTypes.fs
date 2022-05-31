namespace Fs1PasswordConnect

open System.IO
open Fleece

type ConnectClientSettings = { Host : ConnectHost; Token : ConnectToken } with
    static member get_Codec () =
        fun h t -> { Host = ConnectHost h; Token = ConnectToken t }
        <!> jreq "Host" (fun { Host = (ConnectHost h) } -> Some h)
        <*> jreq "Token" (fun { Token = (ConnectToken t) } -> Some t)
        |> ofObjCodec
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
