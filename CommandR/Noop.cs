using MediatR;

namespace CommandR
{
    /// <summary>
    /// Example command that doesn't do anything.
    /// </summary>
    public class Noop : IRequest<Unit>
    {
        internal class Handler : IRequestHandler<Noop, Unit>
        {
            public Unit Handle(Noop request)
            {
                return Unit.Value;
            }
        }
    };
}