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
using System.IO;
using BYTE = System.Byte;
using DWORD = System.UInt32;
using ULONGLONG = System.UInt64;
using WORD = System.UInt16;

namespace Managed.Reflection.Reader
{
    sealed class MSDOS_HEADER
    {
        internal const WORD MAGIC_MZ = 0x5A4D;

        internal WORD signature;    // 'MZ'
                                    // skip 58 bytes
        internal DWORD peSignatureOffset;
    }

    sealed class IMAGE_NT_HEADERS
    {
        public const DWORD MAGIC_SIGNATURE = 0x00004550;    // "PE\0\0"

        public DWORD Signature;
        public IMAGE_FILE_HEADER FileHeader = new IMAGE_FILE_HEADER();
        public IMAGE_OPTIONAL_HEADER OptionalHeader = new IMAGE_OPTIONAL_HEADER();

        internal void Read(BinaryReader br)
        {
            Signature = br.ReadUInt32();
            if (Signature != IMAGE_NT_HEADERS.MAGIC_SIGNATURE)
            {
                throw new BadImageFormatException();
            }
            FileHeader.Read(br);
            long optionalHeaderPosition = br.BaseStream.Position;
            OptionalHeader.Read(br);
            if (br.BaseStream.Position > optionalHeaderPosition + FileHeader.SizeOfOptionalHeader)
            {
                throw new BadImageFormatException();
            }
            br.BaseStream.Seek(optionalHeaderPosition + FileHeader.SizeOfOptionalHeader, SeekOrigin.Begin);
        }
    }

    sealed class IMAGE_FILE_HEADER
    {
        public const WORD IMAGE_FILE_MACHINE_I386 = 0x014c;
        public const WORD IMAGE_FILE_MACHINE_IA64 = 0x0200;
        public const WORD IMAGE_FILE_MACHINE_AMD64 = 0x8664;

        public const WORD IMAGE_FILE_32BIT_MACHINE = 0x0100;
        public const WORD IMAGE_FILE_EXECUTABLE_IMAGE = 0x0002;
        public const WORD IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020;
        public const WORD IMAGE_FILE_DLL = 0x2000;

        public WORD Machine;
        public WORD NumberOfSections;
        public DWORD TimeDateStamp;
        public DWORD PointerToSymbolTable;
        public DWORD NumberOfSymbols;
        public WORD SizeOfOptionalHeader;
        public WORD Characteristics;

        internal void Read(BinaryReader br)
        {
            Machine = br.ReadUInt16();
            NumberOfSections = br.ReadUInt16();
            TimeDateStamp = br.ReadUInt32();
            PointerToSymbolTable = br.ReadUInt32();
            NumberOfSymbols = br.ReadUInt32();
            SizeOfOptionalHeader = br.ReadUInt16();
            Characteristics = br.ReadUInt16();
        }
    }

    sealed class IMAGE_OPTIONAL_HEADER
    {
        public const WORD IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b;
        public const WORD IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b;

        public const WORD IMAGE_SUBSYSTEM_WINDOWS_GUI = 2;
        public const WORD IMAGE_SUBSYSTEM_WINDOWS_CUI = 3;

        public WORD Magic;
        public BYTE MajorLinkerVersion;
        public BYTE MinorLinkerVersion;
        public DWORD SizeOfCode;
        public DWORD SizeOfInitializedData;
        public DWORD SizeOfUninitializedData;
        public DWORD AddressOfEntryPoint;
        public DWORD BaseOfCode;
        public DWORD BaseOfData;
        public ULONGLONG ImageBase;
        public DWORD SectionAlignment;
        public DWORD FileAlignment;
        public WORD MajorOperatingSystemVersion;
        public WORD MinorOperatingSystemVersion;
        public WORD MajorImageVersion;
        public WORD MinorImageVersion;
        public WORD MajorSubsystemVersion;
        public WORD MinorSubsystemVersion;
        public DWORD Win32VersionValue;
        public DWORD SizeOfImage;
        public DWORD SizeOfHeaders;
        public DWORD CheckSum;
        public WORD Subsystem;
        public WORD DllCharacteristics;
        public ULONGLONG SizeOfStackReserve;
        public ULONGLONG SizeOfStackCommit;
        public ULONGLONG SizeOfHeapReserve;
        public ULONGLONG SizeOfHeapCommit;
        public DWORD LoaderFlags;
        public DWORD NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY[] DataDirectory;

