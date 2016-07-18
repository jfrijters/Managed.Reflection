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
        internal readonly bool bigDocument;
        internal readonly bool bigImportScope;
        internal readonly bool bigLocalVariable;
        internal readonly bool bigLocalConstant;
        internal readonly bool bigHasCustomDebugInformation;

        protected MetadataRW(Table[] tables, bool bigStrings, bool bigGuids, bool bigBlobs)
        {
            this.bigStrings = bigStrings;
            this.bigGuids = bigGuids;
            this.bigBlobs = bigBlobs;
            this.bigField = tables[FieldTable.Index].IsBig;
            this.bigMethodDef = tables[MethodDefTable.Index].IsBig;
            this.bigParam = tables[ParamTable.Index].IsBig;
            this.bigTypeDef = tables[TypeDefTable.Index].IsBig;
            this.bigProperty = tables[PropertyTable.Index].IsBig;
            this.bigEvent = tables[EventTable.Index].IsBig;
            this.bigGenericParam = tables[GenericParamTable.Index].IsBig;
            this.bigModuleRef = tables[ModuleRefTable.Index].IsBig;
            this.bigDocument = tables[DocumentTable.Index]?.IsBig ?? false;
            this.bigImportScope = tables[ImportScopeTable.Index]?.IsBig ?? false;
            this.bigLocalVariable = tables[LocalVariableTable.Index]?.IsBig ?? false;
            this.bigLocalConstant = tables[LocalConstantTable.Index]?.IsBig ?? false;
            this.bigResolutionScope = IsBig(tables, 2, ModuleTable.Index, ModuleRefTable.Index, AssemblyRefTable.Index, TypeRefTable.Index);
            this.bigTypeDefOrRef = IsBig(tables, 2, TypeDefTable.Index, TypeRefTable.Index, TypeSpecTable.Index);
            this.bigMemberRefParent = IsBig(tables, 3, TypeDefTable.Index, TypeRefTable.Index, ModuleRefTable.Index, MethodDefTable.Index, TypeSpecTable.Index);
            this.bigMethodDefOrRef = IsBig(tables, 1, MethodDefTable.Index, MemberRefTable.Index);
            this.bigHasCustomAttribute = IsBig(tables, 5, MethodDefTable.Index, FieldTable.Index, TypeRefTable.Index, TypeDefTable.Index, ParamTable.Index,
                InterfaceImplTable.Index, MemberRefTable.Index, ModuleTable.Index, /*PermissionTable.Index,*/ PropertyTable.Index, EventTable.Index,
                StandAloneSigTable.Index, ModuleRefTable.Index, TypeSpecTable.Index, AssemblyTable.Index, AssemblyRefTable.Index, FileTable.Index,
                ExportedTypeTable.Index, ManifestResourceTable.Index, GenericParamTable.Index, GenericParamConstraintTable.Index, MethodSpecTable.Index);
            this.bigCustomAttributeType = IsBig(tables, 3, MethodDefTable.Index, MemberRefTable.Index);
            this.bigHasConstant = IsBig(tables, 2, FieldTable.Index, ParamTable.Index, PropertyTable.Index);
            this.bigHasSemantics = IsBig(tables, 1, EventTable.Index, PropertyTable.Index);
            this.bigHasFieldMarshal = IsBig(tables, 1, FieldTable.Index, ParamTable.Index);
            this.bigHasDeclSecurity = IsBig(tables, 2, TypeDefTable.Index, MethodDefTable.Index, AssemblyTable.Index);
            this.bigTypeOrMethodDef = IsBig(tables, 1, TypeDefTable.Index, MethodDefTable.Index);
            this.bigMemberForwarded = IsBig(tables, 1, FieldTable.Index, MethodDefTable.Index);
            this.bigImplementation = IsBig(tables, 2, FileTable.Index, AssemblyRefTable.Index, ExportedTypeTable.Index);
            this.bigHasCustomDebugInformation = IsBig(tables, 5, MethodDefTable.Index, FieldTable.Index, TypeRefTable.Index, TypeDefTable.Index,
                ParamTable.Index, InterfaceImplTable.Index, MemberRefTable.Index, ModuleTable.Index, DeclSecurityTable.Index, PropertyTable.Index,
                EventTable.Index, StandAloneSigTable.Index, ModuleRefTable.Index, TypeSpecTable.Index, AssemblyTable.Index, AssemblyRefTable.Index,
                FileTable.Index, ExportedTypeTable.Index, ManifestResourceTable.Index, GenericParamTable.Index, GenericParamConstraintTable.Index,
                MethodSpecTable.Index, DocumentTable.Index, LocalScopeTable.Index, LocalVariableTable.Index, LocalConstantTable.Index, ImportScopeTable.Index);
        }

        private static bool IsBig(Table[] all, int bitsUsed, params int[] tables)
        {
            int limit = 1 << (16 - bitsUsed);
            foreach (var table in tables)
            {
                if (all[table]?.RowCount >= limit)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
