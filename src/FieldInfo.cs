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
using System.Diagnostics;

namespace Managed.Reflection
{
    public abstract class FieldInfo : MemberInfo
    {
        // prevent external subclasses
        internal FieldInfo()
        {
        }

        public sealed override MemberTypes MemberType
        {
            get { return MemberTypes.Field; }
        }

        public abstract FieldAttributes Attributes { get; }
        public abstract void __GetDataFromRVA(byte[] data, int offset, int length);
        public abstract int __FieldRVA { get; }
        public abstract Object GetRawConstantValue();
        internal abstract FieldSignature FieldSignature { get; }

        public Type FieldType
        {
            get { return this.FieldSignature.FieldType; }
        }

        public CustomModifiers __GetCustomModifiers()
        {
            return this.FieldSignature.GetCustomModifiers();
        }

        public Type[] GetOptionalCustomModifiers()
        {
            return __GetCustomModifiers().GetOptional();
        }

        public Type[] GetRequiredCustomModifiers()
        {
            return __GetCustomModifiers().GetRequired();
        }

        public bool IsStatic
        {
            get { return (Attributes & FieldAttributes.Static) != 0; }
        }

        public bool IsLiteral
        {
            get { return (Attributes & FieldAttributes.Literal) != 0; }
        }

        public bool IsInitOnly
        {
            get { return (Attributes & FieldAttributes.InitOnly) != 0; }
        }

        public bool IsNotSerialized
        {
            get { return (Attributes & FieldAttributes.NotSerialized) != 0; }
        }

        public bool IsSpecialName
        {
            get { return (Attributes & FieldAttributes.SpecialName) != 0; }
        }

        public bool IsPublic
        {
            get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public; }
        }

        public bool IsPrivate
        {
            get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private; }
        }

        public bool IsFamily
        {
            get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family; }
        }

        public bool IsFamilyOrAssembly
        {
            get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem; }
        }

        public bool IsAssembly
        {
            get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly; }
        }

        public bool IsFamilyAndAssembly
        {
            get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem; }
        }

        public bool IsPinvokeImpl
        {
            get { return (Attributes & FieldAttributes.PinvokeImpl) != 0; }
        }

        public virtual FieldInfo __GetFieldOnTypeDefinition()
        {
            return this;
        }

        public abstract bool __TryGetFieldOffset(out int offset);

        public bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
        {
            return FieldMarshal.ReadFieldMarshal(this.Module, GetCurrentToken(), out fieldMarshal);
        }

        internal abstract int ImportTo(Emit.ModuleBuilder module);

        internal virtual FieldInfo BindTypeParameters(Type type)
        {
            return new GenericFieldInstance(this.DeclaringType.BindTypeParameters(type), this);
        }

        internal sealed override bool BindingFlagsMatch(BindingFlags flags)
        {
            return BindingFlagsMatch(IsPublic, flags, BindingFlags.Public, BindingFlags.NonPublic)
                && BindingFlagsMatch(IsStatic, flags, BindingFlags.Static, BindingFlags.Instance);
        }

        internal sealed override bool BindingFlagsMatchInherited(BindingFlags flags)
        {
            return (Attributes & FieldAttributes.FieldAccessMask) > FieldAttributes.Private
                && BindingFlagsMatch(IsPublic, flags, BindingFlags.Public, BindingFlags.NonPublic)
                && BindingFlagsMatch(IsStatic, flags, BindingFlags.Static | BindingFlags.FlattenHierarchy, BindingFlags.Instance);
        }

        internal sealed override MemberInfo SetReflectedType(Type type)
        {
            return new FieldInfoWithReflectedType(type, this);
        }

        internal sealed override List<CustomAttributeData> GetPseudoCustomAttributes(Type attributeType)
        {
            Module module = this.Module;
            List<CustomAttributeData> list = new List<CustomAttributeData>();
            if (attributeType == null || attributeType.IsAssignableFrom(module.universe.System_Runtime_InteropServices_MarshalAsAttribute))
            {
                FieldMarshal spec;
                if (__TryGetFieldMarshal(out spec))
                {
                    list.Add(CustomAttributeData.CreateMarshalAsPseudoCustomAttribute(module, spec));
                }
            }
            if (attributeType == null || attributeType.IsAssignableFrom(module.universe.System_Runtime_InteropServices_FieldOffsetAttribute))
            {
                int offset;
                if (__TryGetFieldOffset(out offset))
                {
                    list.Add(CustomAttributeData.CreateFieldOffsetPseudoCustomAttribute(module, offset));
                }
            }
            return list;
        }
    }

    sealed class FieldInfoWithReflectedType : FieldInfo
    {
        private readonly Type reflectedType;
        private readonly FieldInfo field;

        internal FieldInfoWithReflectedType(Type reflectedType, FieldInfo field)
        {
            Debug.Assert(reflectedType != field.DeclaringType);
            this.reflectedType = reflectedType;
            this.field = field;
        }

        public override FieldAttributes Attributes
        {
            get { return field.Attributes; }
        }

        public override void __GetDataFromRVA(byte[] data, int offset, int length)
        {
            field.__GetDataFromRVA(data, offset, length);
        }

        public override int __FieldRVA
        {
            get { return field.__FieldRVA; }
        }

        public override bool __TryGetFieldOffset(out int offset)
        {
            return field.__TryGetFieldOffset(out offset);
        }

        public override Object GetRawConstantValue()
        {
            return field.GetRawConstantValue();
        }

        internal override FieldSignature FieldSignature
        {
            get { return field.FieldSignature; }
        }

        public override FieldInfo __GetFieldOnTypeDefinition()
        {
            return field.__GetFieldOnTypeDefinition();
        }

        internal override int ImportTo(Emit.ModuleBuilder module)
        {
            return field.ImportTo(module);
        }

        internal override FieldInfo BindTypeParameters(Type type)
        {
            return field.BindTypeParameters(type);
        }

        public override bool __IsMissing
        {
            get { return field.__IsMissing; }
        }

        public override Type DeclaringType
        {
            get { return field.DeclaringType; }
        }

        public override Type ReflectedType
        {
            get { return reflectedType; }
        }

        public override bool Equals(object obj)
        {
            FieldInfoWithReflectedType other = obj as FieldInfoWithReflectedType;
            return other != null
                && other.reflectedType == reflectedType
                && other.field == field;
        }

        public override int GetHashCode()
        {
            return reflectedType.GetHashCode() ^ field.GetHashCode();
        }

        public override int MetadataToken
        {
            get { return field.MetadataToken; }
        }

        public override Module Module
        {
            get { return field.Module; }
        }

        public override string Name
        {
            get { return field.Name; }
        }

        public override string ToString()
        {
            return field.ToString();
        }

        internal override int GetCurrentToken()
        {
            return field.GetCurrentToken();
        }

        internal override bool IsBaked
        {
            get { return field.IsBaked; }
        }
    }
}
