/*
  The MIT License (MIT) 
  Copyright (C) 2008-2016 Jeroen Frijters
  
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
using System.IO;
using Managed.Reflection.Emit;
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Writer
{
    abstract class MetadataWriter : MetadataRW
    {
        private readonly Stream stream;
        private readonly byte[] buffer = new byte[8];

        internal MetadataWriter(Stream stream, Table[] tables, bool bigStrings, bool bigGuids, bool bigBlobs)
            : base(tables, bigStrings, bigGuids, bigBlobs)
        {
            this.stream = stream;
        }

        internal abstract Table[] GetTables();

        internal abstract int MDStreamVersion { get; }

        internal uint Position
        {
            get { return (uint)stream.Position; }
        }

        internal void WriteMetadata(string version, params Heap[] streams)
        {
            Write(0x424A5342);              // Signature ("BSJB")
            Write((ushort)1);               // MajorVersion
            Write((ushort)1);               // MinorVersion
            Write(0);                       // Reserved
            var v = StringToPaddedUTF8(version);
            Write(v.Length);                // Length
            Write(v);
            Write((ushort)0);               // Flags
            Write((ushort)streams.Length);  // Streams

            int offset = GetHeaderLength(version, streams);

            // Streams
            foreach (var stream in streams)
            {
                Write(offset);               // Offset
                Write(stream.Length);        // Size
                Write(StringToPaddedUTF8(stream.Name));
                offset += stream.Length;
            }

            foreach (var stream in streams)
            {
                stream.Write(this);
            }
        }

        private static int GetHeaderLength(string version, Heap[] streams)
        {
            var length =
                4 + // Signature
                2 + // MajorVersion
                2 + // MinorVersion
                4 + // Reserved
                4 + // Version Length
                StringToPaddedUTF8Length(version) +
                2 + // Flags
                2;  // Streams
            foreach (var stream in streams)
            {
                length += 4;    // Offset
                length += 4;    // Size
                length += StringToPaddedUTF8Length(stream.Name);
            }
            return length;
        }

        private static int StringToPaddedUTF8Length(string str)
        {
            return (System.Text.Encoding.UTF8.GetByteCount(str) + 4) & ~3;
        }

        private static byte[] StringToPaddedUTF8(string str)
        {
            byte[] buf = new byte[(System.Text.Encoding.UTF8.GetByteCount(str) + 4) & ~3];
            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, buf, 0);
            return buf;
        }

        internal void Write(ByteBuffer bb)
        {
            bb.WriteTo(stream);
        }

        internal void WriteAsciiz(string value)
        {
            foreach (char c in value)
            {
                stream.WriteByte((byte)c);
            }
            stream.WriteByte(0);
        }

        internal void Write(byte[] value)
        {
            stream.Write(value, 0, value.Length);
        }

        internal void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        internal void Write(byte value)
        {
            stream.WriteByte(value);
        }

        internal void Write(ushort value)
        {
            Write((short)value);
        }

        internal void Write(short value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        internal void Write(uint value)
        {
            Write((int)value);
        }

        internal void Write(int value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            stream.Write(buffer, 0, 4);
        }

        internal void Write(ulong value)
        {
            Write((long)value);
        }

        internal void Write(long value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            stream.Write(buffer, 0, 8);
        }

        internal void WriteCompressedUInt(int value)
        {
            if (value <= 0x7F)
            {
                Write((byte)value);
            }
            else if (value <= 0x3FFF)
            {
                Write((byte)(0x80 | (value >> 8)));
                Write((byte)value);
            }
            else
            {
                Write((byte)(0xC0 | (value >> 24)));
                Write((byte)(value >> 16));
                Write((byte)(value >> 8));
                Write((byte)value);
            }
        }

        internal static int GetCompressedUIntLength(int value)
        {
            if (value <= 0x7F)
            {
                return 1;
            }
            else if (value <= 0x3FFF)
            {
                return 2;
            }
            else
            {
                return 4;
            }
        }

        internal void WriteStringIndex(int index)
        {
            if (bigStrings)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteGuidIndex(int index)
        {
            if (bigGuids)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteBlobIndex(int index)
        {
            if (bigBlobs)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteTypeDefOrRef(int token)
        {
            switch (token >> 24)
            {
                case 0:
                    break;
                case TypeDefTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 0;
                    break;
                case TypeRefTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 1;
                    break;
                case TypeSpecTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 2;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigTypeDefOrRef)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteEncodedTypeDefOrRef(int encodedToken)
        {
            if (bigTypeDefOrRef)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteHasCustomAttribute(int token)
        {
            int encodedToken = CustomAttributeTable.EncodeHasCustomAttribute(token);
            if (bigHasCustomAttribute)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteCustomAttributeType(int token)
        {
            switch (token >> 24)
            {
                case MethodDefTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 2;
                    break;
                case MemberRefTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 3;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigCustomAttributeType)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteField(int index)
        {
            if (bigField)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteMethodDef(int index)
        {
            if (bigMethodDef)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteParam(int index)
        {
            if (bigParam)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteTypeDef(int index)
        {
            if (bigTypeDef)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteEvent(int index)
        {
            if (bigEvent)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteProperty(int index)
        {
            if (bigProperty)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteGenericParam(int index)
        {
            if (bigGenericParam)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteModuleRef(int index)
        {
            if (bigModuleRef)
            {
                Write(index & 0xFFFFFF);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteResolutionScope(int token)
        {
            switch (token >> 24)
            {
                case ModuleTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 0;
                    break;
                case ModuleRefTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 1;
                    break;
                case AssemblyRefTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 2;
                    break;
                case TypeRefTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 3;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigResolutionScope)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteMemberRefParent(int token)
        {
            switch (token >> 24)
            {
                case TypeDefTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 0;
                    break;
                case TypeRefTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 1;
                    break;
                case ModuleRefTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 2;
                    break;
                case MethodDefTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 3;
                    break;
                case TypeSpecTable.Index:
                    token = (token & 0xFFFFFF) << 3 | 4;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigMemberRefParent)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteMethodDefOrRef(int token)
        {
            switch (token >> 24)
            {
                case MethodDefTable.Index:
                    token = (token & 0xFFFFFF) << 1 | 0;
                    break;
                case MemberRefTable.Index:
                    token = (token & 0xFFFFFF) << 1 | 1;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigMethodDefOrRef)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteHasConstant(int token)
        {
            int encodedToken = ConstantTable.EncodeHasConstant(token);
            if (bigHasConstant)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteHasSemantics(int encodedToken)
        {
            // NOTE because we've already had to do the encoding (to be able to sort the table)
            // here we simple write the value
            if (bigHasSemantics)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteImplementation(int token)
        {
            switch (token >> 24)
            {
                case 0:
                    break;
                case FileTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 0;
                    break;
                case AssemblyRefTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 1;
                    break;
                case ExportedTypeTable.Index:
                    token = (token & 0xFFFFFF) << 2 | 2;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigImplementation)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteTypeOrMethodDef(int encodedToken)
        {
            // NOTE because we've already had to do the encoding (to be able to sort the table)
            // here we simple write the value
            if (bigTypeOrMethodDef)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteHasDeclSecurity(int encodedToken)
        {
            // NOTE because we've already had to do the encoding (to be able to sort the table)
            // here we simple write the value
            if (bigHasDeclSecurity)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteMemberForwarded(int token)
        {
            switch (token >> 24)
            {
                case FieldTable.Index:
                    token = (token & 0xFFFFFF) << 1 | 0;
                    break;
                case MethodDefTable.Index:
                    token = (token & 0xFFFFFF) << 1 | 1;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (bigMemberForwarded)
            {
                Write(token);
            }
            else
            {
                Write((short)token);
            }
        }

        internal void WriteHasFieldMarshal(int token)
        {
            int encodedToken = FieldMarshalTable.EncodeHasFieldMarshal(token);
            if (bigHasFieldMarshal)
            {
                Write(encodedToken);
            }
            else
            {
                Write((short)encodedToken);
            }
        }

        internal void WriteDocument(int index)
        {
            if (bigDocument)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteImportScope(int index)
        {
            if (bigImportScope)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteLocalVariable(int index)
        {
            if (bigLocalVariable)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        internal void WriteLocalConstant(int index)
        {
            if (bigLocalConstant)
            {
                Write(index);
            }
            else
            {
                Write((short)index);
            }
        }

        private static int EncodeHasCustomDebugInformation(int token)
        {
            switch (token >> 24)
            {
                case MethodDefTable.Index:
                    return (token & 0xFFFFFF) << 5 | 0;
                case FieldTable.Index:
                    return (token & 0xFFFFFF) << 5 | 1;
                case TypeRefTable.Index:
                    return (token & 0xFFFFFF) << 5 | 2;
                case TypeDefTable.Index:
                    return (token & 0xFFFFFF) << 5 | 3;
                case ParamTable.Index:
                    return (token & 0xFFFFFF) << 5 | 4;
                case InterfaceImplTable.Index:
                    return (token & 0xFFFFFF) << 5 | 5;
                case MemberRefTable.Index:
                    return (token & 0xFFFFFF) << 5 | 6;
                case ModuleTable.Index:
                    return (token & 0xFFFFFF) << 5 | 7;
                case DeclSecurityTable.Index:
                    return (token & 0xFFFFFF) << 5 | 8;
                case PropertyTable.Index:
                    return (token & 0xFFFFFF) << 5 | 9;
                case EventTable.Index:
                    return (token & 0xFFFFFF) << 5 | 10;
                case StandAloneSigTable.Index:
                    return (token & 0xFFFFFF) << 5 | 11;
                case ModuleRefTable.Index:
                    return (token & 0xFFFFFF) << 5 | 12;
                case TypeSpecTable.Index:
                    return (token & 0xFFFFFF) << 5 | 13;
                case AssemblyTable.Index:
                    return (token & 0xFFFFFF) << 5 | 14;
                case AssemblyRefTable.Index:
                    return (token & 0xFFFFFF) << 5 | 15;
                case FileTable.Index:
                    return (token & 0xFFFFFF) << 5 | 16;
                case ExportedTypeTable.Index:
                    return (token & 0xFFFFFF) << 5 | 17;
                case ManifestResourceTable.Index:
                    return (token & 0xFFFFFF) << 5 | 18;
                case GenericParamTable.Index:
                    return (token & 0xFFFFFF) << 5 | 19;
                case GenericParamConstraintTable.Index:
                    return (token & 0xFFFFFF) << 5 | 20;
                case MethodSpecTable.Index:
                    return (token & 0xFFFFFF) << 5 | 21;
                case DocumentTable.Index:
                    return (token & 0xFFFFFF) << 5 | 22;
                case LocalScopeTable.Index:
                    return (token & 0xFFFFFF) << 5 | 23;
                case LocalVariableTable.Index:
                    return (token & 0xFFFFFF) << 5 | 24;
                case LocalConstantTable.Index:
                    return (token & 0xFFFFFF) << 5 | 25;
                case ImportScopeTable.Index:
                    return (token & 0xFFFFFF) << 5 | 26;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal void WriteHasCustomDebugInformation(int token)
        {
            int encoded = EncodeHasCustomDebugInformation(token);
            if (bigHasCustomDebugInformation)
            {
                Write(encoded);
            }
            else
            {
                Write((short)encoded);
            }
        }

        internal abstract void WriteTypeDefVirtualTable();
        internal abstract void WriteFieldVirtualTable();
        internal abstract void WriteMethodDefVirtualTable(int baseRVA);
        internal abstract void WriteParamVirtualTable();
    }

    sealed class ModuleBuilderMetadataWriter : MetadataWriter
    {
        private readonly ModuleBuilder module;

        internal ModuleBuilderMetadataWriter(ModuleBuilder module, Stream stream)
            : base(stream, module.GetTables(), module.Strings.IsBig, module.Guids.IsBig, module.Blobs.IsBig)
        {
            this.module = module;
        }

        internal override Table[] GetTables()
        {
            return module.GetTables();
        }

        internal override int MDStreamVersion
        {
            get { return module.MDStreamVersion; }
        }

        internal override void WriteFieldVirtualTable()
        {
            module.WriteFieldTable(this);
        }

        internal override void WriteMethodDefVirtualTable(int baseRVA)
        {
            module.WriteMethodDefTable(baseRVA, this);
        }

        internal override void WriteParamVirtualTable()
        {
            module.WriteParamTable(this);
        }

        internal override void WriteTypeDefVirtualTable()
        {
            module.WriteTypeDefTable(this);
        }
    }

    sealed class PortablePdbMetadataWriter : MetadataWriter
    {
        private readonly Table[] tables;

        internal PortablePdbMetadataWriter(Stream stream, Table[] tables, Table[] tablesForRowCountOnly, bool bigStrings, bool bigGuids, bool bigBlobs)
            : base(stream, tablesForRowCountOnly, bigStrings, bigGuids, bigBlobs)
        {
            this.tables = tables;
        }

        internal override Table[] GetTables()
        {
            return tables;
        }

        internal override int MDStreamVersion
        {
            get { return 0x20000; }
        }

        internal override void WriteFieldVirtualTable()
        {
            throw new NotSupportedException();
        }

        internal override void WriteMethodDefVirtualTable(int baseRVA)
        {
            throw new NotSupportedException();
        }

        internal override void WriteParamVirtualTable()
        {
            throw new NotSupportedException();
        }

        internal override void WriteTypeDefVirtualTable()
        {
            throw new NotSupportedException();
        }
    }
}
