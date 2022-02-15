using System;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCode {
    public static Register RegularFrameRegister, VariadicFrameRegister,
                           ReturnAddressRegister;
    public const Register ReturnValueRegister = Register.bx, 
                          ShiftRegister = Register.cl;

    static AssemblyCode() {
      RegularFrameRegister = RegisterToSize(Register.bp, TypeSize.PointerSize);
      VariadicFrameRegister = RegisterToSize(Register.di, TypeSize.PointerSize);
      ReturnAddressRegister = RegisterToSize(Register.bx, TypeSize.PointerSize);
    }

    private AssemblyOperator m_operator;
    private object[] m_operandArray = new object[3];
  
    public AssemblyCode(AssemblyOperator objectOp, object operand0,
                        object operand1, object operand2 = null,
                        int size = 0) {
      m_operator = objectOp;
      m_operandArray[0] = operand0;
      m_operandArray[1] = operand1;
      m_operandArray[2] = operand2;
      FromAdditionToIncrement();
      CheckSize(size);
    }

    public AssemblyOperator Operator {
      get { return m_operator; }
      set { m_operator = value; }
    }

    public object this[int index] {
      get { return m_operandArray[index]; }
      set { m_operandArray[index] = value; }
    }

    private void FromAdditionToIncrement() {
      if (((Operator == AssemblyOperator.add) ||
           (Operator == AssemblyOperator.sub)) &&
          ((m_operandArray[0] is Track) || (m_operandArray[0] is Register) ||
           (m_operandArray[0] is string))) {

        if ((m_operandArray[1] is int) && (m_operandArray[2] is BigInteger)) {
          CheckIncrement(2);
        }
        else if ((m_operandArray[1] is BigInteger) &&
                 (m_operandArray[2] == null)) {
          CheckIncrement(1);
        }
      }
    }

    private void CheckIncrement(int valueIndex) {
      int value = (int) ((BigInteger) m_operandArray[valueIndex]);

      if (((Operator == AssemblyOperator.add) && (value == 1)) ||
          ((Operator == AssemblyOperator.sub) && (value == -1))) {
        m_operator = AssemblyOperator.inc;
        m_operandArray[valueIndex] = null;
      }
      else if (((Operator == AssemblyOperator.add) && (value == -1)) ||
               ((Operator == AssemblyOperator.sub) && (value == 1))) {
        m_operator = AssemblyOperator.dec;
        m_operandArray[valueIndex] = null;
      }
    }

    private void CheckSize(int size) {
      if ((size != 0) && (m_operandArray[0] is string) &&
          (m_operandArray[1] is string) && (m_operandArray[2] == null)) {
        m_operator = OperatorToSize(m_operator, size);
      }
      
      if ((size != 0) && ((m_operandArray[0] is Register) ||
           (m_operandArray[0] is Track)|| (m_operandArray[0] is String)) &&
          (m_operandArray[1] is int) &&
          (IsUnary() || ((m_operandArray[2] is BigInteger) ||
                         (m_operandArray[2] is String)))) {
        m_operator = OperatorToSize(m_operator, size);
      }
    }

    public bool IsNullary() {
      string operatorName = Enum.GetName(typeof(AssemblyOperator), Operator);
      return operatorName.StartsWith("fldz") ||
             operatorName.StartsWith("fld1") ||
             operatorName.StartsWith("fchs") ||
             operatorName.StartsWith("fadd") ||
             operatorName.StartsWith("fsub") ||
             operatorName.StartsWith("fmul") ||
             operatorName.StartsWith("fdiv") ||
             operatorName.StartsWith("sahf") ||
             operatorName.StartsWith("fcomp") ||
             operatorName.StartsWith("syscall");
    }

    public bool IsUnary() {
      string operatorName = Enum.GetName(typeof(AssemblyOperator), Operator);
      return operatorName.StartsWith("neg") ||
             operatorName.StartsWith("not") ||
             operatorName.StartsWith("inc") ||
             operatorName.StartsWith("dec") ||
             operatorName.StartsWith("mul") ||
             operatorName.StartsWith("imul") ||
             operatorName.StartsWith("div") ||
             operatorName.StartsWith("idiv") ||
             operatorName.StartsWith("fst") ||
             operatorName.StartsWith("fld") ||
             operatorName.StartsWith("fist") ||
             operatorName.StartsWith("fild") ||
             operatorName.StartsWith("pop") ||
             operatorName.StartsWith("int");
    }

    public bool IsBinary() {
      string operatorName = Enum.GetName(typeof(AssemblyOperator), Operator);
      return operatorName.StartsWith("mov") ||
             operatorName.StartsWith("add") ||
             operatorName.StartsWith("sub") ||
             operatorName.StartsWith("and") ||
             operatorName.StartsWith("or") ||
             operatorName.StartsWith("xor") ||
             operatorName.StartsWith("shl") ||
             operatorName.StartsWith("shr") ||
             operatorName.StartsWith("cmp");
    }

    public bool IsJumpRegister() {
      return (Operator == AssemblyOperator.jmp) &&
             (m_operandArray[0] is Register);
    }

    public bool IsJumpNotRegister() {
      return (Operator == AssemblyOperator.jmp) &&
             !(m_operandArray[0] is Register);
    }

    public bool IsCallRegister() {
      return (Operator == AssemblyOperator.call) &&
             (m_operandArray[0] is Register);
    }

    public bool IsCallNotRegister() {
      return (Operator == AssemblyOperator.call) &&
             (m_operandArray[0] is string);
    }

    public bool IsRelationNotRegister() {
      switch (Operator) {
        case AssemblyOperator.je:
        case AssemblyOperator.jne:
        case AssemblyOperator.jl:
        case AssemblyOperator.jle:
        case AssemblyOperator.jg:
        case AssemblyOperator.jge:
        case AssemblyOperator.jb:
        case AssemblyOperator.jbe:
        case AssemblyOperator.ja:
        case AssemblyOperator.jae:
        case AssemblyOperator.jc:
        case AssemblyOperator.jnc:
          return true;

        default:
          return false;        
      }
    }

    private static ISet<ISet<Register>> m_registerOverlapSet =
      new HashSet<ISet<Register>>() {
        new HashSet<Register>() {Register.al, Register.ax,
                                  Register.eax, Register.rax},
        new HashSet<Register>() {Register.ah, Register.ax,
                                  Register.eax, Register.rax},
        new HashSet<Register>() {Register.bl, Register.bx,
                                  Register.ebx, Register.rbx},
        new HashSet<Register>() {Register.bh, Register.bx,
                                  Register.ebx, Register.rbx},
        new HashSet<Register>() {Register.cl, Register.cx,
                                  Register.ecx, Register.rcx},
        new HashSet<Register>() {Register.ch, Register.cx,
                                  Register.ecx, Register.rcx},
        new HashSet<Register>() {Register.dl, Register.dx,
                                  Register.edx, Register.rdx},
        new HashSet<Register>() {Register.dh, Register.dx,
                                  Register.edx, Register.rdx},
        new HashSet<Register>() {Register.si, Register.esi, Register.rsi},
        new HashSet<Register>() {Register.di, Register.edi, Register.rdi},
        new HashSet<Register>() {Register.bp, Register.ebp, Register.rbp},
        new HashSet<Register>() {Register.sp, Register.esp, Register.rsp}
      };

    public static bool RegisterOverlap(Register? register1,
                                       Register? register2) {
      if ((register1 == null) || (register2 == null)) {
        return false;
      }

      foreach (ISet<Register> registerSet in m_registerOverlapSet) {
        if (registerSet.Contains(register1.Value) &&
            registerSet.Contains(register2.Value)) {
          return true;
        }
      }

      return false;
    }

    /*public static bool RegisterOverlapX(Register? register1,
                                        Register? register2) {
      if ((register1 == null) || (register2 == null)) {
        return false;
      }
    
      if (register1.Equals(register2)) {
        return true;
      }

      string name1 = Enum.GetName(typeof(Register), register1),
             name2 = Enum.GetName(typeof(Register), register2);

      if ((name1.Contains("h") && name2.Contains("l")) ||
          (name1.Contains("l") && name2.Contains("h"))) {
        return false;
      }

      name1 = name1.Replace("h", "").Replace("l", "").Replace("x", "").Replace("e", "").Replace("r", "");
      name2 = name2.Replace("h", "").Replace("l", "").Replace("x", "").Replace("e", "").Replace("r", "");
      return name1.Equals(name2);
    }
  
    public static int SizeOfRegisterX(Register register) {
      string name = Enum.GetName(typeof(Register), register);
    
      if (name.Contains("r")) {
        return 8;
      }
      else if (name.Contains("e")) {
        return 4;
      }
      else if (name.Contains("l") || name.Contains("h")) {
        return 1;
      }
      else {
        return 2;
      }
    }*/

    /*public static Register RegisterToSizeX(Register register, int size) {
      string name = Enum.GetName(typeof(Register), register);

      switch (size) {
        case 1:
          name = name.Replace("x", "l").Replace("e", "").Replace("r", "");
          break;

        case 2:
          Error.ErrorXXX(!name.Contains("h"));
          name = name.Replace("l", "x").Replace("e", "").Replace("r", "");
          break;

        case 4:
          Error.ErrorXXX(!name.Contains("h"));
          name = "e" + name.Replace("l", "x").Replace("e", "")
                                             .Replace("r", "");
          break;

        case 8:
          Error.ErrorXXX(!name.Contains("h"));
          name = "r" + name.Replace("l", "x").Replace("e", "")
                                             .Replace("r", "");
          break;
      }

      return (Register) Enum.Parse(typeof(Register), name);
    }*/

    private static IDictionary<Register,int> m_registerSizeMap =
      new Dictionary<Register,int>() {
       {Register.al, 1}, {Register.bl, 1}, {Register.cl, 1}, {Register.dl, 1},
       {Register.ah, 1}, {Register.bh, 1}, {Register.ch, 1}, {Register.dh, 1},
       {Register.ax, 2}, {Register.bx, 2}, {Register.cx, 2}, {Register.dx, 2},
       {Register.eax,4}, {Register.ebx,4}, {Register.ecx,4}, {Register.edx,4},
       {Register.rax,8}, {Register.rbx,8}, {Register.rcx,8}, {Register.rdx,8},
       {Register.si, 2}, {Register.di, 2}, {Register.sp, 2}, {Register.bp, 2},
       {Register.esi,4}, {Register.edi,4}, {Register.esp,4}, {Register.ebp,4},
       {Register.rsi,8}, {Register.rdi,8}, {Register.rsp,8}, {Register.rbp, 8}
      };

    public static int SizeOfRegister(Register register) {
      return m_registerSizeMap[register];
    }

    /*private static IList<ISet<Register>> m_registerSetList =
      new HashSet<Register>[]{
        new HashSet<Register>() {Register.al, Register.bl, Register.cl, Register.dl,
                                 Register.ah, Register.bh, Register.ch, Register.dh},
        new HashSet<Register>() {Register.ax, Register.bx, Register.cx, Register.dx,
                                 Register.si, Register.di, Register.bp, Register.sp},
        new HashSet<Register>() {Register.eax, Register.ebx, Register.ecx, Register.edx,
                                 Register.esi, Register.edi, Register.ebp, Register.esp},
        new HashSet<Register>() {Register.rax, Register.rbx, Register.rcx, Register.rdx,
                                 Register.rsi, Register.rdi, Register.rbp, Register.rsp}
     };

    private static IDictionary<int,int> m_indexToSizeMap =
      new Dictionary<int,int>() {{0, 1}, {1, 2}, {2, 4}, {3, 8}};

    public static int SizeOfRegister(Register register) {
      for (int index = 0; index < m_registerSetList.Count; ++index)
      if (m_registerSetList[index].Contains(register)) {
        return m_indexToSizeMap[index];
      }

      Error.ErrorXXX(false);
      return 0;
    }*/

    private static ISet<IList<Register>> m_registerListSet =
      new HashSet<IList<Register>>() {
        new Register[] {Register.al, Register.ax, Register.eax, Register.rax},
        new Register[] {Register.bl, Register.bx, Register.ebx, Register.rbx},
        new Register[] {Register.cl, Register.cx, Register.ecx, Register.rcx},
        new Register[] {Register.dl, Register.dx, Register.edx, Register.rdx},
        new Register[] {default(Register), Register.si, Register.esi, Register.rsi},
        new Register[] {default(Register), Register.di, Register.edi, Register.rdi},
        new Register[] {default(Register), Register.bp, Register.ebp, Register.rbp},
        new Register[] {default(Register), Register.sp, Register.esp, Register.rsp}
      };

    private static IDictionary<int,int> m_sizeToIndexMap =
      new Dictionary<int, int>() {{1, 0}, {2, 1}, {4, 2}, {8, 3}};

    public static Register RegisterToSize(Register register, int size) {
      if (m_registerSizeMap[register] == size) {
        return register;
      }

      foreach (IList<Register> registerList in m_registerListSet) {
        if (registerList.Contains(register)) {
          int index = m_sizeToIndexMap[size];
          Error.ErrorXXX((index >= 0) && (index < registerList.Count));
          return registerList[index];
        }
      }

      Error.ErrorXXX(false);
      return default(Register);
    }

    /*private static IDictionary<Pair<Register,int>,Register> m_registerToSizeMap =
      new Dictionary<Pair<Register,int>,Register>() {
       {new Pair<Register,int>(Register.al, 2), Register.ax},
       {new Pair<Register,int>(Register.al, 4), Register.eax},
       {new Pair<Register,int>(Register.al, 8), Register.rax},
       {new Pair<Register,int>(Register.ax, 1), Register.al},
       {new Pair<Register,int>(Register.ax, 4), Register.eax},
       {new Pair<Register,int>(Register.ax, 8), Register.rax},
       {new Pair<Register,int>(Register.eax, 1), Register.al},
       {new Pair<Register,int>(Register.eax, 2), Register.ax},
       {new Pair<Register,int>(Register.eax, 8), Register.rax},
       {new Pair<Register,int>(Register.rax, 1), Register.al},
       {new Pair<Register,int>(Register.rax, 2), Register.ax},
       {new Pair<Register,int>(Register.rax, 4), Register.eax},

       {new Pair<Register,int>(Register.bl, 2), Register.bx},
       {new Pair<Register,int>(Register.bl, 4), Register.ebx},
       {new Pair<Register,int>(Register.bl, 8), Register.rbx},
       {new Pair<Register,int>(Register.bx, 1), Register.bl},
       {new Pair<Register,int>(Register.bx, 4), Register.ebx},
       {new Pair<Register,int>(Register.bx, 8), Register.rbx},
       {new Pair<Register,int>(Register.ebx, 1), Register.bl},
       {new Pair<Register,int>(Register.ebx, 2), Register.bx},
       {new Pair<Register,int>(Register.ebx, 8), Register.rbx},
       {new Pair<Register,int>(Register.rbx, 1), Register.bl},
       {new Pair<Register,int>(Register.rbx, 2), Register.bx},
       {new Pair<Register,int>(Register.rbx, 4), Register.ebx},

       {new Pair<Register,int>(Register.cl, 2), Register.cx},
       {new Pair<Register,int>(Register.cl, 4), Register.ecx},
       {new Pair<Register,int>(Register.cl, 8), Register.rcx},
       {new Pair<Register,int>(Register.cx, 1), Register.cl},
       {new Pair<Register,int>(Register.cx, 4), Register.ecx},
       {new Pair<Register,int>(Register.cx, 8), Register.rcx},
       {new Pair<Register,int>(Register.ecx, 1), Register.cl},
       {new Pair<Register,int>(Register.ecx, 2), Register.cx},
       {new Pair<Register,int>(Register.ecx, 8), Register.rcx},
       {new Pair<Register,int>(Register.rcx, 1), Register.cl},
       {new Pair<Register,int>(Register.rcx, 2), Register.cx},
       {new Pair<Register,int>(Register.rcx, 4), Register.ecx},

       {new Pair<Register,int>(Register.dl, 2), Register.dx},
       {new Pair<Register,int>(Register.dl, 4), Register.edx},
       {new Pair<Register,int>(Register.dl, 8), Register.rdx},
       {new Pair<Register,int>(Register.dx, 1), Register.dl},
       {new Pair<Register,int>(Register.dx, 4), Register.edx},
       {new Pair<Register,int>(Register.dx, 8), Register.rdx},
       {new Pair<Register,int>(Register.edx, 1), Register.dl},
       {new Pair<Register,int>(Register.edx, 2), Register.dx},
       {new Pair<Register,int>(Register.edx, 8), Register.rdx},
       {new Pair<Register,int>(Register.rdx, 1), Register.dl},
       {new Pair<Register,int>(Register.rdx, 2), Register.dx},
       {new Pair<Register,int>(Register.rdx, 4), Register.edx},

       {new Pair<Register,int>(Register.si, 4), Register.esi},
       {new Pair<Register,int>(Register.si, 8), Register.rsi},
       {new Pair<Register,int>(Register.esi, 2), Register.si},
       {new Pair<Register,int>(Register.esi, 8), Register.rsi},
       {new Pair<Register,int>(Register.rsi, 2), Register.si},
       {new Pair<Register,int>(Register.rsi, 4), Register.esi},

       {new Pair<Register,int>(Register.di, 4), Register.edi},
       {new Pair<Register,int>(Register.di, 8), Register.rdi},
       {new Pair<Register,int>(Register.edi, 2), Register.di},
       {new Pair<Register,int>(Register.edi, 8), Register.rdi},
       {new Pair<Register,int>(Register.rdi, 2), Register.di},
       {new Pair<Register,int>(Register.rdi, 4), Register.edi},

       {new Pair<Register,int>(Register.bp, 4), Register.ebp},
       {new Pair<Register,int>(Register.bp, 8), Register.rbp},
       {new Pair<Register,int>(Register.ebp, 2), Register.bp},
       {new Pair<Register,int>(Register.ebp, 8), Register.rbp},
       {new Pair<Register,int>(Register.rbp, 2), Register.bp},
       {new Pair<Register,int>(Register.rbp, 4), Register.ebp},

       {new Pair<Register,int>(Register.sp, 4), Register.esp},
       {new Pair<Register,int>(Register.sp, 8), Register.rsp},
       {new Pair<Register,int>(Register.esp, 2), Register.sp},
       {new Pair<Register,int>(Register.esp, 8), Register.rsp},
       {new Pair<Register,int>(Register.rsp, 2), Register.sp},
       {new Pair<Register,int>(Register.rsp, 4), Register.esp},
      };

    public static Register RegisterToSize(Register register, int size) {
      Error.ErrorXXX((size == 1) || (size == 2) || (size == 4) || (size == 8));

      if (m_registerSizeMap[register] == size) {
        return register;
      }
      else {
        Pair<Register,int> pair = new Pair<Register,int>(register, size);
        Error.ErrorXXX(m_registerToSizeMap.ContainsKey(pair));
        return m_registerToSizeMap[pair];
      }
    }

    private static IDictionary<AssemblyOperator,int> m_operatorSizeMap =
      new Dictionary<AssemblyOperator,int>() {
       {AssemblyOperator.mov_byte, 1}, {AssemblyOperator.mov_word, 2},
       {AssemblyOperator.mov_dword, 4}, {AssemblyOperator.mov_qword, 8},
       {AssemblyOperator.cmp_byte, 1}, {AssemblyOperator.cmp_word, 2},
       {AssemblyOperator.cmp_dword, 4}, {AssemblyOperator.cmp_qword, 8},

       {AssemblyOperator.add_byte, 1}, {AssemblyOperator.add_word, 2},
       {AssemblyOperator.add_dword, 4}, {AssemblyOperator.add_qword, 8},
       {AssemblyOperator.sub_byte, 1}, {AssemblyOperator.sub_word, 2},
       {AssemblyOperator.sub_dword, 4}, {AssemblyOperator.sub_qword, 8},

       {AssemblyOperator.mul_byte, 1}, {AssemblyOperator.mul_word, 2},
       {AssemblyOperator.mul_dword, 4}, {AssemblyOperator.mul_qword, 8},
       {AssemblyOperator.div_byte, 1}, {AssemblyOperator.div_word, 2},
       {AssemblyOperator.div_dword, 4}, {AssemblyOperator.div_qword, 8},

       {AssemblyOperator.imul_byte, 1}, {AssemblyOperator.imul_word, 2},
       {AssemblyOperator.imul_dword, 4}, {AssemblyOperator.imul_qword, 8},
       {AssemblyOperator.idiv_byte, 1}, {AssemblyOperator.idiv_word, 2},
       {AssemblyOperator.idiv_dword, 4}, {AssemblyOperator.idiv_qword, 8},

       {AssemblyOperator.inc_byte, 1}, {AssemblyOperator.inc_word, 2},
       {AssemblyOperator.inc_dword, 4}, {AssemblyOperator.inc_qword, 8},
       {AssemblyOperator.dec_byte, 1}, {AssemblyOperator.dec_word, 2},
       {AssemblyOperator.dec_dword, 4}, {AssemblyOperator.dec_qword, 8},

       {AssemblyOperator.neg_byte, 1}, {AssemblyOperator.neg_word, 2},
       {AssemblyOperator.neg_dword, 4}, {AssemblyOperator.neg_qword, 8},
       {AssemblyOperator.not_byte, 1}, {AssemblyOperator.not_word, 2},
       {AssemblyOperator.not_dword, 4}, {AssemblyOperator.not_qword, 8},

       {AssemblyOperator.and_byte, 1}, {AssemblyOperator.and_word, 2},
       {AssemblyOperator.and_dword, 4}, {AssemblyOperator.and_qword, 8},
       {AssemblyOperator.or_byte, 1}, {AssemblyOperator.or_word, 2},
       {AssemblyOperator.or_dword, 4}, {AssemblyOperator.or_qword, 8},
       {AssemblyOperator.xor_byte, 1}, {AssemblyOperator.xor_word, 2},
       {AssemblyOperator.xor_dword, 4}, {AssemblyOperator.xor_qword, 8},

       {AssemblyOperator.shl_byte, 1}, {AssemblyOperator.shl_word, 2},
       {AssemblyOperator.shl_dword, 4}, {AssemblyOperator.shl_qword, 8},
       {AssemblyOperator.shr_byte, 1}, {AssemblyOperator.shr_word, 2},
       {AssemblyOperator.shr_dword, 4}, {AssemblyOperator.shr_qword, 8},
       
       {AssemblyOperator.fld_dword, 4}, {AssemblyOperator.fld_qword, 8},
       {AssemblyOperator.fst_dword, 4}, {AssemblyOperator.fst_qword, 8},
       {AssemblyOperator.fstp_dword, 4}, {AssemblyOperator.fstp_qword, 8},
       
                                         {AssemblyOperator.fild_word, 2},
       {AssemblyOperator.fild_dword, 4}, {AssemblyOperator.fild_qword, 8},
                                         {AssemblyOperator.fist_word, 2},
       {AssemblyOperator.fist_dword, 4}, {AssemblyOperator.fist_qword, 8},
                                          {AssemblyOperator.fistp_word, 2},
       {AssemblyOperator.fistp_dword, 4}, {AssemblyOperator.fistp_qword, 8}
      };

    public static int SizeOfOperator(AssemblyOperator objectOp) {
      return m_operatorSizeMap[objectOp];
    }*/

    public static int SizeOfOperator(AssemblyOperator objectOp) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);
      string suffix = name.Substring(name.IndexOf("_") + 1);

      switch (suffix) {
        case "byte":
          return 1;

        case "word":
          return 2;

        case "dword":
          return 4;

        default: // "qword":
          return 8;
      }
    }
  
    public static AssemblyOperator OperatorToSize
                                   (AssemblyOperator objectOp, int size) {
      string name = Enum.GetName(typeof(AssemblyOperator), objectOp);    
      Error.ErrorXXX(objectOp != AssemblyOperator.interrupt);
    
      switch (size) {
        case 1:
          name = name + "_byte";
          break;
        
        case 2:
          name = name + "_word";
          break;
        
        case 4:
          name = name + "_dword";
          break;

        case 8:
          name = name + "_qword";
          break;
      }

      Error.ErrorXXX(name.Contains("_"));
      return ((AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name));
    }

    public static int SizeOfValue(BigInteger value, AssemblyOperator op) {
      string name = Enum.GetName(typeof(AssemblyOperator), op);

      if (name.StartsWith("mov")) {
        return SizeOfOperator(op);
      }
      else if (name.StartsWith("cmp") && (value == 0)) {
        return 1;
      }
      else {
        return SizeOfValue(value);
      }
    }

    public static int SizeOfValue(BigInteger value, AssemblyOperator op, Register register) {
      if ((op == AssemblyOperator.mov) || (op == AssemblyOperator.and)) {
        return SizeOfRegister(register);
      }
      else {
        return SizeOfValue(value);
      }
    }

    public static int SizeOfValue(BigInteger value) {
      if (value == 0) {
        return 0;
      }
      else if ((-128 <= value) && (value <= 127)) {
        return 1;
      }
      else if ((-32768 <= value) && (value <= 32767)) {
        return 2;
      }
      else if ((-2147483648 <= value) && (value <= 2147483647)) {
        return 4;
      }
      else {
        return 8;
      }
    }

    /*public static int SizeOfValue(BigInteger value) {
      if (value == 0) {
        return 0;
      }
      else if ((-128 <= value) && (value <= 255)) {
        return 1;
      }
      else if ((-32768 <= value) && (value <= 65535)) {
        return 2;
      }
      else if ((-2147483648 <= value) && (value <= 4294967295)) {
        return 4;
      }
      else {
        return 8;
      }
    }*/

    public override string ToString() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];
      string operatorName = Enum.GetName(typeof(AssemblyOperator),
                                         Operator).Replace("_", " ");

      // lahf; syscall
      if (IsNullary()) {
        Error.ErrorXXX((operand0 == null) &&  (operand1 == null) && (operand2 == null));
        return "\t" + operatorName;
      }
      else if (IsUnary()) {
        // inc [bp + 2]; inc [global + 4]
        if (((operand0 is Register) || (operand0 is string)) &&
                 (operand1 is int) && (operand2 == null)) {
          return "\t" + operatorName + " [" + operand0 +
                 WithSign(operand1) + "]";
        }
        // inc ax
        else if ((operand0 is Register) && (operand1 == null) &&
                 (operand2 == null)) {
          Error.ErrorXXX(!(operand0 is BigInteger));
          return "\t" + operatorName + " " + operand0;
        }
      }
      else if (IsBinary()) {
        // mov ax, bx; mov ax, global; mov ax, 123
        if ((operand0 is Register) && ((operand1 is Register) ||
            (operand1 is string) || (operand1 is BigInteger)) &&
            (operand2 == null)) {
          Error.ErrorXXX(!(operand0 is string));
          return "\t" + operatorName + " " + operand0 + ", " + operand1;
        }
        // mov ax, [bp + 2]; mov ax, [global + 4]
        else if ((operand0 is Register) && ((operand1 is Register) ||
                 (operand1 is string)) && (operand2 is int)) {
          return "\t" + operatorName + " " + operand0 +
                 ", [" + operand1 + WithSign(operand2) + "]";
        }
        // mov [bp + 2], ax; mov [global + 4], ax; mov [bp + 2], 123; mov [global + 4], 123; mov [bp + 2], global; mov [global + 4], global
        else if (((operand0 is Register) || (operand0 is string)) &&
                 (operand1 is int) && ((operand2 is Register) ||
                  (operand2 is string) || (operand2 is BigInteger))) {
          return "\t" + operatorName +
                 " [" + operand0 + WithSign(operand1) + "], " + operand2;
        }
      }
      else if (Operator == AssemblyOperator.label) {
        return "\n " + operand0 + ":";
      }
      else if (Operator == AssemblyOperator.comment) {
        return "\t; " + operand0;
      }
      else if (Operator == AssemblyOperator.define_address) {
        string name = (string) operand0;
        int offset = (int) operand1;
        return "\tdq " + name + WithSign(offset);
      }
      else if (Operator == AssemblyOperator.define_zero_sequence) {
        int size = (int) operand0;
        return "\ttimes " + size + " db 0";
      }
      else if (Operator == AssemblyOperator.define_value) {
        Sort sort = (Sort) operand0;
        object value = operand1;

        if (sort == Sort.String) {
          return "\tdb " + ToVisibleString((string) operand1);
        }
        else {
          string text = operand1.ToString();

          if (((sort == Sort.Float) || (sort == Sort.Double) ||
              (sort == Sort.LongDouble)) && !text.Contains(".")) {
            text += ".0";
          }

          switch (TypeSize.Size(sort)) {
            case 1:
              return "\tdb " + text;

            case 2:
              return "\tdw " + text;

            case 4:
              return "\tdd " + text;

            case 8: 
              return "\tdq " + text;
          }
        }
      }
      else if (IsJumpRegister() || IsCallRegister() ||
               IsCallNotRegister()) {
        return "\tjmp " + operand0;
      }
      else if (Operator == AssemblyOperator.return_address) {
        string target = SymbolTable.CurrentFunction.UniqueName +
                        Symbol.SeparatorId + operand2;
        return "\tmov qword [" + operand0 + WithSign(operand1) + "], " +
               target;
      }
      else if (IsRelationNotRegister() || IsJumpNotRegister()) {
        Error.ErrorXXX(operand2 is int);
        string label = SymbolTable.CurrentFunction.UniqueName +
                        Symbol.SeparatorId + operand2;
        return "\t" + operatorName + " " + label;

        /*if (operand2 is int) {
          string label = SymbolTable.CurrentFunction.UniqueName +
                          Symbol.SeparatorId + operand2;
          return "\t" + operatorName + " " + label;
        }
        /*else if (operand2 is string) {
          return "\t" + operatorName + " " + operand2;
        }
        else {
          int labelIndex = (int) operand1;
          string labelText = MakeLabel(labelIndex);
          return "\t" + operatorName + " " + labelText;
        }*/
      }
      else if (Operator == AssemblyOperator.empty) { // XXX
        return null;
      }
      else if (Operator == AssemblyOperator.new_middle_code) {
        return null;
      }

      Error.ErrorXXX(false);
      return null;
    }

    private static string ToVisibleString(string text) {
      StringBuilder buffer = new StringBuilder();
      bool insideString = false;

      foreach (char c in text) {
        if (Char.IsControl(c) || (c == '\"') || (c == '\'')) {
          if (insideString) {
            buffer.Append("\", " + ((int) c).ToString() + ", ");
            insideString = false;
          }
          else {
            buffer.Append(((int) c).ToString() + ", ");
          }
        }
        else {
          if (insideString) {
            buffer.Append(c);
          }
          else {
            buffer.Append("\"" + c.ToString());
            insideString = true;
          }
        }
      }

      if (insideString) {
        buffer.Append("\", 0");
      }
      else {
        buffer.Append("0");
      }

      return buffer.ToString();
    }

    /*public static string MakeLabel(int labelIndex) {
      return "label" + Symbol.SeparatorId + labelIndex;
    }*/

    private string WithSign(object value) {
      int offset = (int) value;
      
      if (offset > 0) {
        return " + " + offset;
      }
      else if (offset < 0) {
        return " - " + (-offset);
      }
      else {
        return "";
      }
    }

    public List<byte> ByteList() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];

      if ((Operator == AssemblyOperator.empty) ||
          (Operator == AssemblyOperator.label) ||
          (Operator == AssemblyOperator.comment)) {
        return (new List<byte>());
      }
      else if (Operator == AssemblyOperator.define_address) {
        int offset = (int) operand1;
        List<byte> byteList = new List<byte>(new byte[TypeSize.PointerSize]);
        LoadByteList(byteList, 0, TypeSize.PointerSize, (BigInteger)offset); 
        return byteList;
      }
      else if (Operator == AssemblyOperator.define_zero_sequence) {
        int size = (int) operand0;
        return (new List<byte>(new byte[size]));
      }
      else if (Operator == AssemblyOperator.define_value) {
        Sort sort = (Sort) operand0;
        object value = operand1;
        List<byte> byteList;

        if (sort == Sort.Float) {
          float floatValue = (float) ((decimal) operand0);
          byteList =  new List<byte>(BitConverter.GetBytes(floatValue));
        }
        else if ((sort == Sort.Double) || (sort == Sort.LongDouble)) {
          double doubleValue = (double) ((decimal) value);
          byteList = new List<byte>(BitConverter.GetBytes(doubleValue));
        }
        else if (sort == Sort.String) {
          string text = (string) value;
          byteList = new List<byte>();

          foreach (char c in text) {
            byteList.Add((byte) c);
          }

          byteList.Add((byte) 0);
        }
        else {
          int size = TypeSize.Size(sort);
          byteList = new List<byte>(new byte[size]);
          
          if (value is StaticAddress) {
            StaticAddress staticAddress = (StaticAddress) value;
            LoadByteList(byteList, 0, size,
                         (BigInteger)staticAddress.Offset);
          }
          else {
            LoadByteList(byteList, 0, size, (BigInteger) value);         
          }
        }

        return byteList;
      }
      else if (IsJumpRegister() || IsCallRegister()) {
        Register register = (Register) operand0;
        return LookupByteArray(AssemblyOperator.jmp, register);
      }
      else if (IsCallNotRegister()) {
        return LookupByteArray(AssemblyOperator.jmp, TypeSize.PointerSize);
      }
      else if (Operator == AssemblyOperator.return_address) {
        Register register = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        int address = (int) ((BigInteger) operand2);
        List<byte> byteList =
          LookupByteArray(AssemblyOperator.mov_word, register,
                          size, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - (size + TypeSize.PointerSize),
                     size, offset);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, address);
        return byteList;
      }
      else if (IsRelationNotRegister() || IsJumpNotRegister()) {
        int address = (int) operand0;

        if (address == 0) {
          return (new List<byte>());
        }
        else {
          int size = SizeOfValue(address);

          if (address == 127) { // XXX
            size = 2;
          }

          List<byte> byteList = LookupByteArray(Operator, size);
          LoadByteList(byteList, byteList.Count - size, size, address);
          return byteList;
        }
      }

      // lahf
      else if ((operand0 == null) && (operand1 == null) &&
               (operand2 == null)) {
        return LookupByteArray(Operator);
      }

      // inc ax
      else if ((operand0 is Register) && (operand1 == null) &&
               (operand2 == null)) {
        Register register = (Register) operand0;
        return LookupByteArray(Operator, register);
      }

      // int 33
      else if ((operand0 is BigInteger) && (operand1 == null) &&
               (operand2 == null)) {
        BigInteger value = (BigInteger) operand0;
        int size = SizeOfValue(value);
        List<byte> byteList = LookupByteArray(Operator, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }

      // inc [bp + 2]
      /*else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 == null)) {
        Register register = (Register) operand0;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        List<byte> byteList = LookupByteArray(Operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList;
      }
      // inc [global + 4]
      else if ((operand0 is string) && (operand1 is int) &&
               (operand2 == null)) {
        int offset = (int) operand1;
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList;
      }*/

      // inc [bp + 2]; inc [global + 4]
      else if (((operand0 is Register) || (operand0 is string)) &&
               (operand1 is int) && (operand2 == null)) {
        int offset = (int) operand1;
        int size = (operand0 is Register) ? SizeOfValue(offset)
                                          : TypeSize.PointerSize;
        List<byte> byteList = LookupByteArray(Operator, operand0, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList;
      }

      // 1: mov ax, bx
      else if ((operand0 is Register) && (operand1 is Register) &&
               (operand2 == null)) {
        Register toRegister = (Register) operand0,
                 fromRegister = (Register) operand1;
        return LookupByteArray(Operator, toRegister, fromRegister);
      }

      // 2: mov ax, global
      else if ((operand0 is Register) && (operand1 is string) &&
               (operand2 == null)) {
        Register register = (Register) operand0;
        List<byte> byteList =
          LookupByteArray(Operator, register, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, 0);
        return byteList;
      }

      // 3: mov ax, 123
      else if ((operand0 is Register) && (operand1 is BigInteger) &&
               (operand2 == null)) {
        Register register = (Register) operand0;
        BigInteger value = (BigInteger) operand1;
        int size;

        if ((Operator == AssemblyOperator.add) &&
            (register == Register.eax) &&
            (SizeOfValue(value) == 2)) {
          size = 4; // CCompiler.Type.LongSize;
        }
        else {
          size = ((Operator == AssemblyOperator.mov) ||
                  (Operator == AssemblyOperator.and))
                  ? SizeOfRegister(register) : SizeOfValue(value);
        }

        List<byte> byteList = LookupByteArray(Operator, register, size);
        LoadByteList(byteList, byteList.Count - size, size, value);
        return byteList;
      }

      /*// mov [bp + 2], ax
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is Register)) {
        Register baseRegister = (Register) operand0,
                 fromRegister = (Register) operand2;
        int offset = (int) operand1;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, size, fromRegister);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      // mov [global + 4], ax
      else if ((operand0 is string) && (operand1 is int) &&
               (operand2 is Register)) {
        int offset = (int) operand1;
        Register fromRegister = (Register) operand2;
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize, fromRegister);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }*/

      // 4: mov [bp + 2], ax; mov [global + 4], ax
      else if (((operand0 is Register) || (operand0 is string)) &&
               (operand1 is int) && (operand2 is Register)) {
        Register register = (Register) operand2;
        int offset = (int) operand1;
        int size = (operand0 is Register) ? SizeOfValue(offset)
                                          : TypeSize.PointerSize;
        List<byte> byteList =
          LookupByteArray(Operator, operand0, size, register);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }

      // mov [bp + 2], global
      /*else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is string)) {
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        int offsetSize = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, offsetSize,
                          TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count -
                    (offsetSize + TypeSize.PointerSize), offsetSize, offset);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, 0);
        return byteList; 
      }
      // mov [global + 4], global
      else if ((operand0 is string) && (operand1 is int) &&
               (operand2 is string)) {
        int offset = (int) operand1;
        List<byte> byteList = LookupByteArray(Operator, null, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count -
                    (TypeSize.PointerSize + TypeSize.PointerSize), TypeSize.PointerSize, offset);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, 0);
        return byteList; 
      }*/

      // 5: mov [bp + 2], global; mov [global + 4], global
      else if (((operand0 is Register) || (operand0 is string)) &&
               (operand1 is int) && (operand2 is string)) {
        int offset = (int) operand1;
        int size = (operand0 is Register) ? SizeOfValue(offset)
                                          : TypeSize.PointerSize;
        List<byte> byteList = LookupByteArray(Operator, operand0,
                                              size, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count -
                    (size + TypeSize.PointerSize), size, offset);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, 0);
        return byteList; 
      }

      /*// mov [bp + 2], 123
      else if ((operand0 is Register) && (operand1 is int) &&
               (operand2 is BigInteger)) {
        Register baseRegister = (Register) operand0;
        int offset = (int) operand1;
        BigInteger value = (BigInteger) operand2;
        int offsetSize = SizeOfValue(offset),
            valueSize = SizeOfValue(value, Operator);
        List<byte> byteList =
          LookupByteArray(Operator, baseRegister, offsetSize, valueSize);
        LoadByteList(byteList, byteList.Count - (offsetSize + valueSize),
                     offsetSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize,
                     valueSize, value);
        return byteList;
      }
      // mov [global + 4], 123
      else if ((operand0 is string) && (operand1 is int) &&
               (operand2 is BigInteger)) {
        int offset = (int) operand1;
        BigInteger value = (BigInteger) operand2;
        int valueSize = SizeOfValue(value, Operator);
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize, valueSize);
        LoadByteList(byteList, byteList.Count - (TypeSize.PointerSize +
                     valueSize), TypeSize.PointerSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize,
                     valueSize, value);
        return byteList; 
      }*/

      // 6: mov [bp + 2], 123; mov [global + 4], 123
      // mov [null + 4], 123; Special case
      else if (((operand0 is Register) || (operand0 is string) ||
                (operand0 == null)) && (operand1 is int) &&
               (operand2 is BigInteger)) {
        int offset = (int) operand1;
        BigInteger value = (BigInteger) operand2;
        int offsetSize = (operand0 is Register) ? SizeOfValue(offset)
                                                : TypeSize.PointerSize,
            valueSize;
        
        if ((Operator == AssemblyOperator.add_dword) &&
            (operand0 is Register) &&
            (operand1 is int) &&
            (operand2 is BigInteger) &&
            (SizeOfValue(value, Operator)) == 2) {
          valueSize = 4; // CCompiler.Type.LongSize;
        }
        else {
          valueSize = SizeOfValue(value, Operator);
        }

        List<byte> byteList =
          LookupByteArray(Operator, operand0, offsetSize, valueSize);
        LoadByteList(byteList, byteList.Count - (offsetSize + valueSize),
                     offsetSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize,
                     valueSize, value);
        return byteList;
      }

      /*// mov ax, [bp + 2]
      else if ((operand0 is Register) && (operand1 is Register) &&
               (operand2 is int)) {
        Register toRegister = (Register) operand0,
                 baseRegister = (Register) operand1;
        int offset = (int) operand2;
        int size = SizeOfValue(offset);
        List<byte> byteList =
          LookupByteArray(Operator, toRegister, baseRegister, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }
      // mov ax, [global + 4]
      else if ((operand0 is Register) && (operand1 is string) &&
               (operand2 is int)) {
        Register toRegister = (Register) operand0;
        int offset = (int) operand2;
        List<byte> byteList =
          LookupByteArray(Operator, toRegister, null, TypeSize.PointerSize);
        LoadByteList(byteList, byteList.Count - TypeSize.PointerSize,
                     TypeSize.PointerSize, offset);
        return byteList; 
      }*/

      // 7: mov ax, [bp + 2]; mov ax, [global + 4]
      else if ((operand0 is Register) && ((operand1 is Register) ||
               (operand1 is string)) && (operand2 is int)) {
        Register register = (Register) operand0;
        int offset = (int) operand2;
        int size = (operand1 is Register) ? SizeOfValue(offset)
                                          : TypeSize.PointerSize;
        List<byte> byteList =
          LookupByteArray(Operator, register, operand1, size);
        LoadByteList(byteList, byteList.Count - size, size, offset);
        return byteList; 
      }

      // mov [null + 4], 123; Special case
      /*else if ((operand0 == null) && (operand1 is int) &&
               (operand2 is BigInteger)) {
        int offset = (int) operand1;
        BigInteger value = (BigInteger) operand2;
        int valueSize = SizeOfValue(value, Operator);
        List<byte> byteList =
          LookupByteArray(Operator, null, TypeSize.PointerSize, valueSize);
        LoadByteList(byteList, byteList.Count - (TypeSize.PointerSize +
                     valueSize), TypeSize.PointerSize, offset);
        LoadByteList(byteList, byteList.Count - valueSize,
                     valueSize, value);
        return byteList; 
      }*/

      Error.ErrorXXX(false);
      return null;
    }

    public static void LoadByteList(IList<byte> byteList, int index,
                                    int size, BigInteger value) {
      switch (size) {
        case 1: {
            if (value < 0) {
              byteList[index] = (byte) ((sbyte) value);
            }
            else {
              byteList[index] = (byte) value;
            }
          }
          break;

        case 2: {
            if (value < 0) {
              short shortValue = (short) value; 
              byteList[index] = (byte) ((sbyte) shortValue);
              byteList[index + 1] = (byte) ((sbyte) (shortValue >> 8));
            }
            else {
              ushort ushortValue = (ushort) value; 
              byteList[index] = (byte) ushortValue;
              byteList[index + 1] = (byte) (ushortValue >> 8);
            }
          }
          break;

        case 4: {
            if (value < 0) {
              int intValue = (int) value; 
              byteList[index] = (byte) ((sbyte) intValue);
              byteList[index + 1] = (byte) ((sbyte) (intValue >> 8));
              byteList[index + 2] = (byte) ((sbyte) (intValue >> 16));
              byteList[index + 3] = (byte) ((sbyte) (intValue >> 24));
            }
            else {
              uint uintValue = (uint) value; 
              byteList[index] = (byte) uintValue;
              byteList[index + 1] = (byte) (uintValue >> 8);
              byteList[index + 2] = (byte) (uintValue >> 16);
              byteList[index + 3] = (byte) (uintValue >> 24);
            }
          }
          break;
      }
    }

    public static List<byte> LookupByteArray(AssemblyOperator objectOp,
                      object operand0 = null, object operand1 = null,
                      object operand2 = null) {
      if ((objectOp == AssemblyOperator.shl) ||
          (objectOp == AssemblyOperator.shr)) {
        operand0 = (operand0 is BigInteger) ? 0L : operand0;
        operand1 = (operand1 is BigInteger) ? 0L : operand1;
        operand2 = (operand2 is BigInteger) ? 0L : operand2;
      }

      operand0 = (operand0 is string) ? null : operand0;
      operand1 = (operand1 is string) ? null : operand1;
      operand2 = (operand2 is string) ? null : operand2;

      ObjectCodeInfo info =
        new ObjectCodeInfo(objectOp, operand0, operand1, operand2);
      byte[] byteArray = ObjectCodeTable.MainArrayMap[info];
      Error.ErrorXXX(byteArray != null);
      List<byte> byteList = new List<byte>();

      foreach (byte b in byteArray) {
        byteList.Add(b);
      }

      return byteList;
    }
  }
}