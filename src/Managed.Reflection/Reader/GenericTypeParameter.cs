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
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Reader
{
    abstract class TypeParameterType : TypeInfo
    {
        protected TypeParameterType(byte sigElementType)
            : base(sigElementType)
        {
        }

        public sealed override string AssemblyQualifiedName
        {
            get { return null; }
        }

        protected sealed override bool IsValueTypeImpl
        {
            get { return (this.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0; }
        }

        public sealed override Type BaseType
        {
            get
            {
                foreach (Type type in GetGenericParameterConstraints())
                {
                    if (!type.IsInterface && !type.IsGenericParameter)
                    {
                        return type;
                    }
                }
                return this.IsValueType ? this.Module.universe.System_ValueType : this.Module.universe.System_Object;
            }
        }

        public override Type[] __GetDeclaredInterfaces()
        {
            List<Type> list = new List<Type>();
            foreach (Type type in GetGenericParameterConstraints())
            {
                if (type.IsInterface)
                {
                    list.Add(type);
                }
            }
            return list.ToArray();
        }

        public sealed override TypeAttributes Attributes
        {
            get { return TypeAttributes.Public; }
        }

        public sealed override string FullName
        {
            get { return null; }
        }

        public sealed override string ToString()
        {
            return this.Name;
        }

        protected sealed override bool ContainsMissingTypeImpl
        {
            get { return ContainsMissingType(GetGenericParameterConstraints()); }
        }
    }

    sealed class UnboundGenericMethodParameter : TypeParameterType
    {
        private static readonly DummyModule module = new DummyModule();
        private readonly int position;

        private sealed class DummyModule : NonPEModule
        {
            internal DummyModule()
                : base(new Universe())
            {
            }

            protected override Exception NotSupportedException()
            {
                return new InvalidOperationException();
            }

            protected override Exception ArgumentOutOfRangeException()
            {
                return new InvalidOperationException();
            }

            public override bool Equals(object obj)
            {
                throw new InvalidOperationException();
            }

            public override int GetHashCode()
            {
                throw new InvalidOperationException();
            }

            public override string ToString()
            {
                throw new InvalidOperationException();
            }

            public override int MDStreamVersion
            {
                get { throw new InvalidOperationException(); }
            }

            public override Assembly Assembly
            {
                get { throw new InvalidOperationException(); }
            }

            internal override Type FindType(TypeName typeName)
            {
                throw new InvalidOperationException();
            }

            internal override Type FindTypeIgnoreCase(TypeName lowerCaseName)
            {
                throw new InvalidOperationException();
            }

            internal override void GetTypesImpl(List<Type> list)
            {
                throw new InvalidOperationException();
            }

            public override string FullyQualifiedName
            {
                get { throw new InvalidOperationException(); }
            }

            public override string Name
            {
                get { throw new InvalidOperationException(); }
            }

            public override Guid ModuleVersionId
            {
                get { throw new InvalidOperationException(); }
            }

            public override string ScopeName
            {
                get { throw new InvalidOperationException(); }
            }
        }

        internal static Type Make(int position)
        {
            return module.universe.CanonicalizeType(new UnboundGenericMethodParameter(position));
        }

        private UnboundGenericMethodParameter(int position)
            : base(Signature.ELEMENT_TYPE_MVAR)
        {
            this.position = position;
        }

        public override bool Equals(object obj)
        {
            UnboundGenericMethodParameter other = obj as UnboundGenericMethodParameter;
            return other != null && other.position == position;
        }

        public override int GetHashCode()
        {
            return position;
        }

        public override string Namespace
        {
            get { throw new InvalidOperationException(); }
        }

        public override string Name
        {
            get { throw new InvalidOperationException(); }
        }

        public override int MetadataToken
        {
            get { throw new InvalidOperationException(); }
        }

        public override Module Module
        {
            get { return module; }
        }

        public override int GenericParameterPosition
        {
            get { return position; }
        }

        public override Type DeclaringType
        {
            get { return null; }
        }

        public override MethodBase DeclaringMethod
        {
            get { throw new InvalidOperationException(); }
        }

        public override Type[] GetGenericParameterConstraints()
        {
            throw new InvalidOperationException();
        }

        public override CustomModifiers[] __GetGenericParameterConstraintCustomModifiers()
        {
            throw new InvalidOperationException();
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get { throw new InvalidOperationException(); }
        }

        internal override Type BindTypeParameters(IGenericBinder binder)
        {
            return binder.BindMethodParameter(this);
        }

        internal override bool IsBaked
        {
            get { throw new InvalidOperationException(); }
        }
    }

    sealed class GenericTypeParameter : TypeParameterType
    {
        private readonly ModuleReader module;
        private readonly int index;

        internal GenericTypeParameter(ModuleReader module, int index, byte sigElementType)
            : base(sigElementType)
        {
            this.module = module;
            this.index = index;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string Namespace
        {
            get { return DeclaringType.Namespace; }
        }

        public override string Name
        {
            get { return module.GetString(module.GenericParam.records[index].Name); }
        }

        public override Module Module
        {
            get { return module; }
        }

        public override int MetadataToken
        {
            get { return (GenericParamTable.Index << 24) + index + 1; }
        }

        public override int GenericParameterPosition
        {
            get { return module.GenericParam.records[index].Number; }
        }

        public override Type DeclaringType
        {
            get
            {
                int owner = module.GenericParam.records[index].Owner;
                return (owner >> 24) == TypeDefTable.Index ? module.ResolveType(owner) : null;
            }
        }

        public override MethodBase DeclaringMethod
        {
            get
            {
                int owner = module.GenericParam.records[index].Owner;
                return (owner >> 24) == MethodDefTable.Index ? module.ResolveMethod(owner) : null;
            }
        }

        public override Type[] GetGenericParameterConstraints()
        {
            IGenericContext context = (this.DeclaringMethod as IGenericContext) ?? this.DeclaringType;
            List<Type> list = new List<Type>();
            foreach (int i in module.GenericParamConstraint.Filter(this.MetadataToken))
            {
                list.Add(module.ResolveType(module.GenericParamConstraint.records[i].Constraint, context));
            }
            return list.ToArray();
        }

        public override CustomModifiers[] __GetGenericParameterConstraintCustomModifiers()
        {
            IGenericContext context = (this.DeclaringMethod as IGenericContext) ?? this.DeclaringType;
            List<CustomModifiers> list = new List<CustomModifiers>();
            foreach (int i in module.GenericParamConstraint.Filter(this.MetadataToken))
            {
                CustomModifiers mods = new CustomModifiers();
                int metadataToken = module.GenericParamConstraint.records[i].Constraint;
                if ((metadataToken >> 24) == TypeSpecTable.Index)
                {
                    int index = (metadataToken & 0xFFFFFF) - 1;
                    mods = CustomModifiers.Read(module, module.GetBlob(module.TypeSpec.records[index]), context);
                }
                list.Add(mods);
            }
            return list.ToArray();
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get { return (GenericParameterAttributes)module.GenericParam.records[index].Flags; }
        }

        internal override Type BindTypeParameters(IGenericBinder binder)
        {
            int owner = module.GenericParam.records[index].Owner;
            if ((owner >> 24) == MethodDefTable.Index)
            {
                return binder.BindMethodParameter(this);
            }
            else
            {
                return binder.BindTypeParameter(this);
            }
        }

        internal override bool IsBaked
        {
            get { return true; }
        }
    }
}
