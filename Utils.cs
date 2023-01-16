using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixNET
{
    public class Utils
    {
        public static string sendRequest(string url, string parametros = null, string method = "POST", List<string> headers = null, int timeOut = 2000, string ContentType = null, bool throwException = false, X509Certificate2Collection certificate = null, int retryQtd = 0)
        {
            try
            {
                WebResponse response = null;
                int i = 0;

                while (i < retryQtd + 1)
                {
                    i++;
                    ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                    ServicePointManager.Expect100Continue = false;

                    String postData = parametros;

                    if (!String.IsNullOrEmpty(ContentType) && !ContentType.Contains("json"))
                        parametros = parametros + (!String.IsNullOrEmpty(parametros) ? @"&" : null) + @"cache=" + DateTime.Now.Ticks.ToString();

                    Boolean verificaConexao = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                    if (method == "GET" && !String.IsNullOrEmpty(postData))
                        url += "?" + postData;
                    else { }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UseDefaultCredentials = true;

                    if (certificate != null)
                        request.ClientCertificates = certificate;
                    else { }

                    IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                    if (defaultProxy != null)
                    {
                        defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                        request.Proxy = defaultProxy;
                    }
                    request.Timeout = System.Threading.Timeout.Infinite;
                    if (timeOut > 0)
                        request.Timeout = timeOut;
                    request.Method = method;
                    if (headers != null)
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
                            throw ex;
                        Thread.Sleep(100);
                    }
                }
                return (new StreamReader(response.GetResponseStream()).ReadToEnd()).Trim();
            }
            catch (Exception e)
            {
                if (throwException)
                {
                    if (e is WebException && ((WebException)e).Response != null)
                    {
                        var stream = ((WebException)e).Response.GetResponseStream();
                        var reader = new StreamReader(stream);
                        string error = reader.ReadToEnd().Trim();
                        throw new WebException(error, ((WebException)e).Status);
                    }
                    throw e;
                }
                return "";
            }
        }

        public async static Task<string> sendRequestAsync(string url, string parametros = null, string method = "POST", List<string> headers = null, int timeOut = 2000, string ContentType = null, bool throwException = false, X509Certificate2Collection certificate = null, int retryQtd = 0)
        {
            try
            {
                WebResponse response = null;
                int i = 0;

                while (i < retryQtd + 1)
                {
                    i++;
                    ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                    ServicePointManager.Expect100Continue = false;

                    String postData = parametros;

                    if (!String.IsNullOrEmpty(ContentType) && !ContentType.Contains("json"))
                        parametros = parametros + (!String.IsNullOrEmpty(parametros) ? @"&" : null) + @"cache=" + DateTime.Now.Ticks.ToString();

                    Boolean verificaConexao = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
                    if (method == "GET" && !String.IsNullOrEmpty(postData))
                        url += "?" + postData;
                    else { }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UseDefaultCredentials = true;

                    if (certificate != null)
                        request.ClientCertificates = certificate;
                    else { }

                    IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                    if (defaultProxy != null)
                    {
                        defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                        request.Proxy = defaultProxy;
                    }
                    request.Timeout = System.Threading.Timeout.Infinite;
                    if (timeOut > 0)
                        request.Timeout = timeOut;
                    request.Method = method;
                    if (headers != null)
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
                                using (var stream = await request.GetRequestStreamAsync())
                                    await stream.WriteAsync(data, 0, data.Length);
                                break;
                            }
                    }
                  
                    try
                    {
                        response = await request.GetResponseAsync() as HttpWebResponse;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (i == (retryQtd + 1))
                            throw ex;
                        Thread.Sleep(100);
                    }
                }
                return (await new StreamReader(response.GetResponseStream()).ReadToEndAsync()).Trim();
            }
            catch (Exception e)
            {
                if (throwException)
                {
                    if (e is WebException && ((WebException)e).Response != null)
                    {
                        var stream = ((WebException)e).Response.GetResponseStream();
                        var reader = new StreamReader(stream);
                        string error = (await reader.ReadToEndAsync()).Trim();
                        throw new WebException(error, ((WebException)e).Status);
                    }
                    throw e;
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
    }
}
