using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PixNET.Services.Pix.Bancos
{
    public class Itau : Banco
    {
        public Itau(PixAmbiente? ambiente)
        {
            endpoint = new Endpoint
            {
                AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://sts.itau.com.br/api/oauth/token" : "https://api.itau.com.br/sandbox/api/oauth/token",
                Pix = ambiente == PixAmbiente.Producao ? "https://secure.api.itau/pix_recebimentos/v2/" : "https://api.itau.com.br/sandbox/pix_recebimentos/v2/"
            };
        }

        public override async Task GetAccessTokenAsync(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;

            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write cobv.read cobv.write";
            List<string> headers = new();

            headers.Add($"x-itau-correlationID: {Guid.NewGuid()}");
            headers.Add($"x-itau-flowID: {Guid.NewGuid()}");
            parameters += $"&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";


            string
                request = await Utils.sendRequestAsync(endpoint.AuthorizationToken, parameters, "POST", headers, timeOut, "application/x-www-form-urlencoded", true, _certificate, 0, cancellationToken).ConfigureAwait(false);

            try
            {
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch { }

        }

        public override void GetAccessToken(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;
            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write";
            List<string> headers = new();

            headers.Add($"x-itau-correlationID: {Guid.NewGuid()}");
            headers.Add($"x-itau-flowID: {Guid.NewGuid()}");
            parameters += $"&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";


            string
                request = Utils.sendRequest(endpoint.AuthorizationToken, parameters, "POST", headers, timeOut, "application/x-www-form-urlencoded", true, _certificate);

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

                List<string> headers = new();

                headers.Add($"x-itau-apikey: {_credentials.clientId}");
                headers.Add($"x-itau-correlationID: {Guid.NewGuid()}");
                headers.Add($"x-itau-flowID: {Guid.NewGuid()}");
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));

                if (
                    ((PixPayload)_payload).infoAdicionais is null ||
                    (((PixPayload)_payload).infoAdicionais is not null && ((PixPayload)_payload).infoAdicionais.Count == 0)
                    )
                    ((PixPayload)_payload).infoAdicionais = null;

                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                string request = null;

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
                    request = await Utils.sendRequestAsync(endpoint.Pix + $"{tipoApi}/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, timeOut, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Model.Errors.Itau.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Itau.Errors>(ex.Message);

                    string error = String.Empty;
                    if (!string.IsNullOrEmpty(errors.title))
                        error += errors.title + Environment.NewLine;
                    if (!string.IsNullOrEmpty(errors.detail))
                        error += errors.detail + Environment.NewLine;
                    if (!string.IsNullOrEmpty(errors.details))
                        error += errors.details + Environment.NewLine;
                    int i = 1;
                    if (errors.violacoes is not null)
                        foreach (var item in errors.violacoes)
                        {
                            error += $"Erro {i}:" + Environment.NewLine;
                            error += $"\tPropriedade: {item.propriedade}{Environment.NewLine}";
                            error += $"\tRazao: {item.razao}{Environment.NewLine}";
                            error += $"\tValor: {item.valor}{Environment.NewLine}";
                            if (i < errors.violacoes.Count)
                                error += $"{Environment.NewLine}----------------{Environment.NewLine}";
                            i++;
                        }
                    throw new Exception(error);
                }

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

                headers.Add($"x-itau-apikey: {_credentials.clientId}");
                headers.Add($"x-itau-correlationID: {Guid.NewGuid()}");
                headers.Add($"x-itau-flowID: {Guid.NewGuid()}");
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = Utils.sendRequest(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, timeOut, "application/json", true, _certificate);
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
                List<string> headers = new();

                headers.Add($"x-itau-apikey: {_credentials.clientId}");
                headers.Add($"x-itau-correlationID: {Guid.NewGuid()}");
                headers.Add($"x-itau-flowID: {Guid.NewGuid()}");
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + txid, "", "GET", headers, timeOut, "", false, _certificate, 0, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(request))
                    request = await Utils.sendRequestAsync(endpoint.Pix + "cobv/" + txid, "", "GET", headers, timeOut, "", false, _certificate, 0, cancellationToken).ConfigureAwait(false);

                PixPayload cobranca = null;
                try
                {
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);
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

                    headers.Add($"x-itau-apikey: {_credentials.clientId}");
                    headers.Add($"x-itau-correlationID: {Guid.NewGuid()}");
                    headers.Add($"x-itau-flowID: {Guid.NewGuid()}");
                    headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                    int
                        paginaAtual = 0;

                    string request = null;

                    bool loop = true;

                    PixRecebidos cobranca = null;
                    while (loop)
                    {
                        await Task.Delay(100);
                        try
                        {
                            string queryString = string.Format
                            (
                                "txIdPresente=true&inicio={0}&fim={1}&paginacao.paginaAtual={2}&t={3}",
                                ((PixRecebidosPayload)_payload).inicio.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ((PixRecebidosPayload)_payload).fim.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                paginaAtual,
                                DateTime.Now.Ticks
                            );
                            request = await Utils.sendRequestAsync(endpoint.Pix + "pix?" + queryString, null, "GET", headers, timeOut, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);

                            cobranca = JsonConvert.DeserializeObject<PixRecebidos>(request);

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
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
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
        public override async Task<PixPayload> CancelarPixAsync()
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {

                List<string> headers = new()
                {
                    $"x-itau-apikey: {_credentials.clientId}",
                    $"x-itau-correlationID: {Guid.NewGuid()}",
                    $"x-itau-flowID: {Guid.NewGuid()}",
                    string.Format("Authorization: Bearer {0}", token.access_token)
                };
                ((PixPayload)_payload).status = "REMOVIDA_PELO_USUARIO_RECEBEDOR";
                ((PixPayload)_payload).calendario = null;
                ((PixPayload)_payload).devedor = null;
                ((PixPayload)_payload).valor = null;
                ((PixPayload)_payload).chave = null;
                ((PixPayload)_payload).solicitacaoPagador = null;
                if (
                    ((PixPayload)_payload).infoAdicionais is null ||
                    (((PixPayload)_payload).infoAdicionais is not null && ((PixPayload)_payload).infoAdicionais.Count == 0)
                    )
                    ((PixPayload)_payload).infoAdicionais = null;

                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                string request;
                try
                {
                    request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PATCH", headers, timeOut, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Model.Errors.Itau.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Itau.Errors>(ex.Message);

                    string error = String.Empty;
                    if (!string.IsNullOrEmpty(errors.title))
                        error += errors.title + Environment.NewLine;
                    if (!string.IsNullOrEmpty(errors.detail))
                        error += errors.detail + Environment.NewLine;
                    if (!string.IsNullOrEmpty(errors.details))
                        error += errors.details + Environment.NewLine;
                    int i = 1;
                    if (errors.violacoes is not null)
                        foreach (var item in errors.violacoes)
                        {
                            error += $"Erro {i}:" + Environment.NewLine;
                            error += $"\tPropriedade: {item.propriedade}{Environment.NewLine}";
                            error += $"\tRazao: {item.razao}{Environment.NewLine}";
                            error += $"\tValor: {item.valor}{Environment.NewLine}";
                            if (i < errors.violacoes.Count)
                                error += $"{Environment.NewLine}----------------{Environment.NewLine}";
                            i++;
                        }
                    throw new Exception(error);
                }

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
    }
}
