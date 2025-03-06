using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using System.Globalization;

#if WINDOWS
using Microsoft.Win32;
#endif
namespace PixNET
{
    public class Utils
    {
        public static string? sendRequest(string url, string parametros = null, string method = "POST", List<string> headers = null, int timeOut = 2000, string ContentType = null, bool throwException = false, X509Certificate2Collection certificate = null, int retryQtd = 0, SecurityProtocolType? SecurityProtocol = SecurityProtocolType.SystemDefault)
        {
            try
            {
                WebResponse response = null;
                int i = 0;

                i++;
                ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                ServicePointManager.Expect100Continue = false;
                FixTlsValidationSize();
                while (i < retryQtd + 1)
                {

                    String postData = parametros;

                    if (!String.IsNullOrEmpty(ContentType) && !ContentType.Contains("json"))
                        parametros = parametros + (!String.IsNullOrEmpty(parametros) ? @"&" : null) + @"cache=" + DateTime.Now.Ticks.ToString();

                    Boolean verificaConexao = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                    if (method == "GET" && !String.IsNullOrEmpty(postData))
                        url += "?" + postData;
                    else { }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UseDefaultCredentials = true;

                    if (certificate is not null)
                        request.ClientCertificates = certificate;
                    else { }

                    IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                    if (defaultProxy is not null)
                    {
                        defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                        request.Proxy = defaultProxy;
                    }
                    request.Timeout = System.Threading.Timeout.Infinite;
                    if (timeOut > 0)
                        request.Timeout = timeOut;
                    request.Method = method;
                    if (headers is not null)
                        foreach (string item in headers)
                            request.Headers.Add(item);
                    else { }

                    request.ServicePoint.Expect100Continue = false;
                    request.ProtocolVersion = HttpVersion.Version10;

                    switch (method)
                    {
                        case "POST":
                        case "PUT":
                        case "PATCH":
                            {

                                var data = Encoding.ASCII.GetBytes(postData);
                                if (String.IsNullOrEmpty(ContentType))
                                    request.ContentType = "application/x-www-form-urlencoded";
                                else
                                    request.ContentType = ContentType;

                                request.ContentLength = data.Length;
                                using (var stream = request.GetRequestStream())
                                    stream.Write(data, 0, data.Length);
                                break;
                            }
                    }

                    try
                    {
                        response = request.GetResponse() as HttpWebResponse;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (i == (retryQtd + 1))
                            throw;
                        Thread.Sleep(100);
                    }
                }
                var res = new StreamReader(response.GetResponseStream()).ReadToEnd().Trim();
                response.Dispose();
                return res;
            }
            catch (Exception e)
            {
                if (throwException)
                {
                    if (e is WebException && ((WebException)e).Response is not null)
                    {
                        var stream = ((WebException)e).Response.GetResponseStream();
                        var reader = new StreamReader(stream);
                        string error = reader.ReadToEnd().Trim();
                        throw new WebException(error, ((WebException)e).Status);
                    }
                    throw;
                }
                return "";
            }
        }

