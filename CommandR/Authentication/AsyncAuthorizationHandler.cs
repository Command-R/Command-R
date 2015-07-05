using System.Threading.Tasks;
using MediatR;

namespace CommandR.Authentication
{
    public class AsyncAuthorizationHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
        where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _inner;
        private readonly AppContext _appContext;

        public AsyncAuthorizationHandler(IAsyncRequestHandler<TRequest, TResponse> inner, AppContext appContext)
        {
            _inner = inner;
            _appContext = appContext;
        }

        public async Task<TResponse> Handle(TRequest command)
        {
            _appContext.VerifyAuthorization(command);
            return await _inner.Handle(command);
        }
    };
}