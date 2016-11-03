using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandR
{
    public class Commander
    {
        private readonly IMediator _mediator;
        private static Dictionary<string, Type> _commandTypes;

        public Commander(IMediator mediator)
        {
            _mediator = mediator;
        }

        public static void Initialize(params Assembly[] assemblies)
        {
            _commandTypes = LoadAllRequestTypes(assemblies.Union(new[] { typeof(Commander).Assembly }));
        }

        public async Task<object> Send(string name, string json)
        {
            var command = LoadCommand(name, json);
            return await Send(command);
        }

        public async Task<dynamic> Send(object command)
        {
            if (command == null)
                return null;

            try
            {
                //Request
                var iRequest = command.GetType().GetInterface("IRequest`1");
                if (iRequest != null)
                {
                    var send = _mediator.GetType().GetMethod("Send").MakeGenericMethod(iRequest.GetGenericArguments());
                    var result = send.Invoke(_mediator, new[] { command });
                    return result;
                }

                //Async Request
                var iAsyncRequest = command.GetType().GetInterface("IAsyncRequest`1");
                var sendAsync = _mediator.GetType().GetMethod("SendAsync").MakeGenericMethod(iAsyncRequest.GetGenericArguments());
                var task = (Task)sendAsync.Invoke(_mediator, new[] { command });
                await task;
                return task.GetType().GetProperty("Result").GetValue(task);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("CommandProcessor.Send ERROR on command type " + command.GetType(), ex);
            }
        }

        public T Send<T>(IRequest<T> command)
        {
            return _mediator.Send(command);
        }

        public IDictionary<string, Type> GetRegisteredCommands()
        {
            return _commandTypes;
        }

        public object CreateCommand(string name)
        {
            if (!_commandTypes.ContainsKey(name))
                throw new ApplicationException("Request type not found: " + name);

            //Instantiate Command
            var type = _commandTypes[name];
            var command = Activator.CreateInstance(type);
            return command;
        }

        public object LoadCommand(string name, string json)
        {
            try
            {
                var request = CreateCommand(name);
                if (string.IsNullOrWhiteSpace(json))
                    return request;

                //Bind Json to Command object
                JsonConvert.PopulateObject(json, request);

                //Check if Request is Patchable
                if (request is IPatchable)
                {
                    var obj = (IDictionary<string, JToken>)JObject.Parse(json);
                    (request as IPatchable).PatchFields = obj.Keys.ToArray();
                }

                return request;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to load command: " + name, ex);
            }
        }

        private static Dictionary<string, Type> LoadAllRequestTypes(IEnumerable<Assembly> assemblies)
        {
            var dict = new Dictionary<string, Type>();

            var iRequest = typeof(IRequest<>);
            var iAsyncRequest = typeof(IAsyncRequest<>);
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (!type.IsAbstract && (InheritsOrImplements(type, iRequest) || InheritsOrImplements(type, iAsyncRequest)))
                        dict[type.Name] = type;
                }
            }

            return dict;
        }

        //REF: http://stackoverflow.com/a/4897426/366559
        public static bool InheritsOrImplements(Type child, Type parent)
        {
            parent = ResolveGenericTypeDefinition(parent);

            var currentChild = child.IsGenericType
                ? child.GetGenericTypeDefinition()
                : child;

            while (currentChild != typeof(object))
            {
                if (parent == currentChild || HasAnyInterfaces(parent, currentChild))
                    return true;

                currentChild = currentChild.BaseType != null
                               && currentChild.BaseType.IsGenericType
                    ? currentChild.BaseType.GetGenericTypeDefinition()
                    : currentChild.BaseType;

                if (currentChild == null)
                    return false;
            }
            return false;
        }

        private static bool HasAnyInterfaces(Type parent, Type child)
        {
            return child.GetInterfaces()
                .Any(childInterface =>
                {
                    var currentInterface = childInterface.IsGenericType
                        ? childInterface.GetGenericTypeDefinition()
                        : childInterface;

                    return currentInterface == parent;
                });
        }

        private static Type ResolveGenericTypeDefinition(Type parent)
        {
            var shouldUseGenericType = parent.IsGenericType && parent.GetGenericTypeDefinition() != parent;

            if (parent.IsGenericType && shouldUseGenericType)
                parent = parent.GetGenericTypeDefinition();

            return parent;
        }
    };
}
