namespace Fs1PasswordConnect

open System.IO
open Fleece
open FSharpPlus

type ConnectClientSettings =
    {
        Host : ConnectHost
        Token : ConnectToken
        AdditionalHeaders : (string * string) list
        Proxy : Proxy option
    } with
    static member get_Codec () =
        let headersCodec =
            let decode e =
                ofEncoding e
                >>= fun (line : string) ->
                    let index = line.IndexOf('=')
                    if index < 0 then Decode.Fail.invalidValue e "Header string must be in the format 'key=value'"
                    else Ok (line.Substring(0, index), line.Substring(index + 1))
            let encode (left, right) = toEncoding $"{left}={right}"
            decode <-> encode
            |> Codecs.array

        (fun h t ah p -> {
            Host = ConnectHost h
            Token = ConnectToken t
            AdditionalHeaders = List.ofArray ah
            Proxy =
                match p with
                | Some ""
                | None -> None
                | Some x -> Some (Proxy x)
        })
        <!> jreq "Host" (fun { Host = (ConnectHost h) } -> Some h)
        <*> jreq "Token" (fun { Token = (ConnectToken t) } -> Some t)
        <*> joptWith headersCodec "AdditionalHeaders" (fun { AdditionalHeaders = ah } -> List.toArray ah)
        <*> jopt "Proxy" (fun { Proxy = p } -> p |>> (fun (Proxy x) -> x))
        |> ofObjCodec
and ConnectHost = ConnectHost of string
and ConnectToken = ConnectToken of string
and Proxy = Proxy of string

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
