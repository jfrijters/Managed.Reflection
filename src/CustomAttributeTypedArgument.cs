/*
  The MIT License (MIT) 
  Copyright (C) 2009 Jeroen Frijters
  
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

namespace Managed.Reflection
{
    public struct CustomAttributeTypedArgument
    {
        private readonly Type type;
        private readonly object value;

        internal CustomAttributeTypedArgument(Type type, object value)
        {
            this.type = type;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            return this == obj as CustomAttributeTypedArgument?;
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ 77 * (value == null ? 0 : value.GetHashCode());
        }

        public Type ArgumentType
        {
            get { return type; }
        }

        public Object Value
        {
            get { return value; }
        }

        public static bool operator ==(CustomAttributeTypedArgument arg1, CustomAttributeTypedArgument arg2)
        {
            return arg1.type.Equals(arg2.type) && (arg1.value == arg2.value || (arg1.value != null && arg1.value.Equals(arg2.value)));
        }

        public static bool operator !=(CustomAttributeTypedArgument arg1, CustomAttributeTypedArgument arg2)
        {
            return !(arg1 == arg2);
        }
    }
}
