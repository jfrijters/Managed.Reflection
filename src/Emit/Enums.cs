/*
  The MIT License (MIT) 
  Copyright (C) 2008 Jeroen Frijters
  
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

namespace Managed.Reflection.Emit
{
    public enum AssemblyBuilderAccess
    {
        Save = 2,
        ReflectionOnly = 6,
    }

    public enum OpCodeType
    {
        Annotation = 0,
        Macro = 1,
        Nternal = 2,
        Objmodel = 3,
        Prefix = 4,
        Primitive = 5,
    }

    public enum OperandType
    {
        InlineBrTarget = 0,
        InlineField = 1,
        InlineI = 2,
        InlineI8 = 3,
        InlineMethod = 4,
        InlineNone = 5,
        InlinePhi = 6,
        InlineR = 7,
        InlineSig = 9,
        InlineString = 10,
        InlineSwitch = 11,
        InlineTok = 12,
        InlineType = 13,
        InlineVar = 14,
        ShortInlineBrTarget = 15,
        ShortInlineI = 16,
        ShortInlineR = 17,
        ShortInlineVar = 18,
    }

    public enum FlowControl
    {
        Branch = 0,
        Break = 1,
        Call = 2,
        Cond_Branch = 3,
        Meta = 4,
        Next = 5,
        Return = 7,
        Throw = 8,
    }

    public enum PackingSize
    {
        Unspecified = 0,
        Size1 = 1,
        Size2 = 2,
        Size4 = 4,
        Size8 = 8,
        Size16 = 16,
        Size32 = 32,
        Size64 = 64,
        Size128 = 128,
    }

    public enum PEFileKinds
    {
        Dll = 1,
        ConsoleApplication = 2,
        WindowApplication = 3,
    }

    public enum StackBehaviour
    {
        Pop0 = 0,
        Pop1 = 1,
        Pop1_pop1 = 2,
        Popi = 3,
        Popi_pop1 = 4,
        Popi_popi = 5,
        Popi_popi8 = 6,
        Popi_popi_popi = 7,
        Popi_popr4 = 8,
        Popi_popr8 = 9,
        Popref = 10,
        Popref_pop1 = 11,
        Popref_popi = 12,
        Popref_popi_popi = 13,
        Popref_popi_popi8 = 14,
        Popref_popi_popr4 = 15,
        Popref_popi_popr8 = 16,
        Popref_popi_popref = 17,
        Push0 = 18,
        Push1 = 19,
        Push1_push1 = 20,
        Pushi = 21,
        Pushi8 = 22,
        Pushr4 = 23,
        Pushr8 = 24,
        Pushref = 25,
        Varpop = 26,
        Varpush = 27,
        Popref_popi_pop1 = 28,
    }
}
