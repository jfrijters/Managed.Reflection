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
using System.Runtime.InteropServices;
using System.Text;
using Managed.Reflection.Emit;
using Managed.Reflection.Metadata;
using Managed.Reflection.Reader;
using Managed.Reflection.Writer;

// VarEnum, UnmanagedType.IDispatch and UnmanagedType.SafeArray are obsolete
#pragma warning disable 618

namespace Managed.Reflection
{
    public struct FieldMarshal
    {
        private const UnmanagedType UnmanagedType_CustomMarshaler = (UnmanagedType)0x2c;
        private const UnmanagedType NATIVE_TYPE_MAX = (UnmanagedType)0x50;
        public UnmanagedType UnmanagedType;
        public UnmanagedType? ArraySubType;
        public short? SizeParamIndex;
        public int? SizeConst;
        public VarEnum? SafeArraySubType;
        public Type SafeArrayUserDefinedSubType;
        public int? IidParameterIndex;
        public string MarshalType;
        public string MarshalCookie;
        public Type MarshalTypeRef;

        internal static bool ReadFieldMarshal(Module module, int token, out FieldMarshal fm)
        {
            fm = new FieldMarshal();
            foreach (int i in module.FieldMarshal.Filter(token))
            {
                ByteReader blob = module.GetBlob(module.FieldMarshal.records[i].NativeType);
                fm.UnmanagedType = (UnmanagedType)blob.ReadCompressedUInt();
                if (fm.UnmanagedType == UnmanagedType.LPArray)
                {
                    fm.ArraySubType = (UnmanagedType)blob.ReadCompressedUInt();
                    if (fm.ArraySubType == NATIVE_TYPE_MAX)
                    {
                        fm.ArraySubType = null;
                    }
                    if (blob.Length != 0)
                    {
                        fm.SizeParamIndex = (short)blob.ReadCompressedUInt();
                        if (blob.Length != 0)
                        {
                            fm.SizeConst = blob.ReadCompressedUInt();
                            if (blob.Length != 0 && blob.ReadCompressedUInt() == 0)
                            {
                                fm.SizeParamIndex = null;
                            }
                        }
                    }
                }
                else if (fm.UnmanagedType == UnmanagedType.SafeArray)
                {
                    if (blob.Length != 0)
                    {
                        fm.SafeArraySubType = (VarEnum)blob.ReadCompressedUInt();
                        if (blob.Length != 0)
                        {
                            fm.SafeArrayUserDefinedSubType = ReadType(module, blob);
                        }
                    }
                }
                else if (fm.UnmanagedType == UnmanagedType.ByValArray)
                {
                    fm.SizeConst = blob.ReadCompressedUInt();
                    if (blob.Length != 0)
                    {
                        fm.ArraySubType = (UnmanagedType)blob.ReadCompressedUInt();
                    }
                }
                else if (fm.UnmanagedType == UnmanagedType.ByValTStr)
                {
                    fm.SizeConst = blob.ReadCompressedUInt();
                }
                else if (fm.UnmanagedType == UnmanagedType.Interface
                    || fm.UnmanagedType == UnmanagedType.IDispatch
                    || fm.UnmanagedType == UnmanagedType.IUnknown)
                {
                    if (blob.Length != 0)
                    {
                        fm.IidParameterIndex = blob.ReadCompressedUInt();
                    }
                }
                else if (fm.UnmanagedType == UnmanagedType_CustomMarshaler)
                {
                    blob.ReadCompressedUInt();
                    blob.ReadCompressedUInt();
                    fm.MarshalType = ReadString(blob);
                    fm.MarshalCookie = ReadString(blob);

                    TypeNameParser parser = TypeNameParser.Parse(fm.MarshalType, false);
                    if (!parser.Error)
                    {
                        fm.MarshalTypeRef = parser.GetType(module.universe, module, false, fm.MarshalType, false, false);
                    }
                }
                return true;
            }
            return false;
        }

        internal static void SetMarshalAsAttribute(ModuleBuilder module, int token, CustomAttributeBuilder attribute)
        {
            attribute = attribute.DecodeBlob(module.Assembly);
            FieldMarshalTable.Record rec = new FieldMarshalTable.Record();
            rec.Parent = token;
            rec.NativeType = WriteMarshallingDescriptor(module, attribute);
            module.FieldMarshal.AddRecord(rec);
        }

