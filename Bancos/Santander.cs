using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
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

            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write";
            List<string> headers = new List<string>();

            headers.Add(string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret)));
            string
                request = await Utils.sendRequestAsync(endpoint.AuthorizationToken + "?grant_type=client_credentials", parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate);

            try
            {
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch { }

        }

        public override void GetAccessToken(bool force = false)
        {
            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write";
            List<string> headers = new List<string>();

            headers.Add($"X-Developer-Application-Key: {_credentials.developerKey}");
            headers.Add(string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret)));
            parameters += $"client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";


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
            await GetAccessTokenAsync();
            if (token != null)
            {

                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));

                if (
                    ((PixPayload)_payload).infoAdicionais == null ||
                    (((PixPayload)_payload).infoAdicionais != null && ((PixPayload)_payload).infoAdicionais.Count == 0)
                    )
                    ((PixPayload)_payload).infoAdicionais = null;

                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                string request = null;

                try
                {
                    request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate);
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
        public override PixPayload CreateCobranca()
        {
            GetAccessToken();
            if (token != null)
            {
                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
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

    }
}
