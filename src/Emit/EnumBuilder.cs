/*
  The MIT License (MIT) 
  Copyright (C) 2010 Jeroen Frijters
  
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

namespace Managed.Reflection.Emit
{
    public sealed class EnumBuilder : TypeInfo
    {
        private readonly TypeBuilder typeBuilder;
        private readonly FieldBuilder fieldBuilder;

        internal EnumBuilder(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
            : base(typeBuilder)
        {
            this.typeBuilder = typeBuilder;
            this.fieldBuilder = fieldBuilder;
        }

        internal override TypeName TypeName
        {
            get { return typeBuilder.TypeName; }
        }

        public override string Name
        {
            get { return typeBuilder.Name; }
        }

        public override string FullName
        {
            get { return typeBuilder.FullName; }
        }

        public override Type BaseType
        {
            get { return typeBuilder.BaseType; }
        }

        public override TypeAttributes Attributes
        {
            get { return typeBuilder.Attributes; }
        }

        public override Module Module
        {
            get { return typeBuilder.Module; }
        }

        public FieldBuilder DefineLiteral(string literalName, object literalValue)
        {
            FieldBuilder fb = typeBuilder.DefineField(literalName, typeBuilder, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
            fb.SetConstant(literalValue);
            return fb;
        }

        public Type CreateType()
        {
            return typeBuilder.CreateType();
        }

        public TypeInfo CreateTypeInfo()
        {
            return typeBuilder.CreateTypeInfo();
        }

        public TypeToken TypeToken
        {
            get { return typeBuilder.TypeToken; }
        }

        public FieldBuilder UnderlyingField
        {
            get { return fieldBuilder; }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            typeBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            typeBuilder.SetCustomAttribute(customBuilder);
        }

        public override Type GetEnumUnderlyingType()
        {
            return fieldBuilder.FieldType;
        }

        protected override bool IsValueTypeImpl
        {
            get { return true; }
        }

        internal override bool IsBaked
        {
            get { return typeBuilder.IsBaked; }
        }
    }
}
