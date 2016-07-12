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
using System.Text;

namespace Managed.Reflection.Reader
{
    sealed class ByteReader
    {
        private byte[] buffer;
        private int pos;
        private int end;

        internal ByteReader(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer;
            this.pos = offset;
            this.end = pos + length;
        }

        internal static ByteReader FromBlob(byte[] blobHeap, int blob)
        {
            ByteReader br = new ByteReader(blobHeap, blob, 4);
            int length = br.ReadCompressedUInt();
            br.end = br.pos + length;
            return br;
        }

        internal int Length
        {
            get { return end - pos; }
        }

        internal byte PeekByte()
        {
            if (pos == end)
                throw new BadImageFormatException();
            return buffer[pos];
        }

        internal byte ReadByte()
        {
            if (pos == end)
                throw new BadImageFormatException();
            return buffer[pos++];
        }

        internal byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new BadImageFormatException();
            if (end - pos < count)
                throw new BadImageFormatException();
            byte[] buf = new byte[count];
            Buffer.BlockCopy(buffer, pos, buf, 0, count);
            pos += count;
            return buf;
        }

        internal int ReadCompressedUInt()
        {
            byte b1 = ReadByte();
            if (b1 <= 0x7F)
            {
                return b1;
            }
            else if ((b1 & 0xC0) == 0x80)
            {
                byte b2 = ReadByte();
                return ((b1 & 0x3F) << 8) | b2;
            }
            else
            {
                byte b2 = ReadByte();
                byte b3 = ReadByte();
                byte b4 = ReadByte();
                return ((b1 & 0x3F) << 24) + (b2 << 16) + (b3 << 8) + b4;
            }
        }

        internal int ReadCompressedInt()
        {
            byte b1 = PeekByte();
            int value = ReadCompressedUInt();
            if ((value & 1) == 0)
            {
                return value >> 1;
            }
            else
            {
                switch (b1 & 0xC0)
                {
                    case 0:
                    case 0x40:
                        return (value >> 1) - 0x40;
                    case 0x80:
                        return (value >> 1) - 0x2000;
                    default:
                        return (value >> 1) - 0x10000000;
                }
            }
        }

        internal string ReadString()
        {
            if (PeekByte() == 0xFF)
            {
                pos++;
                return null;
            }
            int length = ReadCompressedUInt();
            string str = Encoding.UTF8.GetString(buffer, pos, length);
            pos += length;
            return str;
        }

        internal char ReadChar()
        {
            return (char)ReadInt16();
        }

        internal sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        internal short ReadInt16()
        {
            if (end - pos < 2)
                throw new BadImageFormatException();
            byte b1 = buffer[pos++];
            byte b2 = buffer[pos++];
            return (short)(b1 | (b2 << 8));
        }

        internal ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        internal int ReadInt32()
        {
            if (end - pos < 4)
                throw new BadImageFormatException();
            byte b1 = buffer[pos++];
            byte b2 = buffer[pos++];
            byte b3 = buffer[pos++];
            byte b4 = buffer[pos++];
            return (int)(b1 | (b2 << 8) | (b3 << 16) | (b4 << 24));
        }

        internal uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        internal long ReadInt64()
        {
            ulong lo = ReadUInt32();
            ulong hi = ReadUInt32();
            return (long)(lo | (hi << 32));
        }

        internal ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        internal float ReadSingle()
        {
            return SingleConverter.Int32BitsToSingle(ReadInt32());
        }

        internal double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble(ReadInt64());
        }

        internal ByteReader Slice(int length)
        {
            if (end - pos < length)
                throw new BadImageFormatException();
            ByteReader br = new ByteReader(buffer, pos, length);
            pos += length;
            return br;
        }

        // NOTE this method only works if the original offset was aligned and for alignments that are a power of 2
        internal void Align(int alignment)
        {
            alignment--;
            pos = (pos + alignment) & ~alignment;
        }
    }
}