        internal void Read(BinaryReader br)
        {
            Magic = br.ReadUInt16();
            if (Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC && Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC)
            {
                throw new BadImageFormatException();
            }
            MajorLinkerVersion = br.ReadByte();
            MinorLinkerVersion = br.ReadByte();
            SizeOfCode = br.ReadUInt32();
            SizeOfInitializedData = br.ReadUInt32();
            SizeOfUninitializedData = br.ReadUInt32();
            AddressOfEntryPoint = br.ReadUInt32();
            BaseOfCode = br.ReadUInt32();
            if (Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                BaseOfData = br.ReadUInt32();
                ImageBase = br.ReadUInt32();
            }
            else
            {
                ImageBase = br.ReadUInt64();
            }
            SectionAlignment = br.ReadUInt32();
            FileAlignment = br.ReadUInt32();
            MajorOperatingSystemVersion = br.ReadUInt16();
            MinorOperatingSystemVersion = br.ReadUInt16();
            MajorImageVersion = br.ReadUInt16();
            MinorImageVersion = br.ReadUInt16();
            MajorSubsystemVersion = br.ReadUInt16();
            MinorSubsystemVersion = br.ReadUInt16();
            Win32VersionValue = br.ReadUInt32();
            SizeOfImage = br.ReadUInt32();
            SizeOfHeaders = br.ReadUInt32();
            CheckSum = br.ReadUInt32();
            Subsystem = br.ReadUInt16();
            DllCharacteristics = br.ReadUInt16();
            if (Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                SizeOfStackReserve = br.ReadUInt32();
                SizeOfStackCommit = br.ReadUInt32();
                SizeOfHeapReserve = br.ReadUInt32();
                SizeOfHeapCommit = br.ReadUInt32();
            }
            else
            {
                SizeOfStackReserve = br.ReadUInt64();
                SizeOfStackCommit = br.ReadUInt64();
                SizeOfHeapReserve = br.ReadUInt64();
                SizeOfHeapCommit = br.ReadUInt64();
            }
            LoaderFlags = br.ReadUInt32();
            NumberOfRvaAndSizes = br.ReadUInt32();
            DataDirectory = new IMAGE_DATA_DIRECTORY[NumberOfRvaAndSizes];
            for (uint i = 0; i < NumberOfRvaAndSizes; i++)
            {
                DataDirectory[i] = new IMAGE_DATA_DIRECTORY();
                DataDirectory[i].Read(br);
            }
        }
    }

    struct IMAGE_DATA_DIRECTORY
    {
        public DWORD VirtualAddress;
        public DWORD Size;

        internal void Read(BinaryReader br)
        {
            VirtualAddress = br.ReadUInt32();
            Size = br.ReadUInt32();
        }
    }

    class SectionHeader
    {
        public const DWORD IMAGE_SCN_CNT_CODE = 0x00000020;
        public const DWORD IMAGE_SCN_CNT_INITIALIZED_DATA = 0x00000040;
        public const DWORD IMAGE_SCN_MEM_DISCARDABLE = 0x02000000;
        public const DWORD IMAGE_SCN_MEM_EXECUTE = 0x20000000;
        public const DWORD IMAGE_SCN_MEM_READ = 0x40000000;
        public const DWORD IMAGE_SCN_MEM_WRITE = 0x80000000;

        public string Name;     // 8 byte UTF8 encoded 0-padded
        public DWORD VirtualSize;
        public DWORD VirtualAddress;
        public DWORD SizeOfRawData;
        public DWORD PointerToRawData;
        public DWORD PointerToRelocations;
        public DWORD PointerToLinenumbers;
        public WORD NumberOfRelocations;
        public WORD NumberOfLinenumbers;
        public DWORD Characteristics;

