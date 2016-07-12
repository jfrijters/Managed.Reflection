/*
  The MIT License (MIT) 
  Copyright (C) 2012 Jeroen Frijters
  
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
using System.Collections.Generic;

namespace Managed.Reflection
{
    public interface IReflectableType
    {
        TypeInfo GetTypeInfo();
    }

    public static class IntrospectionExtensions
    {
        // we target .NET 2.0 so we can't define an extension method
        public static TypeInfo GetTypeInfo(/*this*/ Type type)
        {
            return type.GetTypeInfo();
        }
    }

    public abstract class TypeInfo : Type, IReflectableType
    {
        private const BindingFlags Flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        internal TypeInfo()
        {
        }

        internal TypeInfo(Type underlyingType)
            : base(underlyingType)
        {
        }

        internal TypeInfo(byte sigElementType)
            : base(sigElementType)
        {
        }

        public IEnumerable<ConstructorInfo> DeclaredConstructors
        {
            get { return GetConstructors(Flags); }
        }

        public IEnumerable<EventInfo> DeclaredEvents
        {
            get { return GetEvents(Flags); }
        }

        public IEnumerable<FieldInfo> DeclaredFields
        {
            get { return GetFields(Flags); }
        }

        public IEnumerable<MemberInfo> DeclaredMembers
        {
            get { return GetMembers(Flags); }
        }

        public IEnumerable<MethodInfo> DeclaredMethods
        {
            get { return GetMethods(Flags); }
        }

        public IEnumerable<TypeInfo> DeclaredNestedTypes
        {
            get
            {
                Type[] types = GetNestedTypes(Flags);
                TypeInfo[] typeInfos = new TypeInfo[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    typeInfos[i] = types[i].GetTypeInfo();
                }
                return typeInfos;
            }
        }

        public IEnumerable<PropertyInfo> DeclaredProperties
        {
            get { return GetProperties(Flags); }
        }

        public Type[] GenericTypeParameters
        {
            get { return IsGenericTypeDefinition ? GetGenericArguments() : Type.EmptyTypes; }
        }

        public IEnumerable<Type> ImplementedInterfaces
        {
            get { return __GetDeclaredInterfaces(); }
        }

        public Type AsType()
        {
            return this;
        }

        public EventInfo GetDeclaredEvent(string name)
        {
            return GetEvent(name, Flags);
        }

        public FieldInfo GetDeclaredField(string name)
        {
            return GetField(name, Flags);
        }

        public MethodInfo GetDeclaredMethod(string name)
        {
            return GetMethod(name, Flags);
        }

        public IEnumerable<MethodInfo> GetDeclaredMethods(string name)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo method in GetMethods(Flags))
            {
                if (method.Name == name)
                {
                    methods.Add(method);
                }
            }
            return methods;
        }

        public TypeInfo GetDeclaredNestedType(string name)
        {
            return GetNestedType(name, Flags).GetTypeInfo();
        }

        public PropertyInfo GetDeclaredProperty(string name)
        {
            return GetProperty(name, Flags);
        }

        public bool IsAssignableFrom(TypeInfo typeInfo)
        {
            return base.IsAssignableFrom(typeInfo);
        }
    }
}
