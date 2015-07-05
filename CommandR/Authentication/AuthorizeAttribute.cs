using System;

namespace CommandR.Authentication
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AuthorizeAttribute : Attribute
    {
        public string Users { get; set; }
        public string Roles { get; set; }
    };
}
