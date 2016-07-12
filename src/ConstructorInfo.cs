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
using System.Collections.Generic;
using System.Diagnostics;

namespace Managed.Reflection
{
    public abstract class ConstructorInfo : MethodBase
    {
        // prevent external subclasses
        internal ConstructorInfo()
        {
        }

        public sealed override string ToString()
        {
            return GetMethodInfo().ToString();
        }

        public static readonly string ConstructorName = ".ctor";
        public static readonly string TypeConstructorName = ".cctor";

        internal abstract MethodInfo GetMethodInfo();

        internal override MethodBase BindTypeParameters(Type type)
        {
            return new ConstructorInfoImpl((MethodInfo)GetMethodInfo().BindTypeParameters(type));
        }

        public sealed override MethodBase __GetMethodOnTypeDefinition()
        {
            return new ConstructorInfoImpl((MethodInfo)GetMethodInfo().__GetMethodOnTypeDefinition());
        }

        public sealed override MemberTypes MemberType
        {
            get { return MemberTypes.Constructor; }
        }

        public sealed override int __MethodRVA
        {
            get { return GetMethodInfo().__MethodRVA; }
        }

        public sealed override bool ContainsGenericParameters
        {
            get { return GetMethodInfo().ContainsGenericParameters; }
        }

        public ParameterInfo __ReturnParameter
        {
            get { return new ParameterInfoWrapper(this, GetMethodInfo().ReturnParameter); }
        }

        public sealed override ParameterInfo[] GetParameters()
        {
            ParameterInfo[] parameters = GetMethodInfo().GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = new ParameterInfoWrapper(this, parameters[i]);
            }
            return parameters;
        }

        public sealed override CallingConventions CallingConvention
        {
            get { return GetMethodInfo().CallingConvention; }
        }

        public sealed override MethodAttributes Attributes
        {
            get { return GetMethodInfo().Attributes; }
        }

        public sealed override MethodImplAttributes GetMethodImplementationFlags()
        {
            return GetMethodInfo().GetMethodImplementationFlags();
        }

        public sealed override Type DeclaringType
        {
            get { return GetMethodInfo().DeclaringType; }
        }

        public sealed override string Name
        {
            get { return GetMethodInfo().Name; }
        }

        public sealed override int MetadataToken
        {
            get { return GetMethodInfo().MetadataToken; }
        }

        public sealed override Module Module
        {
            get { return GetMethodInfo().Module; }
        }

        public sealed override MethodBody GetMethodBody()
        {
            return GetMethodInfo().GetMethodBody();
        }

        public sealed override bool __IsMissing
        {
            get { return GetMethodInfo().__IsMissing; }
        }

        internal sealed override int ParameterCount
        {
            get { return GetMethodInfo().ParameterCount; }
        }

        internal sealed override MemberInfo SetReflectedType(Type type)
        {
            return new ConstructorInfoWithReflectedType(type, this);
        }

        internal sealed override int GetCurrentToken()
        {
            return GetMethodInfo().GetCurrentToken();
        }

        internal sealed override List<CustomAttributeData> GetPseudoCustomAttributes(Type attributeType)
        {
            return GetMethodInfo().GetPseudoCustomAttributes(attributeType);
        }

        internal sealed override bool IsBaked
        {
            get { return GetMethodInfo().IsBaked; }
        }

        internal sealed override MethodSignature MethodSignature
        {
            get { return GetMethodInfo().MethodSignature; }
        }

        internal sealed override int ImportTo(Emit.ModuleBuilder module)
        {
            return GetMethodInfo().ImportTo(module);
        }
    }

    sealed class ConstructorInfoImpl : ConstructorInfo
    {
        private readonly MethodInfo method;

        internal ConstructorInfoImpl(MethodInfo method)
        {
            this.method = method;
        }

        public override bool Equals(object obj)
        {
            ConstructorInfoImpl other = obj as ConstructorInfoImpl;
            return other != null && other.method.Equals(method);
        }

        public override int GetHashCode()
        {
            return method.GetHashCode();
        }

        internal override MethodInfo GetMethodInfo()
        {
            return method;
        }

        internal override MethodInfo GetMethodOnTypeDefinition()
        {
            return method.GetMethodOnTypeDefinition();
        }
    }

    sealed class ConstructorInfoWithReflectedType : ConstructorInfo
    {
        private readonly Type reflectedType;
        private readonly ConstructorInfo ctor;

        internal ConstructorInfoWithReflectedType(Type reflectedType, ConstructorInfo ctor)
        {
            Debug.Assert(reflectedType != ctor.DeclaringType);
            this.reflectedType = reflectedType;
            this.ctor = ctor;
        }

        public override bool Equals(object obj)
        {
            ConstructorInfoWithReflectedType other = obj as ConstructorInfoWithReflectedType;
            return other != null
                && other.reflectedType == reflectedType
                && other.ctor == ctor;
        }

        public override int GetHashCode()
        {
            return reflectedType.GetHashCode() ^ ctor.GetHashCode();
        }

        public override Type ReflectedType
        {
            get { return reflectedType; }
        }

        internal override MethodInfo GetMethodInfo()
        {
            return ctor.GetMethodInfo();
        }

        internal override MethodInfo GetMethodOnTypeDefinition()
        {
            return ctor.GetMethodOnTypeDefinition();
        }
    }
}
