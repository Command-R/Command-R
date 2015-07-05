using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using CommandR.Authentication;

namespace CommandR.WebApi
{
    public class ApiAuthenticationFilter : IActionFilter
    {
        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext context, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            var tokenId = GetTokenIdFromRequest(context.Request);
            var tokenService = GetInstance<ITokenService>(context);
            var dict = tokenService.GetTokenData(tokenId);
            var appContext = new AppContext(dict)
            {
                RequestIsLocal = context.Request.IsLocal(),
            };
            GetInstance<ExecutionEnvironment>(context).AppContext = appContext;
            return await continuation();
        }

        public bool AllowMultiple
        {
            get { return false; }
        }

        private static string GetTokenIdFromRequest(HttpRequestMessage request)
        {
            var auth = request.Headers.Contains(JsonRpcClient.AuthenticationKey)
                ? request.Headers.GetValues(JsonRpcClient.AuthenticationKey).First()
                : request.GetQueryNameValuePairs()
                         .Where(x => x.Key == JsonRpcClient.AuthenticationKey)
                         .Select(x => x.Value)
                         .FirstOrDefault();

            if (auth == null)
                return null;

            return auth.Replace("Bearer ", "");
        }

        private static T GetInstance<T>(HttpActionContext context)
        {
            return (T)context.Request.GetDependencyScope().GetService(typeof(T));
        }
    };
}