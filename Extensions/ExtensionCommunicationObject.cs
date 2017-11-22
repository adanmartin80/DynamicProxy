using GestorDeErrores.Proxy.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace GestorDeErrores.Proxy.Extensions
{
    public static class ExtensionCommunicationObject
    {
        public static Func<object[], bool> GetMethod(this IEnumerable<ICommunicationObject> channelsFactory, string nameMethod)
        {
            var delegateMethod = default(Func<object[], bool>);
            var method = default(MethodInfo);
            var service = default(object);

            foreach (var channel in channelsFactory)
            {
                if (method != null)
                    break;

                var type = channel.GetType();
                var serviceInstance = type.GetMethod("CreateChannel", new Type[0]).Invoke(channel, null);
                var typeService = serviceInstance.GetType();
                method = typeService.GetMethod(nameMethod);
                service = serviceInstance;
            }

            delegateMethod = (parameters) =>
            {
                return Convert.ToBoolean(method.Invoke(service, parameters));
            };
            return delegateMethod;
        }

        public static ICommunicationObject GetService(this ConfigurationInterface config)
        {
            var typeService = typeof(ChannelFactory<>);
            var typeServiceWithGenericType = typeService.MakeGenericType(config.Type);
            var instance = Activator.CreateInstance(typeServiceWithGenericType, new object[] { new BasicHttpBinding(), config.Address });

            return instance as ICommunicationObject;
        }
    }

    public static class CreateServices
    {
        public static IEnumerable<object> Load()
        {
            var config = ConfigurationService.GetConfig;

            throw new NotImplementedException();
        } 
    } 
}
