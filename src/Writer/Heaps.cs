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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Writer
{
    abstract class Heap
    {
        protected bool frozen;
        protected int unalignedlength;

        internal void Write(MetadataWriter mw)
        {
            uint pos = mw.Position;
            WriteImpl(mw);
            Debug.Assert(mw.Position == pos + unalignedlength);
            int align = Length - unalignedlength;
            for (int i = 0; i < align; i++)
            {
                mw.Write((byte)0);
            }
        }

        internal bool IsBig
        {
            get { return Length > 65535; }
        }

        internal int Length
        {
            get
            {
                if (!frozen)
                    throw new InvalidOperationException();
                return (unalignedlength + 3) & ~3;
            }
        }

        protected abstract void WriteImpl(MetadataWriter mw);
    }

    abstract class SimpleHeap : Heap
    {
        internal void Freeze()
        {
            if (frozen)
                throw new InvalidOperationException();
            frozen = true;
            unalignedlength = GetLength();
        }

        protected abstract int GetLength();
    }

    sealed class TableHeap : Heap
    {
        internal void Freeze(MetadataWriter mw)
        {
            if (frozen)
                throw new InvalidOperationException();
            frozen = true;
            unalignedlength = GetLength(mw);
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            Table[] tables = mw.GetTables();
            // Header
            mw.Write(0);        // Reserved
            int ver = mw.MDStreamVersion;
            mw.Write((byte)(ver >> 16));    // MajorVersion
            mw.Write((byte)ver);            // MinorVersion
            byte heapSizes = 0;
            if (mw.bigStrings)
            {
                heapSizes |= 0x01;
            }
            if (mw.bigGuids)
            {
                heapSizes |= 0x02;
            }
            if (mw.bigBlobs)
            {
                heapSizes |= 0x04;
            }
            mw.Write(heapSizes);// HeapSizes
            // LAMESPEC spec says reserved, but .NET 2.0 Ref.Emit sets it to 0x10
            mw.Write((byte)0x10);// Reserved
            long bit = 1;
            long valid = 0;
            foreach (Table table in tables)
            {
                if (table != null && table.RowCount > 0)
                {
                    valid |= bit;
                }
                bit <<= 1;
            }
            mw.Write(valid);// Valid
            mw.Write(0x0016003301FA00L);// Sorted
            // Rows
            foreach (Table table in tables)
            {
                if (table != null && table.RowCount > 0)
                {
                    mw.Write(table.RowCount);
                }
            }
            // Tables
            foreach (Table table in tables)
            {
                if (table != null && table.RowCount > 0)
                {
                    uint pos = mw.Position;
                    table.Write(mw);
                    Debug.Assert(mw.Position - pos == table.GetLength(mw));
                }
            }
            // unexplained extra padding
            mw.Write((byte)0);
        }

        private static int GetLength(MetadataWriter mw)
        {
            int len = 4 + 4 + 8 + 8;
            foreach (Table table in mw.GetTables())
            {
                if (table != null && table.RowCount > 0)
                {
                    len += 4;   // row count
                    len += table.GetLength(mw);
                }
            }
            // note that we pad one extra (unexplained) byte
            return len + 1;
        }
    }

    sealed class StringHeap : SimpleHeap
    {
        private List<string> list = new List<string>();
        private Dictionary<string, int> strings = new Dictionary<string, int>();
        private int nextOffset;

        internal StringHeap()
        {
            Add("");
        }

        internal int Add(string str)
        {
            Debug.Assert(!frozen);
            int offset;
            if (!strings.TryGetValue(str, out offset))
            {
                offset = nextOffset;
                nextOffset += System.Text.Encoding.UTF8.GetByteCount(str) + 1;
                list.Add(str);
                strings.Add(str, offset);
            }
            return offset;
        }

        internal string Find(int index)
        {
            foreach (KeyValuePair<string, int> kv in strings)
            {
                if (kv.Value == index)
                {
                    return kv.Key;
                }
            }
            return null;
        }

        protected override int GetLength()
        {
            return nextOffset;
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            foreach (string str in list)
            {
                mw.Write(System.Text.Encoding.UTF8.GetBytes(str));
                mw.Write((byte)0);
            }
        }
    }

    sealed class UserStringHeap : SimpleHeap
    {
        private List<string> list = new List<string>();
        private Dictionary<string, int> strings = new Dictionary<string, int>();
        private int nextOffset;

        internal UserStringHeap()
        {
            nextOffset = 1;
        }

        internal bool IsEmpty
        {
            get { return nextOffset == 1; }
        }

        internal int Add(string str)
        {
            Debug.Assert(!frozen);
            int offset;
            if (!strings.TryGetValue(str, out offset))
            {
                int length = str.Length * 2 + 1 + MetadataWriter.GetCompressedUIntLength(str.Length * 2 + 1);
                if (nextOffset + length > 0xFFFFFF)
                {
                    throw new FileFormatLimitationExceededException("No logical space left to create more user strings.", FileFormatLimitationExceededException.META_E_STRINGSPACE_FULL);
                }
                offset = nextOffset;
                nextOffset += length;
                list.Add(str);
                strings.Add(str, offset);
            }
            return offset;
        }

        protected override int GetLength()
        {
            return nextOffset;
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            mw.Write((byte)0);
            foreach (string str in list)
            {
                mw.WriteCompressedUInt(str.Length * 2 + 1);
                byte hasSpecialChars = 0;
                foreach (char ch in str)
                {
                    mw.Write((ushort)ch);
                    if (hasSpecialChars == 0 && (ch < 0x20 || ch > 0x7E))
                    {
                        if (ch > 0x7E
                            || (ch >= 0x01 && ch <= 0x08)
                            || (ch >= 0x0E && ch <= 0x1F)
                            || ch == 0x27
                            || ch == 0x2D)
                        {
                            hasSpecialChars = 1;
                        }
                    }
                }
                mw.Write(hasSpecialChars);
            }
        }
    }

    sealed class GuidHeap : SimpleHeap
    {
        private List<Guid> list = new List<Guid>();

        internal GuidHeap()
        {
        }

        internal int Add(Guid guid)
        {
            Debug.Assert(!frozen);
            list.Add(guid);
            return list.Count;
        }

        protected override int GetLength()
        {
            return list.Count * 16;
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            foreach (Guid guid in list)
            {
                mw.Write(guid.ToByteArray());
            }
        }
    }

    sealed class BlobHeap : SimpleHeap
    {
        private Key[] map = new Key[8179];
        private readonly ByteBuffer buf = new ByteBuffer(32);

        private struct Key
        {
            internal Key[] next;
            internal int len;
            internal int hash;
            internal int offset;
        }

        internal BlobHeap()
        {
            buf.Write((byte)0);
        }

        internal int Add(ByteBuffer bb)
        {
            Debug.Assert(!frozen);
            int bblen = bb.Length;
            if (bblen == 0)
            {
                return 0;
            }
            int lenlen = MetadataWriter.GetCompressedUIntLength(bblen);
            int hash = bb.Hash();
            int index = (hash & 0x7FFFFFFF) % map.Length;
            Key[] keys = map;
            int last = index;
            while (keys[index].offset != 0)
            {
                if (keys[index].hash == hash
                    && keys[index].len == bblen
                    && buf.Match(keys[index].offset + lenlen, bb, 0, bblen))
                {
                    return keys[index].offset;
                }
                if (index == last)
                {
                    if (keys[index].next == null)
                    {
                        keys[index].next = new Key[4];
                        keys = keys[index].next;
                        index = 0;
                        break;
                    }
                    keys = keys[index].next;
                    index = -1;
                    last = keys.Length - 1;
                }
                index++;
            }
            int offset = buf.Position;
            buf.WriteCompressedUInt(bblen);
            buf.Write(bb);
            keys[index].len = bblen;
            keys[index].hash = hash;
            keys[index].offset = offset;
            return offset;
        }

        protected override int GetLength()
        {
            return buf.Position;
        }

        protected override void WriteImpl(MetadataWriter mw)
        {
            mw.Write(buf);
        }

        internal bool IsEmpty
        {
            get { return buf.Position == 1; }
        }

        internal Managed.Reflection.Reader.ByteReader GetBlob(int blobIndex)
        {
            return buf.GetBlob(blobIndex);
        }
    }
}
