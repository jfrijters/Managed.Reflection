/*
  The MIT License (MIT) 
  Copyright (C) 2011 Jeroen Frijters
  
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
using System.Collections.Generic;

namespace Managed.Reflection.Emit
{
    public sealed class CustomModifiersBuilder
    {
        private readonly List<Item> list = new List<Item>();

        internal struct Item
        {
            internal Type type;
            internal bool required;
        }

        public void AddRequired(Type type)
        {
            Item item;
            item.type = type;
            item.required = true;
            list.Add(item);
        }

        public void AddOptional(Type type)
        {
            Item item;
            item.type = type;
            item.required = false;
            list.Add(item);
        }

        // this adds the custom modifiers in the same order as the normal SRE APIs
        // (the advantage over using the SRE APIs is that a CustomModifiers object is slightly more efficient,
        // because unlike the Type arrays it doesn't need to be copied)
        public void Add(Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            foreach (CustomModifiers.Entry entry in CustomModifiers.FromReqOpt(requiredCustomModifiers, optionalCustomModifiers))
            {
                Item item;
                item.type = entry.Type;
                item.required = entry.IsRequired;
                list.Add(item);
            }
        }

        public CustomModifiers Create()
        {
            return new CustomModifiers(list);
        }
    }
}
