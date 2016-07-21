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
using System.IO;

namespace Managed.Reflection.Metadata
{
    struct RvaSize
    {
        internal uint VirtualAddress;
        internal uint Size;

        internal void Read(BinaryReader br)
        {
            VirtualAddress = br.ReadUInt32();
            Size = br.ReadUInt32();
        }

        internal void Write(Managed.Reflection.Writer.MetadataWriter mw)
        {
            mw.Write(VirtualAddress);
            mw.Write(Size);
        }
    }

    sealed class CliHeader
    {
        internal const uint COMIMAGE_FLAGS_ILONLY = 0x00000001;
        internal const uint COMIMAGE_FLAGS_32BITREQUIRED = 0x00000002;
        internal const uint COMIMAGE_FLAGS_STRONGNAMESIGNED = 0x00000008;
        internal const uint COMIMAGE_FLAGS_NATIVE_ENTRYPOINT = 0x00000010;
        internal const uint COMIMAGE_FLAGS_32BITPREFERRED = 0x00020000;

        internal uint Cb = 0x48;
        internal ushort MajorRuntimeVersion;
        internal ushort MinorRuntimeVersion;
        internal RvaSize MetaData;
        internal uint Flags;
        internal uint EntryPointToken;
        internal RvaSize Resources;
        internal RvaSize StrongNameSignature;
        internal RvaSize CodeManagerTable;
        internal RvaSize VTableFixups;
        internal RvaSize ExportAddressTableJumps;
        internal RvaSize ManagedNativeHeader;

        internal void Read(BinaryReader br)
        {
            Cb = br.ReadUInt32();
            MajorRuntimeVersion = br.ReadUInt16();
            MinorRuntimeVersion = br.ReadUInt16();
            MetaData.Read(br);
            Flags = br.ReadUInt32();
            EntryPointToken = br.ReadUInt32();
            Resources.Read(br);
            StrongNameSignature.Read(br);
            CodeManagerTable.Read(br);
            VTableFixups.Read(br);
            ExportAddressTableJumps.Read(br);
            ManagedNativeHeader.Read(br);
        }

        internal void Write(Managed.Reflection.Writer.MetadataWriter mw)
        {
            mw.Write(Cb);
            mw.Write(MajorRuntimeVersion);
            mw.Write(MinorRuntimeVersion);
            MetaData.Write(mw);
            mw.Write(Flags);
            mw.Write(EntryPointToken);
            Resources.Write(mw);
            StrongNameSignature.Write(mw);
            CodeManagerTable.Write(mw);
            VTableFixups.Write(mw);
            ExportAddressTableJumps.Write(mw);
            ManagedNativeHeader.Write(mw);
        }
    }
}
