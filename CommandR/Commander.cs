using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandR
{
    public class Commander
    {
        private readonly IMediator _mediator;
        private static Dictionary<string, Type> _requestTypes;

        public Commander(IMediator mediator)
        {
            _mediator = mediator;
        }

        public static void Initialize(params Assembly[] assemblies)
        {
            _requestTypes = LoadAllRequestTypes(assemblies.Union(new[] { typeof(Commander).Assembly }));
        }

        public async Task<object> Send(string name, string json)
        {
            var request = LoadRequest(name, json);
            var response = await Send(request);
            return response;
        }

        public async Task<dynamic> Send(object request)
        {
            if (request == null)
                return null;

            try
            {
                //Request
                var requestInterface = request.GetType().GetInterface("IRequest`1");
                if (requestInterface != null)
                {
                    var send = _mediator.GetType().GetMethod("Send").MakeGenericMethod(requestInterface.GetGenericArguments());
                    var result = send.Invoke(_mediator, new[] { request });
                    return result;
                }

                //Async Request
                var asyncRequestInterface = request.GetType().GetInterface("IAsyncRequest`1");
                var sendAsync = _mediator.GetType().GetMethod("SendAsync").MakeGenericMethod(asyncRequestInterface.GetGenericArguments());
                var task = (Task)sendAsync.Invoke(_mediator, new[] { request });
                await task;
                return task.GetType().GetProperty("Result").GetValue(task);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("CommandProcessor.Send ERROR on request type " + request.GetType(), ex);
            }
        }

        public T Send<T>(IRequest<T> request)
        {
            return _mediator.Send(request);
        }

        public IDictionary<string, Type> GetRegisteredCommands()
        {
            return _requestTypes;
        }

        public object CreateRequest(string name)
        {
            if (!_requestTypes.ContainsKey(name))
                throw new ApplicationException("Request type not found: " + name);

            //Instantiate Command
            var type = _requestTypes[name];
            var request = Activator.CreateInstance(type);
            return request;
        }

        public object LoadRequest(string name, string json)
        {
            try
            {
                var request = CreateRequest(name);
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
                throw new ApplicationException("Unable to load request: " + name, ex);
            }
        }

        private static Dictionary<string, Type> LoadAllRequestTypes(IEnumerable<Assembly> assemblies)
        {
            var dict = new Dictionary<string, Type>();

            var requestType = typeof(IRequest<>);
            var asyncRequestType = typeof(IAsyncRequest<>);
            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (!type.IsAbstract && (InheritsOrImplements(type, requestType) || InheritsOrImplements(type, asyncRequestType)))
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
