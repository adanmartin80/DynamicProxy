using GestorDeErrores.Entities;
using GestorDeErrores.Proxy.Configuration;
using GestorDeErrores.Proxy.Extensions;
using GestorDeErrores.SCOM;
using GestorDeErrores.Utilidades;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;


namespace GestorDeErrores.WindowsService.Logic
{
    public class ProxyClient
    {
        readonly string __PATH_INTERFACES__ = Path.Combine(System.Environment.CurrentDirectory, "interfaces");
        const string __ASSEMBLY__ = "GestorDeErrores.Proxy.Interfaces";
        ConfigurationService _config;

        public ProxyClient(ConfigurationService config)
        {
            _config = config;
            GenerateInterfaces();
            CreateServices();
            //using (ChannelFactory<IService> scf = new ChannelFactory<IService>(new BasicHttpBinding(), "http://localhost:8000/Soap"))

        }

        private void GenerateInterfaces()
        {
            try
            {
                var SvcUtilPath = Path.Combine(System.Environment.CurrentDirectory, "Tools", "SvcUtil.exe");

                if (!File.Exists(SvcUtilPath))
                    throw new ApplicationException($"No se puede encontrar el fichero {SvcUtilPath}");

                _config.Interfaces.ToList().ForEach(x => 
                {
                    Process proceso;
                    proceso = new Process();
                    proceso.EnableRaisingEvents = false; 
                    ProcessStartInfo p = new ProcessStartInfo(SvcUtilPath, $"{x.Address} /directory:{Path.Combine(System.Environment.CurrentDirectory, "interfaces")} /out:{x.Name} /noConfig");
                    p.CreateNoWindow = true;
                    p.UseShellExecute = false;
                    proceso.StartInfo = p;
                    proceso.Start();
                });
            }
            catch (Exception e)
            {
                Log.Logging(Codigos.Error.ErrorGenerico, e, Assembly.GetExecutingAssembly().GetName().Name, MethodBase.GetCurrentMethod().Name, e.Message, MethodBase.GetCurrentMethod().GetParameters());
            }
        }

        private void CreateServices()
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var result = default(CompilerResults);
            _config.Interfaces.ToList().ForEach(x => 
            {
                CompilerParameters parameters = new CompilerParameters();
                var filePath = Path.Combine(__PATH_INTERFACES__, $"{x.Name}.cs");

                if (!File.Exists(filePath))
                    throw new ApplicationException($"No se puede encontrar el fichero {filePath}");
                
                parameters.GenerateExecutable = false;
                parameters.OutputAssembly = __ASSEMBLY__;
                parameters.GenerateInMemory = true;
                parameters.TreatWarningsAsErrors = false;
                parameters.ReferencedAssemblies.AddRange(new[] 
                {
                    "System.ServiceModel.dll",
                    "mscorlib.dll",
                    "System.dll"
                });

                result = provider.CompileAssemblyFromFile(parameters, filePath);

                if (result.Errors.Count > 0)
                {
                    var exChild = default(Exception);
                    foreach (CompilerError error in result.Errors)
                    {
                        if (exChild != null)
                        {
                            var exOld = exChild;
                            exChild = new Exception(error.ToString(), exOld);
                        }
                        else
                            exChild = new Exception(error.ToString());
                    }
                    var ex = new Exception($"Error al construir la interface de servicio {filePath} en {result.PathToAssembly}", exChild);
                    Log.Logging(Codigos.Error.ErrorGenerico, ex, Assembly.GetExecutingAssembly().GetName().Name, MethodBase.GetCurrentMethod().Name, null, MethodBase.GetCurrentMethod().GetParameters());
                }
                else
                {
                    var types = result.CompiledAssembly.GetTypes();
                    var typeClass = types.SingleOrDefault(t => t.IsClass);
                    var typeInterface = typeClass.GetInterfaces().SingleOrDefault(i => types.Any(t => t.Name == i.Name));
                    x.Type = typeInterface;
                }
            });
        }

        public Resquest LlamadaWebService(TaskError task)
        {
            var services = new List<ICommunicationObject>();
            ConfigurationService.GetConfig.Interfaces.ToList().ForEach(x => services.Add(x.GetService()));

           // return services.GetMethod(task.Aplicacion)(task);
            var request = new Resquest(services.GetMethod(task.Aplicacion));
            return request;
        }

        public class Resquest
        {
            Func<object[], bool> _delegate;

            public Resquest(Func<object[], bool> delegateRequest)
            {
                _delegate = delegateRequest;
            }

            public bool Send(params object[] parameters)
            {
                return _delegate(parameters);
            }
        }
    }
}
