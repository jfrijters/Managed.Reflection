/*
  The MIT License (MIT) 
  Copyright (C) 2010-2012 Jeroen Frijters
  
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
using System.Globalization;

namespace Managed.Reflection
{
    public abstract class Binder
    {
        protected Binder()
        {
        }

        public virtual MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
        {
            throw new InvalidOperationException();
        }

        public virtual FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public virtual object ChangeType(object value, Type type, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public virtual void ReorderArgumentArray(ref object[] args, object state)
        {
            throw new InvalidOperationException();
        }

        public abstract MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers);
        public abstract PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers);
    }

    sealed class DefaultBinder : Binder
    {
        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            int matchCount = 0;
            foreach (MethodBase method in match)
            {
                if (MatchParameterTypes(method.GetParameters(), types))
                {
                    match[matchCount++] = method;
                }
            }

            if (matchCount == 0)
            {
                return null;
            }

            MethodBase bestMatch = match[0];
            bool ambiguous = false;
            for (int i = 1; i < matchCount; i++)
            {
                SelectBestMatch(match[i], types, ref bestMatch, ref ambiguous);
            }
            if (ambiguous)
            {
                throw new AmbiguousMatchException();
            }
            return bestMatch;
        }

        private static bool MatchParameterTypes(ParameterInfo[] parameters, Type[] types)
        {
            if (parameters.Length != types.Length)
            {
                return false;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                Type sourceType = types[i];
                Type targetType = parameters[i].ParameterType;
                if (sourceType != targetType
                    && !targetType.IsAssignableFrom(sourceType)
                    && !IsAllowedPrimitiveConversion(sourceType, targetType))
                {
                    return false;
                }
            }
            return true;
        }

        private static void SelectBestMatch(MethodBase candidate, Type[] types, ref MethodBase currentBest, ref bool ambiguous)
        {
            switch (MatchSignatures(currentBest, candidate, types))
            {
                case 1:
                    return;
                case 2:
                    ambiguous = false;
                    currentBest = candidate;
                    return;
            }

            if (currentBest.MethodSignature.MatchParameterTypes(candidate.MethodSignature))
            {
                int depth1 = GetInheritanceDepth(currentBest.DeclaringType);
                int depth2 = GetInheritanceDepth(candidate.DeclaringType);
                if (depth1 > depth2)
                {
                    return;
                }
                else if (depth1 < depth2)
                {
                    ambiguous = false;
                    currentBest = candidate;
                    return;
                }
            }

            ambiguous = true;
        }

        private static int GetInheritanceDepth(Type type)
        {
            int depth = 0;
            while (type != null)
            {
                depth++;
                type = type.BaseType;
            }
            return depth;
        }

        private static int MatchSignatures(MethodBase mb1, MethodBase mb2, Type[] types)
        {
            MethodSignature sig1 = mb1.MethodSignature;
            MethodSignature sig2 = mb2.MethodSignature;
            IGenericBinder gb1 = mb1 as IGenericBinder ?? mb1.DeclaringType;
            IGenericBinder gb2 = mb2 as IGenericBinder ?? mb2.DeclaringType;
            for (int i = 0; i < sig1.GetParameterCount(); i++)
            {
                Type type1 = sig1.GetParameterType(gb1, i);
                Type type2 = sig2.GetParameterType(gb2, i);
                if (type1 != type2)
                {
                    return MatchTypes(type1, type2, types[i]);
                }
            }
            return 0;
        }

        private static int MatchSignatures(PropertySignature sig1, PropertySignature sig2, Type[] types)
        {
            for (int i = 0; i < sig1.ParameterCount; i++)
            {
                Type type1 = sig1.GetParameter(i);
                Type type2 = sig2.GetParameter(i);
                if (type1 != type2)
                {
                    return MatchTypes(type1, type2, types[i]);
                }
            }
            return 0;
        }

        private static int MatchTypes(Type type1, Type type2, Type type)
        {
            if (type1 == type)
            {
                return 1;
            }
            if (type2 == type)
            {
                return 2;
            }
            bool conv = type1.IsAssignableFrom(type2);
            return conv == type2.IsAssignableFrom(type1) ? 0 : conv ? 2 : 1;
        }

        private static bool IsAllowedPrimitiveConversion(Type source, Type target)
        {
            // we need to check for primitives, because GetTypeCode will return the underlying type for enums
            if (!source.IsPrimitive || !target.IsPrimitive)
            {
                return false;
            }
            TypeCode sourceType = Type.GetTypeCode(source);
            TypeCode targetType = Type.GetTypeCode(target);
            switch (sourceType)
            {
                case TypeCode.Char:
                    switch (targetType)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.Int32:
                        case TypeCode.UInt64:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Byte:
                    switch (targetType)
                    {
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                        case TypeCode.Int16:
                        case TypeCode.UInt32:
                        case TypeCode.Int32:
                        case TypeCode.UInt64:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.SByte:
                    switch (targetType)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.UInt16:
                    switch (targetType)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.Int32:
                        case TypeCode.UInt64:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Int16:
                    switch (targetType)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.UInt32:
                    switch (targetType)
                    {
                        case TypeCode.UInt64:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Int32:
                    switch (targetType)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.UInt64:
                    switch (targetType)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Int64:
                    switch (targetType)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                case TypeCode.Single:
                    switch (targetType)
                    {
                        case TypeCode.Double:
                            return true;
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
        {
            int matchCount = 0;
            foreach (PropertyInfo property in match)
            {
                if (indexes == null || MatchParameterTypes(property.GetIndexParameters(), indexes))
                {
                    if (returnType != null)
                    {
                        if (property.PropertyType.IsPrimitive)
                        {
                            if (!IsAllowedPrimitiveConversion(returnType, property.PropertyType))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!property.PropertyType.IsAssignableFrom(returnType))
                            {
                                continue;
                            }
                        }
                    }
                    match[matchCount++] = property;
                }
            }

            if (matchCount == 0)
            {
                return null;
            }

            if (matchCount == 1)
            {
                return match[0];
            }

            PropertyInfo bestMatch = match[0];
            bool ambiguous = false;
            for (int i = 1; i < matchCount; i++)
            {
                int best = MatchTypes(bestMatch.PropertyType, match[i].PropertyType, returnType);
                if (best == 0 && indexes != null)
                {
                    best = MatchSignatures(bestMatch.PropertySignature, match[i].PropertySignature, indexes);
                }
                if (best == 0)
                {
                    int depth1 = GetInheritanceDepth(bestMatch.DeclaringType);
                    int depth2 = GetInheritanceDepth(match[i].DeclaringType);
                    if (bestMatch.Name == match[i].Name && depth1 != depth2)
                    {
                        if (depth1 > depth2)
                        {
                            best = 1;
                        }
                        else
                        {
                            best = 2;
                        }
                    }
                    else
                    {
                        ambiguous = true;
                    }
                }
                if (best == 2)
                {
                    ambiguous = false;
                    bestMatch = match[i];
                }
            }
            if (ambiguous)
            {
                throw new AmbiguousMatchException();
            }
            return bestMatch;
        }
    }
}
