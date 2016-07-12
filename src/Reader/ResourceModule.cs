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
using System.Collections.Generic;

namespace Managed.Reflection.Reader
{
    sealed class ResourceModule : NonPEModule
    {
        private readonly ModuleReader manifest;
        private readonly int index;
        private readonly string location;

        internal ResourceModule(ModuleReader manifest, int index, string location)
            : base(manifest.universe)
        {
            this.manifest = manifest;
            this.index = index;
            this.location = location;
        }

        public override int MDStreamVersion
        {
            get { throw new NotSupportedException(); }
        }

        public override bool IsResource()
        {
            return true;
        }

        public override Assembly Assembly
        {
            get { return manifest.Assembly; }
        }

        public override string FullyQualifiedName
        {
            get { return location ?? "<Unknown>"; }
        }

        public override string Name
        {
            get { return location == null ? "<Unknown>" : System.IO.Path.GetFileName(location); }
        }

        public override string ScopeName
        {
            get { return manifest.GetString(manifest.File.records[index].Name); }
        }

        public override Guid ModuleVersionId
        {
            get { throw new NotSupportedException(); }
        }

        public override byte[] __ModuleHash
        {
            get
            {
                int blob = manifest.File.records[index].HashValue;
                return blob == 0 ? Empty<byte>.Array : manifest.GetBlobCopy(blob);
            }
        }

        internal override Type FindType(TypeName typeName)
        {
            return null;
        }

        internal override Type FindTypeIgnoreCase(TypeName lowerCaseName)
        {
            return null;
        }

        internal override void GetTypesImpl(List<Type> list)
        {
        }

        protected override Exception ArgumentOutOfRangeException()
        {
            return new NotSupportedException();
        }
    }
}
