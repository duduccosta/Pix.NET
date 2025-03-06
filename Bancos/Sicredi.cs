using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PixNET.Services.Pix.Bancos
{
    public class Sicredi : Banco
    {
        public Sicredi(PixAmbiente? ambiente)
        {
            endpoint = new Endpoint
            {
                AuthorizationToken = ambiente == PixAmbiente.Producao ? "https://api-pix.sicredi.com.br/oauth/token" : "https://api-pix-h.sicredi.com.br/oauth/token",
                Pix = ambiente == PixAmbiente.Producao ? "https://api-pix.sicredi.com.br/api/v2/" : "https://api-pix-h.sicredi.com.br/api/v2"
            };
        }

        public override async Task GetAccessTokenAsync(bool force = false)
        {
            if (_credentials is null || String.IsNullOrEmpty(_credentials?.clientId) || String.IsNullOrEmpty(_credentials?.clientSecret))
                return;

            bool isPixCobranca = false;
            if (_payload is PixPayload)
                if (
                    ((PixPayload)_payload).valor is not null &&
                    (
                        ((PixPayload)_payload).valor.multa is not null ||
                        ((PixPayload)_payload).valor.juros is not null ||
                        ((PixPayload)_payload).valor.desconto is not null
                    )
                  )
                    isPixCobranca = true;

            string parameters = "grant_type=client_credentials&scope=cob.write cob.read pix.read webhook.read webhook.write pix.read pix.write";
            if (isPixCobranca)
                parameters += " cobv.write cobv.read";

            List<string> headers = [];

            //headers.Add(string.Format("client_id: {0}", _credentials.clientId));
            var contentBase = $"{_credentials.clientId}:{_credentials.clientSecret}";
            headers.Add($"Authorization: Basic {Utils.Base64Encode(contentBase)}");

            string
                request = await Utils.sendRequestAsync(endpoint.AuthorizationToken, parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate, 0, cancellationToken).ConfigureAwait(false);

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


            bool isPixCobranca = false;
            if (_payload is PixPayload)
                if (
                    ((PixPayload)_payload).valor is not null &&
                    (
                        ((PixPayload)_payload).valor.multa is not null ||
                        ((PixPayload)_payload).valor.juros is not null ||
                        ((PixPayload)_payload).valor.desconto is not null
                    )
                  )
                    isPixCobranca = true;

            string parameters = "grant_type=client_credentials&scope=cob.write cob.read pix.read webhook.read webhook.write pix.read pix.write";
            if (isPixCobranca)
                parameters += " cobv.write cobv.read";

            List<string> headers = [];

            //headers.Add(string.Format("client_id: {0}", _credentials.clientId));
            var contentBase = $"{_credentials.clientId}:{_credentials.clientSecret}";
            headers.Add($"Authorization: Basic {Utils.Base64Encode(contentBase)}");

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
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd"
                });
                List<string> headers =
                [
                    string.Format("Authorization: Bearer {0}", token.access_token),
                    string.Format("client_id: {0}", _credentials.clientId)
                ];
                string? request = null;
                PixPayload? cobranca = null;

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
                    //if (String.IsNullOrEmpty(cobranca.pixCopiaECola))
                    //    cobranca.pixCopiaECola = GerarQrCode(cobranca);

                    if (cobranca.calendario is not null && cobranca.calendario.criacao is not null)
                        cobranca.calendario.criacao = TimeZoneInfo.ConvertTime(Convert.ToDateTime(cobranca.calendario.criacao), TimeZoneInfo.Local);

                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);
                }
                catch (Exception ex)
                {
                    Model.Errors.Sicredi.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Sicredi.Errors>(ex.Message);

                    string error = String.Empty;
                    if (!String.IsNullOrEmpty(errors.title))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.title;
                    if (!String.IsNullOrEmpty(errors.detail))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.detail;
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
                List<string> headers =
                [
                    string.Format("Authorization: Bearer {0}", token.access_token),
                    string.Format("client_id: {0}", _credentials.clientId),
                ];
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PATCH", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);
                PixPayload cobranca = null;

                try
                {
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);

                    if (cobranca.calendario is not null && cobranca.calendario.criacao is not null)
                        cobranca.calendario.criacao = TimeZoneInfo.ConvertTime(Convert.ToDateTime(cobranca.calendario.criacao), TimeZoneInfo.Local);
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
                List<string> headers =
                [
                    string.Format("Authorization: Bearer {0}", token.access_token),
                    string.Format("client_id: {0}", _credentials.clientId)
                ];
                string? request = null;
                PixPayload? cobranca = null;

                try
                {
                    request = Utils.sendRequest(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate, 0);

                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    if (String.IsNullOrEmpty(cobranca.pixCopiaECola))
                        cobranca.pixCopiaECola = GerarQrCode(cobranca);

                    cobranca.textoImagemQRcode = cobranca.pixCopiaECola;

                    if (cobranca.calendario is not null && cobranca.calendario.criacao is not null)
                        cobranca.calendario.criacao = TimeZoneInfo.ConvertTime(Convert.ToDateTime(cobranca.calendario.criacao), TimeZoneInfo.Local);
                }
                catch (Exception ex)
                {
                    Model.Errors.Sicredi.Errors errors = JsonConvert.DeserializeObject<Model.Errors.Sicredi.Errors>(ex.Message);

                    string error = String.Empty;
                    if (!String.IsNullOrEmpty(errors.title))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.title;
                    if (!String.IsNullOrEmpty(errors.detail))
                        error = (error.Length > 0 ? Environment.NewLine : "") + errors.detail;
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

                token = null;
                request = null;
                return cobranca;

            }
            return null;
        }
        public override async Task<PixPayload?> ConsultaCobrancaAsync(string txid)
        {
            await GetAccessTokenAsync().ConfigureAwait(false);
            if (token is not null)
            {
                List<string> headers =
                [
                    string.Format("client_id: {0}", _credentials.clientId),
                    string.Format("Authorization: Bearer {0}", token.access_token),
                ];
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + txid, "", "GET", headers, 0, "", false, _certificate, 0, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(request))
                    request = await Utils.sendRequestAsync(endpoint.Pix + "cobv/" + txid, "", "GET", headers, 0, "", false, _certificate, 0, cancellationToken).ConfigureAwait(false);

                PixPayload cobranca = null;
                try
                {
                    cobranca = JsonConvert.DeserializeObject<PixPayload>(request);
                    cobranca.textoImagemQRcode = GerarQrCode(cobranca);

                    if (cobranca.calendario is not null && cobranca.calendario.criacao is not null)
                        cobranca.calendario.criacao = TimeZoneInfo.ConvertTime(Convert.ToDateTime(cobranca.calendario.criacao), TimeZoneInfo.Local);
                }
                catch { }
                return cobranca;

            }
            return null;
        }
        public override async Task<List<Model.Pix>?> ConsultaPixRecebidosAsync()
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

                    List<string> headers =
                    [
                        string.Format("client_id: {0}", _credentials.clientId),
                        string.Format("Authorization: Bearer {0}", token.access_token),
                    ];
                    int
                        paginaAtual = 0;

                    string request = null;

                    bool loop = true;

                    CobsRecebidos? cobranca = null;
                    while (loop)
                    {
                        await Task.Delay(100, (CancellationToken)cancellationToken);

                        try
                        {
                            string queryString = string.Format
                            (
                                "txIdPresente=true&inicio={0}&fim={1}&paginacao.paginaAtual={2}&status=CONCLUIDA",
                                ((PixRecebidosPayload)_payload).inicio.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ((PixRecebidosPayload)_payload).fim.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                paginaAtual
                            );
                            request = await Utils.sendRequestAsync(endpoint.Pix + "cob?" + queryString, null, "GET", headers, 0, "application/json", true, _certificate, 0, cancellationToken).ConfigureAwait(false);

                            cobranca = JsonConvert.DeserializeObject<CobsRecebidos>(request);
                            if (hasTxId)
                                listaPix.AddRange(cobranca.cobs.SelectMany(T => T.pix.Where(x => !string.IsNullOrEmpty(x.txid)).Select(x =>
                                {
                                    return new Model.Pix()
                                    {
                                        endToEndId = x.endToEndId,
                                        horario = x.horario,
                                        infoPagador = x.infoPagador,
                                        txid = x.txid,
                                        valor = Convert.ToDecimal(x.valor.Replace(".", "")) / 100,
                                        pagador = x.pagador
                                    };
                                })));
                            else
                                listaPix.AddRange(cobranca.cobs.SelectMany(T => T.pix.Select(x =>
                                {
                                    return new Model.Pix()
                                    {
                                        endToEndId = x.endToEndId,
                                        horario = x.horario,
                                        infoPagador = x.infoPagador,
                                        txid = x.txid,
                                        valor = Convert.ToDecimal(x.valor),
                                        pagador = x.pagador
                                    };
                                })));

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

                    listaPix = listaPix.Select(x =>
                    {
                        x.horario = TimeZoneInfo.ConvertTime(x.horario, TimeZoneInfo.Local);
                        return x;
                    }).ToList();
                    return listaPix;

                }
                return null;
            }
            catch (WebException ex)
            {
                try
                {
                    PixError? er = JsonConvert.DeserializeObject<PixError>(ex.Message);
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
