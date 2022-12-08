using Newtonsoft.Json;
using PixNET.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PixNET.Services.Pix.Bancos
{
    public class Banco : IBanco
    {

        internal object _payload = null;
        internal Credentials _credentials = null;
        internal Endpoint endpoint = null;
        internal ProvedorToken _psp;
        internal AccessToken token = null;
        internal X509Certificate2Collection _certificate = null;
        internal string nomeRazaoSocial = null;
        internal string cidade = null;
        public virtual async Task<PixPayload> ConsultaCobrancaAsync(string txid)
        {
            await GetAccessTokenAsync();
            if (token != null)
            {
                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + txid, "", "GET", headers, 0, "", false, _certificate);
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
        public virtual PixPayload ConsultaCobranca(string txid)
        {
            GetAccessToken();
            if (token != null)
            {
                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = Utils.sendRequest(endpoint.Pix + "cob/" + txid, "", "GET", headers, 0, "", false, _certificate);
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
        public virtual async Task<List<Model.Pix>> ConsultaPixRecebidosAsync()
        {
            List<Model.Pix> listaPix = new List<Model.Pix>();

            try
            {
                await GetAccessTokenAsync();
                if (token != null)
                {
                    string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    List<string> headers = new List<string>();
                    headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                    int
                        paginaAtual = 0;

                    string request = null;

                    bool loop = true;

                    PixRecebidos cobranca = null;
                    while (loop)
                    {
                        string queryString = string.Format
                            (
                                "inicio={0}&fim={1}&paginaAtual={2}",
                                ((PixRecebidosPayload)_payload).inicio.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ((PixRecebidosPayload)_payload).fim.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                paginaAtual
                            );
                        request = await Utils.sendRequestAsync(endpoint.Pix + "pix?" + queryString, null, "GET", headers, 0, "application/json", false, _certificate);
                        try
                        {
                            cobranca = JsonConvert.DeserializeObject<PixRecebidos>(request);
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
                    if (er != null)
                    {
                        if (er.erros.Find(T => T.codigo == "4769515") != null)
                            return listaPix;
                    }
                }
                catch { }
                throw ex;
            }
        }

        public virtual List<Model.Pix> ConsultaPixRecebidos()
        {
            List<Model.Pix> listaPix = new List<Model.Pix>();

            try
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
                    int
                        paginaAtual = 0;

                    string request = null;

                    bool loop = true;

                    PixRecebidos cobranca = null;
                    while (loop)
                    {
                        string queryString = string.Format
                            (
                                "txIdPresente=true&inicio={0}&fim={1}&paginaAtual={2}",
                                ((PixRecebidosPayload)_payload).inicio.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ((PixRecebidosPayload)_payload).fim.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                paginaAtual
                            );
                        request = Utils.sendRequest(endpoint.Pix + "pix?" + queryString, null, "GET", headers, 0, "application/json", false, _certificate);
                        try
                        {
                            cobranca = JsonConvert.DeserializeObject<PixRecebidos>(request);
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
                    if (er != null)
                    {
                        if (er.erros.Find(T => T.codigo == "4769515") != null)
                            return listaPix;
                    }
                }
                catch { }
                throw ex;
            }
        }
        public virtual PixPayload CreateCobranca()
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
        public virtual async Task<PixPayload> CreateCobrancaAsync()
        {
            await GetAccessTokenAsync();
            if (token != null)
            {
                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PUT", headers, 0, "application/json", true, _certificate);
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
        public virtual async Task<PixDevolucao> DevolverPixAsync()
        {
            await GetAccessTokenAsync();
            if (token != null)
            {
                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = await Utils.sendRequestAsync(endpoint.Pix + $"pix/{((PixDevolucaoPayload)_payload).e2eid}/devolucao/{((PixDevolucaoPayload)_payload).id}", parameters, "PUT", headers, 0, "application/json", true, _certificate);
                PixDevolucao devolucao = null;

                try
                {
                    devolucao = JsonConvert.DeserializeObject<PixDevolucao>(request);
                }
                catch { }

                token = null;
                request = null;
                return devolucao;

            }
            return null;
        }
        public virtual async Task<PixPayload> CancelarPixAsync()
        {
            //REMOVIDA_PELO_USUARIO_RECEBEDOR
            await GetAccessTokenAsync();
            if (token != null)
            {
                ((PixPayload)_payload).status = "REMOVIDA_PELO_USUARIO_RECEBEDOR";
                string parameters = JsonConvert.SerializeObject(_payload, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                List<string> headers = new List<string>();
                headers.Add(string.Format("Authorization: Bearer {0}", token.access_token));
                string request = await Utils.sendRequestAsync(endpoint.Pix + "cob/" + ((PixPayload)_payload).txid, parameters, "PATCH", headers, 0, "application/json", true, _certificate);
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
        public virtual void GetAccessToken(bool force = false)
        {
            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write";
            List<string> headers = new List<string>();

            headers.Add(string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret)));
            parameters += $"&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";

            string
                request = Utils.sendRequest(endpoint.AuthorizationToken, parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate);

            try
            {
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch { }
        }
        public virtual async Task GetAccessTokenAsync(bool force = false)
        {
            string parameters = "grant_type=client_credentials&scope=cob.read cob.write pix.read pix.write";
            List<string> headers = new List<string>();
            headers.Add(string.Format("Authorization: Basic {0}", Utils.Base64Encode(_credentials.clientId + ":" + _credentials.clientSecret)));
            parameters += $"&client_id={_credentials.clientId}&client_secret={_credentials.clientSecret}";


            string
                request = await Utils.sendRequestAsync(endpoint.AuthorizationToken, parameters, "POST", headers, 0, "application/x-www-form-urlencoded", true, _certificate);

            try
            {
                token = JsonConvert.DeserializeObject<AccessToken>(request);
                token.lastTokenTime = DateTime.Now;
            }
            catch { }
        }
        public string GerarQrCode(PixPayload cobranca)
        {
            StringBuilder payloadQrCode = new StringBuilder();
            payloadQrCode.Append("000201");
            payloadQrCode.Append("010212");
            payloadQrCode.Append($"26{22 + cobranca.location.Length}");
            payloadQrCode.Append("0014br.gov.bcb.pix");
            payloadQrCode.Append($"25{cobranca.location.Length.ToString().PadLeft(2, '0')}{cobranca.location}");
            payloadQrCode.Append("52040000");
            payloadQrCode.Append("5303986");
            payloadQrCode.Append($"54{cobranca.valor.original.Length.ToString().PadLeft(2, '0')}{cobranca.valor.original}");
            payloadQrCode.Append("5802BR");

            if (!String.IsNullOrEmpty(nomeRazaoSocial))
                payloadQrCode.Append($"59{nomeRazaoSocial.Length.ToString().PadLeft(2, '0')}{nomeRazaoSocial}");
            else
                payloadQrCode.Append($"5900");

            if (!String.IsNullOrEmpty(cidade))
                payloadQrCode.Append($"60{cidade.Length.ToString().PadLeft(2, '0')}{cidade}");
            else
                payloadQrCode.Append($"6000");

            payloadQrCode.Append("6207");
            payloadQrCode.Append("0503***");
            payloadQrCode.Append("6304");
            var bytes = Encoding.UTF8.GetBytes(payloadQrCode.ToString().Trim());
            ushort crc = new Crc16CcittKermit().ComputeChecksum(bytes);
            payloadQrCode.Append(crc.ToString("x4").ToUpper());
            return payloadQrCode.ToString();
        }
        public void SetCertificateFile(string certificateFile, string password)
        {
            try
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                certificates.Import(certificateFile, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                _certificate = certificates;
            }
            catch
            {
                _certificate = null;
            }
        }
        public void SetCertificateFile(byte[] certificate, string password)
        {
            try
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                certificates.Import(certificate, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                _certificate = certificates;
            }
            catch
            {
                _certificate = null;
            }
        }
        public void SetCidade(string cidade)
        {
            var _cidade = cidade;
            if (!String.IsNullOrEmpty(_cidade))
            {
                if (_cidade.Length > 15)
                    _cidade = _cidade.Substring(0, 15);
                this.cidade = Utils.RemoverAcentos(_cidade).ToUpper();
            }
        }
        public void SetCredentials(Credentials credentials)
        {
            _credentials = credentials;
        }
        public void SetNomeRazaoSocial(string nome)
        {
            var _nome = nome;
            if (!String.IsNullOrEmpty(_nome))
            {
                if (_nome.Length > 25)
                    _nome = _nome.Substring(0, 25);
                nomeRazaoSocial = Utils.RemoverAcentos(_nome).ToUpper();
            }
        }
        public void SetPayload(BasePayload payload)
        {
            _payload = payload;
        }

    }
}
