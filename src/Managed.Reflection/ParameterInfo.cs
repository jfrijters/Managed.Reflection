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
using System.Collections.Generic;

namespace Managed.Reflection
{
    public abstract class ParameterInfo : ICustomAttributeProvider
    {
        // prevent external subclasses
        internal ParameterInfo()
        {
        }

        public sealed override bool Equals(object obj)
        {
            ParameterInfo other = obj as ParameterInfo;
            return other != null && other.Member == this.Member && other.Position == this.Position;
        }

        public sealed override int GetHashCode()
        {
            return this.Member.GetHashCode() * 1777 + this.Position;
        }

        public static bool operator ==(ParameterInfo p1, ParameterInfo p2)
        {
            return ReferenceEquals(p1, p2) || (!ReferenceEquals(p1, null) && p1.Equals(p2));
        }

        public static bool operator !=(ParameterInfo p1, ParameterInfo p2)
        {
            return !(p1 == p2);
        }

        public abstract string Name { get; }
        public abstract Type ParameterType { get; }
        public abstract ParameterAttributes Attributes { get; }
        public abstract int Position { get; }
        public abstract object RawDefaultValue { get; }
        public abstract CustomModifiers __GetCustomModifiers();
        public abstract bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal);
        public abstract MemberInfo Member { get; }
        public abstract int MetadataToken { get; }
        internal abstract Module Module { get; }

        public Type[] GetOptionalCustomModifiers()
        {
            return __GetCustomModifiers().GetOptional();
        }

        public Type[] GetRequiredCustomModifiers()
        {
            return __GetCustomModifiers().GetRequired();
        }

        public bool IsIn
        {
            get { return (Attributes & ParameterAttributes.In) != 0; }
        }

        public bool IsOut
        {
            get { return (Attributes & ParameterAttributes.Out) != 0; }
        }

        public bool IsLcid
        {
            get { return (Attributes & ParameterAttributes.Lcid) != 0; }
        }

        public bool IsRetval
        {
            get { return (Attributes & ParameterAttributes.Retval) != 0; }
        }

        public bool IsOptional
        {
            get { return (Attributes & ParameterAttributes.Optional) != 0; }
        }

        public bool HasDefaultValue
        {
            get { return (Attributes & ParameterAttributes.HasDefault) != 0; }
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
    }

    sealed class ParameterInfoWrapper : ParameterInfo
    {
        private readonly MemberInfo member;
        private readonly ParameterInfo forward;

        internal ParameterInfoWrapper(MemberInfo member, ParameterInfo forward)
        {
            this.member = member;
            this.forward = forward;
        }

        public override string Name
        {
            get { return forward.Name; }
        }

        public override Type ParameterType
        {
            get { return forward.ParameterType; }
        }

        public override ParameterAttributes Attributes
        {
            get { return forward.Attributes; }
        }

        public override int Position
        {
            get { return forward.Position; }
        }

        public override object RawDefaultValue
        {
            get { return forward.RawDefaultValue; }
        }

        public override CustomModifiers __GetCustomModifiers()
        {
            return forward.__GetCustomModifiers();
        }

        public override bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
        {
            return forward.__TryGetFieldMarshal(out fieldMarshal);
        }

        public override MemberInfo Member
        {
            get { return member; }
        }

        public override int MetadataToken
        {
            get { return forward.MetadataToken; }
        }

        internal override Module Module
        {
            get { return member.Module; }
        }
    }
}
