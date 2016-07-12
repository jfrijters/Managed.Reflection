/*
  The MIT License (MIT) 
  Copyright (C) 2008-2011 Jeroen Frijters
  
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

namespace Managed.Reflection.Emit
{
    public sealed class ConstructorBuilder : ConstructorInfo
    {
        private readonly MethodBuilder methodBuilder;

        internal ConstructorBuilder(MethodBuilder mb)
        {
            this.methodBuilder = mb;
        }

        public override bool Equals(object obj)
        {
            ConstructorBuilder other = obj as ConstructorBuilder;
            return other != null && other.methodBuilder.Equals(methodBuilder);
        }

        public override int GetHashCode()
        {
            return methodBuilder.GetHashCode();
        }

        public void __SetSignature(Type returnType, CustomModifiers returnTypeCustomModifiers, Type[] parameterTypes, CustomModifiers[] parameterTypeCustomModifiers)
        {
            methodBuilder.__SetSignature(returnType, returnTypeCustomModifiers, parameterTypes, parameterTypeCustomModifiers);
        }

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string strParamName)
        {
            return methodBuilder.DefineParameter(position, attributes, strParamName);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            methodBuilder.SetCustomAttribute(customBuilder);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            methodBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        public void __AddDeclarativeSecurity(CustomAttributeBuilder customBuilder)
        {
            methodBuilder.__AddDeclarativeSecurity(customBuilder);
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
            methodBuilder.SetImplementationFlags(attributes);
        }

        public ILGenerator GetILGenerator()
        {
            return methodBuilder.GetILGenerator();
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            return methodBuilder.GetILGenerator(streamSize);
        }

        public void __ReleaseILGenerator()
        {
            methodBuilder.__ReleaseILGenerator();
        }

        public Type ReturnType
        {
            get { return methodBuilder.ReturnType; }
        }

        public Module GetModule()
        {
            return methodBuilder.GetModule();
        }

        public MethodToken GetToken()
        {
            return methodBuilder.GetToken();
        }

        public bool InitLocals
        {
            get { return methodBuilder.InitLocals; }
            set { methodBuilder.InitLocals = value; }
        }

        public void SetMethodBody(byte[] il, int maxStack, byte[] localSignature, IEnumerable<ExceptionHandler> exceptionHandlers, IEnumerable<int> tokenFixups)
        {
            methodBuilder.SetMethodBody(il, maxStack, localSignature, exceptionHandlers, tokenFixups);
        }

        internal override MethodInfo GetMethodInfo()
        {
            return methodBuilder;
        }

        internal override MethodInfo GetMethodOnTypeDefinition()
        {
            return methodBuilder;
        }
    }
}
