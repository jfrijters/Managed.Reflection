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
using System;
using System.Collections.Generic;
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Emit
{
    public sealed class PropertyBuilder : PropertyInfo
    {
        private readonly TypeBuilder typeBuilder;
        private readonly string name;
        private PropertyAttributes attributes;
        private PropertySignature sig;
        private MethodBuilder getter;
        private MethodBuilder setter;
        private readonly List<Accessor> accessors = new List<Accessor>();
        private int lazyPseudoToken;
        private bool patchCallingConvention;

        private struct Accessor
        {
            internal short Semantics;
            internal MethodBuilder Method;
        }

        internal PropertyBuilder(TypeBuilder typeBuilder, string name, PropertyAttributes attributes, PropertySignature sig, bool patchCallingConvention)
        {
            this.typeBuilder = typeBuilder;
            this.name = name;
            this.attributes = attributes;
            this.sig = sig;
            this.patchCallingConvention = patchCallingConvention;
        }

        internal override PropertySignature PropertySignature
        {
            get { return sig; }
        }

        public void SetGetMethod(MethodBuilder mdBuilder)
        {
            getter = mdBuilder;
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.Getter;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void SetSetMethod(MethodBuilder mdBuilder)
        {
            setter = mdBuilder;
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.Setter;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.Other;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder.KnownCA == KnownCA.SpecialNameAttribute)
            {
                attributes |= PropertyAttributes.SpecialName;
            }
            else
            {
                if (lazyPseudoToken == 0)
                {
                    lazyPseudoToken = typeBuilder.ModuleBuilder.AllocPseudoToken();
                }
                typeBuilder.ModuleBuilder.SetCustomAttribute(lazyPseudoToken, customBuilder);
            }
        }

        public override object GetRawConstantValue()
        {
            if (lazyPseudoToken != 0)
            {
                return typeBuilder.ModuleBuilder.Constant.GetRawConstantValue(typeBuilder.ModuleBuilder, lazyPseudoToken);
            }
            throw new InvalidOperationException();
        }

        public override PropertyAttributes Attributes
        {
            get { return attributes; }
        }

        public override bool CanRead
        {
            get { return getter != null; }
        }

        public override bool CanWrite
        {
            get { return setter != null; }
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return nonPublic || (getter != null && getter.IsPublic) ? getter : null;
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return nonPublic || (setter != null && setter.IsPublic) ? setter : null;
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (Accessor acc in accessors)
            {
                AddAccessor(list, nonPublic, acc.Method);
            }
            return list.ToArray();
        }

        private static void AddAccessor(List<MethodInfo> list, bool nonPublic, MethodInfo method)
        {
            if (method != null && (nonPublic || method.IsPublic))
            {
                list.Add(method);
            }
        }

        public override Type DeclaringType
        {
            get { return typeBuilder; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override Module Module
        {
            get { return typeBuilder.Module; }
        }

        public void SetConstant(object defaultValue)
        {
            if (lazyPseudoToken == 0)
            {
                lazyPseudoToken = typeBuilder.ModuleBuilder.AllocPseudoToken();
            }
            attributes |= PropertyAttributes.HasDefault;
            typeBuilder.ModuleBuilder.AddConstant(lazyPseudoToken, defaultValue);
        }

        internal void Bake()
        {
            if (patchCallingConvention)
            {
                sig.HasThis = !this.IsStatic;
            }

            PropertyTable.Record rec = new PropertyTable.Record();
            rec.Flags = (short)attributes;
            rec.Name = typeBuilder.ModuleBuilder.Strings.Add(name);
            rec.Type = typeBuilder.ModuleBuilder.GetSignatureBlobIndex(sig);
            int token = 0x17000000 | typeBuilder.ModuleBuilder.Property.AddRecord(rec);

            if (lazyPseudoToken == 0)
            {
                lazyPseudoToken = token;
            }
            else
            {
                typeBuilder.ModuleBuilder.RegisterTokenFixup(lazyPseudoToken, token);
            }

            foreach (Accessor acc in accessors)
            {
                AddMethodSemantics(acc.Semantics, acc.Method.MetadataToken, token);
            }
        }

        private void AddMethodSemantics(short semantics, int methodToken, int propertyToken)
        {
            MethodSemanticsTable.Record rec = new MethodSemanticsTable.Record();
            rec.Semantics = semantics;
            rec.Method = methodToken;
            rec.Association = propertyToken;
            typeBuilder.ModuleBuilder.MethodSemantics.AddRecord(rec);
        }

        internal override bool IsPublic
        {
            get
            {
                foreach (Accessor acc in accessors)
                {
                    if (acc.Method.IsPublic)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal override bool IsNonPrivate
        {
            get
            {
                foreach (Accessor acc in accessors)
                {
                    if ((acc.Method.Attributes & MethodAttributes.MemberAccessMask) > MethodAttributes.Private)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal override bool IsStatic
        {
            get
            {
                foreach (Accessor acc in accessors)
                {
                    if (acc.Method.IsStatic)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal override bool IsBaked
        {
            get { return typeBuilder.IsBaked; }
        }

        internal override int GetCurrentToken()
        {
            if (typeBuilder.ModuleBuilder.IsSaved && ModuleBuilder.IsPseudoToken(lazyPseudoToken))
            {
                return typeBuilder.ModuleBuilder.ResolvePseudoToken(lazyPseudoToken);
            }
            else
            {
                return lazyPseudoToken;
            }
        }
    }
}
