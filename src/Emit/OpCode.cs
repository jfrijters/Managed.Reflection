/*
  The MIT License (MIT) 
  Copyright (C) 2008-2010 Jeroen Frijters
  
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
    public struct OpCode
    {
        private const int ValueCount = 1024;
        private const int OperandTypeCount = 19;
        private const int FlowControlCount = 9;
        private const int StackDiffCount = 5;
        private const int OpCodeTypeCount = 6;
        private const int StackBehaviourPopCount = 20;
        private const int StackBehaviourPushCount = 9;
        private static readonly StackBehaviour[] pop = {
            StackBehaviour.Pop0,
            StackBehaviour.Pop1,
            StackBehaviour.Pop1_pop1,
            StackBehaviour.Popi,
            StackBehaviour.Popi_pop1,
            StackBehaviour.Popi_popi,
            StackBehaviour.Popi_popi8,
            StackBehaviour.Popi_popi_popi,
            StackBehaviour.Popi_popr4,
            StackBehaviour.Popi_popr8,
            StackBehaviour.Popref,
            StackBehaviour.Popref_pop1,
            StackBehaviour.Popref_popi,
            StackBehaviour.Popref_popi_popi,
            StackBehaviour.Popref_popi_popi8,
            StackBehaviour.Popref_popi_popr4,
            StackBehaviour.Popref_popi_popr8,
            StackBehaviour.Popref_popi_popref,
            StackBehaviour.Varpop,
            StackBehaviour.Popref_popi_pop1
        };
        private static readonly StackBehaviour[] push = {
            StackBehaviour.Push0,
            StackBehaviour.Push1,
            StackBehaviour.Push1_push1,
            StackBehaviour.Pushi,
            StackBehaviour.Pushi8,
            StackBehaviour.Pushr4,
            StackBehaviour.Pushr8,
            StackBehaviour.Pushref,
            StackBehaviour.Varpush
        };
        private readonly int value;

        internal OpCode(int value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            return this == obj as OpCode?;
        }

        public override int GetHashCode()
        {
            return value;
        }

        public bool Equals(OpCode other)
        {
            return this == other;
        }

        public static bool operator ==(OpCode a, OpCode b)
        {
            return a.value == b.value;
        }

        public static bool operator !=(OpCode a, OpCode b)
        {
            return !(a == b);
        }

        public short Value
        {
            get { return (short)(value >> 22); }
        }

        public int Size
        {
            get { return value < 0 ? 2 : 1; }
        }

#if !GENERATOR
        public string Name
        {
            get { return OpCodes.GetName(this.Value); }
        }
#endif

        public OperandType OperandType
        {
            get { return (OperandType)((value & 0x3FFFFF) % OperandTypeCount); }
        }

        public FlowControl FlowControl
        {
            get { return (FlowControl)(((value & 0x3FFFFF) / OperandTypeCount) % FlowControlCount); }
        }

        internal int StackDiff
        {
            get { return ((((value & 0x3FFFFF) / (OperandTypeCount * FlowControlCount)) % StackDiffCount) - 3); }
        }

        public OpCodeType OpCodeType
        {
            get { return (OpCodeType)(((value & 0x3FFFFF) / (OperandTypeCount * FlowControlCount * StackDiffCount)) % OpCodeTypeCount); }
        }

        public StackBehaviour StackBehaviourPop
        {
            get { return pop[(((value & 0x3FFFFF) / (OperandTypeCount * FlowControlCount * StackDiffCount * OpCodeTypeCount)) % StackBehaviourPopCount)]; }
        }

        public StackBehaviour StackBehaviourPush
        {
            get { return push[(((value & 0x3FFFFF) / (OperandTypeCount * FlowControlCount * StackDiffCount * OpCodeTypeCount * StackBehaviourPopCount)) % StackBehaviourPushCount)]; }
        }

#if GENERATOR
        static void Main(string[] args)
        {
            Debug.Assert(pop.Length == StackBehaviourPopCount);
            Debug.Assert(push.Length == StackBehaviourPushCount);
            CheckEnumRange(typeof(FlowControl), FlowControlCount);
            CheckEnumRange(typeof(OpCodeType), OpCodeTypeCount);
            CheckEnumRange(typeof(OperandType), OperandTypeCount);
            foreach (var field in typeof(System.Reflection.Emit.OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                System.Reflection.Emit.OpCode opc1 = (System.Reflection.Emit.OpCode)field.GetValue(null);
                Managed.Reflection.Emit.OpCode opc2 = new Managed.Reflection.Emit.OpCode(Pack(opc1));
                Debug.Assert(opc1.Value == opc2.Value);
                Debug.Assert(opc1.Size == opc2.Size);
                Debug.Assert((int)opc1.FlowControl == (int)opc2.FlowControl);
                Debug.Assert((int)opc1.OpCodeType == (int)opc2.OpCodeType);
                Debug.Assert((int)opc1.OperandType == (int)opc2.OperandType);
                Debug.Assert((int)opc1.StackBehaviourPop == (int)opc2.StackBehaviourPop);
                Debug.Assert((int)opc1.StackBehaviourPush == (int)opc2.StackBehaviourPush);
                Console.WriteLine("\t\tpublic static readonly OpCode {0} = new OpCode({1});", field.Name, Pack(opc1));
            }
            Console.WriteLine();
            Console.WriteLine("\t\tinternal static string GetName(int value)");
            Console.WriteLine("\t\t{");
            Console.WriteLine("\t\t\tswitch (value)");
            Console.WriteLine("\t\t\t{");
            foreach (var field in typeof(System.Reflection.Emit.OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                System.Reflection.Emit.OpCode opc1 = (System.Reflection.Emit.OpCode)field.GetValue(null);
                Console.WriteLine("\t\t\t\tcase {0}:", opc1.Value);
                Console.WriteLine("\t\t\t\t\treturn \"{0}\";", opc1.Name);
            }
            Console.WriteLine("\t\t\t}");
            Console.WriteLine("\t\t\tthrow new ArgumentOutOfRangeException();");
            Console.WriteLine("\t\t}");
            Console.WriteLine();
            Console.WriteLine("\t\tpublic static bool TakesSingleByteArgument(OpCode inst)");
            Console.WriteLine("\t\t{");
            Console.WriteLine("\t\t\tswitch (inst.Value)");
            Console.WriteLine("\t\t\t{");
            foreach (var field in typeof(System.Reflection.Emit.OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                System.Reflection.Emit.OpCode opc1 = (System.Reflection.Emit.OpCode)field.GetValue(null);
                if (System.Reflection.Emit.OpCodes.TakesSingleByteArgument(opc1))
                {
                    Console.WriteLine("\t\t\t\tcase {0}:", opc1.Value);
                }
            }
            Console.WriteLine("\t\t\t\t\treturn true;");
            Console.WriteLine("\t\t\t\tdefault:");
            Console.WriteLine("\t\t\t\t\treturn false;");
            Console.WriteLine("\t\t\t}");
            Console.WriteLine("\t\t}");
        }

        private static void CheckEnumRange(System.Type type, int count)
        {
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                int value = (int)field.GetValue(null);
                Debug.Assert(value >= 0 && value < count);
            }
        }

        static int Pack(System.Reflection.Emit.OpCode opcode)
        {
            int value = 0;
            value *= StackBehaviourPushCount;
            value += Map(push, opcode.StackBehaviourPush);
            value *= StackBehaviourPopCount;
            value += Map(pop, opcode.StackBehaviourPop);
            value *= OpCodeTypeCount;
            value += (int)opcode.OpCodeType;
            value *= StackDiffCount;
            value += 3 + GetStackDiff(opcode.StackBehaviourPush) + GetStackDiff(opcode.StackBehaviourPop);
            value *= FlowControlCount;
            value += (int)opcode.FlowControl;
            value *= OperandTypeCount;
            value += (int)opcode.OperandType;
            return (opcode.Value << 22) | value;
        }

        private static int Map(StackBehaviour[] array, System.Reflection.Emit.StackBehaviour stackBehaviour)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if ((int)array[i] == (int)stackBehaviour)
                {
                    return i;
                }
            }
            throw new InvalidOperationException();
        }

        static int GetStackDiff(System.Reflection.Emit.StackBehaviour sb)
        {
            switch (sb)
            {
                case System.Reflection.Emit.StackBehaviour.Pop0:
                case System.Reflection.Emit.StackBehaviour.Push0:
                case System.Reflection.Emit.StackBehaviour.Varpop:
                case System.Reflection.Emit.StackBehaviour.Varpush:
                    return 0;
                case System.Reflection.Emit.StackBehaviour.Pop1:
                case System.Reflection.Emit.StackBehaviour.Popi:
                case System.Reflection.Emit.StackBehaviour.Popref:
                    return -1;
                case System.Reflection.Emit.StackBehaviour.Pop1_pop1:
                case System.Reflection.Emit.StackBehaviour.Popi_pop1:
                case System.Reflection.Emit.StackBehaviour.Popi_popi:
                case System.Reflection.Emit.StackBehaviour.Popi_popi8:
                case System.Reflection.Emit.StackBehaviour.Popi_popr4:
                case System.Reflection.Emit.StackBehaviour.Popi_popr8:
                case System.Reflection.Emit.StackBehaviour.Popref_pop1:
                case System.Reflection.Emit.StackBehaviour.Popref_popi:
                    return -2;
                case System.Reflection.Emit.StackBehaviour.Popi_popi_popi:
                case System.Reflection.Emit.StackBehaviour.Popref_popi_pop1:
                case System.Reflection.Emit.StackBehaviour.Popref_popi_popi:
                case System.Reflection.Emit.StackBehaviour.Popref_popi_popi8:
                case System.Reflection.Emit.StackBehaviour.Popref_popi_popr4:
                case System.Reflection.Emit.StackBehaviour.Popref_popi_popr8:
                case System.Reflection.Emit.StackBehaviour.Popref_popi_popref:
                    return -3;
                case System.Reflection.Emit.StackBehaviour.Push1:
                case System.Reflection.Emit.StackBehaviour.Pushi:
                case System.Reflection.Emit.StackBehaviour.Pushi8:
                case System.Reflection.Emit.StackBehaviour.Pushr4:
                case System.Reflection.Emit.StackBehaviour.Pushr8:
                case System.Reflection.Emit.StackBehaviour.Pushref:
                    return 1;
                case System.Reflection.Emit.StackBehaviour.Push1_push1:
                    return 2;
            }
            throw new InvalidOperationException();
        }
#endif // GENERATOR
    }
}
