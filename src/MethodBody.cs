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
using System;
using System.Collections.Generic;
using System.IO;
using Managed.Reflection.Reader;

namespace Managed.Reflection
{
    public sealed class MethodBody
    {
        private readonly IList<ExceptionHandlingClause> exceptionClauses;
        private readonly IList<LocalVariableInfo> locals;
        private readonly bool initLocals;
        private readonly int maxStack;
        private readonly int localVarSigTok;
        private byte[] body;

        internal MethodBody(ModuleReader module, int rva, IGenericContext context)
        {
            const byte CorILMethod_TinyFormat = 0x02;
            const byte CorILMethod_FatFormat = 0x03;
            const byte CorILMethod_MoreSects = 0x08;
            const byte CorILMethod_InitLocals = 0x10;
            const byte CorILMethod_Sect_EHTable = 0x01;
            const byte CorILMethod_Sect_FatFormat = 0x40;
            const byte CorILMethod_Sect_MoreSects = 0x80;

            List<ExceptionHandlingClause> exceptionClauses = new List<ExceptionHandlingClause>();
            List<LocalVariableInfo> locals = new List<LocalVariableInfo>();
            Stream stream = module.GetStream();
            module.SeekRVA(rva);
            BinaryReader br = new BinaryReader(stream);
            byte b = br.ReadByte();
            if ((b & 3) == CorILMethod_TinyFormat)
            {
                initLocals = true;
                body = br.ReadBytes(b >> 2);
                maxStack = 8;
            }
            else if ((b & 3) == CorILMethod_FatFormat)
            {
                initLocals = (b & CorILMethod_InitLocals) != 0;
                short flagsAndSize = (short)(b | (br.ReadByte() << 8));
                if ((flagsAndSize >> 12) != 3)
                {
                    throw new BadImageFormatException("Fat format method header size should be 3");
                }
                maxStack = br.ReadUInt16();
                int codeLength = br.ReadInt32();
                localVarSigTok = br.ReadInt32();
                body = br.ReadBytes(codeLength);
                if ((b & CorILMethod_MoreSects) != 0)
                {
                    stream.Position = (stream.Position + 3) & ~3;
                    int hdr = br.ReadInt32();
                    if ((hdr & CorILMethod_Sect_MoreSects) != 0 || (hdr & CorILMethod_Sect_EHTable) == 0)
                    {
                        throw new NotImplementedException();
                    }
                    else if ((hdr & CorILMethod_Sect_FatFormat) != 0)
                    {
                        int count = ComputeExceptionCount((hdr >> 8) & 0xFFFFFF, 24);
                        for (int i = 0; i < count; i++)
                        {
                            int flags = br.ReadInt32();
                            int tryOffset = br.ReadInt32();
                            int tryLength = br.ReadInt32();
                            int handlerOffset = br.ReadInt32();
                            int handlerLength = br.ReadInt32();
                            int classTokenOrFilterOffset = br.ReadInt32();
                            exceptionClauses.Add(new ExceptionHandlingClause(module, flags, tryOffset, tryLength, handlerOffset, handlerLength, classTokenOrFilterOffset, context));
                        }
                    }
                    else
                    {
                        int count = ComputeExceptionCount((hdr >> 8) & 0xFF, 12);
                        for (int i = 0; i < count; i++)
                        {
                            int flags = br.ReadUInt16();
                            int tryOffset = br.ReadUInt16();
                            int tryLength = br.ReadByte();
                            int handlerOffset = br.ReadUInt16();
                            int handlerLength = br.ReadByte();
                            int classTokenOrFilterOffset = br.ReadInt32();
                            exceptionClauses.Add(new ExceptionHandlingClause(module, flags, tryOffset, tryLength, handlerOffset, handlerLength, classTokenOrFilterOffset, context));
                        }
                    }
                }
                if (localVarSigTok != 0)
                {
                    ByteReader sig = module.GetStandAloneSig((localVarSigTok & 0xFFFFFF) - 1);
                    Signature.ReadLocalVarSig(module, sig, context, locals);
                }
            }
            else
            {
                throw new BadImageFormatException();
            }
            this.exceptionClauses = exceptionClauses.AsReadOnly();
            this.locals = locals.AsReadOnly();
        }

        private static int ComputeExceptionCount(int size, int itemLength)
        {
            // LAMESPEC according to the spec, the count should be calculated as "(size - 4) / itemLength",
            // FXBUG but to workaround a VB compiler bug that specifies the size incorrectly,
            // we do a truncating division instead.
            return size / itemLength;
        }

        public IList<ExceptionHandlingClause> ExceptionHandlingClauses
        {
            get { return exceptionClauses; }
        }

        public bool InitLocals
        {
            get { return initLocals; }
        }

        public IList<LocalVariableInfo> LocalVariables
        {
            get { return locals; }
        }

        public byte[] GetILAsByteArray()
        {
            return body;
        }

        public int LocalSignatureMetadataToken
        {
            get { return localVarSigTok; }
        }

        public int MaxStackSize
        {
            get { return maxStack; }
        }
    }
}
