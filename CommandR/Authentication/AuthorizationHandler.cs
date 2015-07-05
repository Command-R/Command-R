using MediatR;

namespace CommandR.Authentication
{
    public class AuthorizationHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _inner;
        private readonly AppContext _appContext;

        public AuthorizationHandler(IRequestHandler<TRequest, TResponse> inner, AppContext appContext)
        {
            _inner = inner;
            _appContext = appContext;
        }

        public TResponse Handle(TRequest command)
        {
            _appContext.VerifyAuthorization(command);
            return _inner.Handle(command);
        }
    };
}