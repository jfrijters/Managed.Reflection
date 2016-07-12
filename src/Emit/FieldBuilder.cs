/*
  The MIT License (MIT) 
  Copyright (C) 2008-2012 Jeroen Frijters
  
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
using Managed.Reflection.Writer;

namespace Managed.Reflection.Emit
{
    public sealed class FieldBuilder : FieldInfo
    {
        private readonly TypeBuilder typeBuilder;
        private readonly string name;
        private readonly int pseudoToken;
        private FieldAttributes attribs;
        private readonly int nameIndex;
        private readonly int signature;
        private readonly FieldSignature fieldSig;

        internal FieldBuilder(TypeBuilder type, string name, Type fieldType, CustomModifiers customModifiers, FieldAttributes attribs)
        {
            this.typeBuilder = type;
            this.name = name;
            this.pseudoToken = type.ModuleBuilder.AllocPseudoToken();
            this.nameIndex = type.ModuleBuilder.Strings.Add(name);
            this.fieldSig = FieldSignature.Create(fieldType, customModifiers);
            ByteBuffer sig = new ByteBuffer(5);
            fieldSig.WriteSig(this.typeBuilder.ModuleBuilder, sig);
            this.signature = this.typeBuilder.ModuleBuilder.Blobs.Add(sig);
            this.attribs = attribs;
            this.typeBuilder.ModuleBuilder.Field.AddVirtualRecord();
        }

        public void SetConstant(object defaultValue)
        {
            attribs |= FieldAttributes.HasDefault;
            typeBuilder.ModuleBuilder.AddConstant(pseudoToken, defaultValue);
        }

        public override object GetRawConstantValue()
        {
            if (!typeBuilder.IsCreated())
            {
                // the .NET FieldBuilder doesn't support this method
                // (since we dont' have a different FieldInfo object after baking, we will support it once we're baked)
                throw new NotSupportedException();
            }
            return typeBuilder.Module.Constant.GetRawConstantValue(typeBuilder.Module, GetCurrentToken());
        }

        public void __SetDataAndRVA(byte[] data)
        {
            SetDataAndRvaImpl(data, typeBuilder.ModuleBuilder.initializedData, 0);
        }

        public void __SetReadOnlyDataAndRVA(byte[] data)
        {
            SetDataAndRvaImpl(data, typeBuilder.ModuleBuilder.methodBodies, unchecked((int)0x80000000));
        }

        private void SetDataAndRvaImpl(byte[] data, ByteBuffer bb, int readonlyMarker)
        {
            attribs |= FieldAttributes.HasFieldRVA;
            FieldRVATable.Record rec = new FieldRVATable.Record();
            bb.Align(8);
            rec.RVA = bb.Position + readonlyMarker;
            rec.Field = pseudoToken;
            typeBuilder.ModuleBuilder.FieldRVA.AddRecord(rec);
            bb.Write(data);
        }

        public override void __GetDataFromRVA(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public override int __FieldRVA
        {
            get { throw new NotImplementedException(); }
        }

        public override bool __TryGetFieldOffset(out int offset)
        {
            int pseudoTokenOrIndex = pseudoToken;
            if (typeBuilder.ModuleBuilder.IsSaved)
            {
                pseudoTokenOrIndex = typeBuilder.ModuleBuilder.ResolvePseudoToken(pseudoToken) & 0xFFFFFF;
            }
            foreach (int i in this.Module.FieldLayout.Filter(pseudoTokenOrIndex))
            {
                offset = this.Module.FieldLayout.records[i].Offset;
                return true;
            }
            offset = 0;
            return false;
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            switch (customBuilder.KnownCA)
            {
                case KnownCA.FieldOffsetAttribute:
                    SetOffset((int)customBuilder.DecodeBlob(this.Module.Assembly).GetConstructorArgument(0));
                    break;
                case KnownCA.MarshalAsAttribute:
                    FieldMarshal.SetMarshalAsAttribute(typeBuilder.ModuleBuilder, pseudoToken, customBuilder);
                    attribs |= FieldAttributes.HasFieldMarshal;
                    break;
                case KnownCA.NonSerializedAttribute:
                    attribs |= FieldAttributes.NotSerialized;
                    break;
                case KnownCA.SpecialNameAttribute:
                    attribs |= FieldAttributes.SpecialName;
                    break;
                default:
                    typeBuilder.ModuleBuilder.SetCustomAttribute(pseudoToken, customBuilder);
                    break;
            }
        }

        public void SetOffset(int iOffset)
        {
            FieldLayoutTable.Record rec = new FieldLayoutTable.Record();
            rec.Offset = iOffset;
            rec.Field = pseudoToken;
            typeBuilder.ModuleBuilder.FieldLayout.AddRecord(rec);
        }

        public override FieldAttributes Attributes
        {
            get { return attribs; }
        }

        public override Type DeclaringType
        {
            get { return typeBuilder.IsModulePseudoType ? null : typeBuilder; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override int MetadataToken
        {
            get { return pseudoToken; }
        }

        public override Module Module
        {
            get { return typeBuilder.Module; }
        }

        public FieldToken GetToken()
        {
            return new FieldToken(pseudoToken);
        }

        internal void WriteFieldRecords(MetadataWriter mw)
        {
            mw.Write((short)attribs);
            mw.WriteStringIndex(nameIndex);
            mw.WriteBlobIndex(signature);
        }

        internal void FixupToken(int token)
        {
            typeBuilder.ModuleBuilder.RegisterTokenFixup(this.pseudoToken, token);
        }

        internal override FieldSignature FieldSignature
        {
            get { return fieldSig; }
        }

        internal override int ImportTo(ModuleBuilder other)
        {
            return other.ImportMethodOrField(typeBuilder, name, fieldSig);
        }

        internal override int GetCurrentToken()
        {
            if (typeBuilder.ModuleBuilder.IsSaved)
            {
                return typeBuilder.ModuleBuilder.ResolvePseudoToken(pseudoToken);
            }
            else
            {
                return pseudoToken;
            }
        }

        internal override bool IsBaked
        {
            get { return typeBuilder.IsBaked; }
        }
    }
}
