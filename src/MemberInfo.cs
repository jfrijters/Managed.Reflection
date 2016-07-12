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
using System.Collections.Generic;

namespace Managed.Reflection
{
    // disable warnings that complain about us having == and != operators without also overriding Equals/GetHashCode,
    // this is intentional because most subtypes use reference equality
#pragma warning disable 660, 661
    public abstract class MemberInfo : ICustomAttributeProvider
    {
        // prevent external subclasses
        internal MemberInfo()
        {
        }

        public abstract string Name { get; }
        public abstract Type DeclaringType { get; }
        public abstract MemberTypes MemberType { get; }

        public virtual Type ReflectedType
        {
            get { return DeclaringType; }
        }

        internal abstract MemberInfo SetReflectedType(Type type);

        public virtual int MetadataToken
        {
            get { throw new NotSupportedException(); }
        }

        public abstract Module Module
        {
            get;
        }

        public virtual bool __IsMissing
        {
            get { return false; }
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

        public static bool operator ==(MemberInfo m1, MemberInfo m2)
        {
            return ReferenceEquals(m1, m2) || (!ReferenceEquals(m1, null) && m1.Equals(m2));
        }

        public static bool operator !=(MemberInfo m1, MemberInfo m2)
        {
            return !(m1 == m2);
        }

        internal abstract int GetCurrentToken();

        internal abstract List<CustomAttributeData> GetPseudoCustomAttributes(Type attributeType);

        internal abstract bool IsBaked { get; }

        internal virtual bool BindingFlagsMatch(BindingFlags flags)
        {
            throw new InvalidOperationException();
        }

        internal virtual bool BindingFlagsMatchInherited(BindingFlags flags)
        {
            throw new InvalidOperationException();
        }

        protected static bool BindingFlagsMatch(bool state, BindingFlags flags, BindingFlags trueFlag, BindingFlags falseFlag)
        {
            return (state && (flags & trueFlag) == trueFlag)
                || (!state && (flags & falseFlag) == falseFlag);
        }

        protected static T SetReflectedType<T>(T member, Type type)
            where T : MemberInfo
        {
            return member == null ? null : (T)member.SetReflectedType(type);
        }

        protected static T[] SetReflectedType<T>(T[] members, Type type)
            where T : MemberInfo
        {
            for (int i = 0; i < members.Length; i++)
            {
                members[i] = SetReflectedType(members[i], type);
            }
            return members;
        }
    }
}
