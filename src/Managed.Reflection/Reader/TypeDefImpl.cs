/*
  The MIT License (MIT) 
  Copyright (C) 2009-2011 Jeroen Frijters
  
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
using System.Text;
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Reader
{
    sealed class TypeDefImpl : TypeInfo
    {
        private readonly ModuleReader module;
        private readonly int index;
        private readonly string typeName;
        private readonly string typeNamespace;
        private Type[] typeArgs;

        internal TypeDefImpl(ModuleReader module, int index)
        {
            this.module = module;
            this.index = index;
            // empty typeName is not allowed, but obfuscators...
            this.typeName = module.GetString(module.TypeDef.records[index].TypeName) ?? "";
            this.typeNamespace = module.GetString(module.TypeDef.records[index].TypeNamespace);
            MarkKnownType(typeNamespace, typeName);
        }

        public override Type BaseType
        {
            get
            {
                int extends = module.TypeDef.records[index].Extends;
                if ((extends & 0xFFFFFF) == 0)
                {
                    return null;
                }
                return module.ResolveType(extends, this);
            }
        }

        public override TypeAttributes Attributes
        {
            get { return (TypeAttributes)module.TypeDef.records[index].Flags; }
        }

        public override EventInfo[] __GetDeclaredEvents()
        {
            foreach (int i in module.EventMap.Filter(this.MetadataToken))
            {
                int evt = module.EventMap.records[i].EventList - 1;
                int end = module.EventMap.records.Length > i + 1 ? module.EventMap.records[i + 1].EventList - 1 : module.Event.records.Length;
                EventInfo[] events = new EventInfo[end - evt];
                if (module.EventPtr.RowCount == 0)
                {
                    for (int j = 0; evt < end; evt++, j++)
                    {
                        events[j] = new EventInfoImpl(module, this, evt);
                    }
                }
                else
                {
                    for (int j = 0; evt < end; evt++, j++)
                    {
                        events[j] = new EventInfoImpl(module, this, module.EventPtr.records[evt] - 1);
                    }
                }
                return events;
            }
            return Empty<EventInfo>.Array;
        }

        public override FieldInfo[] __GetDeclaredFields()
        {
            int field = module.TypeDef.records[index].FieldList - 1;
            int end = module.TypeDef.records.Length > index + 1 ? module.TypeDef.records[index + 1].FieldList - 1 : module.Field.records.Length;
            FieldInfo[] fields = new FieldInfo[end - field];
            if (module.FieldPtr.RowCount == 0)
            {
                for (int i = 0; field < end; i++, field++)
                {
                    fields[i] = module.GetFieldAt(this, field);
                }
            }
            else
            {
                for (int i = 0; field < end; i++, field++)
                {
                    fields[i] = module.GetFieldAt(this, module.FieldPtr.records[field] - 1);
                }
            }
            return fields;
        }

        public override Type[] __GetDeclaredInterfaces()
        {
            List<Type> list = null;
            foreach (int i in module.InterfaceImpl.Filter(this.MetadataToken))
            {
                if (list == null)
                {
                    list = new List<Type>();
                }
                list.Add(module.ResolveType(module.InterfaceImpl.records[i].Interface, this));
            }
            return Util.ToArray(list, Type.EmptyTypes);
        }

        public override MethodBase[] __GetDeclaredMethods()
        {
            int method = module.TypeDef.records[index].MethodList - 1;
            int end = module.TypeDef.records.Length > index + 1 ? module.TypeDef.records[index + 1].MethodList - 1 : module.MethodDef.records.Length;
            MethodBase[] methods = new MethodBase[end - method];
            if (module.MethodPtr.RowCount == 0)
            {
                for (int i = 0; method < end; method++, i++)
                {
                    methods[i] = module.GetMethodAt(this, method);
                }
            }
            else
            {
                for (int i = 0; method < end; method++, i++)
                {
                    methods[i] = module.GetMethodAt(this, module.MethodPtr.records[method] - 1);
                }
            }
            return methods;
        }

        public override __MethodImplMap __GetMethodImplMap()
        {
            PopulateGenericArguments();
            List<MethodInfo> bodies = new List<MethodInfo>();
            List<List<MethodInfo>> declarations = new List<List<MethodInfo>>();
            foreach (int i in module.MethodImpl.Filter(this.MetadataToken))
            {
                MethodInfo body = (MethodInfo)module.ResolveMethod(module.MethodImpl.records[i].MethodBody, typeArgs, null);
                int index = bodies.IndexOf(body);
                if (index == -1)
                {
                    index = bodies.Count;
                    bodies.Add(body);
                    declarations.Add(new List<MethodInfo>());
                }
                MethodInfo declaration = (MethodInfo)module.ResolveMethod(module.MethodImpl.records[i].MethodDeclaration, typeArgs, null);
                declarations[index].Add(declaration);
            }
            __MethodImplMap map = new __MethodImplMap();
            map.TargetType = this;
            map.MethodBodies = bodies.ToArray();
            map.MethodDeclarations = new MethodInfo[declarations.Count][];
            for (int i = 0; i < map.MethodDeclarations.Length; i++)
            {
                map.MethodDeclarations[i] = declarations[i].ToArray();
            }
            return map;
        }

        public override Type[] __GetDeclaredTypes()
        {
            int token = this.MetadataToken;
            List<Type> list = new List<Type>();
            // note that the NestedClass table is sorted on NestedClass, so we can't use binary search
            for (int i = 0; i < module.NestedClass.records.Length; i++)
            {
                if (module.NestedClass.records[i].EnclosingClass == token)
                {
                    list.Add(module.ResolveType(module.NestedClass.records[i].NestedClass));
                }
            }
            return list.ToArray();
        }

        public override PropertyInfo[] __GetDeclaredProperties()
        {
            foreach (int i in module.PropertyMap.Filter(this.MetadataToken))
            {
                int property = module.PropertyMap.records[i].PropertyList - 1;
                int end = module.PropertyMap.records.Length > i + 1 ? module.PropertyMap.records[i + 1].PropertyList - 1 : module.Property.records.Length;
                PropertyInfo[] properties = new PropertyInfo[end - property];
                if (module.PropertyPtr.RowCount == 0)
                {
                    for (int j = 0; property < end; property++, j++)
                    {
                        properties[j] = new PropertyInfoImpl(module, this, property);
                    }
                }
                else
                {
                    for (int j = 0; property < end; property++, j++)
                    {
                        properties[j] = new PropertyInfoImpl(module, this, module.PropertyPtr.records[property] - 1);
                    }
                }
                return properties;
            }
            return Empty<PropertyInfo>.Array;
        }

        internal override TypeName TypeName
        {
            get { return new TypeName(typeNamespace, typeName); }
        }

        public override string Name
        {
            get { return TypeNameParser.Escape(typeName); }
        }

        public override string FullName
        {
            get { return GetFullName(); }
        }

        public override int MetadataToken
        {
            get { return (TypeDefTable.Index << 24) + index + 1; }
        }

        public override Type[] GetGenericArguments()
        {
            PopulateGenericArguments();
            return Util.Copy(typeArgs);
        }

        private void PopulateGenericArguments()
        {
            if (typeArgs == null)
            {
                int token = this.MetadataToken;
                int first = module.GenericParam.FindFirstByOwner(token);
                if (first == -1)
                {
                    typeArgs = Type.EmptyTypes;
                }
                else
                {
                    List<Type> list = new List<Type>();
                    int len = module.GenericParam.records.Length;
                    for (int i = first; i < len && module.GenericParam.records[i].Owner == token; i++)
                    {
                        list.Add(new GenericTypeParameter(module, i, Signature.ELEMENT_TYPE_VAR));
                    }
                    typeArgs = list.ToArray();
                }
            }
        }

        internal override Type GetGenericTypeArgument(int index)
        {
            PopulateGenericArguments();
            return typeArgs[index];
        }

        public override CustomModifiers[] __GetGenericArgumentsCustomModifiers()
        {
            PopulateGenericArguments();
            return new CustomModifiers[typeArgs.Length];
        }

        public override bool IsGenericType
        {
            get { return IsGenericTypeDefinition; }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                if ((typeFlags & (TypeFlags.IsGenericTypeDefinition | TypeFlags.NotGenericTypeDefinition)) == 0)
                {
                    typeFlags |= module.GenericParam.FindFirstByOwner(this.MetadataToken) == -1
                        ? TypeFlags.NotGenericTypeDefinition
                        : TypeFlags.IsGenericTypeDefinition;
                }
                return (typeFlags & TypeFlags.IsGenericTypeDefinition) != 0;
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            if (IsGenericTypeDefinition)
            {
                return this;
            }
            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.FullName);
            string sep = "[";
            foreach (Type arg in GetGenericArguments())
            {
                sb.Append(sep);
                sb.Append(arg);
                sep = ",";
            }
            if (sep != "[")
            {
                sb.Append(']');
            }
            return sb.ToString();
        }

        internal bool IsNestedByFlags
        {
            get { return (this.Attributes & TypeAttributes.VisibilityMask & ~TypeAttributes.Public) != 0; }
        }

        public override Type DeclaringType
        {
            get
            {
                // note that we cannot use Type.IsNested for this, because that calls DeclaringType
                if (!IsNestedByFlags)
                {
                    return null;
                }
                foreach (int i in module.NestedClass.Filter(this.MetadataToken))
                {
                    return module.ResolveType(module.NestedClass.records[i].EnclosingClass, null, null);
                }
                throw new InvalidOperationException();
            }
        }

        public override bool __GetLayout(out int packingSize, out int typeSize)
        {
            foreach (int i in module.ClassLayout.Filter(this.MetadataToken))
            {
                packingSize = module.ClassLayout.records[i].PackingSize;
                typeSize = module.ClassLayout.records[i].ClassSize;
                return true;
            }
            packingSize = 0;
            typeSize = 0;
            return false;
        }

        public override Module Module
        {
            get { return module; }
        }

        internal override bool IsModulePseudoType
        {
            get { return index == 0; }
        }

        internal override bool IsBaked
        {
            get { return true; }
        }

        protected override bool IsValueTypeImpl
        {
            get
            {
                Type baseType = this.BaseType;
                if (baseType != null && baseType.IsEnumOrValueType && !this.IsEnumOrValueType)
                {
                    typeFlags |= TypeFlags.ValueType;
                    return true;
                }
                else
                {
                    typeFlags |= TypeFlags.NotValueType;
                    return false;
                }
            }
        }
    }
}
