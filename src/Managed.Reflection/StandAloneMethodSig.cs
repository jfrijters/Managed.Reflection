/*
  The MIT License (MIT) 
  Copyright (C) 2010 Jeroen Frijters
  
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
using System.Runtime.InteropServices;

namespace Managed.Reflection
{
    public sealed class __StandAloneMethodSig
    {
        private readonly bool unmanaged;
        private readonly CallingConvention unmanagedCallingConvention;
        private readonly CallingConventions callingConvention;
        private readonly Type returnType;
        private readonly Type[] parameterTypes;
        private readonly Type[] optionalParameterTypes;
        private readonly PackedCustomModifiers customModifiers;

        internal __StandAloneMethodSig(bool unmanaged, CallingConvention unmanagedCallingConvention, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes, PackedCustomModifiers customModifiers)
        {
            this.unmanaged = unmanaged;
            this.unmanagedCallingConvention = unmanagedCallingConvention;
            this.callingConvention = callingConvention;
            this.returnType = returnType;
            this.parameterTypes = parameterTypes;
            this.optionalParameterTypes = optionalParameterTypes;
            this.customModifiers = customModifiers;
        }

        public bool Equals(__StandAloneMethodSig other)
        {
            return other != null
                && other.unmanaged == unmanaged
                && other.unmanagedCallingConvention == unmanagedCallingConvention
                && other.callingConvention == callingConvention
                && other.returnType == returnType
                && Util.ArrayEquals(other.parameterTypes, parameterTypes)
                && Util.ArrayEquals(other.optionalParameterTypes, optionalParameterTypes)
                && other.customModifiers.Equals(customModifiers);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as __StandAloneMethodSig);
        }

        public override int GetHashCode()
        {
            return returnType.GetHashCode()
                ^ Util.GetHashCode(parameterTypes);
        }

        public bool IsUnmanaged
        {
            get { return unmanaged; }
        }

        public CallingConventions CallingConvention
        {
            get { return callingConvention; }
        }

        public CallingConvention UnmanagedCallingConvention
        {
            get { return unmanagedCallingConvention; }
        }

        public Type ReturnType
        {
            get { return returnType; }
        }

        public CustomModifiers GetReturnTypeCustomModifiers()
        {
            return customModifiers.GetReturnTypeCustomModifiers();
        }

        public Type[] ParameterTypes
        {
            get { return Util.Copy(parameterTypes); }
        }

        public Type[] OptionalParameterTypes
        {
            get { return Util.Copy(optionalParameterTypes); }
        }

        public CustomModifiers GetParameterCustomModifiers(int index)
        {
            return customModifiers.GetParameterCustomModifiers(index);
        }

        public bool ContainsMissingType
        {
            get
            {
                return returnType.__ContainsMissingType
                    || Type.ContainsMissingType(parameterTypes)
                    || Type.ContainsMissingType(optionalParameterTypes)
                    || customModifiers.ContainsMissingType;
            }
        }

        internal int ParameterCount
        {
            get { return parameterTypes.Length + optionalParameterTypes.Length; }
        }
    }
}