        private static int WriteMarshallingDescriptor(ModuleBuilder module, CustomAttributeBuilder attribute)
        {
            UnmanagedType unmanagedType;
            object val = attribute.GetConstructorArgument(0);
            if (val is short)
            {
                unmanagedType = (UnmanagedType)(short)val;
            }
            else if (val is int)
            {
                unmanagedType = (UnmanagedType)(int)val;
            }
            else
            {
                unmanagedType = (UnmanagedType)val;
            }

            ByteBuffer bb = new ByteBuffer(5);
            bb.WriteCompressedUInt((int)unmanagedType);

            if (unmanagedType == UnmanagedType.LPArray)
            {
                UnmanagedType arraySubType = attribute.GetFieldValue<UnmanagedType>("ArraySubType") ?? NATIVE_TYPE_MAX;
                bb.WriteCompressedUInt((int)arraySubType);
                int? sizeParamIndex = attribute.GetFieldValue<short>("SizeParamIndex");
                int? sizeConst = attribute.GetFieldValue<int>("SizeConst");
                if (sizeParamIndex != null)
                {
                    bb.WriteCompressedUInt(sizeParamIndex.Value);
                    if (sizeConst != null)
                    {
                        bb.WriteCompressedUInt(sizeConst.Value);
                        bb.WriteCompressedUInt(1); // flag that says that SizeParamIndex was specified
                    }
                }
                else if (sizeConst != null)
                {
                    bb.WriteCompressedUInt(0); // SizeParamIndex
                    bb.WriteCompressedUInt(sizeConst.Value);
                    bb.WriteCompressedUInt(0); // flag that says that SizeParamIndex was not specified
                }
            }
            else if (unmanagedType == UnmanagedType.SafeArray)
            {
                VarEnum? safeArraySubType = attribute.GetFieldValue<VarEnum>("SafeArraySubType");
                if (safeArraySubType != null)
                {
                    bb.WriteCompressedUInt((int)safeArraySubType);
                    Type safeArrayUserDefinedSubType = (Type)attribute.GetFieldValue("SafeArrayUserDefinedSubType");
                    if (safeArrayUserDefinedSubType != null)
                    {
                        WriteType(module, bb, safeArrayUserDefinedSubType);
                    }
                }
            }
            else if (unmanagedType == UnmanagedType.ByValArray)
            {
                bb.WriteCompressedUInt(attribute.GetFieldValue<int>("SizeConst") ?? 1);
                UnmanagedType? arraySubType = attribute.GetFieldValue<UnmanagedType>("ArraySubType");
                if (arraySubType != null)
                {
                    bb.WriteCompressedUInt((int)arraySubType);
                }
            }
            else if (unmanagedType == UnmanagedType.ByValTStr)
            {
                bb.WriteCompressedUInt(attribute.GetFieldValue<int>("SizeConst").Value);
            }
            else if (unmanagedType == UnmanagedType.Interface
                || unmanagedType == UnmanagedType.IDispatch
                || unmanagedType == UnmanagedType.IUnknown)
            {
                int? iidParameterIndex = attribute.GetFieldValue<int>("IidParameterIndex");
                if (iidParameterIndex != null)
                {
                    bb.WriteCompressedUInt(iidParameterIndex.Value);
                }
            }
            else if (unmanagedType == UnmanagedType_CustomMarshaler)
            {
                bb.WriteCompressedUInt(0);
                bb.WriteCompressedUInt(0);
                string marshalType = (string)attribute.GetFieldValue("MarshalType");
                if (marshalType != null)
                {
                    WriteString(bb, marshalType);
                }
                else
                {
                    WriteType(module, bb, (Type)attribute.GetFieldValue("MarshalTypeRef"));
                }
                WriteString(bb, (string)attribute.GetFieldValue("MarshalCookie") ?? "");
            }

            return module.Blobs.Add(bb);
        }

        private static Type ReadType(Module module, ByteReader br)
        {
            string str = ReadString(br);
            if (str == "")
            {
                return null;
            }
            return module.Assembly.GetType(str) ?? module.universe.GetType(str, true);
        }

        private static void WriteType(Module module, ByteBuffer bb, Type type)
        {
            WriteString(bb, type.Assembly == module.Assembly ? type.FullName : type.AssemblyQualifiedName);
        }

        private static string ReadString(ByteReader br)
        {
            return Encoding.UTF8.GetString(br.ReadBytes(br.ReadCompressedUInt()));
        }

        private static void WriteString(ByteBuffer bb, string str)
        {
            byte[] buf = Encoding.UTF8.GetBytes(str);
            bb.WriteCompressedUInt(buf.Length);
            bb.Write(buf);
        }
    }
}
