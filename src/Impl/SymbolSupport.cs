/*
  The MIT License (MIT) 
  Copyright (C) 2008-2009 Jeroen Frijters
  
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
#if NO_SYMBOL_WRITER
            throw new NotSupportedException("Managed.Reflection compiled with NO_SYMBOL_WRITER does not support writing debugging symbols.");
#else
            if (Universe.MonoRuntime)
            {
#if MONO
                return new MdbWriter(moduleBuilder);
#else
                throw new NotSupportedException("Managed.Reflection must be compiled with MONO defined to support writing Mono debugging symbols.");
#endif
            }
            else
            {
                return new PdbWriter(moduleBuilder);
            }
#endif
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
}
