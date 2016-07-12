/*
  The MIT License (MIT) 
  Copyright (C) 2012 Jeroen Frijters
  
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

namespace Managed.Reflection.Emit
{
    public struct ExceptionHandler : IEquatable<ExceptionHandler>
    {
        private readonly int tryOffset;
        private readonly int tryLength;
        private readonly int filterOffset;
        private readonly int handlerOffset;
        private readonly int handlerLength;
        private readonly ExceptionHandlingClauseOptions kind;
        private readonly int exceptionTypeToken;

        public ExceptionHandler(int tryOffset, int tryLength, int filterOffset, int handlerOffset, int handlerLength, ExceptionHandlingClauseOptions kind, int exceptionTypeToken)
        {
            if (tryOffset < 0 || tryLength < 0 || filterOffset < 0 || handlerOffset < 0 || handlerLength < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            this.tryOffset = tryOffset;
            this.tryLength = tryLength;
            this.filterOffset = filterOffset;
            this.handlerOffset = handlerOffset;
            this.handlerLength = handlerLength;
            this.kind = kind;
            this.exceptionTypeToken = exceptionTypeToken;
        }

        public int TryOffset
        {
            get { return tryOffset; }
        }

        public int TryLength
        {
            get { return tryLength; }
        }

        public int FilterOffset
        {
            get { return filterOffset; }
        }

        public int HandlerOffset
        {
            get { return handlerOffset; }
        }

        public int HandlerLength
        {
            get { return handlerLength; }
        }

        public ExceptionHandlingClauseOptions Kind
        {
            get { return kind; }
        }

        public int ExceptionTypeToken
        {
            get { return exceptionTypeToken; }
        }

        public bool Equals(ExceptionHandler other)
        {
            return tryOffset == other.tryOffset
                && tryLength == other.tryLength
                && filterOffset == other.filterOffset
                && handlerOffset == other.handlerOffset
                && handlerLength == other.handlerLength
                && kind == other.kind
                && exceptionTypeToken == other.exceptionTypeToken;
        }

        public override bool Equals(object obj)
        {
            ExceptionHandler? other = obj as ExceptionHandler?;
            return other != null && Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return tryOffset ^ tryLength * 33 ^ filterOffset * 333 ^ handlerOffset * 3333 ^ handlerLength * 33333;
        }

        public static bool operator ==(ExceptionHandler left, ExceptionHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExceptionHandler left, ExceptionHandler right)
        {
            return !left.Equals(right);
        }
    }
}
