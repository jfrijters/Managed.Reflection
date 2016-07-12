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
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Reader
{
    sealed class FieldDefImpl : FieldInfo
    {
        private readonly ModuleReader module;
        private readonly TypeDefImpl declaringType;
        private readonly int index;
        private FieldSignature lazyFieldSig;

        internal FieldDefImpl(ModuleReader module, TypeDefImpl declaringType, int index)
        {
            this.module = module;
            this.declaringType = declaringType;
            this.index = index;
        }

        public override FieldAttributes Attributes
        {
            get { return (FieldAttributes)module.Field.records[index].Flags; }
        }

        public override Type DeclaringType
        {
            get { return declaringType.IsModulePseudoType ? null : declaringType; }
        }

        public override string Name
        {
            get { return module.GetString(module.Field.records[index].Name); }
        }

        public override string ToString()
        {
            return this.FieldType.Name + " " + this.Name;
        }

        public override Module Module
        {
            get { return module; }
        }

        public override int MetadataToken
        {
            get { return (FieldTable.Index << 24) + index + 1; }
        }

        public override object GetRawConstantValue()
        {
            return module.Constant.GetRawConstantValue(module, this.MetadataToken);
        }

        public override void __GetDataFromRVA(byte[] data, int offset, int length)
        {
            int rva = this.__FieldRVA;
            if (rva == 0)
            {
                // C++ assemblies can have fields that have an RVA that is zero
                Array.Clear(data, offset, length);
                return;
            }
            module.__ReadDataFromRVA(rva, data, offset, length);
        }

        public override int __FieldRVA
        {
            get
            {
                foreach (int i in module.FieldRVA.Filter(index + 1))
                {
                    return module.FieldRVA.records[i].RVA;
                }
                throw new InvalidOperationException();
            }
        }

        public override bool __TryGetFieldOffset(out int offset)
        {
            foreach (int i in this.Module.FieldLayout.Filter(index + 1))
            {
                offset = this.Module.FieldLayout.records[i].Offset;
                return true;
            }
            offset = 0;
            return false;
        }

        internal override FieldSignature FieldSignature
        {
            get { return lazyFieldSig ?? (lazyFieldSig = FieldSignature.ReadSig(module, module.GetBlob(module.Field.records[index].Signature), declaringType)); }
        }

        internal override int ImportTo(Emit.ModuleBuilder module)
        {
            return module.ImportMethodOrField(declaringType, this.Name, this.FieldSignature);
        }

        internal override int GetCurrentToken()
        {
            return this.MetadataToken;
        }

        internal override bool IsBaked
        {
            get { return true; }
        }
    }
}
