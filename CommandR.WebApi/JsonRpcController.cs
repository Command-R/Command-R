using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using CfgDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandR.WebApi
{
    [RoutePrefix("jsonrpc")]
    public class JsonRpcController : ApiController
    {
        private readonly JsonRpc _jsonRpc;
        private readonly Commander _commander;
        private readonly Settings _settings;

        public JsonRpcController(JsonRpc jsonRpc, Commander commander, Settings settings)
        {
            _jsonRpc = jsonRpc;
            _commander = commander;
            _settings = settings;
        }

        [Route("{command?}")]
        public async Task<dynamic> Post(string command = null)
        {
            var json = await RetrieveJsonFromRequest(Request.Content);
            return await _jsonRpc.Execute(json);
        }

        [Route("{command}")]
        public async Task<dynamic> Get(string command)
        {
            var queryString = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            var json = JsonConvert.SerializeObject(queryString);
            return await _commander.Send(command, json);
        }

        //REF: http://shazwazza.com/post/uploading-files-and-json-data-in-the-same-request-with-angular-js/
        private async Task<string> RetrieveJsonFromRequest(HttpContent content)
        {
            if (!content.IsMimeMultipartContent())
            {
                return await content.ReadAsStringAsync();
            }

            var provider = LoadMultipartStreamProvider();
            var result = await content.ReadAsMultipartAsync(provider);
            return MapFilePathsToCommandJson(result);
        }

        private MultipartFormDataStreamProvider LoadMultipartStreamProvider()
        {
            var path = _settings.UploadFolder;
            if (path == null)
                throw new ApplicationException("JsonRpcController ERROR null uploads path");

            if (path.StartsWith("~"))
            {
                path = HostingEnvironment.MapPath(path) ?? string.Empty;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            return new FilenameMultipartFormDataStreamProvider(path);
        }

        private static string MapFilePathsToCommandJson(MultipartFormDataStreamProvider result)
        {
            var json = result.FormData["json"];
            if (json == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var jObject = JObject.Parse(json);
            foreach (var file in result.FileData)
            {
                var name = file.Headers.ContentDisposition.Name.Replace("\"", string.Empty);
                jObject["params"][name] = file.LocalFileName;
            }

            return jObject.ToString();
        }

        public class Settings : BaseSettings
        {
            public string UploadFolder { get; set; }

            public override void Validate()
            {
                if (string.IsNullOrWhiteSpace(UploadFolder))
                    UploadFolder = "~/App_Data/JsonRpcUploads/";

                if (UploadFolder.StartsWith("~"))
                    return; //allow mapped paths, but can't fail fast

                if (!Directory.Exists(UploadFolder))
                    Directory.CreateDirectory(UploadFolder);
            }
        };
    };
}
