using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.Assemblies;

namespace _BOA_
{
    /// <summary>
    /// AssemblyLoadContext exists in CoreCLR, but is not exposed by Unity's .NET Standard 2.1 references.
    /// </summary>
    sealed class ScriptLoadContext
    {
        readonly object context;
        readonly EventInfo resolving;
        readonly Delegate resolver;
        readonly Func<Stream, Assembly> load;
        readonly Action unload;

        //----------------------------------------------------------------------------------------------------------

        public ScriptLoadContext(string name)
        {
            var contextType = Type.GetType("System.Runtime.Loader.AssemblyLoadContext, System.Private.CoreLib", throwOnError: true);
            context = Activator.CreateInstance(contextType, new object[] { name, true });
            resolving = contextType.GetEvent("Resolving");
            resolver = typeof(ScriptLoadContext).GetMethod(nameof(ResolveAssembly), BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(resolving.EventHandlerType);
            load = (Func<Stream, Assembly>)contextType.GetMethod("LoadFromStream", new[] { typeof(Stream) }).CreateDelegate(typeof(Func<Stream, Assembly>), context);
            unload = (Action)contextType.GetMethod("Unload").CreateDelegate(typeof(Action), context);
            resolving.AddEventHandler(context, resolver);
        }

        //----------------------------------------------------------------------------------------------------------

        static Assembly ResolveAssembly(object context, AssemblyName name) => CurrentAssemblies.GetLoadedAssemblies().FirstOrDefault(assembly => AssemblyName.ReferenceMatchesDefinition(name, assembly.GetName()));

        public Assembly Load(Stream stream) => load(stream);

        //----------------------------------------------------------------------------------------------------------

        public void Unload()
        {
            resolving.RemoveEventHandler(context, resolver);
            unload();
        }
    }
}