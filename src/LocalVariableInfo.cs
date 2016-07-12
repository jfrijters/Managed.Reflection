/*
  The MIT License (MIT) 
  Copyright (C) 2009-2011 Jeroen Frijters
  
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

namespace Managed.Reflection
{
    public class LocalVariableInfo
    {
        private readonly int index;
        private readonly Type type;
        private readonly bool pinned;
        private readonly CustomModifiers customModifiers;

        internal LocalVariableInfo(int index, Type type, bool pinned)
        {
            this.index = index;
            this.type = type;
            this.pinned = pinned;
        }

        internal LocalVariableInfo(int index, Type type, bool pinned, CustomModifiers customModifiers)
            : this(index, type, pinned)
        {
            this.customModifiers = customModifiers;
        }

        public bool IsPinned
        {
            get { return pinned; }
        }

        public int LocalIndex
        {
            get { return index; }
        }

        public Type LocalType
        {
            get { return type; }
        }

        public CustomModifiers __GetCustomModifiers()
        {
            return customModifiers;
        }

        public override string ToString()
        {
            return String.Format(pinned ? "{0} ({1}) (pinned)" : "{0} ({1})", type, index);
        }
    }
}
