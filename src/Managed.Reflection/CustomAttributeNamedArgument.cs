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

namespace Managed.Reflection
{
    public struct CustomAttributeNamedArgument
    {
        private readonly MemberInfo member;
        private readonly CustomAttributeTypedArgument value;

        internal CustomAttributeNamedArgument(MemberInfo member, CustomAttributeTypedArgument value)
        {
            this.member = member;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            return this == obj as CustomAttributeNamedArgument?;
        }

        public override int GetHashCode()
        {
            return member.GetHashCode() ^ 53 * value.GetHashCode();
        }

        public MemberInfo MemberInfo
        {
            get { return member; }
        }

        public CustomAttributeTypedArgument TypedValue
        {
            get { return value; }
        }

        public bool IsField
        {
            get { return member.MemberType == MemberTypes.Field; }
        }

        public string MemberName
        {
            get { return member.Name; }
        }

        public static bool operator ==(CustomAttributeNamedArgument arg1, CustomAttributeNamedArgument arg2)
        {
            return arg1.member.Equals(arg2.member) && arg1.value == arg2.value;
        }

        public static bool operator !=(CustomAttributeNamedArgument arg1, CustomAttributeNamedArgument arg2)
        {
            return !(arg1 == arg2);
        }
    }
}
