﻿using CommandR.Authentication;
using MediatR;

namespace CommandR
{
    /// <summary>
    /// Example command that doesn't do anything.
    /// </summary>
    [AllowAnonymous]
    public class Noop : IRequest<Unit>
    {
        internal class Handler : IRequestHandler<Noop, Unit>
        {
            public Unit Handle(Noop command)
            {
                return Unit.Value;
            }
        }
    };
}