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
using System.Collections.Generic;
using Managed.Reflection.Metadata;

namespace Managed.Reflection.Emit
{
    public sealed class EventBuilder : EventInfo
    {
        private readonly TypeBuilder typeBuilder;
        private readonly string name;
        private EventAttributes attributes;
        private readonly int eventtype;
        private MethodBuilder addOnMethod;
        private MethodBuilder removeOnMethod;
        private MethodBuilder fireMethod;
        private readonly List<Accessor> accessors = new List<Accessor>();
        private int lazyPseudoToken;

        private struct Accessor
        {
            internal short Semantics;
            internal MethodBuilder Method;
        }

        internal EventBuilder(TypeBuilder typeBuilder, string name, EventAttributes attributes, Type eventtype)
        {
            this.typeBuilder = typeBuilder;
            this.name = name;
            this.attributes = attributes;
            this.eventtype = typeBuilder.ModuleBuilder.GetTypeTokenForMemberRef(eventtype);
        }

        public void SetAddOnMethod(MethodBuilder mdBuilder)
        {
            addOnMethod = mdBuilder;
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.AddOn;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
        {
            removeOnMethod = mdBuilder;
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.RemoveOn;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void SetRaiseMethod(MethodBuilder mdBuilder)
        {
            fireMethod = mdBuilder;
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.Fire;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            Accessor acc;
            acc.Semantics = MethodSemanticsTable.Other;
            acc.Method = mdBuilder;
            accessors.Add(acc);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder.KnownCA == KnownCA.SpecialNameAttribute)
            {
                attributes |= EventAttributes.SpecialName;
            }
            else
            {
                if (lazyPseudoToken == 0)
                {
                    lazyPseudoToken = typeBuilder.ModuleBuilder.AllocPseudoToken();
                }
                typeBuilder.ModuleBuilder.SetCustomAttribute(lazyPseudoToken, customBuilder);
            }
        }

        public override EventAttributes Attributes
        {
            get { return attributes; }
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            return nonPublic || (addOnMethod != null && addOnMethod.IsPublic) ? addOnMethod : null;
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return nonPublic || (removeOnMethod != null && removeOnMethod.IsPublic) ? removeOnMethod : null;
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return nonPublic || (fireMethod != null && fireMethod.IsPublic) ? fireMethod : null;
        }

        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (Accessor acc in accessors)
            {
                if (acc.Semantics == MethodSemanticsTable.Other && (nonPublic || acc.Method.IsPublic))
                {
                    list.Add(acc.Method);
                }
            }
            return list.ToArray();
        }

        public override MethodInfo[] __GetMethods()
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (Accessor acc in accessors)
            {
                list.Add(acc.Method);
            }
            return list.ToArray();
        }

        public override Type DeclaringType
        {
            get { return typeBuilder; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override Module Module
        {
            get { return typeBuilder.ModuleBuilder; }
        }

        public EventToken GetEventToken()
        {
            if (lazyPseudoToken == 0)
            {
                lazyPseudoToken = typeBuilder.ModuleBuilder.AllocPseudoToken();
            }
            return new EventToken(lazyPseudoToken);
        }

        public override Type EventHandlerType
        {
            get { return typeBuilder.ModuleBuilder.ResolveType(eventtype); }
        }

        internal void Bake()
        {
            EventTable.Record rec = new EventTable.Record();
            rec.EventFlags = (short)attributes;
            rec.Name = typeBuilder.ModuleBuilder.Strings.Add(name);
            rec.EventType = eventtype;
            int token = 0x14000000 | typeBuilder.ModuleBuilder.Event.AddRecord(rec);

            if (lazyPseudoToken == 0)
            {
                lazyPseudoToken = token;
            }
            else
            {
                typeBuilder.ModuleBuilder.RegisterTokenFixup(lazyPseudoToken, token);
            }

            foreach (Accessor acc in accessors)
            {
                AddMethodSemantics(acc.Semantics, acc.Method.MetadataToken, token);
            }
        }

        private void AddMethodSemantics(short semantics, int methodToken, int propertyToken)
        {
            MethodSemanticsTable.Record rec = new MethodSemanticsTable.Record();
            rec.Semantics = semantics;
            rec.Method = methodToken;
            rec.Association = propertyToken;
            typeBuilder.ModuleBuilder.MethodSemantics.AddRecord(rec);
        }

        internal override bool IsPublic
        {
            get
            {
                foreach (Accessor acc in accessors)
                {
                    if (acc.Method.IsPublic)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal override bool IsNonPrivate
        {
            get
            {
                foreach (Accessor acc in accessors)
                {
                    if ((acc.Method.Attributes & MethodAttributes.MemberAccessMask) > MethodAttributes.Private)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal override bool IsStatic
        {
            get
            {
                foreach (Accessor acc in accessors)
                {
                    if (acc.Method.IsStatic)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal override bool IsBaked
        {
            get { return typeBuilder.IsBaked; }
        }

        internal override int GetCurrentToken()
        {
            if (typeBuilder.ModuleBuilder.IsSaved && ModuleBuilder.IsPseudoToken(lazyPseudoToken))
            {
                return typeBuilder.ModuleBuilder.ResolvePseudoToken(lazyPseudoToken);
            }
            else
            {
                return lazyPseudoToken;
            }
        }
    }
}
