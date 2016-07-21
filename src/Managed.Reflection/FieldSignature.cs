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
using Managed.Reflection.Emit;
using Managed.Reflection.Reader;
using Managed.Reflection.Writer;

namespace Managed.Reflection
{
    sealed class FieldSignature : Signature
    {
        private readonly Type fieldType;
        private readonly CustomModifiers mods;

        internal static FieldSignature Create(Type fieldType, CustomModifiers customModifiers)
        {
            return new FieldSignature(fieldType, customModifiers);
        }

        private FieldSignature(Type fieldType, CustomModifiers mods)
        {
            this.fieldType = fieldType;
            this.mods = mods;
        }

        public override bool Equals(object obj)
        {
            FieldSignature other = obj as FieldSignature;
            return other != null
                && other.fieldType.Equals(fieldType)
                && other.mods.Equals(mods);
        }

        public override int GetHashCode()
        {
            return fieldType.GetHashCode() ^ mods.GetHashCode();
        }

        internal Type FieldType
        {
            get { return fieldType; }
        }

        internal CustomModifiers GetCustomModifiers()
        {
            return mods;
        }

        internal FieldSignature ExpandTypeParameters(Type declaringType)
        {
            return new FieldSignature(
                fieldType.BindTypeParameters(declaringType),
                mods.Bind(declaringType));
        }

        internal static FieldSignature ReadSig(ModuleReader module, ByteReader br, IGenericContext context)
        {
            if (br.ReadByte() != FIELD)
            {
                throw new BadImageFormatException();
            }
            CustomModifiers mods = CustomModifiers.Read(module, br, context);
            Type fieldType = ReadType(module, br, context);
            return new FieldSignature(fieldType, mods);
        }

        internal override void WriteSig(ModuleBuilder module, ByteBuffer bb)
        {
            bb.Write(FIELD);
            WriteCustomModifiers(module, bb, mods);
            WriteType(module, bb, fieldType);
        }
    }
}
