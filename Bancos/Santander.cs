using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PixNET.Services.Pix.Bancos
{
    public class Santander : Banco
    {
        public Santander(PixAmbiente? ambiente)
        {
            endpoint = new Endpoint
            {
                AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://trust-pix.santander.com.br/oauth/token" : "https://pix.santander.com.br/sandbox/oauth/token",
                Pix = ambiente == PixAmbiente.Producao ? "https://trust-pix.santander.com.br/api/v1/" : "https://pix.santander.com.br/api/v1/sandbox/"
            };
        }

        public override async Task GetAccessTokenAsync(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;

            string parameters = $"grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write cobv.read cobv.write&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";
            List<string> headers =
            [
                string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret)),
            ];
            string
                request = await Utils.sendRequestAsync(endpoint.AuthorizationToken + "?grant_type=client_credentials", parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate, 0, cancellationToken).ConfigureAwait(false);

            try
            {
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch { }

        }

        public override void GetAccessToken(bool force = false)
        {
            string parameters = $"grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";
            List<string> headers =
            [
                string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret)),
            ];
            string
                request = Utils.sendRequest(endpoint.AuthorizationToken + "?grant_type=client_credentials", parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate);

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

                List<string> headers = [string.Format("Authorization: Bearer {0}", token.access_token)];
                bool isPixCobranca = false;

                if (_payload is PixPayload)
                {
                    if (
                        ((PixPayload)_payload).infoAdicionais is null ||
                        (((PixPayload)_payload).infoAdicionais is not null && ((PixPayload)_payload).infoAdicionais.Count == 0)
                        )
                        ((PixPayload)_payload).infoAdicionais = null;

                    if (
                       ((PixPayload)_payload).valor is not null &&
                       (
                           ((PixPayload)_payload).valor.multa is not null ||
                           ((PixPayload)_payload).valor.juros is not null ||
                           ((PixPayload)_payload).valor.desconto is not null
                       )
                     )
                        isPixCobranca = true;
                }

                try
                {
                    var prev = ((PixPayload)_payload).solicitacaoPagador ?? "";
                    ((PixPayload)_payload).solicitacaoPagador = $"{prev}#txid;{((PixPayload)_payload).txid}";
                }
                catch { }

                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                string request = null;
                var tipoApi = "cob";
                if (isPixCobranca)
                    tipoApi = "cobv";
                try
                {
                    request = await Utils.sendRequestAsync(endpoint.Pix + $"{tipoApi}/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Model.Errors.Santander.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Santander.Errors>(ex.Message);

                    string error = String.Empty;
                    if (!String.IsNullOrEmpty(errors.title))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.title;
                    if (!String.IsNullOrEmpty(errors.detail))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.detail;
                    if (!String.IsNullOrEmpty(errors.details))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.details;
                    int i = 1;

                    if (errors.violacoes is not null)
                    {
                        error = error.Length > 0 ? Environment.NewLine : "";

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
        public override async Task<PixPayload> CancelarPixAsync()
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {

                List<string> headers = [string.Format("Authorization: Bearer {0}", token.access_token)];
                ((PixPayload)_payload).status = "REMOVIDA_PELO_USUARIO_RECEBEDOR";

                if (
                    ((PixPayload)_payload).infoAdicionais is null ||
                    (((PixPayload)_payload).infoAdicionais is not null && ((PixPayload)_payload).infoAdicionais.Count == 0)
                    )
                    ((PixPayload)_payload).infoAdicionais = null;

                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                string? request = null;

                try
                {
                    request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PATCH", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Model.Errors.Santander.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Santander.Errors>(ex.Message);

                    string error = String.Empty;
                    if (!String.IsNullOrEmpty(errors.title))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.title;
                    if (!String.IsNullOrEmpty(errors.detail))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.detail;
                    int i = 1;

                    if (errors.violacoes is not null)
                    {
                        if (!string.IsNullOrEmpty(errors.title))
                            error += errors.title + Environment.NewLine;
                        if (!string.IsNullOrEmpty(errors.detail))
                            error += errors.detail + Environment.NewLine;
                        if (!string.IsNullOrEmpty(errors.details))
                            error += errors.details + Environment.NewLine;
                        error = error.Length > 0 ? Environment.NewLine : "";

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
                    }

                    throw new Exception(error);
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
                List<string> headers = [string.Format("Authorization: Bearer {0}", token.access_token)];
                string request = null;
                try
                {
                    request = Utils.sendRequest(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate);
                }
                catch (Exception ex)
                {
                    Model.Errors.Itau.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Itau.Errors>(ex.Message);

                    string error = String.Empty;
                    error += errors.title + Environment.NewLine;
                    error += errors.detail + Environment.NewLine;
                    int i = 1;
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

        public override async Task<List<Model.Pix>> ConsultaPixRecebidosAsync()
        {
            List<Model.Pix> listaPix = [];

            try
            {
                await GetAccessTokenAsync().ConfigureAwait(false);
                if (token is not null)
                {
                    string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    List<string> headers = [string.Format("Authorization: Bearer {0}", token.access_token)];
                    int
                        paginaAtual = 0;

                    string request = null;

                    

                    PixRecebidos cobranca = null;

                    var dates = ((PixRecebidosPayload)_payload).inicio.RangeTo(((PixRecebidosPayload)_payload).fim).ToArray();
                    for (int i = 0; i < dates.Length; i++)
                    {
                        var data = dates[i];
                        DateTime
                            dataInicial = data.Date,
                            dataFinal = Convert.ToDateTime(data.Date.ToString("yyyy-MM-ddT23:59:59"));
                        bool loop = true;
                        while (loop)
                        {
                            await Task.Delay(100);
                            try
                            {
                                string queryString = string.Format
                                (
                                    "inicio={0}&fim={1}&paginaAtual={2}&paginacao.paginaAtual={2}",
                                    dataInicial.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    dataFinal.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    paginaAtual
                                );
                                request = await Utils.sendRequestAsync(endpoint.Pix.Replace("/api/", "/payment-order/") + "pixrecebidos?" + queryString, null, "GET", headers, timeOut, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);

                                if (!string.IsNullOrEmpty(request))
                                {
                                    cobranca = JsonConvert.DeserializeObject<PixRecebidos>(request);

                                    cobranca.pix = cobranca.pix?.Select(x =>
                                    {

                                        var txId = x.txid;
                                        var infoPagador = x.infoPagador;
                                        if(string.IsNullOrEmpty(x.txid) && infoPagador is not null && infoPagador.Contains("#txid;"))
                                        {

                                            txId = x.infoPagador?.Split("#txid;", StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault()?.Trim();
                                            infoPagador = x.infoPagador?.Split("#txid;", StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault()?.Trim();
                                        }
                                        return new Model.Pix()
                                        {
                                            endToEndId = x.endToEndId,
                                            devolucoes = x.devolucoes,
                                            horario = x.horario,
                                            pagador = x.pagador,
                                            valor = x.valor,
                                            infoPagador = infoPagador,
                                            txid = txId
                                        };
                                    })?.ToList();

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
                            }
                            catch
                            {
                                loop = false;
                                break;
                            }
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
                    var er = JsonConvert.DeserializeObject<PixError>(ex.Message);
                    if (er is not null)
                    {
                        if (er.erros.Find(T => T.codigo == "4769515") is not null)
                            return listaPix;
                    }
                }
                catch { }
                throw;
            }
        }

    }
}
