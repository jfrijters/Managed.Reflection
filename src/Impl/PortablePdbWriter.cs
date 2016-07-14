/*
  The MIT License (MIT) 
  Copyright (C) 2016 Jeroen Frijters
  
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
using System.Diagnostics;
using System.Text;
using Managed.Reflection.Emit;
using Managed.Reflection.Metadata;
using Managed.Reflection.Writer;

namespace Managed.Reflection.Impl
{
    sealed class PortablePdbWriter : ISymbolWriterImpl
    {
        private const string ImageRuntimeVersion = "PDB v1.0";
        private readonly ModuleBuilder moduleBuilder;
        private readonly Guid guid = Guid.NewGuid();
        private uint timestamp;
        private readonly TableHeap Tables = new TableHeap();
        private readonly StringHeap Strings = new StringHeap();
        private readonly UserStringHeap UserStrings = new UserStringHeap();
        private readonly GuidHeap Guids = new GuidHeap();
        private readonly BlobHeap Blobs = new BlobHeap();
        private readonly Dictionary<int, int> tokenMap = new Dictionary<int, int>();
        private readonly DocumentTable Document = new DocumentTable();
        private readonly List<MethodRec> methods = new List<MethodRec>();
        private int userEntryPointToken;
        private int currentMethod;

        struct MethodRec
        {
            internal int Token;
            internal int Document;
            internal int SequencePoints;
        }

        internal PortablePdbWriter(ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
        }

        public bool IsDeterministic
        {
            get { return false; }
        }

        public void DefineLocalVariable2(string name, FieldAttributes attributes, int signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset)
        {
        }

        private string GetFileName()
        {
            return System.IO.Path.ChangeExtension(moduleBuilder.FullyQualifiedName, ".pdb");
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
            var fileName = GetFileName();
            var data = new byte[4 + 16 + 4 + Encoding.UTF8.GetByteCount(fileName) + 1];
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
            Encoding.UTF8.GetBytes(fileName, 0, fileName.Length, data, 4 + 16 + 4);

            // update IMAGE_DEBUG_DIRECTORY fields
            idd.Type = 2;   // IMAGE_DEBUG_TYPE_CODEVIEW
            idd.SizeOfData = (uint)data.Length;
            idd.MajorVersion = 0x0100;
            idd.MinorVersion = 0x504D;

            // TODO we get called multiple times with a dummy IMAGE_DEBUG_DIRECTORY, that shouldn't happen)
            // HACK
            // BUGBUG
            if (idd.TimeDateStamp != 0)
            {
                // remember the TimeStamp, we need it later
                timestamp = idd.TimeDateStamp;
            }
            return data;
        }

        public void OpenMethod(SymbolToken symbolToken, MethodBase mb)
        {
            currentMethod = symbolToken.value;
        }

        public void CloseMethod()
        {
            currentMethod = 0;
        }

        public void RemapToken(int oldToken, int newToken)
        {
            tokenMap.Add(oldToken, newToken);
        }

        public void DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
        {
            // Sequence Points Blob
            var bb = new ByteBuffer(offsets.Length * 10);
            // header
            // LocalSignature
            // TODO
            bb.WriteCompressedUInt(0);
            // we don't support multiple documents per method, so we don't need to write InitialDocument
            var previousILOffset = 0;
            var previousStartLine = 0;
            var previousStartColumn = 0;
            for (var i = 0; i < offsets.Length; i++)
            {
                // sequence-point-record
                Debug.Assert(previousILOffset == 0 || offsets[i] - previousILOffset != 0);
                bb.WriteCompressedUInt(offsets[i] - previousILOffset);
                previousILOffset = offsets[i];
                bb.WriteCompressedUInt(endLines[i] - lines[i]);
                bb.WriteCompressedUInt(endColumns[i] - columns[i]);
                bb.WriteCompressedUInt(lines[i] - previousStartLine);
                previousStartLine = lines[i];
                bb.WriteCompressedUInt(columns[i] - previousStartColumn);
                previousStartColumn = columns[i];
            }
            methods.Add(new MethodRec { Token = currentMethod, Document = ((DocumentImpl)document).rId, SequencePoints = Blobs.Add(bb) });
        }

        public void OpenScope(int startOffset)
        {
        }

        public void CloseScope(int endOffset)
        {
        }

        public void UsingNamespace(string usingNamespace)
        {
        }

        public ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType)
        {
            DocumentTable.Record rec;
            rec.Name = AddDocumentNameBlob(url);
            rec.HashAlgorithm = 0;
            rec.Hash = 0;
            rec.Language = Guids.Add(language);
            return new DocumentImpl(Document.AddRecord(rec));
        }

        private int AddDocumentNameBlob(string name)
        {
            // TODO optimize the name encoding
            var bb = new ByteBuffer(2);
            bb.Write((byte)0);
            // LAMESPEC spec says "part is a compressed integer into the #Blob heap"
            bb.WriteCompressedUInt(Blobs.Add(ByteBuffer.Wrap(Encoding.UTF8.GetBytes(name))));
            return Blobs.Add(bb);
        }

        public void SetUserEntryPoint(SymbolToken symbolToken)
        {
            userEntryPointToken = symbolToken.value;
        }

        public void Close()
        {
            Strings.Freeze();
            UserStrings.Freeze();
            Guids.Freeze();
            Blobs.Freeze();

            using (var fs = System.IO.File.Create(GetFileName()))
            {
                // TODO do we need this?
                uint guidOffset;
                var tablesForRowCountOnly = moduleBuilder.GetTables();
                var tables = new Table[64];
                tables[DocumentTable.Index] = Document;
                tables[MethodDebugInformationTable.Index] = CreateMethodDebugInformation();
                for (var i = 0; i < tables.Length; i++)
                {
                    if (tables[i] != null)
                    {
                        tablesForRowCountOnly[i] = tables[i];
                    }
                }
                WriteMetadata(new PortablePdbMetadataWriter(fs, tables, tablesForRowCountOnly, Strings.IsBig, Guids.IsBig, Blobs.IsBig), out guidOffset);
            }
        }

        private MethodDebugInformationTable CreateMethodDebugInformation()
        {
            var table = new MethodDebugInformationTable();
            table.RowCount = moduleBuilder.MethodDef.RowCount;
            foreach (var method in methods)
            {
                var index = (tokenMap[method.Token] & 0xFFFFFF) - 1;
                table.records[index].Document = method.Document;
                table.records[index].SequencePoints = method.SequencePoints;
            }
            return table;
        }

        private int GetHeaderLength()
        {
            return
                4 + // Signature
                2 + // MajorVersion
                2 + // MinorVersion
                4 + // Reserved
                4 + // ImageRuntimeVersion Length
                StringToPaddedUTF8Length(ImageRuntimeVersion) +
                2 + // Flags
                2 + // Streams
                4 + // #Pdb Offset
                4 + // #Pdb Size
                8 + // StringToPaddedUTF8Length("#Pdb")
                4 + // #~ Offset
                4 + // #~ Size
                4 + // StringToPaddedUTF8Length("#~")
                4 + // #Strings Offset
                4 + // #Strings Size
                12 + // StringToPaddedUTF8Length("#Strings")
                4 + // #US Offset
                4 + // #US Size
                4 + // StringToPaddedUTF8Length("#US")
                4 + // #GUID Offset
                4 + // #GUID Size
                8 + // StringToPaddedUTF8Length("#GUID")
                4 + // #Blob Offset
                4 + // #Blob Size
                8   // StringToPaddedUTF8Length("#Blob")
                ;
        }

        internal void WriteMetadata(MetadataWriter mw, out uint guidHeapOffset)
        {
            var entryPointToken = 0;
            if (userEntryPointToken != 0)
            {
                entryPointToken = userEntryPointToken;
            }
            else if (moduleBuilder.Assembly?.EntryPoint?.Module == moduleBuilder)
            {
                entryPointToken = -moduleBuilder.Assembly.EntryPoint.MetadataToken | 0x06000000;
            }
            if (entryPointToken != 0)
            {
                entryPointToken = tokenMap[entryPointToken];
            }
            Tables.Freeze(mw);
            var pdb = new PdbHeap(guid, timestamp, moduleBuilder.GetTables(), entryPointToken);

            mw.Write(0x424A5342);           // Signature ("BSJB")
            mw.Write((ushort)1);            // MajorVersion
            mw.Write((ushort)1);            // MinorVersion
            mw.Write(0);                    // Reserved
            byte[] version = StringToPaddedUTF8(ImageRuntimeVersion);
            mw.Write(version.Length);       // Length
            mw.Write(version);
            mw.Write((ushort)0);            // Flags
            mw.Write((ushort)6);            // Streams

            int offset = GetHeaderLength();

            // Streams
            mw.Write(offset);               // Offset
            mw.Write(pdb.Length);           // Size
            mw.Write(StringToPaddedUTF8("#Pdb"));
            offset += pdb.Length;

            mw.Write(offset);               // Offset
            mw.Write(Tables.Length);        // Size
            mw.Write(StringToPaddedUTF8("#~"));
            offset += Tables.Length;

            mw.Write(offset);               // Offset
            mw.Write(Strings.Length);       // Size
            mw.Write(StringToPaddedUTF8("#Strings"));
            offset += Strings.Length;

            mw.Write(offset);               // Offset
            mw.Write(UserStrings.Length);   // Size
            mw.Write(StringToPaddedUTF8("#US"));
            offset += UserStrings.Length;

            mw.Write(offset);               // Offset
            mw.Write(Guids.Length);         // Size
            mw.Write(StringToPaddedUTF8("#GUID"));
            offset += Guids.Length;

            mw.Write(offset);               // Offset
            mw.Write(Blobs.Length);         // Size
            mw.Write(StringToPaddedUTF8("#Blob"));

            pdb.Write(mw);
            Tables.Write(mw);
            Strings.Write(mw);
            UserStrings.Write(mw);
            guidHeapOffset = mw.Position;
            Guids.Write(mw);
            Blobs.Write(mw);
        }

        // TODO move this to MetadataWriter
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
    }

    sealed class PdbHeap : SimpleHeap
    {
        private readonly Guid guid;
        private readonly uint timestamp;
        private readonly Table[] referencedTables;
        private readonly int entryPointToken;

        internal PdbHeap(Guid guid, uint timestamp, Table[] referencedTables, int entryPointToken)
        {
            this.guid = guid;
            this.timestamp = timestamp;
            this.referencedTables = referencedTables;
            this.entryPointToken = entryPointToken;
            Freeze();
        }

        protected override int GetLength()
        {
            var tableCount = 0;
            foreach (var table in referencedTables)
            {
                if (table != null && table.RowCount != 0)
                {
                    tableCount++;
                }
            }
            return 20 + 4 + 8 + tableCount * 4;
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            // PDB id
            mw.Write(guid.ToByteArray());
            mw.Write(timestamp);
            // EntryPoint
            // LAMESPEC the spec says "The same value as stored in CLI header of the PE file.",
            // but the intent is clearly to allow a separate "user" entry point
            // (i.e. it should default to the entry point defined in the PE file, but SetUserEntryPoint should override it)
            mw.Write(entryPointToken);
            // ReferencedTypeSystemTables
            var bit = 1L;
            var valid = 0L;
            foreach (var table in referencedTables)
            {
                if (table != null && table.RowCount != 0)
                {
                    valid |= bit;
                }
                bit <<= 1;
            }
            mw.Write(valid);
            // TypeSystemTableRows
            foreach (var table in referencedTables)
            {
                if (table != null && table.RowCount != 0)
                {
                    mw.Write(table.RowCount);
                }
            }
        }
    }

    sealed class DocumentImpl : ISymbolDocumentWriter
    {
        internal readonly int rId;

        internal DocumentImpl(int rId)
        {
            this.rId = rId;
        }
    }
}