        public async static Task<string?> sendRequestAsync(string url, string? parametros = null, string method = "POST", List<string>? headers = null, int timeOut = 2000, string? ContentType = null, bool throwException = false, X509Certificate2Collection? certificate = null, int retryQtd = 0, CancellationToken? cancellationToken = null, SecurityProtocolType? SecurityProtocol = SecurityProtocolType.SystemDefault)
        {
            try
            {
                int i = 0;

                cancellationToken ??= CancellationToken.None;

                FixTlsValidationSize();

                ServicePointManager.ServerCertificateValidationCallback = SSLCallback;

                if (SecurityProtocol is not null)
                    System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)SecurityProtocol;

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.CheckCertificateRevocationList = false;

                WebResponse? response = null;
                while (i < retryQtd + 1)
                {
                    i++;


                    String? postData = parametros;

                    if (!String.IsNullOrEmpty(ContentType) && !ContentType.Contains("json"))
                        parametros = parametros + (!String.IsNullOrEmpty(parametros) ? @"&" : null) + @"cache=" + DateTime.Now.Ticks.ToString();

                    Boolean verificaConexao = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                    if (method == "GET" && !String.IsNullOrEmpty(postData))
                        url += "?" + postData;
                    else { }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UseDefaultCredentials = false;
                    if (certificate is not null)
                        request.ClientCertificates = certificate;
                    else { }

                    IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                    if (defaultProxy is not null)
                    {
                        defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                        request.Proxy = defaultProxy;
                    }
                    request.Timeout = System.Threading.Timeout.Infinite;
                    if (timeOut > 0)
                        request.Timeout = timeOut;
                    request.Method = method;
                    request.ServerCertificateValidationCallback = SSLCallback;
                    if (headers is not null)
                        foreach (string item in headers)
                            request.Headers.Add(item);
                    else { }

                    request.ServicePoint.Expect100Continue = false;
                    request.ProtocolVersion = HttpVersion.Version10;
                    switch (method)
                    {
                        case "POST":
                        case "PUT":
                        case "PATCH":
                            {

                                var data = Encoding.ASCII.GetBytes(postData);
                                if (String.IsNullOrEmpty(ContentType))
                                    request.ContentType = "application/x-www-form-urlencoded";
                                else
                                    request.ContentType = ContentType;

                                request.ContentLength = data.Length;
                                using var stream = await request.GetRequestStreamAsync().ConfigureAwait(false);
                                await stream.WriteAsync(data, 0, data.Length, (CancellationToken)cancellationToken).ConfigureAwait(false);
                                break;
                            }
                    }

                    try
                    {
                        response = await request.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (i == (retryQtd + 1))
                            throw;
                        await Task.Delay(100, (CancellationToken)cancellationToken).ConfigureAwait(false);
                    }
                }
                if (response is null)
                    return "";

                var _res = (await new StreamReader(response.GetResponseStream()).ReadToEndAsync().ConfigureAwait(false)).Trim();
                response.Dispose();
                return _res;
            }
            catch (Exception e)
            {
                if (throwException)
                {
                    if (e is WebException exception && exception.Response is not null)
                    {
                        var stream = exception.Response.GetResponseStream();
                        var reader = new StreamReader(stream);
                        string error = (await reader.ReadToEndAsync().ConfigureAwait(false)).Trim();
                        throw new WebException(error, exception.Status);
                    }
                    throw;
                }
                return "";
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string RemoverAcentos(string text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;
            string normalized = text.Normalize(NormalizationForm.FormKD);
            Encoding removal = Encoding.GetEncoding(Encoding.ASCII.CodePage,
                                                    new EncoderReplacementFallback(""),
                                                    new DecoderReplacementFallback(""));
            byte[] bytes = removal.GetBytes(normalized);
            return Encoding.ASCII.GetString(bytes);
        }

        public static void FixTlsValidationSize()
        {
            try
            {
#if WINDOWS
                var tlsValidationSize = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Messaging", "MessageLimitClient", "");
                if (tlsValidationSize is null || String.IsNullOrEmpty(tlsValidationSize as string) || (tlsValidationSize is not null && Convert.ToInt32(tlsValidationSize) < 4292608))
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Messaging", "MessageLimitClient", 4292608, RegistryValueKind.DWord);
#endif
            }
            catch { }
        }

        private static bool SSLCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
    public static class Extentions
    {
        public static bool IsJson(this string s)
        {
            try
            {
                JToken.Parse(s);
                return true;
            }
            catch (JsonReaderException ex)
            {
                Trace.WriteLine(ex);
                return false;
            }
        }

        public static DateTime ToLocalTimeWithoutZone(this DateTime date)
        {
            return DateTime.Parse(Convert.ToDateTime(date).ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss"), CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public static IEnumerable<DateTime> RangeTo(this DateTime from, DateTime to, Func<DateTime, DateTime> step = null)
        {
            if (step == null)
            {
                step = x => x.AddDays(1);
            }

            while (from < to)
            {
                yield return from;
                from = step(from);
            }
        }

        public static IEnumerable<DateTime> RangeFrom(this DateTime to, DateTime from, Func<DateTime, DateTime> step = null)
        {
            return from.RangeTo(to, step);
        }
    }

    public static class CpfCnpjUtils
    {
        public static bool IsValid(string cpfCnpj)
        {
            return IsCpf(cpfCnpj) || IsCnpj(cpfCnpj);
        }

        private static bool IsCpf(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            cpf = cpf.Trim().Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;

            for (int j = 0; j < 10; j++)
                if (j.ToString().PadLeft(11, char.Parse(j.ToString())) == cpf)
                    return false;

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }

        private static bool IsCnpj(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }
    }
}
