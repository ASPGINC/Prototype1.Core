using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Net;
using System.IO;
using System.Web;

namespace Prototype1.Foundation.Web
{
    public static class HttpRequestExecutor
    {
        public static HttpRequestResult ExecuteGetRequest(out string response, string url, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecuteGetRequest(out response, url, true, new Dictionary<string, string>(), timeout, securityProtocolType);
        }

        public static HttpRequestResult ExecuteGetRequest(out string response, out Dictionary<string, string> responseHeaders, string url, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecuteGetRequest(out response, out responseHeaders, url, true, new Dictionary<string, string>(), timeout, securityProtocolType);
        }

        public static HttpRequestResult ExecuteGetRequest(out string response, string url, bool disableCertificateValidation,
            Dictionary<string, string> customHeaders, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            Dictionary<string, string> responseHeaders;
            return ExecuteGetRequest(out response, out responseHeaders, url, disableCertificateValidation, customHeaders,
                timeout, securityProtocolType);
        }

        public static HttpRequestResult ExecuteGetRequest(out string response, out Dictionary<string, string> responseHeaders, string url, bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecuteGetRequest(out response, out responseHeaders, url, disableCertificateValidation, customHeaders, null, timeout, securityProtocolType);
        }

        public static HttpRequestResult ExecuteGetRequest(out string response, out Dictionary<string, string> responseHeaders, string url, bool disableCertificateValidation, Dictionary<string, string> customHeaders, HttpCookieCollection cookies, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecuteWebRequest(out response, out responseHeaders, url, "", disableCertificateValidation,
                customHeaders, cookies, "GET", timeout, securityProtocolType: securityProtocolType);
        }

        public static HttpRequestResult ExecutePostRequest(out string response, string url, string postContent, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecutePostRequest(out response, url, postContent, true, new Dictionary<string, string>(), timeout, securityProtocolType);
        }

        public static HttpRequestResult ExecutePostRequest(out string response, out Dictionary<string, string> responseHeaders, string url, string postContent, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecutePostRequest(out response, out responseHeaders, url, postContent, true, new Dictionary<string, string>(), timeout, securityProtocolType);
        }

        public static HttpRequestResult ExecutePostRequest(out string response, string url, string postContent, bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            Dictionary<string, string> responseHeaders;
            return ExecuteWebRequest(out response, out responseHeaders, url, postContent, disableCertificateValidation, customHeaders, null, "POST", timeout, securityProtocolType: securityProtocolType);
        }

        public static HttpRequestResult ExecutePostRequest(out string response, out Dictionary<string, string> responseHeaders, string url, string postContent,
            bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            return ExecuteWebRequest(out response, out responseHeaders, url, postContent, disableCertificateValidation, customHeaders, null, "POST", timeout, securityProtocolType: securityProtocolType);
        }

        public static HttpRequestResult ExecutePutRequest(out string response, string url, string postContent, bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            Dictionary<string, string> responseHeaders;
            return ExecuteWebRequest(out response, out responseHeaders, url, postContent, disableCertificateValidation, customHeaders, null, "PUT", timeout, securityProtocolType: securityProtocolType);
        }

        public static HttpRequestResult ExecutePutRequest(out string response, out Dictionary<string, string> responseHeaders, string url, string postContent, bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, string requestMethod = "POST", SecurityProtocolType? securityProtocolType = null)
        {
            return ExecuteWebRequest(out response, out responseHeaders, url, postContent, disableCertificateValidation, customHeaders, null, "PUT", timeout, securityProtocolType: securityProtocolType);
        }

        public static HttpRequestResult ExecuteJsonWebRequest<T>(out string response, string url, T postContent,
            bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, SecurityProtocolType? securityProtocolType = null)
        {
            Dictionary<string, string> responseHeaders;
            return ExecuteJsonWebRequest(out response, out responseHeaders, url, postContent, disableCertificateValidation,
                customHeaders, timeout, securityProtocolType: securityProtocolType);
        }

        public static HttpRequestResult ExecuteJsonWebRequest<T>(out string response, out Dictionary<string, string> responseHeaders, string url, T postContent, bool disableCertificateValidation, Dictionary<string, string> customHeaders, TimeSpan? timeout = null, string requestMethod = "POST", SecurityProtocolType? securityProtocolType = null)
        {
            var postData = postContent.ToJson();

            var existingContentTypeKey =
                customHeaders.Keys.FirstOrDefault(
                    k => k.Equals("content-type", StringComparison.CurrentCultureIgnoreCase));
            if (existingContentTypeKey != null && !existingContentTypeKey.IsNullOrEmpty())
                customHeaders[existingContentTypeKey] = "application/json";
            else
                customHeaders.Add("content-type", "application/json");

            return ExecuteWebRequest(out response, out responseHeaders, url, postData, disableCertificateValidation, customHeaders, null, requestMethod, timeout, securityProtocolType: securityProtocolType);
        }
        
