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

namespace Managed.Reflection.Emit
{
    public struct EventToken
    {
        public static readonly EventToken Empty;
        private readonly int token;

        internal EventToken(int token)
        {
            this.token = token;
        }

        public int Token
        {
            get { return token; }
        }

        public override bool Equals(object obj)
        {
            return obj as EventToken? == this;
        }

        public override int GetHashCode()
        {
            return token;
        }

        public bool Equals(EventToken other)
        {
            return this == other;
        }

        public static bool operator ==(EventToken et1, EventToken et2)
        {
            return et1.token == et2.token;
        }

        public static bool operator !=(EventToken et1, EventToken et2)
        {
            return et1.token != et2.token;
        }
    }

    public struct FieldToken
    {
        public static readonly FieldToken Empty;
        private readonly int token;

        internal FieldToken(int token)
        {
            this.token = token;
        }

        public int Token
        {
            get { return token; }
        }

        public override bool Equals(object obj)
        {
            return obj as FieldToken? == this;
        }

        public override int GetHashCode()
        {
            return token;
        }

        public bool Equals(FieldToken other)
        {
            return this == other;
        }

        public static bool operator ==(FieldToken ft1, FieldToken ft2)
        {
            return ft1.token == ft2.token;
        }

        public static bool operator !=(FieldToken ft1, FieldToken ft2)
        {
            return ft1.token != ft2.token;
        }
    }

    public struct MethodToken
    {
        public static readonly MethodToken Empty;
        private readonly int token;

        internal MethodToken(int token)
        {
            this.token = token;
        }

        public int Token
        {
            get { return token; }
        }

        public override bool Equals(object obj)
        {
            return obj as MethodToken? == this;
        }

        public override int GetHashCode()
        {
            return token;
        }

        public bool Equals(MethodToken other)
        {
            return this == other;
        }

        public static bool operator ==(MethodToken mt1, MethodToken mt2)
        {
            return mt1.token == mt2.token;
        }

        public static bool operator !=(MethodToken mt1, MethodToken mt2)
        {
            return mt1.token != mt2.token;
        }
    }

    public struct SignatureToken
    {
        public static readonly SignatureToken Empty;
        private readonly int token;

        internal SignatureToken(int token)
        {
            this.token = token;
        }

        public int Token
        {
            get { return token; }
        }

        public override bool Equals(object obj)
        {
            return obj as SignatureToken? == this;
        }

        public override int GetHashCode()
        {
            return token;
        }

        public bool Equals(SignatureToken other)
        {
            return this == other;
        }

        public static bool operator ==(SignatureToken st1, SignatureToken st2)
        {
            return st1.token == st2.token;
        }

        public static bool operator !=(SignatureToken st1, SignatureToken st2)
        {
            return st1.token != st2.token;
        }
    }

    public struct StringToken
    {
        private readonly int token;

        internal StringToken(int token)
        {
            this.token = token;
        }

        public int Token
        {
            get { return token; }
        }

        public override bool Equals(object obj)
        {
            return obj as StringToken? == this;
        }

        public override int GetHashCode()
        {
            return token;
        }

        public bool Equals(StringToken other)
        {
            return this == other;
        }

        public static bool operator ==(StringToken st1, StringToken st2)
        {
            return st1.token == st2.token;
        }

        public static bool operator !=(StringToken st1, StringToken st2)
        {
            return st1.token != st2.token;
        }
    }

    public struct TypeToken
    {
        public static readonly TypeToken Empty;
        private readonly int token;

        internal TypeToken(int token)
        {
            this.token = token;
        }

        public int Token
        {
            get { return token; }
        }

        public override bool Equals(object obj)
        {
            return obj as TypeToken? == this;
        }

        public override int GetHashCode()
        {
            return token;
        }

        public bool Equals(TypeToken other)
        {
            return this == other;
        }

        public static bool operator ==(TypeToken tt1, TypeToken tt2)
        {
            return tt1.token == tt2.token;
        }

        public static bool operator !=(TypeToken tt1, TypeToken tt2)
        {
            return tt1.token != tt2.token;
        }
    }
}
