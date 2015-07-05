using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandR
{
    public class JsonRpc
    {
        private readonly Commander _commander;

        public JsonRpc(Commander commander)
        {
            _commander = commander;
        }

        public async Task<object> Execute(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            if (data.TrimStart().StartsWith("[")) //batch is an array instead of object
            {
                return await ExecuteBatch(data);
            }
            else
            {
                return await ExecuteSingle(data);
            }
        }

        private async Task<List<JsonRpcResponse>> ExecuteBatch(string data)
        {
            var requests = JsonConvert.DeserializeObject<JsonRpcRequest[]>(data);
            var responses = new List<JsonRpcResponse>();

            // Execute each Request
            foreach (var request in requests)
            {
                var response = await ExecuteCommand(request);
                responses.Add(response);
            }

            return responses;
        }

        private async Task<JsonRpcResponse> ExecuteSingle(string data)
        {
            var request = JsonConvert.DeserializeObject<JsonRpcRequest>(data);
            var response = await ExecuteCommand(request);
            return response;
        }

        private async Task<JsonRpcResponse> ExecuteCommand(JsonRpcRequest request)
        {
            var response = new JsonRpcResponse(request);
            try
            {
                var json = request.@params == null ? null : request.@params.ToString();
                response.result = await _commander.Send(request.method, json);
            }
            catch (Exception ex)
            {
                response.error = new JsonRpcError(ex.GetBaseException());
            }
            return response;
        }
    };

    // ReSharper disable InconsistentNaming
    public class JsonRpcRequest
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public JObject @params { get; set; }
        public string id { get; set; }
    };

    public class JsonRpcResponse
    {
        public JsonRpcResponse() { }

        public JsonRpcResponse(JsonRpcRequest request)
        {
            jsonrpc = request.jsonrpc;
            id = request.id;
        }

        public string jsonrpc { get; set; }
        public object result { get; set; }
        public JsonRpcError error { get; set; }
        public string id { get; set; }
    };

    public class JsonRpcError
    {
        public JsonRpcError() { }

        public JsonRpcError(Exception ex)
        {
            code = ex.HResult;
            message = ex.Message;
            data = ex;
        }

        public int code { get; set; }
        public string message { get; set; }
        public object data { get; set; }
    };
    // ReSharper restore InconsistentNaming
}