        public static HttpRequestResult ExecuteWebRequest(out string response, out Dictionary<string, string> responseHeaders, string url, string postContent, bool disableCertificateValidation, Dictionary<string, string> customHeaders, HttpCookieCollection cookies, string requestMethod = "POST", TimeSpan? timeout = null, X509Certificate2 certificate = null, SecurityProtocolType? securityProtocolType = null)
        {
            responseHeaders = new Dictionary<string, string>();

            var encoding = new UTF8Encoding();
            var data = encoding.GetBytes(postContent);

            if (disableCertificateValidation)
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            ServicePointManager.Expect100Continue = false;
            if (securityProtocolType.HasValue)
                ServicePointManager.SecurityProtocol = securityProtocolType.Value;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = requestMethod;
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.ContentLength = data.Length;
            httpWebRequest.UseDefaultCredentials = true;

            if(certificate != null)
                httpWebRequest.ClientCertificates.Add(certificate);

            if (timeout.HasValue)
                httpWebRequest.Timeout = httpWebRequest.ReadWriteTimeout = (int) timeout.Value.TotalMilliseconds;

            var userInfo = httpWebRequest.Address.UserInfo;
            if (!string.IsNullOrEmpty(userInfo) && userInfo.Contains(':'))
            {
                httpWebRequest.Credentials = new NetworkCredential(userInfo.Split(':').First(), userInfo.Split(':').Last());
                httpWebRequest.PreAuthenticate = true;
            }

            if (cookies != null)
            {
                httpWebRequest.CookieContainer = new CookieContainer();
                for (var i = 0; i < cookies.Count; i++)
                {
                    var currentCookie = cookies.Get(i);
                    if (currentCookie == null) continue;

                    var cookie = new Cookie
                    {
                        Domain = httpWebRequest.RequestUri.Host,
                        Expires = currentCookie.Expires,
                        Name = currentCookie.Name,
                        Path = currentCookie.Path,
                        Secure = currentCookie.Secure,
                        Value = currentCookie.Value
                    };

                    httpWebRequest.CookieContainer.Add(cookie);
                }
            }

            httpWebRequest.Accept = "*/*";
            foreach (var key in customHeaders.Keys)
                if (key.Equals("content-type", StringComparison.InvariantCultureIgnoreCase))
                    httpWebRequest.ContentType = customHeaders[key];
                else if (key.Equals("transfer-encoding", StringComparison.InvariantCultureIgnoreCase))
                {
                    httpWebRequest.SendChunked = true;
                    httpWebRequest.TransferEncoding = customHeaders[key];
                }
                else if (key.Equals("user-agent", StringComparison.InvariantCultureIgnoreCase))
                    httpWebRequest.UserAgent = customHeaders[key];
                else if (key.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
                    httpWebRequest.Accept = customHeaders[key];
                else if (key.Equals("accept-encoding", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var decompress in customHeaders[key].Split(',').Select(s => s.Trim().ToLower()))
                        if (decompress == "deflate")
                            httpWebRequest.AutomaticDecompression |= DecompressionMethods.Deflate;
                        else if (decompress == "gzip")
                            httpWebRequest.AutomaticDecompression |= DecompressionMethods.GZip;
                }
                else
                    httpWebRequest.Headers.Add(key, customHeaders[key]);

            try
            {
                if (data.Length > 0)
                    using (var newStream = httpWebRequest.GetRequestStream())
                    {
                        newStream.Write(data, 0, data.Length);
                    }

                using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (var sr = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        response = sr.ReadToEnd();
                        sr.Close();
                    }
                    httpWebResponse.Close();
                    responseHeaders = httpWebResponse.Headers.AllKeys.ToDictionary(k => k, k => httpWebResponse.Headers[k]);
                }
                return HttpRequestResult.Success;
            }
            catch (WebException webEx)
            {
                using (var webResponse = webEx.Response)
                {
                    var httpResponse = (HttpWebResponse) webResponse;
                    if (webResponse != null)
                    {
                        using (var stream = webResponse.GetResponseStream())
                        {
                            var text = new StreamReader(stream).ReadToEnd();
                            response = text.Trim().IfNullOrEmpty(httpResponse.StatusCode.ToString());
                            responseHeaders = webResponse.Headers.AllKeys.ToDictionary(k => k, k => webResponse.Headers[k]);
                            return HttpRequestResult.WebException;
                        }
                    }
                    
                    response = webEx.GetFullMessage();
                    return HttpRequestResult.Exception;
                }
            }
            catch (Exception ex)
            {
                response = ex.GetFullMessage();
                return HttpRequestResult.Exception;
            }
        }

        public enum HttpRequestResult
        {
            Success,
            WebException,
            Exception
        }
    }
}
