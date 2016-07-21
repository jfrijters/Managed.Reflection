/*
  The MIT License (MIT) 
  Copyright (C) 2009-2011 Jeroen Frijters
  
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
    sealed class PropertySignature : Signature
    {
        private CallingConventions callingConvention;
        private readonly Type propertyType;
        private readonly Type[] parameterTypes;
        private readonly PackedCustomModifiers customModifiers;

        internal static PropertySignature Create(CallingConventions callingConvention, Type propertyType, Type[] parameterTypes, PackedCustomModifiers customModifiers)
        {
            return new PropertySignature(callingConvention, propertyType, Util.Copy(parameterTypes), customModifiers);
        }

        private PropertySignature(CallingConventions callingConvention, Type propertyType, Type[] parameterTypes, PackedCustomModifiers customModifiers)
        {
            this.callingConvention = callingConvention;
            this.propertyType = propertyType;
            this.parameterTypes = parameterTypes;
            this.customModifiers = customModifiers;
        }

        public override bool Equals(object obj)
        {
            PropertySignature other = obj as PropertySignature;
            return other != null
                && other.propertyType.Equals(propertyType)
                && other.customModifiers.Equals(customModifiers);
        }

        public override int GetHashCode()
        {
            return propertyType.GetHashCode() ^ customModifiers.GetHashCode();
        }

        internal int ParameterCount
        {
            get { return parameterTypes.Length; }
        }

        internal bool HasThis
        {
            set
            {
                if (value)
                {
                    callingConvention |= CallingConventions.HasThis;
                }
                else
                {
                    callingConvention &= ~CallingConventions.HasThis;
                }
            }
        }

        internal Type PropertyType
        {
            get { return propertyType; }
        }

        internal CustomModifiers GetCustomModifiers()
        {
            return customModifiers.GetReturnTypeCustomModifiers();
        }

        internal PropertySignature ExpandTypeParameters(Type declaringType)
        {
            return new PropertySignature(
                callingConvention,
                propertyType.BindTypeParameters(declaringType),
                BindTypeParameters(declaringType, parameterTypes),
                customModifiers.Bind(declaringType));
        }

        internal override void WriteSig(ModuleBuilder module, ByteBuffer bb)
        {
            byte flags = PROPERTY;
            if ((callingConvention & CallingConventions.HasThis) != 0)
            {
                flags |= HASTHIS;
            }
            if ((callingConvention & CallingConventions.ExplicitThis) != 0)
            {
                flags |= EXPLICITTHIS;
            }
            if ((callingConvention & CallingConventions.VarArgs) != 0)
            {
                flags |= VARARG;
            }
            bb.Write(flags);
            bb.WriteCompressedUInt(parameterTypes == null ? 0 : parameterTypes.Length);
            WriteCustomModifiers(module, bb, customModifiers.GetReturnTypeCustomModifiers());
            WriteType(module, bb, propertyType);
            if (parameterTypes != null)
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    WriteCustomModifiers(module, bb, customModifiers.GetParameterCustomModifiers(i));
                    WriteType(module, bb, parameterTypes[i]);
                }
            }
        }

        internal Type GetParameter(int parameter)
        {
            return parameterTypes[parameter];
        }

        internal CustomModifiers GetParameterCustomModifiers(int parameter)
        {
            return customModifiers.GetParameterCustomModifiers(parameter);
        }

        internal CallingConventions CallingConvention
        {
            get { return callingConvention; }
        }

        internal bool MatchParameterTypes(Type[] types)
        {
            return Util.ArrayEquals(types, parameterTypes);
        }

        internal static PropertySignature ReadSig(ModuleReader module, ByteReader br, IGenericContext context)
        {
            byte flags = br.ReadByte();
            if ((flags & PROPERTY) == 0)
            {
                throw new BadImageFormatException();
            }
            CallingConventions callingConvention = CallingConventions.Standard;
            if ((flags & HASTHIS) != 0)
            {
                callingConvention |= CallingConventions.HasThis;
            }
            if ((flags & EXPLICITTHIS) != 0)
            {
                callingConvention |= CallingConventions.ExplicitThis;
            }
            Type returnType;
            Type[] parameterTypes;
            int paramCount = br.ReadCompressedUInt();
            CustomModifiers[] mods = null;
            PackedCustomModifiers.Pack(ref mods, 0, CustomModifiers.Read(module, br, context), paramCount + 1);
            returnType = ReadRetType(module, br, context);
            parameterTypes = new Type[paramCount];
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                PackedCustomModifiers.Pack(ref mods, i + 1, CustomModifiers.Read(module, br, context), paramCount + 1);
                parameterTypes[i] = ReadParam(module, br, context);
            }
            return new PropertySignature(callingConvention, returnType, parameterTypes, PackedCustomModifiers.Wrap(mods));
        }
    }
}
