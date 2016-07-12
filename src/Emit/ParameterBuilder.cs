/*
  The MIT License (MIT) 
  Copyright (C) 2008 Jeroen Frijters
  
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
using Managed.Reflection.Writer;

namespace Managed.Reflection.Emit
{
    public sealed class ParameterBuilder
    {
        private readonly ModuleBuilder moduleBuilder;
        private short flags;
        private readonly short sequence;
        private readonly int nameIndex;
        private readonly string name;
        private int lazyPseudoToken;

        internal ParameterBuilder(ModuleBuilder moduleBuilder, int sequence, ParameterAttributes attribs, string name)
        {
            this.moduleBuilder = moduleBuilder;
            this.flags = (short)attribs;
            this.sequence = (short)sequence;
            this.nameIndex = name == null ? 0 : moduleBuilder.Strings.Add(name);
            this.name = name;
        }

        internal int PseudoToken
        {
            get
            {
                if (lazyPseudoToken == 0)
                {
                    // we lazily create the token, because if we don't need it we don't want the token fixup cost
                    lazyPseudoToken = moduleBuilder.AllocPseudoToken();
                }
                return lazyPseudoToken;
            }
        }

        public string Name
        {
            get { return name; }
        }

        public int Position
        {
            // note that this differs from ParameterInfo.Position, which is zero based
            get { return sequence; }
        }

        public int Attributes
        {
            get { return flags; }
        }

        public bool IsIn
        {
            get { return (flags & (short)ParameterAttributes.In) != 0; }
        }

        public bool IsOut
        {
            get { return (flags & (short)ParameterAttributes.Out) != 0; }
        }

        public bool IsOptional
        {
            get { return (flags & (short)ParameterAttributes.Optional) != 0; }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public void SetCustomAttribute(CustomAttributeBuilder customAttributeBuilder)
        {
            switch (customAttributeBuilder.KnownCA)
            {
                case KnownCA.InAttribute:
                    flags |= (short)ParameterAttributes.In;
                    break;
                case KnownCA.OutAttribute:
                    flags |= (short)ParameterAttributes.Out;
                    break;
                case KnownCA.OptionalAttribute:
                    flags |= (short)ParameterAttributes.Optional;
                    break;
                case KnownCA.MarshalAsAttribute:
                    FieldMarshal.SetMarshalAsAttribute(moduleBuilder, PseudoToken, customAttributeBuilder);
                    flags |= (short)ParameterAttributes.HasFieldMarshal;
                    break;
                default:
                    moduleBuilder.SetCustomAttribute(PseudoToken, customAttributeBuilder);
                    break;
            }
        }

        public void SetConstant(object defaultValue)
        {
            flags |= (short)ParameterAttributes.HasDefault;
            moduleBuilder.AddConstant(PseudoToken, defaultValue);
        }

        internal void WriteParamRecord(MetadataWriter mw)
        {
            mw.Write(flags);
            mw.Write(sequence);
            mw.WriteStringIndex(nameIndex);
        }

        internal void FixupToken(int parameterToken)
        {
            if (lazyPseudoToken != 0)
            {
                moduleBuilder.RegisterTokenFixup(lazyPseudoToken, parameterToken);
            }
        }
    }
}
