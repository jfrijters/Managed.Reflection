/*
  The MIT License (MIT) 
  Copyright (C) 2009-2012 Jeroen Frijters
  
  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:
  
  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.
  
  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/
using System;
using System.Collections.Generic;

namespace Managed.Reflection
{
    public delegate Module ModuleResolveEventHandler(object sender, ResolveEventArgs e);

    public abstract class Assembly : ICustomAttributeProvider
    {
        internal readonly Universe universe;
        protected string fullName;  // AssemblyBuilder needs access to this field to clear it when the name changes
        protected List<ModuleResolveEventHandler> resolvers;

        internal Assembly(Universe universe)
        {
            this.universe = universe;
        }

        public sealed override string ToString()
        {
            return FullName;
        }

        public event ModuleResolveEventHandler ModuleResolve
        {
            add
            {
                if (resolvers == null)
                {
                    resolvers = new List<ModuleResolveEventHandler>();
                }
                resolvers.Add(value);
            }
            remove
            {
                resolvers.Remove(value);
            }
        }

        public abstract Type[] GetTypes();
        public abstract AssemblyName GetName();
        public abstract string ImageRuntimeVersion { get; }
        public abstract Module ManifestModule { get; }
        public abstract MethodInfo EntryPoint { get; }
        public abstract string Location { get; }
        public abstract AssemblyName[] GetReferencedAssemblies();
        public abstract Module[] GetModules(bool getResourceModules);
        public abstract Module[] GetLoadedModules(bool getResourceModules);
        public abstract Module GetModule(string name);
        public abstract string[] GetManifestResourceNames();
        public abstract ManifestResourceInfo GetManifestResourceInfo(string resourceName);
        public abstract System.IO.Stream GetManifestResourceStream(string name);

        internal abstract Type FindType(TypeName name);
        internal abstract Type FindTypeIgnoreCase(TypeName lowerCaseName);

        // The differences between ResolveType and FindType are:
        // - ResolveType is only used when a type is assumed to exist (because another module's metadata claims it)
        // - ResolveType can return a MissingType
        internal Type ResolveType(Module requester, TypeName typeName)
        {
            return FindType(typeName) ?? universe.GetMissingTypeOrThrow(requester, this.ManifestModule, null, typeName);
        }

        public string FullName
        {
            get { return fullName ?? (fullName = GetName().FullName); }
        }

        public Module[] GetModules()
        {
            return GetModules(true);
        }

        public IEnumerable<Module> Modules
        {
            get { return GetLoadedModules(); }
        }

        public Module[] GetLoadedModules()
        {
            return GetLoadedModules(true);
        }

        public AssemblyName GetName(bool copiedName)
        {
            return GetName();
        }

        public bool ReflectionOnly
        {
            get { return true; }
        }

        public Type[] GetExportedTypes()
        {
            List<Type> list = new List<Type>();
            foreach (Type type in GetTypes())
            {
                if (type.IsVisible)
                {
                    list.Add(type);
                }
            }
            return list.ToArray();
        }

        public IEnumerable<Type> ExportedTypes
        {
            get { return GetExportedTypes(); }
        }

        public IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                Type[] types = GetTypes();
                TypeInfo[] typeInfos = new TypeInfo[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    typeInfos[i] = types[i].GetTypeInfo();
                }
                return typeInfos;
            }
        }

        public Type GetType(string name)
        {
            return GetType(name, false);
        }

        public Type GetType(string name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            TypeNameParser parser = TypeNameParser.Parse(name, throwOnError);
            if (parser.Error)
            {
                return null;
            }
            if (parser.AssemblyName != null)
            {
                if (throwOnError)
                {
                    throw new ArgumentException("Type names passed to Assembly.GetType() must not specify an assembly.");
                }
                else
                {
                    return null;
                }
            }
            TypeName typeName = TypeName.Split(TypeNameParser.Unescape(parser.FirstNamePart));
            Type type = ignoreCase
                ? FindTypeIgnoreCase(typeName.ToLowerInvariant())
                : FindType(typeName);
            if (type == null && __IsMissing)
            {
                throw new MissingAssemblyException((MissingAssembly)this);
            }
            return parser.Expand(type, this.ManifestModule, throwOnError, name, false, ignoreCase);
        }

        public virtual Module LoadModule(string moduleName, byte[] rawModule)
        {
            throw new NotSupportedException();
        }

        public Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
        {
            return LoadModule(moduleName, rawModule);
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttributeData.__GetCustomAttributes(this, attributeType, inherit).Count != 0;
        }

        public IList<CustomAttributeData> __GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CustomAttributeData.__GetCustomAttributes(this, attributeType, inherit);
        }

        public IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributes(this);
        }

        public IEnumerable<CustomAttributeData> CustomAttributes
        {
            get { return GetCustomAttributesData(); }
        }

        public static string CreateQualifiedName(string assemblyName, string typeName)
        {
            return typeName + ", " + assemblyName;
        }

        public static Assembly GetAssembly(Type type)
        {
            return type.Assembly;
        }

        public string CodeBase
        {
            get
            {
                string path = this.Location.Replace(System.IO.Path.DirectorySeparatorChar, '/');
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }
                return "file://" + path;
            }
        }

        public virtual bool IsDynamic
        {
            get { return false; }
        }

        public virtual bool __IsMissing
        {
            get { return false; }
        }

        public AssemblyNameFlags __AssemblyFlags
        {
            get { return GetAssemblyFlags(); }
        }

        protected virtual AssemblyNameFlags GetAssemblyFlags()
        {
            return GetName().Flags;
        }

        internal abstract IList<CustomAttributeData> GetCustomAttributesData(Type attributeType);
    }
}
