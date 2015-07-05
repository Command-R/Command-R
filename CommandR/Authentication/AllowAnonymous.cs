using System;

namespace CommandR.Authentication
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllowAnonymousAttribute : Attribute
    {
    };
}
