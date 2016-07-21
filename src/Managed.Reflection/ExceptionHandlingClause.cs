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
using Managed.Reflection.Reader;

namespace Managed.Reflection
{
    [Flags]
    public enum ExceptionHandlingClauseOptions
    {
        Clause = 0x0000,
        Filter = 0x0001,
        Finally = 0x0002,
        Fault = 0x0004,
    }

    public sealed class ExceptionHandlingClause
    {
        private readonly int flags;
        private readonly int tryOffset;
        private readonly int tryLength;
        private readonly int handlerOffset;
        private readonly int handlerLength;
        private readonly Type catchType;
        private readonly int filterOffset;

        internal ExceptionHandlingClause(ModuleReader module, int flags, int tryOffset, int tryLength, int handlerOffset, int handlerLength, int classTokenOrfilterOffset, IGenericContext context)
        {
            this.flags = flags;
            this.tryOffset = tryOffset;
            this.tryLength = tryLength;
            this.handlerOffset = handlerOffset;
            this.handlerLength = handlerLength;
            this.catchType = flags == (int)ExceptionHandlingClauseOptions.Clause && classTokenOrfilterOffset != 0 ? module.ResolveType(classTokenOrfilterOffset, context) : null;
            this.filterOffset = flags == (int)ExceptionHandlingClauseOptions.Filter ? classTokenOrfilterOffset : 0;
        }

        public Type CatchType
        {
            get { return catchType; }
        }

        public int FilterOffset
        {
            get { return filterOffset; }
        }

        public ExceptionHandlingClauseOptions Flags
        {
            get { return (ExceptionHandlingClauseOptions)flags; }
        }

        public int HandlerLength
        {
            get { return handlerLength; }
        }

        public int HandlerOffset
        {
            get { return handlerOffset; }
        }

        public int TryLength
        {
            get { return tryLength; }
        }

        public int TryOffset
        {
            get { return tryOffset; }
        }
    }
}
