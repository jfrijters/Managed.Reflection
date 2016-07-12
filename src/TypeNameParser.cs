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
using System;
using System.Diagnostics;
using System.Text;

namespace Managed.Reflection
{
    // this respresents a type name as in metadata:
    // - ns will be null for empty the namespace (never the empty string)
    // - the strings are not escaped
    struct TypeName : IEquatable<TypeName>
    {
        private readonly string ns;
        private readonly string name;

        internal TypeName(string ns, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            this.ns = ns;
            this.name = name;
        }

        internal string Name
        {
            get { return name; }
        }

        internal string Namespace
        {
            get { return ns; }
        }

        public static bool operator ==(TypeName o1, TypeName o2)
        {
            return o1.ns == o2.ns && o1.name == o2.name;
        }

        public static bool operator !=(TypeName o1, TypeName o2)
        {
            return o1.ns != o2.ns || o1.name != o2.name;
        }

        public override int GetHashCode()
        {
            return ns == null ? name.GetHashCode() : ns.GetHashCode() * 37 + name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            TypeName? other = obj as TypeName?;
            return other != null && other.Value == this;
        }

        public override string ToString()
        {
            return ns == null ? name : ns + "." + name;
        }

        bool IEquatable<TypeName>.Equals(TypeName other)
        {
            return this == other;
        }

        internal bool Matches(string fullName)
        {
            if (ns == null)
            {
                return name == fullName;
            }
            if (ns.Length + 1 + name.Length == fullName.Length)
            {
                return fullName.StartsWith(ns, StringComparison.Ordinal)
                    && fullName[ns.Length] == '.'
                    && fullName.EndsWith(name, StringComparison.Ordinal);
            }
            return false;
        }

        internal TypeName ToLowerInvariant()
        {
            return new TypeName(ns == null ? null : ns.ToLowerInvariant(), name.ToLowerInvariant());
        }

        internal static TypeName Split(string name)
        {
            int dot = name.LastIndexOf('.');
            if (dot == -1)
            {
                return new TypeName(null, name);
            }
            else
            {
                return new TypeName(name.Substring(0, dot), name.Substring(dot + 1));
            }
        }
    }

    struct TypeNameParser
    {
        private const string SpecialChars = "\\+,[]*&";
        private const short SZARRAY = -1;
        private const short BYREF = -2;
        private const short POINTER = -3;
        private readonly string name;
        private readonly string[] nested;
        private readonly string assemblyName;
        private readonly short[] modifiers;
        private readonly TypeNameParser[] genericParameters;

