/*
  The MIT License (MIT) 
  Copyright (C) 2009-2012 Jeroen Frijters
  
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
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Reader
{
    sealed class EventInfoImpl : EventInfo
    {
        private readonly ModuleReader module;
        private readonly Type declaringType;
        private readonly int index;
        private bool isPublic;
        private bool isNonPrivate;
        private bool isStatic;
        private bool flagsCached;

        internal EventInfoImpl(ModuleReader module, Type declaringType, int index)
        {
            this.module = module;
            this.declaringType = declaringType;
            this.index = index;
        }

        public override bool Equals(object obj)
        {
            EventInfoImpl other = obj as EventInfoImpl;
            return other != null && other.declaringType == declaringType && other.index == index;
        }

        public override int GetHashCode()
        {
            return declaringType.GetHashCode() * 123 + index;
        }

        public override EventAttributes Attributes
        {
            get { return (EventAttributes)module.Event.records[index].EventFlags; }
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            return module.MethodSemantics.GetMethod(module, this.MetadataToken, nonPublic, MethodSemanticsTable.AddOn);
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return module.MethodSemantics.GetMethod(module, this.MetadataToken, nonPublic, MethodSemanticsTable.Fire);
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return module.MethodSemantics.GetMethod(module, this.MetadataToken, nonPublic, MethodSemanticsTable.RemoveOn);
        }

        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            return module.MethodSemantics.GetMethods(module, this.MetadataToken, nonPublic, MethodSemanticsTable.Other);
        }

        public override MethodInfo[] __GetMethods()
        {
            return module.MethodSemantics.GetMethods(module, this.MetadataToken, true, -1);
        }

        public override Type EventHandlerType
        {
            get { return module.ResolveType(module.Event.records[index].EventType, declaringType); }
        }

        public override string Name
        {
            get { return module.GetString(module.Event.records[index].Name); }
        }

        public override Type DeclaringType
        {
            get { return declaringType; }
        }

        public override Module Module
        {
            get { return module; }
        }

        public override int MetadataToken
        {
            get { return (EventTable.Index << 24) + index + 1; }
        }

        internal override bool IsPublic
        {
            get
            {
                if (!flagsCached)
                {
                    ComputeFlags();
                }
                return isPublic;
            }
        }

        internal override bool IsNonPrivate
        {
            get
            {
                if (!flagsCached)
                {
                    ComputeFlags();
                }
                return isNonPrivate;
            }
        }

        internal override bool IsStatic
        {
            get
            {
                if (!flagsCached)
                {
                    ComputeFlags();
                }
                return isStatic;
            }
        }

        private void ComputeFlags()
        {
            module.MethodSemantics.ComputeFlags(module, this.MetadataToken, out isPublic, out isNonPrivate, out isStatic);
            flagsCached = true;
        }

        internal override bool IsBaked
        {
            get { return true; }
        }

        internal override int GetCurrentToken()
        {
            return this.MetadataToken;
        }
    }
}
