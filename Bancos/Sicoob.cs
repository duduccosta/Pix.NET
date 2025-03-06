using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PixNET.Services.Pix.Bancos
{
    public class Sicoob : Banco
    {
        public Sicoob(PixAmbiente? ambiente)
        {
            endpoint = new Endpoint
            {
                AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token" : "https://api-homol.sicoob.com.br/cooperado/pix/token",
                Pix = ambiente == PixAmbiente.Producao ? "https://apis.sicoob.com.br/cooperado/pix/api/v2/" : "https://api-homol.sicoob.com.br/cooperado/pix/api/v2"
            };
        }

        public override async Task GetAccessTokenAsync(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;

            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write cobv.read cobv.write";
            List<string> headers = new();

            parameters += $"&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";
            headers.Add(string.Format("client_id: {0}", _credentials.clientId));
            try
            {
                var
                    request = await Utils.sendRequestAsync(endpoint.AuthorizationToken, parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch
            {
                throw;
            }

        }

        public override void GetAccessToken(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;
            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write";
            List<string> headers = new();

            parameters += $"&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";
            headers.Add(string.Format("client_id: {0}", _credentials.clientId));

            string
                request = Utils.sendRequest(endpoint.AuthorizationToken, parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate);

            try
            {
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch { }
        }

        public override async Task<PixPayload> CreateCobrancaAsync()
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {
                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                headers.Add(string.Format("client_id: {0}", _credentials.clientId));
                PixPayload? cobranca = null;
                string? request = null;

                bool isPixCobranca = false;
                var tipoApi = "cob";
                if (
                    ((PixPayload)_payload).valor is not null &&
                    (
                        ((PixPayload)_payload).valor.multa is not null ||
                        ((PixPayload)_payload).valor.juros is not null ||
                        ((PixPayload)_payload).valor.desconto is not null
                    )
                  )
                    isPixCobranca = true;

                if (isPixCobranca)
                    tipoApi = "cobv";

                try
                {
                    request = await Utils.sendRequestAsync(endpoint.Pix + $"{tipoApi}/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);
                }
                catch
                {
                    throw;
                }

                token = null;
                request = null;
                return cobranca;

            }
            return null;
        }
        public override async Task<PixPayload> CancelarPixAsync()
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {
                ((PixPayload)_payload).status = "REMOVIDA_PELO_USUARIO_RECEBEDOR";

                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                headers.Add(string.Format("client_id: {0}", _credentials.clientId));
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PATCH", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                PixPayload cobranca = null;

                try
                {
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);
                }
                catch { }

                token = null;
                request = null;
                return cobranca;

            }
            return null;
        }
        public override PixPayload CreateCobranca()
        {
            GetAccessToken();
            if (token is not null)
            {
                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                headers.Add(string.Format("client_id: {0}", _credentials.clientId));
                string request = Utils.sendRequest(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate);
                PixPayload cobranca = null;

                try
                {
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);
                }
                catch { }

                token = null;
                request = null;
                return cobranca;

            }
            return null;
        }
        public override async Task<PixPayload> ConsultaCobrancaAsync(string txid)
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {

                var headers = new List<string>()
                {
                    string.Format("Authorization: Bearer {0}", token.access_token),
                    string.Format("client_id: {0}", _credentials.clientId)
                };
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + txid, "", "GET", headers, 0, "", false, _certificate, 0, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(request))
                    request = await Utils.sendRequestAsync(endpoint.Pix + "cobv/" + txid, "", "GET", headers, 0, "", false, _certificate, 0, cancellationToken).ConfigureAwait(false);

                PixPayload? cobranca = null;
                try
                {
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    if (cobranca is not null)
                    {
                        cobranca.textoImagemQRcode = GerarQrCode(cobranca);
                        if (cobranca.pix is not null)
                            cobranca.pix = cobranca.pix.Select(X =>
                            {
                                X.horario = X.horario.ToLocalTimeWithoutZone();
                                return X;
                            }).ToList();


                        if (cobranca.calendario is not null)
                            cobranca.calendario.criacao = Convert.ToDateTime(cobranca.calendario.criacao).ToLocalTimeWithoutZone();
                    }
                }
                catch { }
                return cobranca;

            }
            return null;
        }
        public override async Task<List<Model.Pix>> ConsultaPixRecebidosAsync()
        {
            List<Model.Pix> listaPix = new();

            try
            {
                await GetAccessTokenAsync().ConfigureAwait(false);
                if (token is not null)
                {
                    string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    List<string> headers = new();

                    headers.Add(string.Format("client_id: {0}", _credentials.clientId));
                    headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                    int
                        paginaAtual = 0;

                    string request = null;

                    bool loop = true;

                    PixRecebidos cobranca = null;
                    while (loop)
                    {
                        await Task.Delay(100, (CancellationToken)cancellationToken);

                        try
                        {
                            string queryString = string.Format
                            (
                                "txIdPresente=true&inicio={0}&fim={1}&paginacao.paginaAtual={2}",
                                HttpUtility.UrlEncode(((PixRecebidosPayload)_payload).inicio.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                                HttpUtility.UrlEncode(((PixRecebidosPayload)_payload).fim.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                                paginaAtual
                            );

                            try
                            {
                                request = await Utils.sendRequestAsync(endpoint.Pix + "pix?" + queryString, null, "GET", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }
                            cobranca = JsonConvert.DeserializeObject<PixRecebidos>(request);

                            if (cobranca is not null && cobranca.pix is not null)
                            {
                                cobranca.pix = cobranca.pix.Select(X =>
                                {
                                    X.horario = X.horario.ToLocalTime();
                                    return X;
                                }).ToList();
                            }

                            if (hasTxId)
                                listaPix.AddRange(cobranca.pix.Where(T => !String.IsNullOrEmpty(T.txid)));
                            else
                                listaPix.AddRange(cobranca.pix);

                            if (
                                cobranca.parametros.paginacao.paginaAtual + 1 < cobranca.parametros.paginacao.quantidadeDePaginas &&
                                cobranca.parametros.paginacao.quantidadeTotalDeItens > listaPix.Count)
                            {
                                paginaAtual = cobranca.parametros.paginacao.paginaAtual + 1;
                                continue;
                            }
                            else
                            {
                                loop = false;
                                break;
                            }
                        }
                        catch
                        {
                            loop = false;
                            break;
                        }
                    }

                    return listaPix;

                }
                return null;
            }
            catch (WebException ex)
            {
                try
                {
                    PixError er = JsonConvert.DeserializeObject<PixError>(ex.Message);
                    if (er is not null)
                    {
                        if (er.erros.Find(T => T.codigo == "4769515") is not null)
                            return listaPix;
                    }
                }
                catch { }
                throw ex;
            }
        }
    }
}
