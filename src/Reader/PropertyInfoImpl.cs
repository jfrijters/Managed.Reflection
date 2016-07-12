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
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Reader
{
    sealed class PropertyInfoImpl : PropertyInfo
    {
        private readonly ModuleReader module;
        private readonly Type declaringType;
        private readonly int index;
        private PropertySignature sig;
        private bool isPublic;
        private bool isNonPrivate;
        private bool isStatic;
        private bool flagsCached;

        internal PropertyInfoImpl(ModuleReader module, Type declaringType, int index)
        {
            this.module = module;
            this.declaringType = declaringType;
            this.index = index;
        }

        public override bool Equals(object obj)
        {
            PropertyInfoImpl other = obj as PropertyInfoImpl;
            return other != null && other.DeclaringType == declaringType && other.index == index;
        }

        public override int GetHashCode()
        {
            return declaringType.GetHashCode() * 77 + index;
        }

        internal override PropertySignature PropertySignature
        {
            get
            {
                if (sig == null)
                {
                    sig = PropertySignature.ReadSig(module, module.GetBlob(module.Property.records[index].Type), declaringType);
                }
                return sig;
            }
        }

        public override PropertyAttributes Attributes
        {
            get { return (PropertyAttributes)module.Property.records[index].Flags; }
        }

        public override object GetRawConstantValue()
        {
            return module.Constant.GetRawConstantValue(module, this.MetadataToken);
        }

        public override bool CanRead
        {
            get { return GetGetMethod(true) != null; }
        }

        public override bool CanWrite
        {
            get { return GetSetMethod(true) != null; }
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return module.MethodSemantics.GetMethod(module, this.MetadataToken, nonPublic, MethodSemanticsTable.Getter);
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return module.MethodSemantics.GetMethod(module, this.MetadataToken, nonPublic, MethodSemanticsTable.Setter);
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return module.MethodSemantics.GetMethods(module, this.MetadataToken, nonPublic, MethodSemanticsTable.Getter | MethodSemanticsTable.Setter | MethodSemanticsTable.Other);
        }

        public override Type DeclaringType
        {
            get { return declaringType; }
        }

        public override Module Module
        {
            get { return module; }
        }

        public override int MetadataToken
        {
            get { return (PropertyTable.Index << 24) + index + 1; }
        }

        public override string Name
        {
            get { return module.GetString(module.Property.records[index].Name); }
        }

        internal override bool IsPublic
        {
            get
            {
                if (!flagsCached)
                {
                    ComputeFlags();
                }
                return isPublic;
            }
        }

        internal override bool IsNonPrivate
        {
            get
            {
                if (!flagsCached)
                {
                    ComputeFlags();
                }
                return isNonPrivate;
            }
        }

        internal override bool IsStatic
        {
            get
            {
                if (!flagsCached)
                {
                    ComputeFlags();
                }
                return isStatic;
            }
        }

        private void ComputeFlags()
        {
            module.MethodSemantics.ComputeFlags(module, this.MetadataToken, out isPublic, out isNonPrivate, out isStatic);
            flagsCached = true;
        }

        internal override bool IsBaked
        {
            get { return true; }
        }

        internal override int GetCurrentToken()
        {
            return this.MetadataToken;
        }
    }
}
