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
        private readonly ModuleBuilder moduleBuilder;
        private readonly string fileName;
        private Guid guid;
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
        private int localVarSigToken;
        private DocumentImpl document;
        private int[] ilOffsets;
        private int[] startLines;
        private int[] startColumns;
        private int[] endLines;
        private int[] endColumns;
        private Scope scope;
        private readonly List<Scope> scopes = new List<Scope>();

        struct MethodRec
        {
            internal int Token;
            internal int Document;
            internal int SequencePoints;
            internal Scope[] Scopes;
        }

        sealed class Scope
        {
            internal readonly Scope Parent;
            internal int StartOffset;
            internal int Length;
            internal readonly List<Variable> VariableList = new List<Variable>();
            internal readonly List<string> Namespaces = new List<string>();

            internal Scope(Scope parent)
            {
                Parent = parent;
            }
        }

        struct Variable
        {
            internal string Name;
            internal int Index;
        }

        internal PortablePdbWriter(ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
            fileName = System.IO.Path.ChangeExtension(IsDeterministic ? moduleBuilder.Name : moduleBuilder.FullyQualifiedName, ".pdb");
        }

        public bool IsDeterministic
        {
            get { return moduleBuilder.universe.Deterministic; }
        }

        public void DefineLocalVariable2(string name, FieldAttributes attributes, int signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset)
        {
            Debug.Assert(localVarSigToken == 0 || localVarSigToken == signature);
            localVarSigToken = signature;
            scope.VariableList.Add(new Variable { Name = name, Index = addr1 });
        }

        public byte[] GetDebugInfo(ref IMAGE_DEBUG_DIRECTORY idd, bool deterministicPatchupPass)
        {
            if (deterministicPatchupPass)
            {
                idd.TimeDateStamp = timestamp;
            }
            else if (!IsDeterministic)
            {
                guid = Guid.NewGuid();
                timestamp = idd.TimeDateStamp;
            }

            // From Roslyn's https://github.com/dotnet/roslyn/blob/86c5958add9e977454f6b052bb190f2cb1754d80/src/Compilers/Core/Portable/NativePdbWriter/PdbWriter.cs
            // Data has the following structure:
            // struct RSDSI
            // {
            //     DWORD dwSig;                 // "RSDS"
            //     GUID guidSig;                // GUID
            //     DWORD age;                   // age
            //     char szPDB[0];               // zero-terminated UTF8 file name passed to the writer
            // };
            var data = new byte[GetDebugInfoLength()];
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

            return data;
        }

        public int GetDebugInfoLength()
        {
            return
                4 +     // DWORD dwSig
                16 +    // GUID guidSig
                4 +     // DWORD age
                Encoding.UTF8.GetByteCount(fileName) +
                1;      // char szPDB[0]
        }

        public void OpenMethod(SymbolToken symbolToken, MethodBase mb)
        {
            currentMethod = symbolToken.value;
        }

        public void CloseMethod()
        {
            if (document != null)
            {
                var blob = DefineSequencePoints();
                methods.Add(new MethodRec { Token = currentMethod, Document = document.rId, SequencePoints = blob, Scopes = scopes.ToArray() });
                document = null;
                ilOffsets = null;
                startLines = null;
                startColumns = null;
                endLines = null;
                endColumns = null;
                scope = null;
                scopes.Clear();
            }
            currentMethod = 0;
            localVarSigToken = 0;
        }

        public void RemapToken(int oldToken, int newToken)
        {
            tokenMap.Add(oldToken, newToken);
        }

        public void DefineSequencePoints(ISymbolDocumentWriter document, int[] ilOffsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns)
        {
            // we only support a single call per method
            Debug.Assert(this.document == null);

            this.document = (DocumentImpl)document;
            this.ilOffsets = ilOffsets;
            this.startLines = startLines;
            this.startColumns = startColumns;
            this.endLines = endLines;
            this.endColumns = endColumns;
        }

        private int DefineSequencePoints()
        {
            // Sequence Points Blob
            var bb = new ByteBuffer(ilOffsets.Length * 10);
            // header
            bb.WriteCompressedUInt(localVarSigToken);   // LocalSignature
            // we don't support multiple documents per method, so we don't need to write InitialDocument

            // sequence-point-record
            bb.WriteCompressedUInt(ilOffsets[0]);
            WriteDeltas(bb, endLines[0] - startLines[0], endColumns[0] - startColumns[0]);
            bb.WriteCompressedUInt(startLines[0]);
            bb.WriteCompressedUInt(startColumns[0]);
            for (var i = 1; i < ilOffsets.Length; i++)
            {
                // make sure we don't accidentally encode a document-record
                Debug.Assert(ilOffsets[i] > ilOffsets[i - 1]);
                // make sure we don't accidentally encode a hidden-sequence-point-record
                Debug.Assert(startLines[i] != endLines[i] || startColumns[i] != endColumns[i]);
                // sequence-point-record
                bb.WriteCompressedUInt(ilOffsets[i] - ilOffsets[i - 1]);
                WriteDeltas(bb, endLines[i] - startLines[i], endColumns[i] - startColumns[i]);
                bb.WriteCompressedInt(startLines[i] - startLines[i - 1]);
                bb.WriteCompressedInt(startColumns[i] - startColumns[i - 1]);
            }
            return Blobs.Add(bb);
        }

        private static void WriteDeltas(ByteBuffer bb, int deltaLines, int deltaColumns)
        {
            bb.WriteCompressedUInt(deltaLines);
            if (deltaLines == 0)
            {
                bb.WriteCompressedUInt(deltaColumns);
            }
            else
            {
                bb.WriteCompressedInt(deltaColumns);
            }
        }

        public void OpenScope(int startOffset)
        {
            scope = new Scope(scope);
            scopes.Add(scope);
            scope.StartOffset = startOffset;
        }

        public void CloseScope(int endOffset)
        {
            scope.Length = endOffset - scope.StartOffset;
            scope = scope.Parent;
        }

        public void UsingNamespace(string usingNamespace)
        {
            scope.Namespaces.Add(usingNamespace);
        }

        public ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType)
        {
            DocumentTable.Record rec;
            rec.Name = AddDocumentNameBlob(url);
            rec.HashAlgorithm = 0;
            rec.Hash = 0;
            rec.Language = Guids.Add(language);
            return new DocumentImpl(this, Document.AddRecord(rec));
        }

        private int AddDocumentNameBlob(string name)
        {
            var bb = new ByteBuffer(7);
            for (var i = name.Length - 1; i >= 0; i--)
            {
                var ch = name[i];
                if (ch == '/' || ch == '\\')
                {
                    bb.Write((byte)ch);
                    WriteDocumentNamePart(bb, name.Substring(0, i));
                    WriteDocumentNamePart(bb, name.Substring(i + 1));
                    return Blobs.Add(bb);
                }
            }
            bb.Write((byte)0);
            WriteDocumentNamePart(bb, name);
            return Blobs.Add(bb);
        }

        private void WriteDocumentNamePart(ByteBuffer bb, string part)
        {
            // LAMESPEC spec says "part is a compressed integer into the #Blob heap"
            bb.WriteCompressedUInt(Blobs.Add(ByteBuffer.Wrap(Encoding.UTF8.GetBytes(part))));
        }

        public void SetUserEntryPoint(SymbolToken symbolToken)
        {
            userEntryPointToken = symbolToken.value;
        }

        public void Close()
        {
            var localScope = new LocalScopeTable();
            var localVariable = new LocalVariableTable();
            var importScope = new ImportScopeTable();
            CreateLocalScopeAndLocalVariables(localScope, localVariable, importScope);

            Strings.Freeze();
            UserStrings.Freeze();
            Guids.Freeze();
            Blobs.Freeze();

            using (var fs = System.IO.File.Create(fileName))
            {
                var tablesForRowCountOnly = moduleBuilder.GetTables();
                var tables = new Table[64];
                tables[DocumentTable.Index] = Document;
                tables[MethodDebugInformationTable.Index] = CreateMethodDebugInformation();
                tables[LocalScopeTable.Index] = localScope;
                tables[LocalVariableTable.Index] = localVariable;
                tables[ImportScopeTable.Index] = importScope;
                for (var i = 0; i < tables.Length; i++)
                {
                    if (tables[i] != null)
                    {
                        tablesForRowCountOnly[i] = tables[i];
                    }
                }
                var mw = new PortablePdbMetadataWriter(fs, tables, tablesForRowCountOnly, Strings.IsBig, Guids.IsBig, Blobs.IsBig);
                Tables.Freeze(mw);
                var pdb = new PdbHeap(guid, timestamp, moduleBuilder.GetTables(), GetEntryPointToken());
                mw.WriteMetadata("PDB v1.0", pdb, Tables, Strings, UserStrings, Guids, Blobs);
                if (IsDeterministic)
                {
                    byte[] hash;
                    using (var sha1 = System.Security.Cryptography.SHA1.Create())
                    {
                        fs.Seek(0, System.IO.SeekOrigin.Begin);
                        hash = sha1.ComputeHash(fs);
                    }
                    timestamp = (uint)BitConverter.ToInt32(hash, 16) | 0x80000000;
                    Array.Resize(ref hash, 16);
                    // set GUID type to "version 4" (random)
                    hash[7] &= 0x0F;
                    hash[7] |= 0x40;
                    hash[8] &= 0x3F;
                    hash[8] |= 0x80;
                    guid = new Guid(hash);
                    fs.Position = pdb.PositionPdbId;
                    PdbHeap.WritePdbId(mw, guid, timestamp);
                }
            }
        }

        private int GetEntryPointToken()
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
            return entryPointToken;
        }

        private void CreateLocalScopeAndLocalVariables(LocalScopeTable localScope, LocalVariableTable localVariable, ImportScopeTable importScope)
        {
            var methods = this.methods.ToArray();
            Array.Sort(methods, (m1, m2) => tokenMap[m1.Token].CompareTo(tokenMap[m2.Token]));
            for (var i = 0; i < methods.Length; i++)
            {
                var scopes = methods[i].Scopes;
                Array.Sort(scopes, (s1, s2) => s1.StartOffset != s2.StartOffset ? s1.StartOffset.CompareTo(s2.StartOffset) : s2.Length.CompareTo(s1.Length));
                for (var j = 0; j < scopes.Length; j++)
                {
                    LocalScopeTable.Record scope;
                    scope.Method = tokenMap[methods[i].Token] & 0xFFFFFF;
                    // TODO we don't set the parent ImportScope, because Visual Studio doesn't seem to need it
                    scope.ImportScope = importScope.FindOrAddRecord(0, CreateNamespaceImportBlob(scopes[j].Namespaces));
                    scope.VariableList = localVariable.RowCount + 1;
                    scope.ConstantList = 1;
                    scope.StartOffset = scopes[j].StartOffset;
                    scope.Length = scopes[j].Length;
                    localScope.AddRecord(scope);
                    for (var k = 0; k < scopes[j].VariableList.Count; k++)
                    {
                        LocalVariableTable.Record variable;
                        variable.Attributes = 0;
                        variable.Index = (short)scopes[j].VariableList[k].Index;
                        variable.Name = Strings.Add(scopes[j].VariableList[k].Name);
                        localVariable.AddRecord(variable);
                    }
                }
            }
        }

        private int CreateNamespaceImportBlob(List<string> namespaces)
        {
            if (namespaces.Count == 0)
            {
                return 0;
            }
            var bb = new ByteBuffer(namespaces.Count * 20);
            foreach (var ns in namespaces)
            {
                // kind
                bb.WriteCompressedUInt(1);
                // target-namespace
                bb.WriteCompressedUInt(Blobs.Add(ByteBuffer.Wrap(Encoding.UTF8.GetBytes(ns))));
            }
            return Blobs.Add(bb);
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

        internal void SetCheckSum(int rId, Guid algorithmId, byte[] checkSum)
        {
            Document.records[rId - 1].HashAlgorithm = Guids.Add(algorithmId);
            Document.records[rId - 1].Hash = Blobs.Add(ByteBuffer.Wrap(checkSum));
        }

        internal void SetSource(int rId, byte[] source)
        {
            // Ref.Emit doesn't implement this either
            throw new NotImplementedException();
        }
    }

    sealed class PdbHeap : SimpleHeap
    {
        private readonly Guid guid;
        private readonly uint timestamp;
        private readonly Table[] referencedTables;
        private readonly int entryPointToken;
        internal long PositionPdbId { get; private set; }

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

        internal static void WritePdbId(MetadataWriter mw, Guid guid, uint timestamp)
        {
            mw.Write(guid.ToByteArray());
            mw.Write(timestamp);
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            // PDB id
            PositionPdbId = mw.Position;
            WritePdbId(mw, guid, timestamp);
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

        internal override string Name
        {
            get { return "#Pdb"; }
        }
    }

    sealed class DocumentImpl : ISymbolDocumentWriter
    {
        private readonly PortablePdbWriter writer;
        internal readonly int rId;

        internal DocumentImpl(PortablePdbWriter writer, int rId)
        {
            this.writer = writer;
            this.rId = rId;
        }

        public void SetCheckSum(Guid algorithmId, byte[] checkSum)
        {
            writer.SetCheckSum(rId, algorithmId, checkSum);
        }

        public void SetSource(byte[] source)
        {
            writer.SetSource(rId, source);
        }
    }
}
