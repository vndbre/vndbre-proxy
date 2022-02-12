module VndbReProxy.Proto.Connection

open System.IO
open System.Net.Security
open System.Net.Sockets

type IsTls =
    | Tls
    | NoTls

type conf =
    { Host: string
      Port: int
      PortTls: int
      Client: string
      ClientVer: string }

let defaultConf =
    { Host = "api.vndb.org"
      Port = 19534
      PortTls = 19535
      Client = "vndbre-proxy"
      ClientVer = "0.0.5" }

let client isTls conf =
    new TcpClient(
        conf.Host,
        match isTls with
        | NoTls -> conf.Port
        | Tls -> conf.PortTls
    )

let private connectNoTls (client: TcpClient) = client.GetStream()

let private connectTls conf (client: TcpClient) =
    let ValidateServerCertificate _sender _certificate _chain _sslPolicyErrors = true

    let sslStream =
        new SslStream(client.GetStream(), false, RemoteCertificateValidationCallback(ValidateServerCertificate), null)

    sslStream.AuthenticateAsClient(conf.Host)
    sslStream

let stream isTls conf client : Stream =
    match isTls with
    | NoTls -> upcast connectNoTls client
    | Tls -> upcast connectTls conf client
