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

    interface ISymbolWriterImpl
    {
        byte[] GetDebugInfo(ref IMAGE_DEBUG_DIRECTORY idd);
        int GetDebugInfoLength();
        void RemapToken(int oldToken, int newToken);
        void DefineLocalVariable2(string name, FieldAttributes attributes, int signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset);
        void OpenMethod(SymbolToken symbolToken, MethodBase mb);
        void CloseMethod();
        bool IsDeterministic { get; }
        void Close();
        void DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns);
        void OpenScope(int startOffset);
        void CloseScope(int endOffset);
        void UsingNamespace(string usingNamespace);
        ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType);
        void SetUserEntryPoint(SymbolToken symbolToken);
    }

    static class SymbolSupport
    {
        internal static ISymbolWriterImpl CreateSymbolWriterFor(ModuleBuilder moduleBuilder)
        {
            return new PortablePdbWriter(moduleBuilder);
        }
    }
}

namespace Managed.Reflection.Emit
{
    public interface ISymbolDocumentWriter
    {
        void SetCheckSum(Guid algorithmId, byte[] checkSum);
        void SetSource(byte[] source);
    }

    struct SymbolToken
    {
        internal readonly int value;

        internal SymbolToken(int value)
        {
            this.value = value;
        }
    }

    enum SymAddressKind
    {
        ILOffset,
    }
}
