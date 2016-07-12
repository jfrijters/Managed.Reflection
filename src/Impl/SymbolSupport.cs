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
using System.Runtime.InteropServices;
using Managed.Reflection.Emit;

namespace Managed.Reflection.Impl
{
    [StructLayout(LayoutKind.Sequential)]
    struct IMAGE_DEBUG_DIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Type;
        public uint SizeOfData;
        public uint AddressOfRawData;
        public uint PointerToRawData;
    }

#if NO_SYMBOL_WRITER
    struct SymbolToken
    {
        internal SymbolToken(int value) { }
    }

    interface ISymbolWriterImpl
    {
        byte[] GetDebugInfo(ref IMAGE_DEBUG_DIRECTORY idd);
        void RemapToken(int oldToken, int newToken);
        void DefineLocalVariable2(string name, FieldAttributes attributes, int signature, int addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset);
        void OpenMethod(SymbolToken symbolToken, MethodBase mb);
        bool IsDeterministic { get; }
        void Close();
    }
#else
    interface ISymbolWriterImpl : ISymbolWriter
    {
        byte[] GetDebugInfo(ref IMAGE_DEBUG_DIRECTORY idd);
        void RemapToken(int oldToken, int newToken);
        void DefineLocalVariable2(string name, FieldAttributes attributes, int signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset);
        void OpenMethod(SymbolToken symbolToken, MethodBase mb);
        bool IsDeterministic { get; }
    }
#endif

    static class SymbolSupport
    {
        internal static ISymbolWriterImpl CreateSymbolWriterFor(ModuleBuilder moduleBuilder)
        {
            return new PortablePdbWriter(moduleBuilder);
        }

        internal static byte[] GetDebugInfo(ISymbolWriterImpl writer, ref IMAGE_DEBUG_DIRECTORY idd)
        {
            return writer.GetDebugInfo(ref idd);
        }

        internal static void RemapToken(ISymbolWriterImpl writer, int oldToken, int newToken)
        {
            writer.RemapToken(oldToken, newToken);
        }
    }

    sealed class PortablePdbWriter : ISymbolWriterImpl
    {
        private readonly ModuleBuilder moduleBuilder;
        private readonly Guid guid = Guid.NewGuid();

        internal PortablePdbWriter(ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
        }

        public bool IsDeterministic
        {
            get { return false; }
        }

        public void DefineLocalVariable2(string name, FieldAttributes attributes, int signature, int addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset)
        {
        }

        public byte[] GetDebugInfo(ref IMAGE_DEBUG_DIRECTORY idd)
        {
            // From Roslyn's https://github.com/dotnet/roslyn/blob/86c5958add9e977454f6b052bb190f2cb1754d80/src/Compilers/Core/Portable/NativePdbWriter/PdbWriter.cs
            // Data has the following structure:
            // struct RSDSI
            // {
            //     DWORD dwSig;                 // "RSDS"
            //     GUID guidSig;                // GUID
            //     DWORD age;                   // age
            //     char szPDB[0];               // zero-terminated UTF8 file name passed to the writer
            // };
            var fileName = System.IO.Path.ChangeExtension(moduleBuilder.FullyQualifiedName, ".pdb");
            var data = new byte[4 + 16 + 4 + System.Text.Encoding.UTF8.GetByteCount(fileName) + 1];
            // dwSig
            data[0] = (byte)'R';
            data[1] = (byte)'S';
            data[2] = (byte)'D';
            data[3] = (byte)'S';
            // guidSig
            Buffer.BlockCopy(guid.ToByteArray(), 0, data, 4, 16);
            // age
            data[4 + 16] = 1;
            // szPDB
            System.Text.Encoding.UTF8.GetBytes(fileName, 0, fileName.Length, data, 4 + 16 + 4);

            // update IMAGE_DEBUG_DIRECTORY fields
            idd.Type = 2;   // IMAGE_DEBUG_TYPE_CODEVIEW
            idd.SizeOfData = (uint)data.Length;
            idd.MajorVersion = 0x0100;
            idd.MinorVersion = 0x504D;
            return data;
        }

        public void OpenMethod(SymbolToken symbolToken, MethodBase mb)
        {
        }

        public void RemapToken(int oldToken, int newToken)
        {
        }

        public void Close()
        {
        }
    }
}