        internal void Read(BinaryReader br)
        {
            char[] name = new char[8];
            int len = 8;
            for (int i = 0; i < 8; i++)
            {
                byte b = br.ReadByte();
                name[i] = (char)b;
                if (b == 0 && len == 8)
                {
                    len = i;
                }
            }
            Name = new String(name, 0, len);
            VirtualSize = br.ReadUInt32();
            VirtualAddress = br.ReadUInt32();
            SizeOfRawData = br.ReadUInt32();
            PointerToRawData = br.ReadUInt32();
            PointerToRelocations = br.ReadUInt32();
            PointerToLinenumbers = br.ReadUInt32();
            NumberOfRelocations = br.ReadUInt16();
            NumberOfLinenumbers = br.ReadUInt16();
            Characteristics = br.ReadUInt32();
        }
    }

    sealed class PEReader
    {
        private MSDOS_HEADER msdos = new MSDOS_HEADER();
        private IMAGE_NT_HEADERS headers = new IMAGE_NT_HEADERS();
        private SectionHeader[] sections;
        private bool mapped;

        internal void Read(BinaryReader br, bool mapped)
        {
            this.mapped = mapped;
            msdos.signature = br.ReadUInt16();
            br.BaseStream.Seek(58, SeekOrigin.Current);
            msdos.peSignatureOffset = br.ReadUInt32();

            if (msdos.signature != MSDOS_HEADER.MAGIC_MZ)
            {
                throw new BadImageFormatException();
            }

            br.BaseStream.Seek(msdos.peSignatureOffset, SeekOrigin.Begin);
            headers.Read(br);
            sections = new SectionHeader[headers.FileHeader.NumberOfSections];
            for (int i = 0; i < sections.Length; i++)
            {
                sections[i] = new SectionHeader();
                sections[i].Read(br);
            }
        }

        internal IMAGE_FILE_HEADER FileHeader
        {
            get { return headers.FileHeader; }
        }

        internal IMAGE_OPTIONAL_HEADER OptionalHeader
        {
            get { return headers.OptionalHeader; }
        }

        internal DWORD GetComDescriptorVirtualAddress()
        {
            return headers.OptionalHeader.DataDirectory[14].VirtualAddress;
        }

        internal void GetDataDirectoryEntry(int index, out int rva, out int length)
        {
            rva = (int)headers.OptionalHeader.DataDirectory[index].VirtualAddress;
            length = (int)headers.OptionalHeader.DataDirectory[index].Size;
        }

        internal long RvaToFileOffset(DWORD rva)
        {
            if (mapped)
            {
                return rva;
            }
            for (int i = 0; i < sections.Length; i++)
            {
                if (rva >= sections[i].VirtualAddress && rva < sections[i].VirtualAddress + sections[i].VirtualSize)
                {
                    return sections[i].PointerToRawData + rva - sections[i].VirtualAddress;
                }
            }
            throw new BadImageFormatException();
        }

        internal bool GetSectionInfo(int rva, out string name, out int characteristics, out int virtualAddress, out int virtualSize, out int pointerToRawData, out int sizeOfRawData)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                if (rva >= sections[i].VirtualAddress && rva < sections[i].VirtualAddress + sections[i].VirtualSize)
                {
                    name = sections[i].Name;
                    characteristics = (int)sections[i].Characteristics;
                    virtualAddress = (int)sections[i].VirtualAddress;
                    virtualSize = (int)sections[i].VirtualSize;
                    pointerToRawData = (int)sections[i].PointerToRawData;
                    sizeOfRawData = (int)sections[i].SizeOfRawData;
                    return true;
                }
            }
            name = null;
            characteristics = 0;
            virtualAddress = 0;
            virtualSize = 0;
            pointerToRawData = 0;
            sizeOfRawData = 0;
            return false;
        }
    }
}
