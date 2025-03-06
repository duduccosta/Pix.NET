using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PixNET.Services.Pix.Bancos
{
    public class BancoBrasil : Banco
    {
        public BancoBrasil(PixAmbiente? ambiente, byte[]? certificate = null, string? password = null)
        {
            if (certificate is not null && certificate.Length > 0)
                SetCertificateFile(certificate, password);

            string versao = _certificate is null ? "v1" : "v2";

            switch (versao)
            {
                case "v1":
                    {
                        endpoint = new Endpoint
                        {
                            AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://oauth.bb.com.br/oauth/token" : "https://oauth.sandbox.bb.com.br/oauth/token",
                            Pix = ambiente == PixAmbiente.Producao ? $"https://api.bb.com.br/pix/v1/" : $"https://api.sandbox.bb.com.br/pix/v1/"
                        };
                        break;
                    }
                case "v2":
                    {
                        endpoint = new Endpoint
                        {
                            AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://oauth.bb.com.br/oauth/token" : "https://oauth.sandbox.bb.com.br/oauth/token",
                            Pix = ambiente == PixAmbiente.Producao ? $"https://api-pix.bb.com.br/pix/v2/" : $"https://api-pix.hm.bb.com.br/pix/v2/"
                        };
                        break;
                    }
            }
        }

        public override async Task GetAccessTokenAsync(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;

            string parameters = $"grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write cobv.read cobv.write&gw-dev-app-key={_credentials.developerKey}";
            List<string> headers = new()
            {
                string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret))
            };
            headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");

            string
                request = await Utils.sendRequestAsync($"{endpoint.AuthorizationToken}?gw-dev-app-key={_credentials.developerKey}", parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate, 0, cancellationToken, SecurityProtocolType.Tls12).ConfigureAwait(false);

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

            string parameters = $"grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write&gw-dev-app-key={_credentials.developerKey}";
            List<string> headers = new()
            {
                string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret))
            };
            headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");

            parameters += $"client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}&gw-dev-app-key={_credentials.developerKey}";

            string
                request = Utils.sendRequest($"{endpoint.AuthorizationToken}?gw-dev-app-key={_credentials.developerKey}", parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate, 0, SecurityProtocolType.Tls12);

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
                List<string> headers = new()
                {
                    string.Format("Authorization: Bearer {0}", token.access_token),
                    $"X-Developer-Application-Key: {_credentials.developerKey}"
                };
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
                    request = await Utils.sendRequestAsync($"{endpoint.Pix}{tipoApi}/{((PixPayload)_payload).txid}?gw-dev-app-key={_credentials.developerKey}", parameters, "PUT", headers, 0, "application/json", true, _certificate, 0, cancellationToken, SecurityProtocolType.Tls12).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex.Message.IsJson())
                    {
                        Model.Errors.BancoBrasil.Errors? error = JsonConvert.DeserializeObject<Model.Errors.BancoBrasil.Errors>(ex.Message);
                        if (error is not null && error.erros is not null)
                        {
                            string errors = String.Join(Environment.NewLine, error.erros.Select(T => T.mensagem).ToArray());
                            throw new Exception(errors);
                        }
                        else
                        {
                            Model.Errors.BancoBrasil.Error? _error = JsonConvert.DeserializeObject<Model.Errors.BancoBrasil.Error>(ex.Message);
                            throw new Exception(_error.message);
                        }
                    }
                    else
                        throw;

                }
                PixPayload? cobranca = null;

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

                headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = null;
                try
                {
                    request = Utils.sendRequest(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate, 0, SecurityProtocolType.Tls12);
                }
                catch (Exception ex)
                {
                    Model.Errors.BancoBrasil.Errors error = JsonConvert.DeserializeObject<Model.Errors.BancoBrasil.Errors>(ex.Message);
                    string errors = String.Join(Environment.NewLine, error.erros.Select(T => T.mensagem).ToArray());
                    throw new Exception(errors);
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
        public override async Task<PixPayload> ConsultaCobrancaAsync(string txid)
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {
                List<string> headers = new()
                {
                    string.Format("Authorization: Bearer {0}", token.access_token)
                };
                headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");
                string request = await Utils.sendRequestAsync($"{endpoint.Pix}cob/{txid}", $"gw-dev-app-key={_credentials.developerKey}", "GET", headers, 0, "", false, _certificate, 0, cancellationToken, SecurityProtocolType.Tls12).ConfigureAwait(false);
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

                headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = null;
                try
                {
                    request = await Utils.sendRequestAsync($"{endpoint.Pix}cob/{((PixPayload)_payload).txid}?gw-dev-app-key={_credentials.developerKey}", parameters, "PATCH", headers, 0, "application/json", true, _certificate, 0, cancellationToken, SecurityProtocolType.Tls12).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Model.Errors.BancoBrasil.Errors error = JsonConvert.DeserializeObject<Model.Errors.BancoBrasil.Errors>(ex.Message);
                    string errors = String.Join(Environment.NewLine, error.erros.Select(T => T.mensagem).ToArray());
                    throw new Exception(errors);
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

                    List<string> headers = new()
                    {
                        string.Format("Authorization: Bearer {0}", token.access_token)
                    };
                    headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");
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
                                "gw-dev-app-key={3}&txIdPresente=true&inicio={0}&fim={1}&paginaAtual={2}&paginacao.paginaAtual={2}",
                                ((PixRecebidosPayload)_payload).inicio.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ((PixRecebidosPayload)_payload).fim.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                paginaAtual,
                                _credentials.developerKey
                            );
                            request = await Utils.sendRequestAsync($"{endpoint.Pix}pix", queryString, "GET", headers, 0, "application/json", false, _certificate, 0, cancellationToken, SecurityProtocolType.Tls12).ConfigureAwait(false);

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
