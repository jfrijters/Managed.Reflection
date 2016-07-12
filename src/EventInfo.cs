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
using System.Collections.Generic;
using System.Diagnostics;

namespace Managed.Reflection
{
    public abstract class EventInfo : MemberInfo
    {
        // prevent external subclasses
        internal EventInfo()
        {
        }

        public sealed override MemberTypes MemberType
        {
            get { return MemberTypes.Event; }
        }

        public abstract EventAttributes Attributes { get; }
        public abstract MethodInfo GetAddMethod(bool nonPublic);
        public abstract MethodInfo GetRaiseMethod(bool nonPublic);
        public abstract MethodInfo GetRemoveMethod(bool nonPublic);
        public abstract MethodInfo[] GetOtherMethods(bool nonPublic);
        public abstract MethodInfo[] __GetMethods();
        public abstract Type EventHandlerType { get; }
        internal abstract bool IsPublic { get; }
        internal abstract bool IsNonPrivate { get; }
        internal abstract bool IsStatic { get; }

        public bool IsSpecialName
        {
            get { return (Attributes & EventAttributes.SpecialName) != 0; }
        }

        public MethodInfo GetAddMethod()
        {
            return GetAddMethod(false);
        }

        public MethodInfo GetRaiseMethod()
        {
            return GetRaiseMethod(false);
        }

        public MethodInfo GetRemoveMethod()
        {
            return GetRemoveMethod(false);
        }

        public MethodInfo[] GetOtherMethods()
        {
            return GetOtherMethods(false);
        }

        public MethodInfo AddMethod
        {
            get { return GetAddMethod(true); }
        }

        public MethodInfo RaiseMethod
        {
            get { return GetRaiseMethod(true); }
        }

        public MethodInfo RemoveMethod
        {
            get { return GetRemoveMethod(true); }
        }

        internal virtual EventInfo BindTypeParameters(Type type)
        {
            return new GenericEventInfo(this.DeclaringType.BindTypeParameters(type), this);
        }

        public override string ToString()
        {
            return this.DeclaringType.ToString() + " " + Name;
        }

        internal sealed override bool BindingFlagsMatch(BindingFlags flags)
        {
            return BindingFlagsMatch(IsPublic, flags, BindingFlags.Public, BindingFlags.NonPublic)
                && BindingFlagsMatch(IsStatic, flags, BindingFlags.Static, BindingFlags.Instance);
        }

        internal sealed override bool BindingFlagsMatchInherited(BindingFlags flags)
        {
            return IsNonPrivate
                && BindingFlagsMatch(IsPublic, flags, BindingFlags.Public, BindingFlags.NonPublic)
                && BindingFlagsMatch(IsStatic, flags, BindingFlags.Static | BindingFlags.FlattenHierarchy, BindingFlags.Instance);
        }

        internal sealed override MemberInfo SetReflectedType(Type type)
        {
            return new EventInfoWithReflectedType(type, this);
        }

        internal sealed override List<CustomAttributeData> GetPseudoCustomAttributes(Type attributeType)
        {
            // events don't have pseudo custom attributes
            return null;
        }
    }

    sealed class EventInfoWithReflectedType : EventInfo
    {
        private readonly Type reflectedType;
        private readonly EventInfo eventInfo;

        internal EventInfoWithReflectedType(Type reflectedType, EventInfo eventInfo)
        {
            Debug.Assert(reflectedType != eventInfo.DeclaringType);
            this.reflectedType = reflectedType;
            this.eventInfo = eventInfo;
        }

        public override EventAttributes Attributes
        {
            get { return eventInfo.Attributes; }
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            return SetReflectedType(eventInfo.GetAddMethod(nonPublic), reflectedType);
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return SetReflectedType(eventInfo.GetRaiseMethod(nonPublic), reflectedType);
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return SetReflectedType(eventInfo.GetRemoveMethod(nonPublic), reflectedType);
        }

        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            return SetReflectedType(eventInfo.GetOtherMethods(nonPublic), reflectedType);
        }

        public override MethodInfo[] __GetMethods()
        {
            return SetReflectedType(eventInfo.__GetMethods(), reflectedType);
        }

        public override Type EventHandlerType
        {
            get { return eventInfo.EventHandlerType; }
        }

        internal override bool IsPublic
        {
            get { return eventInfo.IsPublic; }
        }

        internal override bool IsNonPrivate
        {
            get { return eventInfo.IsNonPrivate; }
        }

        internal override bool IsStatic
        {
            get { return eventInfo.IsStatic; }
        }

        internal override EventInfo BindTypeParameters(Type type)
        {
            return eventInfo.BindTypeParameters(type);
        }

        public override string ToString()
        {
            return eventInfo.ToString();
        }

        public override bool __IsMissing
        {
            get { return eventInfo.__IsMissing; }
        }

        public override Type DeclaringType
        {
            get { return eventInfo.DeclaringType; }
        }

        public override Type ReflectedType
        {
            get { return reflectedType; }
        }

        public override bool Equals(object obj)
        {
            EventInfoWithReflectedType other = obj as EventInfoWithReflectedType;
            return other != null
                && other.reflectedType == reflectedType
                && other.eventInfo == eventInfo;
        }

        public override int GetHashCode()
        {
            return reflectedType.GetHashCode() ^ eventInfo.GetHashCode();
        }

        public override int MetadataToken
        {
            get { return eventInfo.MetadataToken; }
        }

        public override Module Module
        {
            get { return eventInfo.Module; }
        }

        public override string Name
        {
            get { return eventInfo.Name; }
        }

        internal override bool IsBaked
        {
            get { return eventInfo.IsBaked; }
        }

        internal override int GetCurrentToken()
        {
            return eventInfo.GetCurrentToken();
        }
    }
}
