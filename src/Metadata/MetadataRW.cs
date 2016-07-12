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

namespace Managed.Reflection.Metadata
{
    // base class for MetadataReader and MetadataWriter
    abstract class MetadataRW
    {
        internal readonly bool bigStrings;
        internal readonly bool bigGuids;
        internal readonly bool bigBlobs;
        internal readonly bool bigResolutionScope;
        internal readonly bool bigTypeDefOrRef;
        internal readonly bool bigMemberRefParent;
        internal readonly bool bigHasCustomAttribute;
        internal readonly bool bigCustomAttributeType;
        internal readonly bool bigMethodDefOrRef;
        internal readonly bool bigHasConstant;
        internal readonly bool bigHasSemantics;
        internal readonly bool bigHasFieldMarshal;
        internal readonly bool bigHasDeclSecurity;
        internal readonly bool bigTypeOrMethodDef;
        internal readonly bool bigMemberForwarded;
        internal readonly bool bigImplementation;
        internal readonly bool bigField;
        internal readonly bool bigMethodDef;
        internal readonly bool bigParam;
        internal readonly bool bigTypeDef;
        internal readonly bool bigProperty;
        internal readonly bool bigEvent;
        internal readonly bool bigGenericParam;
        internal readonly bool bigModuleRef;

        protected MetadataRW(Module module, bool bigStrings, bool bigGuids, bool bigBlobs)
        {
            this.bigStrings = bigStrings;
            this.bigGuids = bigGuids;
            this.bigBlobs = bigBlobs;
            this.bigField = module.Field.IsBig;
            this.bigMethodDef = module.MethodDef.IsBig;
            this.bigParam = module.Param.IsBig;
            this.bigTypeDef = module.TypeDef.IsBig;
            this.bigProperty = module.Property.IsBig;
            this.bigEvent = module.Event.IsBig;
            this.bigGenericParam = module.GenericParam.IsBig;
            this.bigModuleRef = module.ModuleRef.IsBig;
            this.bigResolutionScope = IsBig(2, module.ModuleTable, module.ModuleRef, module.AssemblyRef, module.TypeRef);
            this.bigTypeDefOrRef = IsBig(2, module.TypeDef, module.TypeRef, module.TypeSpec);
            this.bigMemberRefParent = IsBig(3, module.TypeDef, module.TypeRef, module.ModuleRef, module.MethodDef, module.TypeSpec);
            this.bigMethodDefOrRef = IsBig(1, module.MethodDef, module.MemberRef);
            this.bigHasCustomAttribute = IsBig(5, module.MethodDef, module.Field, module.TypeRef, module.TypeDef, module.Param, module.InterfaceImpl, module.MemberRef,
                module.ModuleTable, /*module.Permission,*/ module.Property, module.Event, module.StandAloneSig, module.ModuleRef, module.TypeSpec, module.AssemblyTable,
                module.AssemblyRef, module.File, module.ExportedType, module.ManifestResource);
            this.bigCustomAttributeType = IsBig(3, module.MethodDef, module.MemberRef);
            this.bigHasConstant = IsBig(2, module.Field, module.Param, module.Property);
            this.bigHasSemantics = IsBig(1, module.Event, module.Property);
            this.bigHasFieldMarshal = IsBig(1, module.Field, module.Param);
            this.bigHasDeclSecurity = IsBig(2, module.TypeDef, module.MethodDef, module.AssemblyTable);
            this.bigTypeOrMethodDef = IsBig(1, module.TypeDef, module.MethodDef);
            this.bigMemberForwarded = IsBig(1, module.Field, module.MethodDef);
            this.bigImplementation = IsBig(2, module.File, module.AssemblyRef, module.ExportedType);
        }

        private static bool IsBig(int bitsUsed, params Table[] tables)
        {
            int limit = 1 << (16 - bitsUsed);
            foreach (Table table in tables)
            {
                if (table.RowCount >= limit)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
