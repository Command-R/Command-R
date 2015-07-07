using System;
using System.Threading;
using CommandR.Authentication;
using MediatR;

namespace CommandR.Services
{
    public interface IQueueService
    {
        void Enqueue<T>(IRequest<T> command, AppContext appContext);
        void Enqueue<T>(IAsyncRequest<T> command, AppContext appContext);
        void StartProcessing(CancellationToken cancellationToken, Action<object, AppContext> execute);
    };
}