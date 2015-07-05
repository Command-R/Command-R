using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Authentication;

namespace CommandR.Authentication
{
    /// <summary>
    /// The AppContext stores information about the running execution environment.
    /// </summary>
    public class AppContext : Dictionary<string, object>
    {
        public AppContext()
        {
            //Deserialization needs an empty constructor to work
        }

        public AppContext(IDictionary<string, object> dict)
        {
            if (dict == null)
                return;

            foreach (var item in dict)
            {
                this[item.Key] = item.Value;
            }
        }

        public virtual string Id
        {
            get { return Get<string>("Id"); }
            set { this["Id"] = value; }
        }

        public virtual string Username
        {
            get { return Get<string>("Username"); }
            set { this["Username"] = value; }
        }

        public virtual string[] Roles
        {
            get { return Get<string[]>("Roles"); }
            set { this["Roles"] = value; }
        }

        public virtual bool RequestIsLocal
        {
            get { return Get<bool>("RequestIsLocal"); }
            set { this["RequestIsLocal"] = value; }
        }

        public virtual T Get<T>(string key, T def = default(T))
        {
            return ContainsKey(key) ? (T)base[key] : def;
        }

        public virtual void VerifyAuthorization(object command)
        {
            var authorize = TypeDescriptor.GetAttributes(command).OfType<AuthorizeAttribute>().FirstOrDefault();
            var anonymous = TypeDescriptor.GetAttributes(command).OfType<AllowAnonymousAttribute>().FirstOrDefault();
            var name = command.GetType().Name;

            if (authorize == null && anonymous == null)
                throw new ApplicationException("Command missing Authorize or AllowAnonymous attribute: " + name);

            if (anonymous != null)
                return;

            if (string.IsNullOrWhiteSpace(Username))
                throw new AuthenticationException("Invalid permissions for " + name);

            if (string.IsNullOrWhiteSpace(authorize.Users) && string.IsNullOrWhiteSpace(authorize.Roles))
                return;

            var users = Split(authorize.Users);
            var roles = Split(authorize.Roles);

            if (Username != null && users.Contains(Username))
                return;

            if (Roles != null && roles.Intersect(Roles).Any())
                return;

            throw new AuthenticationException("Invalid permissions for " + name);
        }

        private static string[] Split(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt))
                return  new string[0];

            return txt.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
        }
    };
}