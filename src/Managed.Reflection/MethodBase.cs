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

namespace Managed.Reflection
{
    public abstract class MethodBase : MemberInfo
    {
        // prevent external subclasses
        internal MethodBase()
        {
        }

        internal abstract MethodSignature MethodSignature { get; }
        internal abstract int ParameterCount { get; }
        public abstract ParameterInfo[] GetParameters();
        public abstract MethodAttributes Attributes { get; }
        public abstract MethodImplAttributes GetMethodImplementationFlags();
        public abstract MethodBody GetMethodBody();
        public abstract CallingConventions CallingConvention { get; }
        public abstract int __MethodRVA { get; }

        public bool IsConstructor
        {
            get
            {
                if ((this.Attributes & MethodAttributes.RTSpecialName) != 0)
                {
                    string name = this.Name;
                    return name == ConstructorInfo.ConstructorName || name == ConstructorInfo.TypeConstructorName;
                }
                return false;
            }
        }

        public bool IsStatic
        {
            get { return (Attributes & MethodAttributes.Static) != 0; }
        }

        public bool IsVirtual
        {
            get { return (Attributes & MethodAttributes.Virtual) != 0; }
        }

        public bool IsAbstract
        {
            get { return (Attributes & MethodAttributes.Abstract) != 0; }
        }

        public bool IsFinal
        {
            get { return (Attributes & MethodAttributes.Final) != 0; }
        }

        public bool IsPublic
        {
            get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; }
        }

        public bool IsFamily
        {
            get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; }
        }

        public bool IsFamilyOrAssembly
        {
            get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; }
        }

        public bool IsAssembly
        {
            get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly; }
        }

        public bool IsFamilyAndAssembly
        {
            get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; }
        }

        public bool IsPrivate
        {
            get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; }
        }

        public bool IsSpecialName
        {
            get { return (Attributes & MethodAttributes.SpecialName) != 0; }
        }

        public bool IsHideBySig
        {
            get { return (Attributes & MethodAttributes.HideBySig) != 0; }
        }

        public MethodImplAttributes MethodImplementationFlags
        {
            get { return GetMethodImplementationFlags(); }
        }

        public virtual Type[] GetGenericArguments()
        {
            return Type.EmptyTypes;
        }

        public virtual bool IsGenericMethod
        {
            get { return false; }
        }

        public virtual bool IsGenericMethodDefinition
        {
            get { return false; }
        }

        public virtual bool ContainsGenericParameters
        {
            get { return IsGenericMethodDefinition; }
        }

        public virtual MethodBase __GetMethodOnTypeDefinition()
        {
            return this;
        }

        // This goes to the (uninstantiated) MethodInfo on the (uninstantiated) Type. For constructors
        // it also has the effect of removing the ConstructorInfo wrapper and returning the underlying MethodInfo.
        internal abstract MethodInfo GetMethodOnTypeDefinition();

        internal abstract int ImportTo(Emit.ModuleBuilder module);

        internal abstract MethodBase BindTypeParameters(Type type);

        internal sealed override bool BindingFlagsMatch(BindingFlags flags)
        {
            return BindingFlagsMatch(IsPublic, flags, BindingFlags.Public, BindingFlags.NonPublic)
                && BindingFlagsMatch(IsStatic, flags, BindingFlags.Static, BindingFlags.Instance);
        }

        internal sealed override bool BindingFlagsMatchInherited(BindingFlags flags)
        {
            return (Attributes & MethodAttributes.MemberAccessMask) > MethodAttributes.Private
                && BindingFlagsMatch(IsPublic, flags, BindingFlags.Public, BindingFlags.NonPublic)
                && BindingFlagsMatch(IsStatic, flags, BindingFlags.Static | BindingFlags.FlattenHierarchy, BindingFlags.Instance);
        }
    }
}
