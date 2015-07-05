using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

namespace CommandR
{
    [RoutePrefix("jsonrpc")]
    public class JsonRpcController : ApiController
    {
        private readonly JsonRpc _jsonRpc;
        private readonly Commander _commander;

        public JsonRpcController(JsonRpc jsonRpc, Commander commander)
        {
            _jsonRpc = jsonRpc;
            _commander = commander;
        }

        [Route("{command?}")]
        public async Task<dynamic> Post(string command = null)
        {
            var json = await Request.Content.ReadAsStringAsync();
            return await _jsonRpc.Execute(json);
        }

        [Route("{command}")]
        public async Task<dynamic> Get(string command)
        {
            var queryString = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            var json = JsonConvert.SerializeObject(queryString);
            return await _commander.Send(command, json);
        }
    };
}