        internal static string Escape(string name)
        {
            if (name == null)
            {
                return null;
            }
            StringBuilder sb = null;
            for (int pos = 0; pos < name.Length; pos++)
            {
                char c = name[pos];
                switch (c)
                {
                    case '\\':
                    case '+':
                    case ',':
                    case '[':
                    case ']':
                    case '*':
                    case '&':
                        if (sb == null)
                        {
                            sb = new StringBuilder(name, 0, pos, name.Length + 3);
                        }
                        sb.Append("\\").Append(c);
                        break;
                    default:
                        if (sb != null)
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb != null ? sb.ToString() : name;
        }

        internal static string Unescape(string name)
        {
            int pos = name.IndexOf('\\');
            if (pos == -1)
            {
                return name;
            }
            StringBuilder sb = new StringBuilder(name, 0, pos, name.Length - 1);
            for (; pos < name.Length; pos++)
            {
                char c = name[pos];
                if (c == '\\')
                {
                    c = name[++pos];
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        internal static TypeNameParser Parse(string typeName, bool throwOnError)
        {
            if (throwOnError)
            {
                Parser parser = new Parser(typeName);
                return new TypeNameParser(ref parser, true);
            }
            else
            {
                try
                {
                    Parser parser = new Parser(typeName);
                    return new TypeNameParser(ref parser, true);
                }
                catch (ArgumentException)
                {
                    return new TypeNameParser();
                }
            }
        }

        private TypeNameParser(ref Parser parser, bool withAssemblyName)
        {
            bool genericParameter = parser.pos != 0;
            name = parser.NextNamePart();
            nested = null;
            parser.ParseNested(ref nested);
            genericParameters = null;
            parser.ParseGenericParameters(ref genericParameters);
            modifiers = null;
            parser.ParseModifiers(ref modifiers);
            assemblyName = null;
            if (withAssemblyName)
            {
                parser.ParseAssemblyName(genericParameter, ref assemblyName);
            }
        }

        internal bool Error
        {
            get { return name == null; }
        }

        internal string FirstNamePart
        {
            get { return name; }
        }

        internal string AssemblyName
        {
            get { return assemblyName; }
        }

        private struct Parser
        {
            private readonly string typeName;
            internal int pos;

            internal Parser(string typeName)
            {
                this.typeName = typeName;
                this.pos = 0;
            }

            private void Check(bool condition)
            {
                if (!condition)
                {
                    throw new ArgumentException("Invalid type name '" + typeName + "'");
                }
            }

            private void Consume(char c)
            {
                Check(pos < typeName.Length && typeName[pos++] == c);
            }

            private bool TryConsume(char c)
            {
                if (pos < typeName.Length && typeName[pos] == c)
                {
                    pos++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            internal string NextNamePart()
            {
                SkipWhiteSpace();
                int start = pos;
                for (; pos < typeName.Length; pos++)
                {
                    char c = typeName[pos];
                    if (c == '\\')
                    {
                        pos++;
                        Check(pos < typeName.Length && SpecialChars.IndexOf(typeName[pos]) != -1);
                    }
                    else if (SpecialChars.IndexOf(c) != -1)
                    {
                        break;
                    }
                }
                Check(pos - start != 0);
                if (start == 0 && pos == typeName.Length)
                {
                    return typeName;
                }
                else
                {
                    return typeName.Substring(start, pos - start);
                }
            }

            internal void ParseNested(ref string[] nested)
            {
                while (TryConsume('+'))
                {
                    Add(ref nested, NextNamePart());
                }
            }

            internal void ParseGenericParameters(ref TypeNameParser[] genericParameters)
            {
                int saved = pos;
                if (TryConsume('['))
                {
                    SkipWhiteSpace();
                    if (TryConsume(']') || TryConsume('*') || TryConsume(','))
                    {
                        // it's not a generic parameter list, but an array instead
                        pos = saved;
                        return;
                    }
                    do
                    {
                        SkipWhiteSpace();
                        if (TryConsume('['))
                        {
                            Add(ref genericParameters, new TypeNameParser(ref this, true));
                            Consume(']');
                        }
                        else
                        {
                            Add(ref genericParameters, new TypeNameParser(ref this, false));
                        }
                    }
                    while (TryConsume(','));
                    Consume(']');
                    SkipWhiteSpace();
                }
            }

            internal void ParseModifiers(ref short[] modifiers)
            {
                while (pos < typeName.Length)
                {
                    switch (typeName[pos])
                    {
                        case '*':
                            pos++;
                            Add(ref modifiers, POINTER);
                            break;
                        case '&':
                            pos++;
                            Add(ref modifiers, BYREF);
                            break;
                        case '[':
                            pos++;
                            Add(ref modifiers, ParseArray());
                            Consume(']');
                            break;
                        default:
                            return;
                    }
                    SkipWhiteSpace();
                }
            }

            internal void ParseAssemblyName(bool genericParameter, ref string assemblyName)
            {
                if (pos < typeName.Length)
                {
                    if (typeName[pos] == ']' && genericParameter)
                    {
                        // ok
                    }
                    else
                    {
                        Consume(',');
                        SkipWhiteSpace();
                        if (genericParameter)
                        {
                            int start = pos;
                            while (pos < typeName.Length)
                            {
                                char c = typeName[pos];
                                if (c == '\\')
                                {
                                    pos++;
                                    // a backslash itself is not legal in an assembly name, so we don't need to check for an escaped backslash
                                    Check(pos < typeName.Length && typeName[pos++] == ']');
                                }
                                else if (c == ']')
                                {
                                    break;
                                }
                                else
                                {
                                    pos++;
                                }
                            }
                            Check(pos < typeName.Length && typeName[pos] == ']');
                            assemblyName = typeName.Substring(start, pos - start).Replace("\\]", "]");
                        }
                        else
                        {
                            // only when an assembly name is used in a generic type parameter, will it be escaped
                            assemblyName = typeName.Substring(pos);
                        }
                        Check(assemblyName.Length != 0);
                    }
                }
                else
                {
                    Check(!genericParameter);
                }
            }

            private short ParseArray()
            {
                SkipWhiteSpace();
                Check(pos < typeName.Length);
                char c = typeName[pos];
                if (c == ']')
                {
                    return SZARRAY;
                }
                else if (c == '*')
                {
                    pos++;
                    SkipWhiteSpace();
                    return 1;
                }
                else
                {
                    short rank = 1;
                    while (TryConsume(','))
                    {
                        Check(rank < short.MaxValue);
                        rank++;
                        SkipWhiteSpace();
                    }
                    return rank;
                }
            }

            private void SkipWhiteSpace()
            {
                while (pos < typeName.Length && Char.IsWhiteSpace(typeName[pos]))
                {
                    pos++;
                }
            }

            private static void Add<T>(ref T[] array, T elem)
            {
                if (array == null)
                {
                    array = new T[] { elem };
                    return;
                }
                Array.Resize(ref array, array.Length + 1);
                array[array.Length - 1] = elem;
            }
        }

        internal Type GetType(Universe universe, Module context, bool throwOnError, string originalName, bool resolve, bool ignoreCase)
        {
            Debug.Assert(!resolve || !ignoreCase);
            TypeName name = TypeName.Split(this.name);
            Type type;
            if (assemblyName != null)
            {
                Assembly asm = universe.Load(assemblyName, context, throwOnError);
                if (asm == null)
                {
                    return null;
                }
                if (resolve)
                {
                    type = asm.ResolveType(context, name);
                }
                else if (ignoreCase)
                {
                    type = asm.FindTypeIgnoreCase(name.ToLowerInvariant());
                }
                else
                {
                    type = asm.FindType(name);
                }
            }
            else if (context == null)
            {
                if (resolve)
                {
                    type = universe.Mscorlib.ResolveType(context, name);
                }
                else if (ignoreCase)
                {
                    type = universe.Mscorlib.FindTypeIgnoreCase(name.ToLowerInvariant());
                }
                else
                {
                    type = universe.Mscorlib.FindType(name);
                }
            }
            else
            {
                if (ignoreCase)
                {
                    name = name.ToLowerInvariant();
                    type = context.FindTypeIgnoreCase(name);
                }
                else
                {
                    type = context.FindType(name);
                }
                if (type == null && context != universe.Mscorlib.ManifestModule)
                {
                    if (ignoreCase)
                    {
                        type = universe.Mscorlib.FindTypeIgnoreCase(name);
                    }
                    else
                    {
                        type = universe.Mscorlib.FindType(name);
                    }
                }
                if (type == null && resolve)
                {
                    if (universe.Mscorlib.__IsMissing && !context.__IsMissing)
                    {
                        type = universe.Mscorlib.ResolveType(context, name);
                    }
                    else
                    {
                        type = context.Assembly.ResolveType(context, name);
                    }
                }
            }
            return Expand(type, context, throwOnError, originalName, resolve, ignoreCase);
        }

        internal Type Expand(Type type, Module context, bool throwOnError, string originalName, bool resolve, bool ignoreCase)
        {
            Debug.Assert(!resolve || !ignoreCase);
            if (type == null)
            {
                if (throwOnError)
                {
                    throw new TypeLoadException(originalName);
                }
                return null;
            }
            if (nested != null)
            {
                Type outer;
                foreach (string nest in nested)
                {
                    outer = type;
                    TypeName name = TypeName.Split(TypeNameParser.Unescape(nest));
                    type = ignoreCase
                        ? outer.FindNestedTypeIgnoreCase(name.ToLowerInvariant())
                        : outer.FindNestedType(name);
                    if (type == null)
                    {
                        if (resolve)
                        {
                            type = outer.Module.universe.GetMissingTypeOrThrow(context, outer.Module, outer, name);
                        }
                        else if (throwOnError)
                        {
                            throw new TypeLoadException(originalName);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            if (genericParameters != null)
            {
                Type[] typeArgs = new Type[genericParameters.Length];
                for (int i = 0; i < typeArgs.Length; i++)
                {
                    typeArgs[i] = genericParameters[i].GetType(type.Assembly.universe, context, throwOnError, originalName, resolve, ignoreCase);
                    if (typeArgs[i] == null)
                    {
                        return null;
                    }
                }
                type = type.MakeGenericType(typeArgs);
            }
            if (modifiers != null)
            {
                foreach (short modifier in modifiers)
                {
                    switch (modifier)
                    {
                        case SZARRAY:
                            type = type.MakeArrayType();
                            break;
                        case BYREF:
                            type = type.MakeByRefType();
                            break;
                        case POINTER:
                            type = type.MakePointerType();
                            break;
                        default:
                            type = type.MakeArrayType(modifier);
                            break;
                    }
                }
            }
            return type;
        }
    }
}
