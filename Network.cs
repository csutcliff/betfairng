using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace BetfairNG
{
    public class Network
    {
        public const int BetfairDelayLogTimeThreshold = 150;

        public static TraceSource TraceSource = new TraceSource("BetfairNG.Network");

        private static readonly HttpClient _httpClient;

        static Network()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                SslProtocols = SslProtocols.Tls12
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(10000)
            };

            // Equivalent to ServicePointManager.Expect100Continue = false
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        public Network() : this(null, null)
        { }

        public Network(
            string appKey,
            string sessionToken,
            Action preRequestAction = null,
            bool gzipCompress = true,
            WebProxy proxy = null)
        {
            this.Host = string.Empty;
            this.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            this.TimeoutMilliseconds = 10000;
            this.AppKey = appKey;
            this.SessionToken = sessionToken;
            this.GZipCompress = gzipCompress;
            this.PreRequestAction = preRequestAction;
            this.Proxy = proxy;
        }

        public string AppKey { get; set; }

        public bool GZipCompress { get; set; }

        public string Host { get; set; }

        public Action PreRequestAction { get; set; }

        public WebProxy Proxy { get; set; }

        public int RetryCount { get; set; }

        public string SessionToken { get; set; }

        public int TimeoutMilliseconds { get; set; }

        public string UserAgent { get; set; }

        private HttpClient GetHttpClient()
        {
            if (Proxy == null)
                return _httpClient;

            // Create dedicated client with proxy
            var handler = new HttpClientHandler
            {
                Proxy = Proxy,
                UseProxy = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                SslProtocols = SslProtocols.Tls12
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(TimeoutMilliseconds)
            };
            client.DefaultRequestHeaders.ExpectContinue = false;
            return client;
        }

        public Task<BetfairServerResponse<T>> Invoke<T>(
            Endpoint endpoint,
            string method,
            IDictionary<string, object> args = null)
        {
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException("method");

            Trace.TraceInformation("Network: {0}, {1}", endpoint, method);
            DateTime requestStart = DateTime.Now;
            Stopwatch watch = new Stopwatch();
            watch.Start();

            string url = "https://api.betfair.com/exchange";

            if (endpoint == Endpoint.Betting)
                url += "/betting/json-rpc/v1";
            else if (endpoint == Endpoint.Account)
                url += "/account/json-rpc/v1";
            else if (endpoint == Endpoint.Scores)
                url += "/scores/json-rpc/v1";

            var call = new JsonRequest { Method = method, Id = 1, Params = args };
            var requestData = JsonConvert.Serialize(call);

            var response = Request(url, requestData, "application/json-rpc", this.AppKey, this.SessionToken);

            var result = response.ContinueWith(c =>
            {
                var lastByte = DateTime.Now;
                var jsonResponse = JsonConvert.Deserialize<JsonResponse<T>>(c.Result);

                watch.Stop();
                TraceSource.TraceInformation("Network finish: {0}ms, {1}, {2}",
                    watch.ElapsedMilliseconds,
                    FormatEndpoint(endpoint),
                    method);

                return ToResponse(jsonResponse, requestStart, lastByte, watch.ElapsedMilliseconds);
            });

            return result;
        }

        public BetfairServerResponse<KeepAliveResponse> KeepAliveSynchronous()
        {
            TraceSource.TraceInformation("KeepAlive");
            DateTime requestStart = DateTime.Now;
            Stopwatch watch = Stopwatch.StartNew();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://identitysso.betfair.com/api/keepAlive");
            request.Headers.Add("X-Application", AppKey);
            request.Headers.Add("X-Authentication", SessionToken);
            request.Headers.Accept.ParseAdd("application/json");

            var client = GetHttpClient();
            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            var lastByte = DateTime.Now;

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var keepAliveResponse = JsonConvert.Deserialize<KeepAliveResponse>(content);

            watch.Stop();
            TraceSource.TraceInformation("KeepAlive finish: {0}ms", watch.ElapsedMilliseconds);

            return new BetfairServerResponse<KeepAliveResponse>
            {
                HasError = !string.IsNullOrWhiteSpace(keepAliveResponse.Error),
                Response = keepAliveResponse,
                LastByte = lastByte,
                RequestStart = requestStart
            };
        }

        private string FormatEndpoint(Endpoint endpoint)
        {
            return endpoint == Endpoint.Betting ? "betting" : "account";
        }

        private async Task<string> Request(
            string url,
            string requestPostData,
            string contentType,
            string appKey,
            string sessionToken)
        {
            var sw = Stopwatch.StartNew();
            PreRequestAction?.Invoke();

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Content
            request.Content = new StringContent(requestPostData, Encoding.UTF8, contentType);

            // Headers
            if (!string.IsNullOrWhiteSpace(appKey))
                request.Headers.Add("X-Application", appKey);
            if (!string.IsNullOrWhiteSpace(sessionToken))
                request.Headers.Add("X-Authentication", sessionToken);
            request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8");
            request.Headers.Add("Accept-Language", "en-US");
            request.Headers.Add("Pragma", "no-cache");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.UserAgent.ParseAdd(UserAgent);

            if (GZipCompress)
                request.Headers.AcceptEncoding.ParseAdd("gzip, deflate");

            var client = GetHttpClient();
            using var response = await client.SendAsync(request);

            sw.Stop();

            if (sw.ElapsedMilliseconds > BetfairDelayLogTimeThreshold)
                Trace.TraceInformation("Betfair request time taken is '{0}' the request Timeout is {1} to {2}", sw.ElapsedMilliseconds, TimeoutMilliseconds, url);

            return await response.Content.ReadAsStringAsync();
        }

        private BetfairServerResponse<T> ToResponse<T>(JsonResponse<T> response, DateTime requestStart, DateTime lastByteStamp, long latency)
        {
            BetfairServerResponse<T> r = new BetfairServerResponse<T>
            {
                Error = BetfairServerException.ToClientException(response.Error),
                HasError = response.HasError,
                Response = response.Result,
                LastByte = lastByteStamp,
                RequestStart = requestStart
            };
            return r;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class JsonRequest
        {
            public JsonRequest()
            {
                JsonRpc = "2.0";
            }

            [JsonProperty(PropertyName = "id")]
            public object Id { get; set; }

            [JsonProperty(PropertyName = "jsonrpc", NullValueHandling = NullValueHandling.Ignore)]
            public string JsonRpc { get; set; }

            [JsonProperty(PropertyName = "method")]
            public string Method { get; set; }

            [JsonProperty(PropertyName = "params")]
            public object Params { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class JsonResponse<T>
        {
            [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore)]
            public Data.Exceptions.Exception Error { get; set; }

            [JsonIgnore]
            public bool HasError
            {
                get { return Error != null; }
            }

            [JsonProperty(PropertyName = "id")]
            public object Id { get; set; }

            [JsonProperty(PropertyName = "jsonrpc", NullValueHandling = NullValueHandling.Ignore)]
            public string JsonRpc { get; set; }

            [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore)]
            public T Result { get; set; }
        }
    }
}