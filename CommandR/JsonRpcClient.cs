using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandR
{
    /// <summary>
    /// JsonRpcClient makes it easy for DotNet code to call Commands
    /// </summary>
    /// <example>
    ///     var client = new JsonRpcClient("https://example.com/jsonrpc");
    ///     var result = client.Send(new Query { Search = "A" });
    /// </example>
    public class JsonRpcClient
    {
        private readonly string _rpcEndpointUrl;
        public const string AuthorizationKey = "Authorization";

        public JsonRpcClient(string rpcEndpointUrl)
        {
            _rpcEndpointUrl = rpcEndpointUrl;
            Headers = new NameValueCollection();
        }

        public NameValueCollection Headers { get; set; }

        public string Authorization
        {
            get { return Headers[AuthorizationKey]; }
            set { Headers[AuthorizationKey] = value; }
        }

        public TResponse Send<TResponse>(IRequest<TResponse> request)
        {
            if (request == null)
                throw new ArgumentException("Request cannot be null", "request");

            var jsonRpcRequest = BuildJsonRpcRequest(request);
            var response = PostJsonRpcCommand<TResponse>(jsonRpcRequest);

            return response;
        }

        private static JsonRpcRequest BuildJsonRpcRequest(Object request)
        {
            return new JsonRpcRequest
            {
                id = Guid.NewGuid().ToString("N"),
                jsonrpc = "2.0",
                method = request.GetType().Name,
                @params = JObject.FromObject(request)
            };
        }

        private TResponse PostJsonRpcCommand<TResponse>(JsonRpcRequest jsonRpcRequest)
        {
            //Post request
            var jsonRpcResponse = PostJson<JsonRpcResponse>(jsonRpcRequest);

            //Throw on error
            if (jsonRpcResponse.error != null)
                throw new JsonRpcErrorException(jsonRpcResponse.error);

            //Transform Result
            if (jsonRpcResponse.result is JObject)
            {
                var result = (JObject)jsonRpcResponse.result;
                return result.ToObject<TResponse>();
            }
            else
            {
                return (TResponse)jsonRpcResponse.result;
            }
        }

        private T PostJson<T>(object data, int timeout = 30)
        {
            var request = WebRequest.CreateHttp(_rpcEndpointUrl);
            request.Headers.Add(Headers);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Timeout = timeout * 1000;
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true; //HACK for DEV certs

            var json = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            request.ContentLength = bytes.Length;

            try
            {
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();

                    using (var response = request.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream == null)
                                throw new ApplicationException("NULL response from server");

                            using (var sr = new StreamReader(responseStream))
                            {
                                var responseData = sr.ReadToEnd().Trim();
                                return JsonConvert.DeserializeObject<T>(responseData);
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    throw new ApplicationException(ex.GetBaseException().Message);

                using (var responseStream = ex.Response.GetResponseStream())
                {
                    if (responseStream == null)
                        throw new ApplicationException("NULL response from server");

                    using (var sr = new StreamReader(responseStream))
                    {
                        var responseData = sr.ReadToEnd().Trim();
                        var message = GetExceptionMessage(responseData);
                        throw new ApplicationException(message);
                    }
                }
            }
        }

        private static string GetExceptionMessage(string data)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<dynamic>(data);
                return (string)error.exceptionMessage;
            }
            catch
            {
                return "Unable to process server response as json";
            }
        }
    };

    public class JsonRpcErrorException : ApplicationException
    {
        public JsonRpcErrorException(JsonRpcError jsonRpcError) : base(jsonRpcError.message)
        {
            Error = jsonRpcError;
        }

        public JsonRpcErrorException(JsonRpcError jsonRpcError, Exception innerException) : base(jsonRpcError.message, innerException)
        {
            Error = jsonRpcError;
        }

        public JsonRpcError Error { get; private set; }
    };
}