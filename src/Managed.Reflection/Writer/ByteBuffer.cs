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
using System.Text;
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Writer
{
    sealed class ByteBuffer
    {
        private byte[] buffer;
        private int pos;
        private int __length;   // __length is only valid if > pos, otherwise pos is the current length

        internal ByteBuffer(int initialCapacity)
        {
            buffer = new byte[initialCapacity];
        }

        private ByteBuffer(byte[] wrap, int length)
        {
            this.buffer = wrap;
            this.pos = length;
        }

        internal int Position
        {
            get { return pos; }
            set
            {
                if (value > this.Length || value > buffer.Length)
                    throw new ArgumentOutOfRangeException();
                __length = Math.Max(__length, pos);
                pos = value;
            }
        }

        internal int Length
        {
            get { return Math.Max(pos, __length); }
        }

        // insert count bytes at the current position (without advancing the current position)
        internal void Insert(int count)
        {
            if (count > 0)
            {
                int len = this.Length;
                int free = buffer.Length - len;
                if (free < count)
                {
                    Grow(count - free);
                }
                Buffer.BlockCopy(buffer, pos, buffer, pos + count, len - pos);
                __length = Math.Max(__length, pos) + count;
            }
            else if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
        }

        private void Grow(int minGrow)
        {
            byte[] newbuf = new byte[Math.Max(buffer.Length + minGrow, buffer.Length * 2)];
            Buffer.BlockCopy(buffer, 0, newbuf, 0, buffer.Length);
            buffer = newbuf;
        }

        // NOTE this does not advance the position
        internal int GetInt32AtCurrentPosition()
        {
            return buffer[pos]
                + (buffer[pos + 1] << 8)
                + (buffer[pos + 2] << 16)
                + (buffer[pos + 3] << 24);
        }

        // NOTE this does not advance the position
        internal byte GetByteAtCurrentPosition()
        {
            return buffer[pos];
        }

        // return the number of bytes that the compressed int at the current position takes
        internal int GetCompressedUIntLength()
        {
            switch (buffer[pos] & 0xC0)
            {
                default:
                    return 1;
                case 0x80:
                    return 2;
                case 0xC0:
                    return 4;
            }
        }

        internal void Write(byte[] value)
        {
            if (pos + value.Length > buffer.Length)
                Grow(value.Length);
            Buffer.BlockCopy(value, 0, buffer, pos, value.Length);
            pos += value.Length;
        }

        internal void Write(byte value)
        {
            if (pos == buffer.Length)
                Grow(1);
            buffer[pos++] = value;
        }

        internal void Write(sbyte value)
        {
            Write((byte)value);
        }

        internal void Write(ushort value)
        {
            Write((short)value);
        }

        internal void Write(short value)
        {
            if (pos + 2 > buffer.Length)
                Grow(2);
            buffer[pos++] = (byte)value;
            buffer[pos++] = (byte)(value >> 8);
        }

        internal void Write(uint value)
        {
            Write((int)value);
        }

        internal void Write(int value)
        {
            if (pos + 4 > buffer.Length)
                Grow(4);
            buffer[pos++] = (byte)value;
            buffer[pos++] = (byte)(value >> 8);
            buffer[pos++] = (byte)(value >> 16);
            buffer[pos++] = (byte)(value >> 24);
        }

        internal void Write(ulong value)
        {
            Write((long)value);
        }

        internal void Write(long value)
        {
            if (pos + 8 > buffer.Length)
                Grow(8);
            buffer[pos++] = (byte)value;
            buffer[pos++] = (byte)(value >> 8);
            buffer[pos++] = (byte)(value >> 16);
            buffer[pos++] = (byte)(value >> 24);
            buffer[pos++] = (byte)(value >> 32);
            buffer[pos++] = (byte)(value >> 40);
            buffer[pos++] = (byte)(value >> 48);
            buffer[pos++] = (byte)(value >> 56);
        }

        internal void Write(float value)
        {
            Write(SingleConverter.SingleToInt32Bits(value));
        }

        internal void Write(double value)
        {
            Write(BitConverter.DoubleToInt64Bits(value));
        }

        internal void Write(string str)
        {
            if (str == null)
            {
                Write((byte)0xFF);
            }
            else
            {
                byte[] buf = Encoding.UTF8.GetBytes(str);
                WriteCompressedUInt(buf.Length);
                Write(buf);
            }
        }

        internal void WriteCompressedUInt(int value)
        {
            if (value <= 0x7F)
            {
                Write((byte)value);
            }
            else if (value <= 0x3FFF)
            {
                Write((byte)(0x80 | (value >> 8)));
                Write((byte)value);
            }
            else
            {
                Write((byte)(0xC0 | (value >> 24)));
                Write((byte)(value >> 16));
                Write((byte)(value >> 8));
                Write((byte)value);
            }
        }

        internal void WriteCompressedInt(int value)
        {
            if (value >= 0)
            {
                WriteCompressedUInt(value << 1);
            }
            else if (value >= -64)
            {
                value = ((value << 1) & 0x7F) | 1;
                Write((byte)value);
            }
            else if (value >= -8192)
            {
                value = ((value << 1) & 0x3FFF) | 1;
                Write((byte)(0x80 | (value >> 8)));
                Write((byte)value);
            }
            else
            {
                value = ((value << 1) & 0x1FFFFFFF) | 1;
                Write((byte)(0xC0 | (value >> 24)));
                Write((byte)(value >> 16));
                Write((byte)(value >> 8));
                Write((byte)value);
            }
        }

        internal void Write(ByteBuffer bb)
        {
            if (pos + bb.Length > buffer.Length)
                Grow(bb.Length);
            Buffer.BlockCopy(bb.buffer, 0, buffer, pos, bb.Length);
            pos += bb.Length;
        }

        internal void WriteTo(System.IO.Stream stream)
        {
            stream.Write(buffer, 0, this.Length);
        }

        internal void Clear()
        {
            pos = 0;
            __length = 0;
        }

        internal void Align(int alignment)
        {
            if (pos + alignment > buffer.Length)
                Grow(alignment);
            int newpos = (pos + alignment - 1) & ~(alignment - 1);
            while (pos < newpos)
                buffer[pos++] = 0;
        }

        internal void WriteTypeDefOrRefEncoded(int token)
        {
            switch (token >> 24)
            {
                case TypeDefTable.Index:
                    WriteCompressedUInt((token & 0xFFFFFF) << 2 | 0);
                    break;
                case TypeRefTable.Index:
                    WriteCompressedUInt((token & 0xFFFFFF) << 2 | 1);
                    break;
                case TypeSpecTable.Index:
                    WriteCompressedUInt((token & 0xFFFFFF) << 2 | 2);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal byte[] ToArray()
        {
            int len = this.Length;
            byte[] buf = new byte[len];
            Buffer.BlockCopy(buffer, 0, buf, 0, len);
            return buf;
        }

        internal static ByteBuffer Wrap(byte[] buf)
        {
            return new ByteBuffer(buf, buf.Length);
        }

        internal static ByteBuffer Wrap(byte[] buf, int length)
        {
            return new ByteBuffer(buf, length);
        }

        internal bool Match(int pos, ByteBuffer bb2, int pos2, int len)
        {
            for (int i = 0; i < len; i++)
            {
                if (buffer[pos + i] != bb2.buffer[pos2 + i])
                {
                    return false;
                }
            }
            return true;
        }

        internal int Hash()
        {
            int hash = 0;
            int len = this.Length;
            for (int i = 0; i < len; i++)
            {
                hash *= 37;
                hash ^= buffer[i];
            }
            return hash;
        }

        internal Managed.Reflection.Reader.ByteReader GetBlob(int offset)
        {
            return Managed.Reflection.Reader.ByteReader.FromBlob(buffer, offset);
        }
    }
}
