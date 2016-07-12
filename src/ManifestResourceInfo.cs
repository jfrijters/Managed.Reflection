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
using Managed.Reflection.Reader;

namespace Managed.Reflection
{
    public sealed class ManifestResourceInfo
    {
        private readonly ModuleReader module;
        private readonly int index;

        internal ManifestResourceInfo(ModuleReader module, int index)
        {
            this.module = module;
            this.index = index;
        }

        public ResourceAttributes __ResourceAttributes
        {
            get { return (ResourceAttributes)module.ManifestResource.records[index].Flags; }
        }

        public int __Offset
        {
            get { return module.ManifestResource.records[index].Offset; }
        }

        public ResourceLocation ResourceLocation
        {
            get
            {
                int implementation = module.ManifestResource.records[index].Implementation;
                if ((implementation >> 24) == AssemblyRefTable.Index)
                {
                    Assembly asm = ReferencedAssembly;
                    if (asm == null || asm.__IsMissing)
                    {
                        return ResourceLocation.ContainedInAnotherAssembly;
                    }
                    return asm.GetManifestResourceInfo(module.GetString(module.ManifestResource.records[index].Name)).ResourceLocation | ResourceLocation.ContainedInAnotherAssembly;
                }
                else if ((implementation >> 24) == FileTable.Index)
                {
                    if ((implementation & 0xFFFFFF) == 0)
                    {
                        return ResourceLocation.ContainedInManifestFile | ResourceLocation.Embedded;
                    }
                    return 0;
                }
                else
                {
                    throw new BadImageFormatException();
                }
            }
        }

        public Assembly ReferencedAssembly
        {
            get
            {
                int implementation = module.ManifestResource.records[index].Implementation;
                if ((implementation >> 24) == AssemblyRefTable.Index)
                {
                    return module.ResolveAssemblyRef((implementation & 0xFFFFFF) - 1);
                }
                return null;
            }
        }

        public string FileName
        {
            get
            {
                int implementation = module.ManifestResource.records[index].Implementation;
                if ((implementation >> 24) == FileTable.Index)
                {
                    if ((implementation & 0xFFFFFF) == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return module.GetString(module.File.records[(implementation & 0xFFFFFF) - 1].Name);
                    }
                }
                return null;
            }
        }
    }
}
