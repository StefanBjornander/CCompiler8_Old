using System.Linq;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCodeGenerator {
    public IDictionary<Symbol,Track> m_trackMap =
      new Dictionary<Symbol,Track>();
    public List<AssemblyCode> m_assemblyCodeList;

    private int m_floatStackSize = 0;
    public const int FloatingStackMaxSize = 7;
    private Stack<int> m_topStack = new Stack<int>();

    //public static string MainName = "main";
    public static string InitializerName = Symbol.SeparatorId + "initializer";
    public static string ArgsName = Symbol.SeparatorId + "args";
    public static string PathName = Symbol.SeparatorId + "PathName";
  
    public AssemblyCodeGenerator(List<AssemblyCode> assemblyCodeList) {
      m_assemblyCodeList = assemblyCodeList;
    }

    private void RegisterAllocation(ISet<Track> trackSet) {
      new RegisterAllocator(trackSet, m_assemblyCodeList);
    }

    public static void GenerateAssembly(List<MiddleCode> middleCodeList,
                                        List<AssemblyCode> assemblyCodeList) {
      AssemblyCodeGenerator objectCodeGenerator =
        new AssemblyCodeGenerator(assemblyCodeList);
      objectCodeGenerator.AssemblyCodeList(middleCodeList);
      ISet<Track> trackSet = objectCodeGenerator.TrackSet();
      objectCodeGenerator.RegisterAllocation(trackSet);
    }
  
    public static void GenerateTargetWindows
      (List<AssemblyCode> assemblyCodeList, List<byte> byteList,
       IDictionary<int,string> accessMap, IDictionary<int,string> callMap,
       ISet<int> returnSet) {
      AssemblyCodeGenerator objectCodeGenerator =
        new AssemblyCodeGenerator(assemblyCodeList);
      objectCodeGenerator.WindowsJumpInfo();
      objectCodeGenerator.WindowsByteList
                          (byteList, accessMap, callMap, returnSet);
    }

    public AssemblyCode AddAssemblyCode(AssemblyOperator objectOp,
                          object operand0 = null, object operand1 = null,
                          object operand2 = null, int size = 0) {
      AssemblyCode assemblyCode =
        new AssemblyCode(objectOp, operand0, operand1, operand2, size);
      m_assemblyCodeList.Add(assemblyCode);
      return assemblyCode;
    }

    public static AssemblyCode AddAssemblyCode(List<AssemblyCode> list,
                    AssemblyOperator objectOp, object operand0 = null,
                    object operand1 = null, object operand2 = null,
                    int size = 0) {
      AssemblyCode assemblyCode =
        new AssemblyCode(objectOp, operand0, operand1, operand2, size);
      list.Add(assemblyCode);
      return assemblyCode;
    }

    public void AssemblyCodeList(List<MiddleCode> middleCodeList){
      for (int middleIndex = 0; middleIndex < middleCodeList.Count;
           ++middleIndex) {
        MiddleCode middleCode = middleCodeList[middleIndex];
        AddAssemblyCode(AssemblyOperator.new_middle_code, middleIndex);

        if (SymbolTable.CurrentFunction != null) {
          if ((middleCode.Operator != MiddleOperator.Initializer) &&
              (middleCode.Operator != MiddleOperator.InitializerZero)) {
            string labelText = SymbolTable.CurrentFunction.UniqueName;
            
            if (middleIndex > 0) {
              labelText += Symbol.SeparatorId + middleIndex;
            }

            AddAssemblyCode(AssemblyOperator.label, labelText);
          }
        }

        AddAssemblyCode(AssemblyOperator.comment, middleCode.ToString());

        /*if ((SymbolTable.CurrentFunction != null) &&
            SymbolTable.CurrentFunction.Name.Equals("printArgument") &&
            (middleIndex == 23)) {
          int i = 1;
        }*/

        switch (middleCode.Operator) {
          case MiddleOperator.PreCall:
            FunctionPreCall(middleCode);
            break;

          case MiddleOperator.Call:
            FunctionCall(middleCode, middleIndex);
            break;

          case MiddleOperator.PostCall:
            FunctionPostCall(middleCode);
            break;

          case MiddleOperator.Return:
            Return(middleCode, middleIndex);
            break;

          case MiddleOperator.Exit:
            Exit(middleCode);
            break;

          case MiddleOperator.Jump:
            Goto(middleCode);
            break;

          case MiddleOperator.AssignRegister:
            LoadToRegister(middleCode);
            break;

          case MiddleOperator.InspectRegister:
            InspectRegister(middleCode);
            break;

          case MiddleOperator.JumpRegister:
            JumpToRegister(middleCode);
            break;

          case MiddleOperator.Interrupt:
            Interrupt(middleCode);
            break;

          case MiddleOperator.SysCall:
            SystemCall(middleCode);
            break;

          case MiddleOperator.Initializer:
            Initializer(middleCode);
            break;

          case MiddleOperator.InitializerZero:
            InitializerZero(middleCode);
            break;
          
          case MiddleOperator.AssignInitSize:
            StructUnionAssignInit(middleCode);
            break;

          case MiddleOperator.Assign: {
              Symbol symbol = (Symbol) middleCode[0];

              if (symbol.Type.IsStructOrUnion()) {
                StructUnionAssign(middleCode, middleIndex);
              }
              else {
                IntegralAssign(middleCode);
              }
            }
            break;
        
          case MiddleOperator.BitwiseAnd:
          case MiddleOperator.BitwiseOr:
          case MiddleOperator.BitwiseXOr:
          case MiddleOperator.ShiftLeft:
          case MiddleOperator.ShiftRight:
            IntegralRelation(middleCode);
            break;
          
          case MiddleOperator.Add:
          case MiddleOperator.Subtract: {
              Symbol resultSymbol = (Symbol) middleCode[1];

              if (resultSymbol.Type.IsFloating()) {
                FloatingBinary(middleCode);
              }
              else {
                IntegralBinary(middleCode);
              }
            }
            break;

          case MiddleOperator.Multiply:
          case MiddleOperator.Divide:
          case MiddleOperator.Modulo: {
              Symbol resultSymbol = (Symbol) middleCode[0];

              if (resultSymbol.Type.IsFloating()) {
                FloatingBinary(middleCode);
              }
              else {
                IntegralMultiply(middleCode);
              }
            }
            break;

          case MiddleOperator.Carry:
          case MiddleOperator.NotCarry:
            CarryExpression(middleCode);
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.LessThan:
          case MiddleOperator.LessThanEqual:
          case MiddleOperator.GreaterThan:
          case MiddleOperator.GreaterThanEqual: {
              Symbol leftSymbol = (Symbol) middleCode[1];

              if (leftSymbol.Type.IsFloating()) {
                FloatingRelation(middleCode);
              }
              else {
                IntegralRelation(middleCode);
              }
            }
            break;
        
          case MiddleOperator.Plus:
          case MiddleOperator.Minus:
          case MiddleOperator.BitwiseNot: {
              Symbol resultSymbol = (Symbol) middleCode[0];

              if (resultSymbol.Type.IsFloating()) {
                FloatingUnary(middleCode);
              }
              else {
                IntegralUnary(middleCode);
              }
            }
            break;

          case MiddleOperator.Case:
            Case(middleCode);
            break;

          case MiddleOperator.CaseEnd:
            CaseEnd(middleCode);
            break;

          case MiddleOperator.Address:
            Address(middleCode);
            break;

          case MiddleOperator.Dereference:
            Dereference(middleCode);
            break;

          case MiddleOperator.DecreaseStack:
            --m_floatStackSize;
            Error.ErrorXXX(m_floatStackSize >= 0);
            break;

          /*case MiddleOperator.CheckTrackMapFloatStack:
            Error.ErrorXXX((m_trackMap.Count == 0) &&
                          (m_floatStackSize == 0));
            break;*/

          case MiddleOperator.PushZero:
            PushSymbol(new Symbol(Type.DoubleType, (decimal) 0));
            break;

          case MiddleOperator.PushOne:
            PushSymbol(new Symbol(Type.DoubleType, (decimal) 1));
            break;

          case MiddleOperator.PushFloat:
            PushSymbol((Symbol) middleCode[0]);
            break;

          case MiddleOperator.TopFloat:
            TopPopSymbol((Symbol) middleCode[0], TopOrPop.Top);
            break;
          
          case MiddleOperator.PopFloat:
            TopPopSymbol((Symbol) middleCode[0], TopOrPop.Pop);
            break;

          case MiddleOperator.PopEmpty:
            PopEmpty();
            break;

          case MiddleOperator.IntegralToIntegral:
            IntegralToIntegral(middleCode, middleIndex);
            break;

          case MiddleOperator.IntegralToFloating:
            IntegralToFloating(middleCode);
            break;

          case MiddleOperator.FloatingToIntegral:
            FloatingToIntegral(middleCode);
            break;

          case MiddleOperator.ParameterInitSize:
            StructUnionParameterInit(middleCode);
            break;

          case MiddleOperator.Parameter: {
              Symbol paramSymbol = (Symbol)middleCode[2];

              if (paramSymbol.Type.IsFloating()) {
                FloatingParameter(middleCode);
              }
              else if (paramSymbol.Type.IsStructOrUnion()) {
                StructUnionParameter(middleCode, middleIndex);
              }
              else {
                IntegralParameter(middleCode);
              }
            }
            break;
          
          case MiddleOperator.GetReturnValue: {
              Symbol returnSymbol = (Symbol) middleCode[0];

              if (returnSymbol.Type.IsStructOrUnion()) {
                StructUnionGetReturnValue(middleCode);
              }
              else if (returnSymbol.Type.IsFloating()) {
                Error.Check((++m_floatStackSize) <= FloatingStackMaxSize,
                             null, Message.Floating_stack_overflow);
              }
              else {
                IntegralGetReturnValue(middleCode);
              }
            }
            break;
          
          /*case MiddleOperator.SetReturnValue: {
              Symbol returnSymbol = (Symbol) middleCode[1];

              if (returnSymbol.Type.IsStructOrUnion()) {
                StructUnionSetReturnValue(middleCode);
              }
              else if (returnSymbol.Type.IsFloating()) {
                Error.ErrorXXX((--m_floatStackSize) == 0);
              }
              else {
                IntegralSetReturnValue(middleCode);
              }
            }
            break;*/

          case MiddleOperator.StackTop:
            StackTop(middleCode);
            break;

          case MiddleOperator.Dot:
          case MiddleOperator.Index:
          case MiddleOperator.SetReturnValue:
          case MiddleOperator.FunctionEnd:
          case MiddleOperator.Empty:
            break;

          default:
            Error.ErrorXXX(false);
            break;
        }
      }
    }

    // Track Set Generation

    private ISet<Track> TrackSet() {
      ISet<Track> trackSet = new HashSet<Track>();

      for (int index = 0; index < m_assemblyCodeList.Count; ++index) {
        AssemblyCode assemblyCode = m_assemblyCodeList[index];
     
        if (assemblyCode.Operator == AssemblyOperator.set_track_size) {
          Track track = (Track)assemblyCode[0];

          if (assemblyCode[1] is int) {
            track.MaxSize = (int) assemblyCode[1];
          }
          else {
            track.MaxSize = ((Track) assemblyCode[1]).MaxSize;
          }
        }
        else {
          CheckTrack(trackSet, assemblyCode, 0, index);
          CheckTrack(trackSet, assemblyCode, 1, index);
          CheckTrack(trackSet, assemblyCode, 2, index);
        }
      }

      return trackSet;
    }

    private void CheckTrack(ISet<Track> trackSet, AssemblyCode assemblyCode,
                            int position, int index) {
      if (assemblyCode[position] is Track) {
        Track track = (Track) assemblyCode[position];
        trackSet.Add(track);
        track.Index = index;
      }
    }

    // Function Calls -----------------------------------------------------------------------

    public Register BaseRegister(Symbol symbol) {
      Error.ErrorXXX((symbol == null) || symbol.IsAutoOrRegister());
    
      if (SymbolTable.CurrentFunction.Type.IsVariadic() &&
          (symbol != null) && !symbol.Parameter) {
        return AssemblyCode.VariadicFrameRegister;
      }
      else {
        return AssemblyCode.RegularFrameRegister;
      }
    }

    private int m_totalExtraSize = 0;
    private Stack<int> m_recordSizeStack = new Stack<int>();
    private Stack<IDictionary<Symbol, Track>> m_trackMapStack =
      new Stack<IDictionary<Symbol, Track>>();
    private Stack<IDictionary<Track,int>> m_registerMapStack =
      new Stack<IDictionary<Track,int>>();

    public void FunctionPreCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      int recordSize = (int) middleCode[0], extraSize = 0;

      int totalSize = 0;
      int doubleTypeSize = Type.DoubleType.Size();
      foreach (int size in m_topStack) {
        totalSize += size;
      }
      extraSize += totalSize * doubleTypeSize;

      IDictionary<Track,int> registerMap = new Dictionary<Track,int>();
      foreach (KeyValuePair<Symbol, Track> pair in m_trackMap) {
        Track track = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, baseRegister,
                        recordSize + extraSize, track);
        registerMap.Add(track, recordSize + extraSize);
        Symbol symbol = pair.Key;
        extraSize += symbol.Type.Size();
      }

      int topSize = m_floatStackSize - totalSize;
      m_topStack.Push(topSize);

      for (int count = 0; count < topSize; ++count) {
        AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister,
                        recordSize + extraSize);
        extraSize += doubleTypeSize;
      }

      m_recordSizeStack.Push(extraSize);
      m_totalExtraSize += extraSize;
      m_trackMapStack.Push(m_trackMap);
      m_registerMapStack.Push(registerMap);
      m_trackMap = new Dictionary<Symbol, Track>();
    }

    private Type m_returnType = null;
    private Track m_returnTrack = null;
    //private bool m_returnFloating = false;

    public void FunctionCall(MiddleCode middleCode, int index) {
      int recordSize = ((int)middleCode[0]) + m_totalExtraSize; 
      Symbol calleeSymbol = (Symbol)middleCode[1];
      int extraSize = (int) middleCode[2];

      Type calleeType = calleeSymbol.Type.IsFunction()
                      ? calleeSymbol.Type : calleeSymbol.Type.PointerType;

      bool callerVariadic = SymbolTable.CurrentFunction.Type.IsVariadic(),
           calleeVariadic = calleeType.IsVariadic();

      Register frameRegister = callerVariadic ? AssemblyCode.VariadicFrameRegister
                                              : AssemblyCode.RegularFrameRegister;               

      AddAssemblyCode(AssemblyOperator.return_address, frameRegister,
                      recordSize + SymbolTable.ReturnAddressOffset,
                      (BigInteger) (index + 1));

      AddAssemblyCode(AssemblyOperator.mov, frameRegister,
                      recordSize + SymbolTable.RegularFrameOffset,
                      AssemblyCode.RegularFrameRegister);

      if (callerVariadic) {
        AddAssemblyCode(AssemblyOperator.mov, frameRegister,
                        recordSize + SymbolTable.VariadicFrameOffset,
                        AssemblyCode.VariadicFrameRegister);
      }

      Track jumpTrack = null;
      if (!calleeSymbol.Type.IsFunction()) {
        jumpTrack = LoadValueToRegister(calleeSymbol);
      }            

      AddAssemblyCode(AssemblyOperator.add, frameRegister, // add di, 10
                      (BigInteger) recordSize);

      if (callerVariadic) { // mov bp, di
        AddAssemblyCode(AssemblyOperator.mov,
                        AssemblyCode.RegularFrameRegister,
                        AssemblyCode.VariadicFrameRegister);
      }
      else if (calleeVariadic) {
        AddAssemblyCode(AssemblyOperator.mov,
                        AssemblyCode.VariadicFrameRegister,
                        AssemblyCode.RegularFrameRegister);
      }

      if (calleeVariadic && (extraSize > 0)) {
        AddAssemblyCode(AssemblyOperator.add,
                        AssemblyCode.VariadicFrameRegister,
                        (BigInteger)extraSize);
      }

      if (calleeSymbol.Type.IsFunction()) {
        AddAssemblyCode(AssemblyOperator.call, calleeSymbol.UniqueName);
        m_returnType = calleeSymbol.Type.ReturnType;
        //m_returnFloating = calleeSymbol.Type.ReturnType.IsFloating();
      }
      else {
        AddAssemblyCode(AssemblyOperator.jmp, jumpTrack);
        m_returnType = calleeSymbol.Type.PointerType.ReturnType;
        //m_returnFloating =
          //calleeSymbol.Type.PointerType.ReturnType.IsFloating();
      }
    }
  
    public void FunctionPostCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      m_trackMap = m_trackMapStack.Pop();
      IDictionary<Track,int> postMap = m_registerMapStack.Pop();

      /*if (m_returnType.IsIntegralLogicalOrPointer() ||
          m_returnType.IsStructOrUnion()) {
        foreach (KeyValuePair<Track,int> pair in postMap) {
          Track track = pair.Key;

          if (AssemblyCode.RegisterOverlap(track.Register, AssemblyCode.ReturnValueRegister)) {
            m_returnTrack = new Track(m_returnType);
            Register returnRegister = AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, m_returnType.ReturnSize());
            AddAssemblyCode(AssemblyOperator.comment, "XXX");
            AddAssemblyCode(AssemblyOperator.mov, m_returnTrack, returnRegister);
            break;
          }
        }
      }*/

      if (m_returnType.IsIntegralOrPointer() || // m_returnType.IsLogical() ||
          m_returnType.IsStructOrUnion()) {
        m_returnTrack = null;

        foreach (KeyValuePair<Track,int> pair in postMap) {
          Track track = pair.Key;

          if (AssemblyCode.RegisterOverlap(track.Register, AssemblyCode.ReturnValueRegister)) {
            m_returnTrack = new Track(m_returnType);
            Register returnRegister = AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, m_returnType.ReturnSize());
            AddAssemblyCode(AssemblyOperator.mov, m_returnTrack, returnRegister);
          }

          int offset = pair.Value;
          AddAssemblyCode(AssemblyOperator.mov, track, baseRegister, offset);
        }

        if (m_returnTrack == null) {
          m_returnTrack = new Track(m_returnType);
          m_returnTrack.Register = AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, m_returnType.ReturnSize());
        }
      }
      else {
        foreach (KeyValuePair<Track,int> pair in postMap) {
          Track track = pair.Key;
          int offset = pair.Value;
          AddAssemblyCode(AssemblyOperator.mov, track, baseRegister, offset);
        }
      }

      Error.ErrorXXX(m_topStack.Count > 0);
      int topSize = m_topStack.Pop();

      if (topSize > 0) {
        int recordOffset = (int) middleCode[0]; 
        int doubleTypeSize = Type.DoubleType.Size();
        int recordSize = m_recordSizeStack.Pop();

        if (m_returnType.IsFloating()) {
          AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister,
                          recordOffset + recordSize);
        }

        int currentOffset = recordOffset + recordSize;
        for (int count = 0; count < topSize; ++count) {
          currentOffset -= doubleTypeSize;
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister,
                          currentOffset);
        }

        if (m_returnType.IsFloating()) {
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister,
                          recordOffset + recordSize);
        }

        m_totalExtraSize -= recordSize;
      }
      else {
        m_totalExtraSize -= m_recordSizeStack.Pop();
      }
    }
	
    // -----------------------------------------------------------------------

    public Track LoadValueToRegister(Symbol symbol,
                                     Register? register = null) {
      if (register != null) {
        CheckRegister(symbol, register.Value);
      }

      Track track;
      if (m_trackMap.TryGetValue(symbol, out track)) {
        m_trackMap.Remove(symbol);

        if ((register != null) && (track.Register != null) &&
            !AssemblyCode.RegisterOverlap(register, track.Register)) {
          Track newTrack = new Track(symbol, register.Value);
          AddAssemblyCode(AssemblyOperator.set_track_size,
                          newTrack, track);
          AddAssemblyCode(AssemblyOperator.mov, newTrack, track);
          return newTrack;
        }
        else {
          if (register != null) {
            track.Register = register;
          }

          return track;
        }
      }
      else {
        track = new Track(symbol, register);
        //Error.ErrorXXX(!(symbol.Type.IsFunction()));

        if (symbol.Value is BigInteger)  {
          AddAssemblyCode(AssemblyOperator.mov, track, symbol.Value);
        }
        else if (symbol.Type.IsArrayFunctionOrString() ||
                 (symbol.Value is StaticAddress)) {
          AddAssemblyCode(AssemblyOperator.mov, track,
                          Base(symbol));

          int offset = Offset(symbol);
          if (offset != 0) {
            AddAssemblyCode(AssemblyOperator.add, track,
                            (BigInteger) offset);
          }
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, track,
                          Base(symbol), Offset(symbol));
        }
        
        return track;
      }
    }

    public void CheckRegister(Symbol symbol, Register register) {
      foreach (KeyValuePair<Symbol,Track> entry in m_trackMap) {
        Symbol oldSymbol = entry.Key;
        Track oldTrack = entry.Value;

        if (!oldSymbol.Equals(symbol) &&
            AssemblyCode.RegisterOverlap(register, oldTrack.Register)) {
          Track newTrack = new Track(oldSymbol);
          m_trackMap[oldSymbol] = newTrack;

          int lastLine;
          for (lastLine = m_assemblyCodeList.Count - 1; lastLine >= 0;
               --lastLine) {
            AssemblyCode assemblyCode = m_assemblyCodeList[lastLine];
            if (oldTrack.Equals(assemblyCode[0])) {
              break;
            }    
          }
          Error.ErrorXXX(lastLine >= 0);

          AssemblyCode setCode =
            new AssemblyCode(AssemblyOperator.set_track_size,
                             newTrack, oldTrack);
          AssemblyCode movCode =
            new AssemblyCode(AssemblyOperator.mov, newTrack, oldTrack);
          m_assemblyCodeList.Insert(lastLine + 1, setCode);
          m_assemblyCodeList.Insert(lastLine + 2, movCode);
          break;
        }
      }
    }

    public Track LoadAddressToRegister(Symbol symbol,
                                       Register? register = null) {
      if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack.Pointer = true;
        return addressTrack;
      }
      else {
        Symbol addressSymbol = new Symbol(new Type(symbol.Type));
        Track addressTrack = new Track(addressSymbol, register);
        Error.ErrorXXX((addressTrack.Register == null) ||
                        RegisterAllocator.VariadicFunctionPointerRegisterSet.
                        Contains(addressTrack.Register.Value));
        addressTrack.Pointer = true;
        Error.ErrorXXX(!(symbol.Value is BigInteger));

        AddAssemblyCode(AssemblyOperator.mov, addressTrack, Base(symbol));

        int offset = Offset(symbol);
        if (offset != 0) {
          AddAssemblyCode(AssemblyOperator.add, addressTrack,
                          (BigInteger) offset);
        }

        return addressTrack;
      }
    }

    // Return, Exit, and Goto --------------------------------------------------------------------------

    public void Return(MiddleCode middleCode, int middleIndex) {
      if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
        Error.ErrorXXX(m_floatStackSize == 0);
        AddAssemblyCode(AssemblyOperator.cmp,
                        AssemblyCode.RegularFrameRegister,
                        SymbolTable.ReturnAddressOffset,
                        BigInteger.Zero, TypeSize.PointerSize);

        AssemblyCode jumpCode =
          AddAssemblyCode(AssemblyOperator.je, null, null, middleIndex + 1);
        Return();
      }
      else {
        SetReturnValue(middleCode);
        Error.ErrorXXX(m_floatStackSize == 0);
        Return();
      }
    }

    private void SetReturnValue(MiddleCode middleCode) {
      if (middleCode[1] != null) {
        Symbol returnSymbol = (Symbol) middleCode[1];

        if (returnSymbol.Type.IsStructOrUnion()) {
          StructUnionSetReturnValue(middleCode);
        }
        else if (returnSymbol.Type.IsFloating()) {
          --m_floatStackSize;
          Error.ErrorXXX(m_floatStackSize == 0);
        }
        else {
          IntegralSetReturnValue(middleCode);
        }
      }
    }

    private void Return() {
      Track track = new Track(Type.VoidPointerType);
      AddAssemblyCode(AssemblyOperator.mov, track,
                  AssemblyCode.RegularFrameRegister,
                  SymbolTable.ReturnAddressOffset);
      AddAssemblyCode(AssemblyOperator.mov,
                      AssemblyCode.VariadicFrameRegister,
                      AssemblyCode.RegularFrameRegister,
                      SymbolTable.VariadicFrameOffset);
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.RegularFrameRegister,
                      AssemblyCode.RegularFrameRegister,
                      SymbolTable.RegularFrameOffset);
      AddAssemblyCode(AssemblyOperator.jmp, track);
    }

    public void IntegralGetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode[0];
      Error.ErrorXXX(m_returnTrack != null);
      m_trackMap.Add(returnSymbol, m_returnTrack);
      AddAssemblyCode(AssemblyOperator.empty, m_returnTrack);

/*      if (m_returnTrack != null) {
        m_trackMap.Add(returnSymbol, m_returnTrack);
        AddAssemblyCode(AssemblyOperator.empty, m_returnTrack);
      }
      else {
        Register returnRegister =
          AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister,
                                      returnSymbol.Type.Size());
//        CheckRegister(returnSymbol, returnRegister);
        Track returnTrack = new Track(returnSymbol, returnRegister);
        m_trackMap.Add(returnSymbol, returnTrack);
        AddAssemblyCode(AssemblyOperator.empty, returnTrack);
      }*/
    }

    public void IntegralSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol)middleCode[1];
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister,
                                    returnSymbol.Type.SizeAddress()); 
      LoadValueToRegister(returnSymbol, returnRegister);
      m_trackMap.Remove(returnSymbol);
    }

    public void Exit(MiddleCode middleCode) {
      Symbol exitSymbol = (Symbol) middleCode[1];

      if (Start.Linux) {
        if (exitSymbol != null) {
          LoadValueToRegister(exitSymbol, Register.rdi);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, Register.rdi,
                          BigInteger.Zero);
        }

        AddAssemblyCode(AssemblyOperator.mov, Register.rax,
                        (BigInteger) 60); // 0x3C
        AddAssemblyCode(AssemblyOperator.syscall);
      }

      if (Start.Windows) {        
        if (exitSymbol != null) {
          LoadValueToRegister(exitSymbol, Register.al);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, Register.al,
                          BigInteger.Zero);
        }

        AddAssemblyCode(AssemblyOperator.mov, Register.ah,
                        (BigInteger) 76); // 0x4C
        AddAssemblyCode(AssemblyOperator.interrupt, (BigInteger) 33); // 0x21
      }
    }

    private void StackTop(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode[0];
      Track track = new Track(symbol);
      m_trackMap.Add(symbol, track);
      AddAssemblyCode(AssemblyOperator.mov, track, Linker.StackStart);
    }

    public void Goto(MiddleCode middleCode) {
      int jumpTarget = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.jmp, null, null, jumpTarget);
    }
    
    // Load and Inspect Registers --------------------------------------------------------------------------
    
    private ISet<Track> m_syscallSet = new HashSet<Track>();

    public void LoadToRegister(MiddleCode middleCode) {
      Register register = (Register) middleCode[0];
      Symbol symbol = (Symbol) middleCode[1];
      Track track = LoadValueToRegister(symbol, register);
      m_syscallSet.Add(track);
    }

    public void InspectRegister(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode[0];
      Register register = (Register) middleCode[1];
      Track track = new Track(symbol, register);
      m_trackMap.Add(symbol, track);
    }
  
    public void CarryExpression(MiddleCode middleCode) {
      AssemblyOperator objectOperator =
        m_middleToIntegralMap[middleCode.Operator];
      int jumpTarget = (int) middleCode[0];
      AddAssemblyCode(objectOperator, null, null, jumpTarget);
    }

    public void JumpToRegister(MiddleCode middleCode) {
      Register jumpRegister = (Register) middleCode[0];
      AddAssemblyCode(AssemblyOperator.jmp, jumpRegister);
    }

    public void Interrupt(MiddleCode middleCode) {
      foreach (Track track in m_syscallSet) {        
        AddAssemblyCode(AssemblyOperator.empty, track);
      }

      AddAssemblyCode(AssemblyOperator.interrupt,
                      (BigInteger) middleCode[0]);
      m_trackMap.Clear();
    }

    public void SystemCall(MiddleCode middleCode) {
      foreach (Track track in m_syscallSet) {        
        AddAssemblyCode(AssemblyOperator.empty, track);
      }

      AddAssemblyCode(AssemblyOperator.syscall);
      m_trackMap.Clear();
    }

    // Initialization

    private void Initializer(MiddleCode middleCode) {
      Sort sort = (Sort) middleCode[0];
      object value = middleCode[1];

      if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        string name = staticAddress.UniqueName;
        int offset = staticAddress.Offset;
        // dw name + offset
        AddAssemblyCode(AssemblyOperator.define_address, name, offset);
      }
      else {
        AddAssemblyCode(AssemblyOperator.define_value, sort, value);
      }
    }

    private void InitializerZero(MiddleCode middleCode) {
      int size = (int) middleCode[0];
      Error.ErrorXXX(size > 0);
      AddAssemblyCode(AssemblyOperator.define_zero_sequence, size);
    }

    // Base and Offset

    private object Base(Symbol symbol) {
      Error.ErrorXXX(!(symbol.Value is BigInteger));

      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        return staticAddress.UniqueName;
      }
      else if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        Error.ErrorXXX((addressTrack.Register == null) ||
                        RegisterAllocator.VariadicFunctionPointerRegisterSet.
                        Contains(addressTrack.Register.Value));
        addressTrack.Pointer = true;
        m_trackMap.Remove(symbol.AddressSymbol);
        return addressTrack;
      }
      else if (symbol.IsExternOrStatic()) {
        return symbol.UniqueName;
      }
      else { //resultSymbol.IsAutoOrRegister()
        return BaseRegister(symbol);
      }
    }

    private int Offset(Symbol symbol) {
      Error.ErrorXXX(!(symbol.Value is BigInteger));

      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        return staticAddress.Offset;
      }
      else if (symbol.AddressSymbol != null) {
        return symbol.AddressOffset;
      }
      else {
        return symbol.Offset;
      }
    }

    // Integral Assignment and Parameters -----------------------------------------------------------------------

    public void IntegralAssign(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             assignSymbol = (Symbol) middleCode[1];
      IntegralAssign(resultSymbol, assignSymbol);
    }

    public void IntegralParameter(MiddleCode middleCode) {
      Type toType = (Type) middleCode[1];
      Symbol toSymbol = new Symbol(null, false, Storage.Auto, toType);
      Symbol fromSymbol = (Symbol) middleCode[2];
      int parameterOffset = (int) middleCode[0];
      toSymbol.Offset = m_totalExtraSize + parameterOffset;
      IntegralAssign(toSymbol, fromSymbol);
    }

    public void IntegralAssign(Symbol resultSymbol, Symbol assignSymbol) {
      Track resultTrack = null, assignTrack = null;
      m_trackMap.TryGetValue(resultSymbol, out resultTrack);
      m_trackMap.TryGetValue(assignSymbol, out assignTrack);
      int typeSize = assignSymbol.Type.SizeAddress();

      if (resultSymbol.IsTemporary()) {
        Error.ErrorXXX(assignTrack == null);

        /*if (assignTrack != null) {
          m_trackMap.Add(resultSymbol, assignTrack);
          m_trackMap.Remove(assignSymbol);
        }*/

        if (resultTrack == null) {
          resultTrack = new Track(resultSymbol);
          m_trackMap.Add(resultSymbol, resultTrack);
        }

        if (assignSymbol.Value is BigInteger) {          
          AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                          assignSymbol.Value);
        }
        else if (assignSymbol.Type.IsArrayFunctionOrString() ||
                 (assignSymbol.Value is StaticAddress)) {
          AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                          Base(assignSymbol));

          int offset = Offset(assignSymbol);
          if (offset != 0) {
            AddAssemblyCode(AssemblyOperator.add, resultTrack,
                            (BigInteger) offset);
          }
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                          Base(assignSymbol), Offset(assignSymbol));
        }
      }
/*      if (resultSymbol.IsTemporary()) {        
        Error.ErrorXXX(resultSymbol.AddressSymbol == null);

        if (assignTrack != null) {
          if (resultTrack != null) {
            m_moveMap.Add(resultTrack, assignTrack);
          }

          m_trackMap[resultSymbol] = assignTrack;
          m_trackMap.Remove(assignSymbol);
        }
        else {
          if (resultTrack == null) {
            resultTrack = new Track(resultSymbol);
            m_trackMap.Add(resultSymbol, resultTrack);
          }
          
          if (assignSymbol.Value is BigInteger) {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            assignSymbol.Value);
          }
          else if (assignSymbol.Type.IsArrayFunctionOrString() ||
                   (assignSymbol.Value is StaticAddress)) {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            Base(assignSymbol));

            int offset = Offset(assignSymbol);
            if (offset != 0) {
              AddAssemblyCode(AssemblyOperator.add, resultTrack,
                              (BigInteger) offset);
            }
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, resultTrack,
                            Base(assignSymbol), Offset(assignSymbol));
          }
        }
      }
      else {
        IntegralBinary(MiddleOperator.Assign, resultSymbol,
                       resultSymbol, assignSymbol);
      }*/
      else {
        if (assignTrack != null) {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), assignTrack);
          m_trackMap.Remove(assignSymbol);
        }
        else if (assignSymbol.Value is BigInteger) {
          BigInteger bigValue = (BigInteger) assignSymbol.Value;
          if (Start.Linux &&
              !((-2147483648 <= bigValue) && (bigValue <= 2147483647))) {
            assignTrack = new Track(assignSymbol);
            AddAssemblyCode(AssemblyOperator.mov, assignTrack, assignSymbol.Value);
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), assignTrack);
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), assignSymbol.Value, typeSize);
          }
        }
        else if (assignSymbol.Type.IsArrayFunctionOrString() ||
                 (assignSymbol.Value is StaticAddress)) {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), Base(assignSymbol),
                          TypeSize.PointerSize);

          int assignOffset = Offset(assignSymbol);
          if (assignOffset != 0) {
            AddAssemblyCode(AssemblyOperator.add, Base(resultSymbol),
                            Offset(resultSymbol), (BigInteger) assignOffset,
                            typeSize);
          }
          
          /*if (assignSymbol.AddressSymbol != null) {
            Track addressTrack = LoadValueToRegister(assignSymbol.AddressSymbol);
            addressTrack.Pointer = true;
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), addressTrack);

            if (assignSymbol.AddressOffset != 0) {
              AddAssemblyCode(AssemblyOperator.add, Base(resultSymbol),
                              Offset(resultSymbol), (BigInteger) assignSymbol.AddressOffset,
                              typeSize);
            }
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), Base(assignSymbol),
                            TypeSize.PointerSize);

            int assignOffset = Offset(assignSymbol);
            if (assignOffset != 0) {
              AddAssemblyCode(AssemblyOperator.add, Base(resultSymbol),
                              Offset(resultSymbol), (BigInteger) assignOffset,
                              typeSize);
            }
          }*/
        }
        /*else if (assignSymbol.AddressSymbol != null) {
          Track addressTrack = LoadValueToRegister(assignSymbol.AddressSymbol);
          addressTrack.Pointer = true;
          assignTrack = new Track(assignSymbol);
          AddAssemblyCode(AssemblyOperator.mov, assignTrack,
                          addressTrack, assignSymbol.AddressOffset);
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), assignTrack);
        }*/
        else {
          assignTrack = LoadValueToRegister(assignSymbol);
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), assignTrack);
        }
      }
    }

    // Integral Unary

    public static IDictionary<MiddleOperator,AssemblyOperator>
      m_middleToIntegralMap =
      new Dictionary<MiddleOperator, AssemblyOperator>() {
        {MiddleOperator.BitwiseNot, AssemblyOperator.not},
        {MiddleOperator.Minus, AssemblyOperator.neg},
        {MiddleOperator.Multiply, AssemblyOperator.imul},
        {MiddleOperator.Divide, AssemblyOperator.idiv},
        {MiddleOperator.Modulo, AssemblyOperator.idiv},
        {MiddleOperator.Assign, AssemblyOperator.mov},
        {MiddleOperator.Add, AssemblyOperator.add},
        {MiddleOperator.Subtract, AssemblyOperator.sub},
        {MiddleOperator.BitwiseAnd, AssemblyOperator.and},
        {MiddleOperator.BitwiseOr, AssemblyOperator.or},
        {MiddleOperator.BitwiseXOr, AssemblyOperator.xor},
        {MiddleOperator.ShiftLeft, AssemblyOperator.shl},
        {MiddleOperator.ShiftRight, AssemblyOperator.shr},
        {MiddleOperator.Equal, AssemblyOperator.je},
        {MiddleOperator.NotEqual, AssemblyOperator.jne},
        {MiddleOperator.Carry, AssemblyOperator.jc},
        {MiddleOperator.NotCarry, AssemblyOperator.jnc},
        {MiddleOperator.Compare, AssemblyOperator.cmp},
        {MiddleOperator.LessThan, AssemblyOperator.jl},
        {MiddleOperator.LessThanEqual,AssemblyOperator.jle},
        {MiddleOperator.GreaterThan, AssemblyOperator.jg},
        {MiddleOperator.GreaterThanEqual, AssemblyOperator.jge}},

      m_unsignedToIntegralMap =
      new Dictionary<MiddleOperator, AssemblyOperator>() {
        {MiddleOperator.Multiply, AssemblyOperator.mul},
        {MiddleOperator.Divide, AssemblyOperator.div},
        {MiddleOperator.Modulo, AssemblyOperator.div},
        {MiddleOperator.Equal, AssemblyOperator.je},
        {MiddleOperator.NotEqual, AssemblyOperator.jne},
        {MiddleOperator.LessThan, AssemblyOperator.jb},
        {MiddleOperator.LessThanEqual,AssemblyOperator.jbe},
        {MiddleOperator.GreaterThan, AssemblyOperator.ja},
        {MiddleOperator.GreaterThanEqual, AssemblyOperator.jae}};

    public void IntegralUnary(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol)middleCode[0],
             unarySymbol = (Symbol)middleCode[1];
      IntegralUnary(middleCode.Operator, resultSymbol, unarySymbol);
    }

    public void IntegralUnary(MiddleOperator middleOperator,
                              Symbol resultSymbol, Symbol unarySymbol) {
      AssemblyOperator objectOperator;
      if (MiddleCode.IsMultiply(middleOperator) &&
          unarySymbol.Type.IsUnsigned()) {
        objectOperator = m_unsignedToIntegralMap[middleOperator];
      }
      else {
        objectOperator = m_middleToIntegralMap[middleOperator];
      }

      int typeSize = unarySymbol.Type.SizeAddress();
      Track unaryTrack = null;
      if (unarySymbol.Value is BigInteger) {
        SymbolTable.StaticSet.Add(ConstantExpression.Value(unarySymbol));
        AddAssemblyCode(objectOperator, unarySymbol.UniqueName,
                        0, null, typeSize);
      }
      else if (m_trackMap.TryGetValue(unarySymbol, out unaryTrack)) {
        if (middleOperator != MiddleOperator.Plus) {
          AddAssemblyCode(objectOperator, unaryTrack);
        }
        m_trackMap.Remove(unarySymbol);
      }
      else if (resultSymbol == unarySymbol) {
        Error.ErrorXXX(unaryTrack == null);
        if (middleOperator != MiddleOperator.Plus) {
          AddAssemblyCode(objectOperator, Base(unarySymbol),
                          Offset(unarySymbol), null, typeSize);
        }
      }
      else {
        unaryTrack = LoadValueToRegister(unarySymbol);

        if (middleOperator != MiddleOperator.Plus) {
          AddAssemblyCode(objectOperator, unaryTrack);
        }

        if (resultSymbol.IsTemporary()) {          
          Error.ErrorXXX(resultSymbol.AddressSymbol == null);
          m_trackMap.Add(resultSymbol, unaryTrack);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                          Offset(resultSymbol), unaryTrack);
        }

        m_trackMap.Remove(unarySymbol);
      }
    }

    // Integral Multiplication, Division, and Modulo

    public static IDictionary<int,Register> m_leftRegisterMap =
      new Dictionary<int,Register>() {{1, Register.al}, {2, Register.ax},
                                      {4, Register.eax}, {8, Register.rax}};

    public static IDictionary<int,Register> m_zeroRegisterMap =
      new Dictionary<int,Register>() {{1, Register.ah}, {2, Register.dx},
                                      {4, Register.edx}, {8, Register.rdx}};

    public static IDictionary<int,Register> m_productQuintentRegisterMap =
      new Dictionary<int,Register>() {{1, Register.al}, {2, Register.ax},
                                      {4, Register.eax}, {8, Register.rax}};

    public static IDictionary<int,Register> m_remainderRegisterMap =
      new Dictionary<int,Register>() {{1, Register.ah}, {2, Register.dx},
                                      {4, Register.edx}, {8, Register.rdx}};

    public void IntegralMultiply(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode[1];
      int typeSize = leftSymbol.Type.SizeAddress();
      Register leftRegister = m_leftRegisterMap[typeSize];
      Track leftTrack = LoadValueToRegister(leftSymbol, leftRegister);

      Register zeroRegister = m_zeroRegisterMap[typeSize];
      Track zeroTrack = new Track(leftSymbol, zeroRegister);
      AddAssemblyCode(AssemblyOperator.xor, zeroTrack, zeroTrack);

      Symbol rightSymbol = (Symbol) middleCode[2];
      IntegralUnary(middleCode.Operator, rightSymbol, rightSymbol);
      Register resultRegister, discardRegister;

      if (middleCode.Operator == MiddleOperator.Modulo) {
        resultRegister = m_remainderRegisterMap[typeSize];
        discardRegister = m_productQuintentRegisterMap[typeSize];
      }
      else {
        resultRegister = m_productQuintentRegisterMap[typeSize];
        discardRegister = m_remainderRegisterMap[typeSize];
      }

      Symbol resultSymbol = (Symbol) middleCode[0];
      Track resultTrack = new Track(resultSymbol, resultRegister);

      if (resultSymbol.IsTemporary()) {        
        Error.ErrorXXX(resultSymbol.AddressSymbol == null);
        m_trackMap.Add(resultSymbol, resultTrack);
        AddAssemblyCode(AssemblyOperator.empty, resultTrack);
      }
      else {
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                        Offset(resultSymbol), resultTrack);
      }

      Track discaredTrack = new Track(resultSymbol, discardRegister);
      AddAssemblyCode(AssemblyOperator.empty, discaredTrack);
    }

    // Case

    public void Case(MiddleCode middleCode) {
      Symbol switchSymbol = (Symbol) middleCode[1];
      Track switchTrack = LoadValueToRegister(switchSymbol);
      Symbol caseSymbol = (Symbol) middleCode[2];
      BigInteger caseValue = (BigInteger) caseSymbol.Value; // cmp ax, 123
      AddAssemblyCode(AssemblyOperator.cmp, switchTrack, caseValue);
      int target = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.je, null, null, target);
      m_trackMap.Add(switchSymbol, switchTrack);
    }
    
    public void CaseEnd(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode[0];
      m_trackMap.Remove(symbol);
    }

    // Integral Relation

    public void IntegralBinary(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol)middleCode[0],
             leftSymbol = (Symbol)middleCode[1],
             rightSymbol = (Symbol)middleCode[2];
      IntegralBinary(middleCode.Operator, resultSymbol,
                     leftSymbol, rightSymbol);
    }

    public void IntegralRelation(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode[1],
             rightSymbol = (Symbol) middleCode[2];
      IntegralBinary(MiddleOperator.Compare, null, leftSymbol, rightSymbol);

      AssemblyOperator objectOperator;
      if (leftSymbol.Type.IsUnsigned()) {
        objectOperator = m_unsignedToIntegralMap[middleCode.Operator];
      }
      else {
        objectOperator = m_middleToIntegralMap[middleCode.Operator];
      }

      int target = (int) middleCode[0];
      AddAssemblyCode(objectOperator, null, null, target);
    }

    public void IntegralBinary(MiddleOperator middleOperator,
                               Symbol resultSymbol,  Symbol leftSymbol,
                               Symbol rightSymbol) {
      Error.ErrorXXX((resultSymbol != null) || (middleOperator == MiddleOperator.Compare));

      Track leftTrack = null, rightTrack = null;
      m_trackMap.TryGetValue(leftSymbol, out leftTrack);
      m_trackMap.TryGetValue(rightSymbol, out rightTrack);

      if ((leftTrack == null) &&
          (resultSymbol != null) && (resultSymbol != leftSymbol)) {
        leftTrack = LoadValueToRegister(leftSymbol);
      }

      if ((leftTrack == null) &&
          ((leftSymbol.Type.IsArrayFunctionOrString() &&
           (leftSymbol.Offset != 0)) ||
           ((leftSymbol.Value is StaticAddress) &&
           (((StaticAddress) leftSymbol.Value).Offset != 0)))) {
        leftTrack = LoadValueToRegister(leftSymbol);
      }

      if ((rightTrack == null) &&
          (middleOperator != MiddleOperator.Add) &&
          (middleOperator != MiddleOperator.Subtract) &&
          ((rightSymbol.Type.IsArrayFunctionOrString() &&
           (rightSymbol.Offset != 0)) ||
           ((rightSymbol.Value is StaticAddress) &&
            (((StaticAddress) rightSymbol.Value).Offset != 0)))) {
        rightTrack = LoadValueToRegister(rightSymbol);
      }

      if ((rightTrack == null) && (rightSymbol.Value is BigInteger) &&
          ((middleOperator != MiddleOperator.Assign) ||
           (leftTrack == null))) {
        BigInteger bigValue = (BigInteger) rightSymbol.Value;
        if (Start.Linux &&
            !((-2147483648 <= bigValue) && (bigValue <= 2147483647))) {
          rightTrack = LoadValueToRegister(rightSymbol);
        }
      }

      if (MiddleCode.IsShift(middleOperator)) {
        rightTrack =
          LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
      }

      if ((leftTrack == null) && (leftSymbol.Value is BigInteger)) {
        leftTrack = LoadValueToRegister(leftSymbol);
      }

      int typeSize = leftSymbol.Type.Size();
      AssemblyOperator objectOperator = m_middleToIntegralMap[middleOperator];
      
      //Error.ErrorXXX(!(leftSymbol.Value is BigInteger));

      if (leftTrack != null) {
        if (rightTrack != null) {
          AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        }
        else if (rightSymbol.Value is BigInteger) {
          AddAssemblyCode(objectOperator, leftTrack, rightSymbol.Value);
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(objectOperator, leftTrack, Base(rightSymbol));

          int rightOffset = Offset(rightSymbol);
          if (rightOffset != 0) {
            AddAssemblyCode(objectOperator, leftTrack,
                            (BigInteger) rightOffset);
          }

          /*if (rightOffset != 0) {
            if (middleOperator == MiddleOperator.Assign) {
              AddAssemblyCode(AssemblyOperator.add, leftTrack,
                              (BigInteger) rightOffset);
            }
            else {
              AddAssemblyCode(objectOperator, leftTrack,
                              (BigInteger) rightOffset);
            }
          }*/
        }
        else {
          if (middleOperator == MiddleOperator.Assign) {
            leftTrack = new Track(resultSymbol);
          }

          AddAssemblyCode(objectOperator, leftTrack,
                          Base(rightSymbol), Offset(rightSymbol));
        }

        if (resultSymbol != null) {
          if (resultSymbol.IsTemporary()) {            
            Error.ErrorXXX(resultSymbol.AddressSymbol == null);
            m_trackMap.Add(resultSymbol, leftTrack);
          }
          else {
            AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol),
                            Offset(resultSymbol), leftTrack);
          }
        }
      }
      /*else if ((leftSymbol.Type.IsArrayFunctionOrString() ||
               (leftSymbol.Value is StaticAddress))) {
        Error.ErrorXXX(objectOperator == AssemblyOperator.cmp);

        if (rightTrack != null) {
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName, rightTrack);
        }
        else if (rightSymbol.Value is BigInteger) {
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName,
                          rightSymbol.Value, null, typeSize);
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName,
                          rightSymbol.UniqueName, null, typeSize);
        }
        else {
          rightTrack = LoadValueToRegister(rightSymbol);
          AddAssemblyCode(objectOperator, leftSymbol.UniqueName,
                          Offset(leftSymbol), rightTrack);
        }
      }*/
      else {
        if (rightTrack != null) {
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), rightTrack);
        }
        else if (rightSymbol.Value is BigInteger) {
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), rightSymbol.Value, typeSize);
        }
        else if (rightSymbol.Type.IsArrayFunctionOrString() ||
                 (rightSymbol.Value is StaticAddress)) {
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), Base(rightSymbol),
                          TypeSize.PointerSize);

          int rightOffset = Offset(rightSymbol);
          if (rightOffset != 0) {
            AddAssemblyCode(objectOperator, Base(leftSymbol),
                            Offset(leftSymbol), (BigInteger) rightOffset,
                            typeSize);
          }

          /*if (rightOffset != 0) {
            if (middleOperator == MiddleOperator.Assign) {
              AddAssemblyCode(AssemblyOperator.add, Base(leftSymbol),
                              Offset(leftSymbol), (BigInteger) rightOffset,
                              typeSize);
            }
            else {
              AddAssemblyCode(objectOperator, Base(leftSymbol),
                              Offset(leftSymbol), (BigInteger) rightOffset,
                              typeSize);
            }
          }*/
        }
        else {
          rightTrack = LoadValueToRegister(rightSymbol);
          AddAssemblyCode(objectOperator, Base(leftSymbol),
                          Offset(leftSymbol), rightTrack);
        }
      }

      m_trackMap.Remove(leftSymbol);
      m_trackMap.Remove(rightSymbol);
    }

    // Address

    public void Address(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0],
             addressSymbol = (Symbol) middleCode[1];
      Track track = LoadAddressToRegister(addressSymbol);
      m_trackMap.Add(resultSymbol, track);
      m_trackMap.Remove(addressSymbol);
    }

    public void Dereference(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode[0];
      Error.ErrorXXX(resultSymbol.AddressSymbol != null);
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);
    }

    // Floating Binary

    public static IDictionary<MiddleOperator, AssemblyOperator>
      m_middleToFloatingMap =
        new Dictionary<MiddleOperator, AssemblyOperator>() {
          {MiddleOperator.Minus, AssemblyOperator.fchs},
          {MiddleOperator.Add, AssemblyOperator.fadd},
          {MiddleOperator.Subtract, AssemblyOperator.fsub},
          {MiddleOperator.Multiply, AssemblyOperator.fmul},
          {MiddleOperator.Divide, AssemblyOperator.fdiv},
          {MiddleOperator.Equal, AssemblyOperator.je},
          {MiddleOperator.NotEqual, AssemblyOperator.jne},
          {MiddleOperator.LessThan, AssemblyOperator.ja},
          {MiddleOperator.LessThanEqual, AssemblyOperator.jae},
          {MiddleOperator.GreaterThan, AssemblyOperator.jb},
          {MiddleOperator.GreaterThanEqual, AssemblyOperator.jbe}
        };

    public void FloatingBinary(MiddleCode middleCode) {
      --m_floatStackSize;
      Error.ErrorXXX(m_floatStackSize >= 0);
      AddAssemblyCode(m_middleToFloatingMap[middleCode.Operator]);
    }

    public void FloatingUnary(MiddleCode middleCode) {
      AddAssemblyCode(m_middleToFloatingMap[middleCode.Operator]);
    }

    public void FloatingParameter(MiddleCode middleCode) {
      Type paramType = (Type) middleCode[1];
      Symbol paramSymbol = new Symbol(paramType);
      int paramOffset = (int) middleCode[0];
      paramSymbol.Offset = m_totalExtraSize + paramOffset;
      TopPopSymbol(paramSymbol, TopOrPop.Pop);
    }

    // Floating Relation

    public void FloatingRelation(MiddleCode middleCode) {
      m_floatStackSize -= 2;
      Error.ErrorXXX(m_floatStackSize >= 0);
      int target = (int) middleCode[0];
      AddAssemblyCode(AssemblyOperator.fcompp);
      AddAssemblyCode(AssemblyOperator.fstsw, Register.ax);
      AddAssemblyCode(AssemblyOperator.sahf);
      AssemblyOperator objectOperator =
        m_middleToFloatingMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, target);
    }

    // Floating Push and Pop

    public static IDictionary<(bool, int), AssemblyOperator>
      m_floatPushMapX = new Dictionary<(bool, int), AssemblyOperator>() {
        {(false, 2), AssemblyOperator.fild_word},
        {(false, 4), AssemblyOperator.fild_dword},
        {(false, 8), AssemblyOperator.fild_qword},
        {(true, 4), AssemblyOperator.fld_dword},
        {(true, 8), AssemblyOperator.fld_qword}
      };

    public static IDictionary<Pair<bool,int>, AssemblyOperator>
      m_floatPushMap = new Dictionary<Pair<bool,int>, AssemblyOperator>() {
        {new Pair<bool,int>(false, 2), AssemblyOperator.fild_word},
        {new Pair<bool,int>(false, 4), AssemblyOperator.fild_dword},
        {new Pair<bool,int>(false, 8), AssemblyOperator.fild_qword},
        {new Pair<bool,int>(true, 4), AssemblyOperator.fld_dword},
        {new Pair<bool,int>(true, 8), AssemblyOperator.fld_qword}
      };

    public void PushSymbol(Symbol symbol) {
      ++m_floatStackSize;
      Error.Check(m_floatStackSize <= FloatingStackMaxSize,
                  Message.Floating_stack_overflow);
      Track track;

      if ((symbol.Value != null) && 
          (symbol.Value.Equals(BigInteger.Zero) ||
           symbol.Value.Equals((decimal) 0))) {
        AddAssemblyCode(AssemblyOperator.fldz);
      }
      else if ((symbol.Value != null) && 
               (symbol.Value.Equals(BigInteger.One) ||
                symbol.Value.Equals((decimal) 1))) {
        AddAssemblyCode(AssemblyOperator.fld1);
      }
      else {
        Pair<bool,int> pair =
          new Pair<bool, int>(symbol.Type.IsFloating(), symbol.Type.Size()); 
        AssemblyOperator objectOperator = m_floatPushMap[pair];

        if ((symbol.Value is BigInteger) || (symbol.Value is decimal)) {
          SymbolTable.StaticSet.Add(ConstantExpression.Value(symbol));
          AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
        }
        else if (m_trackMap.TryGetValue(symbol, out track)) {
          m_trackMap.Remove(symbol);
          string containerName = AddStaticContainer(symbol.Type);
          AddAssemblyCode(AssemblyOperator.mov, containerName, 0, track);
          AddAssemblyCode(objectOperator, containerName, 0);
        }
        else if (symbol.Type.IsArrayFunctionOrString()) {
          string containerName = AddStaticContainer(symbol.Type);
          AddAssemblyCode(AssemblyOperator.mov, containerName, 0,
                          Base(symbol), TypeSize.PointerSize);

          int offset = Offset(symbol);
          if (offset != 0) {
            AddAssemblyCode(AssemblyOperator.add, containerName, 0,
                            (BigInteger) offset, TypeSize.PointerSize);
          }

          AddAssemblyCode(objectOperator, containerName, 0);
        }
        else {
          AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
        }
      }
    }

    private static string AddStaticContainer(Type type) {
      string containerName = "container" + type.Size() +
                             "bytes" + Symbol.NumberId;
      SymbolTable.StaticSet.Add(ConstantExpression.Value(containerName,
                                                         type, null));
      return containerName;
    }

    public enum TopOrPop {Top, Pop};

    public void PopEmpty() {
      string containerName = AddStaticContainer(Type.LongDoubleType);
      AddAssemblyCode(AssemblyOperator.fistp_word, containerName, 0);
    }

    public static IDictionary<Pair<bool,int>,AssemblyOperator>
      m_floatPopMap = new Dictionary<Pair<bool,int>, AssemblyOperator>() {
        {new Pair<bool,int>(false, 2), AssemblyOperator.fistp_word},
        {new Pair<bool,int>(false, 4), AssemblyOperator.fistp_dword},
        {new Pair<bool,int>(false, 8), AssemblyOperator.fistp_qword},
        {new Pair<bool,int>(true, 4), AssemblyOperator.fstp_dword},
        {new Pair<bool,int>(true, 8), AssemblyOperator.fstp_qword}
      };

    public static IDictionary<Pair<bool,int>,AssemblyOperator>
      m_floatTopMap = new Dictionary<Pair<bool,int>, AssemblyOperator>() {
        {new Pair<bool,int>(false, 2), AssemblyOperator.fist_word},
        {new Pair<bool,int>(false, 4), AssemblyOperator.fist_dword},
        {new Pair<bool,int>(false, 8), AssemblyOperator.fist_qword},
        {new Pair<bool,int>(true, 4), AssemblyOperator.fst_dword},
        {new Pair<bool,int>(true, 8), AssemblyOperator.fst_qword}
      };

    public void TopPopSymbol(Symbol symbol, TopOrPop topOrPop) {
      Error.ErrorXXX(symbol != null);
      Pair<bool,int> pair =
        new Pair<bool,int>(symbol.Type.IsFloating(), symbol.Type.Size());
      AssemblyOperator objectOperator;

      if (topOrPop == TopOrPop.Pop) {
        objectOperator = m_floatPopMap[pair];
        --m_floatStackSize;
        Error.ErrorXXX(m_floatStackSize >= 0);
      }
      else {
        objectOperator = m_floatTopMap[pair];
      }

      if (symbol.Type.IsIntegralPointerOrArray() && symbol.IsTemporary()) {        
        string containerName = AddStaticContainer(symbol.Type);
        AddAssemblyCode(objectOperator, containerName, 0);
        Track track = new Track(symbol);
        AddAssemblyCode(AssemblyOperator.mov, track, containerName, 0);
        m_trackMap.Add(symbol, track);
      }
      else {
        AddAssemblyCode(objectOperator, Base(symbol), Offset(symbol));
      }
    }

    // Type Conversion

    public void IntegralToIntegral(MiddleCode middleCode, int index) {
      Symbol toSymbol = (Symbol) middleCode[0],
             fromSymbol = (Symbol) middleCode[1];

      Type toType = toSymbol.Type, fromType = fromSymbol.Type;
      int toSize = toType.SizeAddress(), fromSize = fromType.SizeAddress();

      //Error.ErrorXXX(fromSize != toSize);
      Track fromTrack = LoadValueToRegister(fromSymbol);
      AddAssemblyCode(AssemblyOperator.set_track_size, fromTrack, toSize);

      if (fromSize != toSize) {
        if (fromSize < toSize) {
          if (toSize == 8) {
            Track toTrack = new Track(toSymbol);
            AddAssemblyCode(AssemblyOperator.mov, toTrack,
                            TypeSize.GetMask(fromType.Sort));
            AddAssemblyCode(AssemblyOperator.and, fromTrack, toTrack);
          }
          else {
            AddAssemblyCode(AssemblyOperator.and, fromTrack,
                            TypeSize.GetMask(fromType.Sort));
          }
        }

        if (fromType.IsSigned() && toType.IsSigned()) {
          AddAssemblyCode(AssemblyOperator.set_track_size,
                          fromTrack, fromSize);
          AddAssemblyCode(AssemblyOperator.cmp, fromTrack, BigInteger.Zero);
          AddAssemblyCode(AssemblyOperator.jge, null, null, index + 1);
          AddAssemblyCode(AssemblyOperator.neg, fromTrack);
          AddAssemblyCode(AssemblyOperator.set_track_size, fromTrack, toSize);
          AddAssemblyCode(AssemblyOperator.neg, fromTrack);
        }
      }

      m_trackMap.Add(toSymbol, fromTrack);
    }

    public void IntegralToFloating(MiddleCode middleCode) {
      Symbol fromSymbol = (Symbol) middleCode[1];
      PushSymbol(fromSymbol);
    }
  
    public void FloatingToIntegral(MiddleCode middleCode) {
      Symbol toSymbol = (Symbol) middleCode[0];
      TopPopSymbol(toSymbol, TopOrPop.Pop);
    }

    // Struct and Union

    public void StructUnionAssignInit(MiddleCode middleCode) {
      Symbol targetSymbol = (Symbol)middleCode[0],
             sourceSymbol = (Symbol)middleCode[1];
      MemoryCopyInit(targetSymbol, sourceSymbol);
    }

    public void StructUnionAssign(MiddleCode middleCode, int middleIndex) {
      MemoryCopyLoop(middleIndex);
    }

    public void StructUnionParameterInit(MiddleCode middleCode) {
      int paramOffset = (int) middleCode[0];
      Symbol sourceSymbol = (Symbol) middleCode[2];
      Symbol targetSymbol = new Symbol(Type.IntegerPointerType);
      targetSymbol.Offset = m_totalExtraSize + paramOffset;
      MemoryCopyInit(targetSymbol, sourceSymbol);
    }

    public void StructUnionParameter(MiddleCode middleCode, int middleIndex) {
      MemoryCopyLoop(middleIndex);
    }

    public void StructUnionGetReturnValue(MiddleCode middleCode) {
      Symbol targetSymbol = (Symbol) middleCode[0];
      CheckRegister(targetSymbol, AssemblyCode.ReturnAddressRegister);
      Track targetAddressTrack =
        new Track(targetSymbol.AddressSymbol, 
                  AssemblyCode.ReturnAddressRegister);
      m_trackMap.Add(targetSymbol.AddressSymbol, targetAddressTrack);
    }

    public void StructUnionSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode[1];
      LoadAddressToRegister(returnSymbol, AssemblyCode.ReturnAddressRegister);
    }

    private Track m_targetAddressTrack = null, m_sourceAddressTrack = null;
    private static Track m_countTrack = null;

    private void MemoryCopyInit(Symbol targetSymbol, Symbol sourceSymbol) {
      m_sourceAddressTrack = LoadAddressToRegister(sourceSymbol);
      m_targetAddressTrack = LoadAddressToRegister(targetSymbol);
      int size = sourceSymbol.Type.Size();
      Type countType = (size < 256) ? Type.UnsignedCharType
                                    : Type.UnsignedIntegerType;
      m_countTrack = new Track(countType);
      AddAssemblyCode(AssemblyOperator.mov, m_countTrack, (BigInteger) size);
    }

    private void MemoryCopyLoop(int middleIndex) {
      Track valueTrack = new Track(Type.UnsignedCharType);
      AddAssemblyCode(AssemblyOperator.mov, valueTrack,
                      m_sourceAddressTrack, 0);
      AddAssemblyCode(AssemblyOperator.mov, m_targetAddressTrack,
                      0, valueTrack);
      AddAssemblyCode(AssemblyOperator.inc, m_sourceAddressTrack);
      AddAssemblyCode(AssemblyOperator.inc, m_targetAddressTrack);
      AddAssemblyCode(AssemblyOperator.dec, m_countTrack);
      AddAssemblyCode(AssemblyOperator.cmp, m_countTrack, BigInteger.Zero);
      AddAssemblyCode(AssemblyOperator.jne, null, null, middleIndex);
    }

    // Initialization Code

    public static void InitializationCodeList() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.label, "_start");
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                      "Initializerialize Stack Pointer");
      AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                      AssemblyCode.RegularFrameRegister,
                      Linker.StackStart);

      if (Start.Linux) {
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                        "Initializerialize Heap Pointer");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_dword,
                        Linker.StackStart, 65534,
                        Linker.StackStart);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add_dword,
                        Linker.StackStart, 65534,
                        (BigInteger) 65534);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                        "Initializerialize FPU Control Word, truncate mode " +
                        "=> set bit 10 and 11.");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.fstcw,
                        AssemblyCode.RegularFrameRegister, 0);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.or_word,
                        AssemblyCode.RegularFrameRegister, 0,
                        (BigInteger)0x0C00);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.fldcw,
                        AssemblyCode.RegularFrameRegister, 0);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Linker.StackStart, 0, BigInteger.Zero,
                        TypeSize.PointerSize);

        List<string> textList = new List<string>();
        textList.Add("section .text");
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                                            externSet);
        SymbolTable.InitSymbol =
          new StaticSymbolLinux(AssemblyCodeGenerator.InitializerName,
                                textList, externSet);
      }

      if (Start.Windows) {
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        null, 65534, (BigInteger)65534);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.fstcw,
                        AssemblyCode.RegularFrameRegister, 0);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.or_word,
                        AssemblyCode.RegularFrameRegister, 0,
                        (BigInteger) 0x0C00);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.fldcw,
                        AssemblyCode.RegularFrameRegister, 0);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Linker.StackStart, 0, BigInteger.Zero,
                        TypeSize.PointerSize);

        List<byte> byteList = new List<byte>();
        IDictionary<int, string> accessMap = new Dictionary<int, string>();
        IDictionary<int, string> callMap = new Dictionary<int, string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.GenerateTargetWindows(assemblyCodeList,
                              byteList, accessMap, callMap, returnSet);
        StaticSymbol staticSymbol =
          new StaticSymbolWindows(AssemblyCodeGenerator.InitializerName,
                                  byteList, accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }
    }

    // Command Line Arguments

    public static void ArgumentCodeList() {
      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();

      if (Start.Linux) {
        /*  pop rbx
            mov rax, rbx
            mov rdx, rbp */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.comment,
                        "Initialize Command Line Arguments");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.pop, Register.rbx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rax, Register.rbx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rdx, Register.rbp);

        /* $args$loop:
             cmp rbx, 0
             je $args$exit
             pop rsi
             mov [rbp], rsi
             add rbp, 8
             dec rbx
             jmp $args$loop */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.label,
                        "$args$loop");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp,
                        Register.rbx, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, null, "$args$exit");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.pop, Register.rsi);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rbp, 0, Register.rsi);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.rbp,
                        (BigInteger) TypeSize.PointerSize);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.dec, Register.rbx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, null, "$args$loop");

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.label,
                        "$args$exit");
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_qword,
                        Register.rbp, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add, Register.rbp,
                        (BigInteger) TypeSize.PointerSize);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.rbp,0, BigInteger.Zero,TypeSize.PointerSize);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.rbp,
                        SymbolTable.FunctionHeaderSize, Register.eax);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov, Register.rbp,
                        SymbolTable.FunctionHeaderSize +
                        TypeSize.SignedIntegerSize, Register.rdx);

        /* $args$exit:
             mov qword [rbp], 0
             add rbp, 8 */

        List<string> textList = new List<string>();
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                                            externSet);
        SymbolTable.ArgsSymbol =
          new StaticSymbolLinux(AssemblyCodeGenerator.ArgsName,
                                textList, externSet);
      }
      
      if (Start.Windows) {
        /*    mov si, bp
              mov word [bp], $Path
              add bp, 2
              mov ax, 1
              mov bx, 129
              cmp byte [bx], 13
              je ListDone */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.si, Register.bp);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        Register.bp, 0, AssemblyCodeGenerator.PathName);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                        Register.bp, (BigInteger) 2);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.ax, BigInteger.One);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bx, (BigInteger) 129);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 13);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count + 17);

        /* SpaceLoop:
             cmp byte [bx], 32
             jne WordStart
             inc bx
             jmp SpaceLoop */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 32);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jne,
                        null, assemblyCodeList.Count + 3);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, assemblyCodeList.Count - 3);

        /* WordStart:
             inc ax
             mov word [bp], bx
             add bp, 2 */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.ax);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bp, 0, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                        Register.bp, (BigInteger) 2);

        /* WordLoop:
             cmp byte [bx], 32
             je WordDone
             cmp byte [bx], 13
             je ListDone
             inc bx
             jmp WordLoop */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 32);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count + 5);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                        Register.bx, 0, (BigInteger) 13);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                        null, assemblyCodeList.Count + 6);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, assemblyCodeList.Count - 5);
    
        /* WordDone:
             mov byte [bx], 0; Space -> Zero
             inc bx; Zero -> Next
             jmp SpaceLoop */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte,
                        Register.bx, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                        null, assemblyCodeList.Count - 15);

        /* ListDone:
             mov byte [bx], 0; Return -> Zero
             mov word [bp], 0
             add bp, 2
             mov word [bp], 0
             mov [bp + 6], ax
             mov [bp + 8], si */

        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte,
                        Register.bx, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        Register.bp, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                        Register.bp, (BigInteger) 2);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                        Register.bp, 0, BigInteger.Zero);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bp, 6, Register.ax);
        AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                        Register.bp, 8, Register.si);

        List<byte> byteList = new List<byte>();
        IDictionary<int, string> accessMap = new Dictionary<int, string>();
        IDictionary<int, string> callMap = new Dictionary<int, string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.
          GenerateTargetWindows(assemblyCodeList, byteList,
                                accessMap, callMap, returnSet);
        StaticSymbol staticSymbol =
          new StaticSymbolWindows(AssemblyCodeGenerator.ArgsName, byteList,
                                  accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }
    }

    /*
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                            Register.si, Register.bp);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                            Register.bp, 0, AssemblyCodeGenerator.PathName);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                            Register.bp, (BigInteger) 2);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                            Register.ax, BigInteger.One);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                            Register.bx, (BigInteger) 129);

            AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                            Register.bx, 0, (BigInteger) 32);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                            null, assemblyCodeList.Count + 5);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                            Register.bx, 0, (BigInteger) 13);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                            null, assemblyCodeList.Count + 13);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                            null, assemblyCodeList.Count - 5);

            AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp,
                            Register.ax, BigInteger.One);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                            null, assemblyCodeList.Count + 2);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte,
                            Register.bx, 0, BigInteger.Zero);

            AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.bx);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.cmp_byte,
                            Register.bx, 0, (BigInteger) 32);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.je,
                            null, assemblyCodeList.Count - 2);
    
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                            Register.bp, 0, Register.bx);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                            Register.bp, (BigInteger) 2);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.inc, Register.ax);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.jmp,
                            null, assemblyCodeList.Count - 15);

            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_byte,
                            Register.bx, 0, BigInteger.Zero);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov_word,
                            Register.bp, 0, BigInteger.Zero);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.add,
                            Register.bp, (BigInteger) 2);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                            Register.bp, 6, Register.ax);
            AddAssemblyCode(assemblyCodeList, AssemblyOperator.mov,
                            Register.bp, 8, Register.si);
*/

// Jump Info

        /*private void WindowsJumpInfo() {
          IDictionary<int,int> middleToAssemblyMap = new Dictionary<int,int>();

          for (int assemblyIndex = 0;
               assemblyIndex < m_assemblyCodeList.Count; ++assemblyIndex) {
            AssemblyCode assemblyCode = m_assemblyCodeList[assemblyIndex];

            if (assemblyCode.Operator == AssemblyOperator.new_middle_code) {
              int middleIndex = (int) assemblyCode[0];
              middleToAssemblyMap.Add(middleIndex, assemblyIndex);
              assemblyCode.Operator = AssemblyOperator.empty;
            }
          }

          for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
            AssemblyCode assemblyCode = m_assemblyCodeList[line];

            if (assemblyCode.IsRelationNotRegister() ||
                assemblyCode.IsJumpNotRegister()) {
              if (!(assemblyCode[1] is int)) {
                int middleTarget = (int) assemblyCode[2];
                assemblyCode[1] = middleToAssemblyMap[middleTarget];
              }
            }
          }

          IDictionary<int,int> assemblyToByteMap = new Dictionary<int,int>();

          { int byteSize = 0, line = 0;
            foreach (AssemblyCode assemblyCode in m_assemblyCodeList) {
              assemblyToByteMap.Add(line++, byteSize);
  
              if (!(assemblyCode.IsRelationNotRegister() ||
                    assemblyCode.IsJumpNotRegister())) {
                byteSize += assemblyCode.ByteList().Count;
              }
            }
            assemblyToByteMap.Add(m_assemblyCodeList.Count, byteSize);
          }

          for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line) {
            AssemblyCode thisCode = m_assemblyCodeList[line],
                         nextCode = m_assemblyCodeList[line + 1];

            if (thisCode.IsRelationNotRegister() ||
                thisCode.IsJumpNotRegister()) {
              int assemblyTarget = (int) thisCode[1];
              int byteSource = assemblyToByteMap[line + 1],
                  byteTarget = assemblyToByteMap[assemblyTarget];
              int byteDistance = byteTarget - byteSource;
              Error.ErrorXXX(byteDistance != 0);
              thisCode[0] = byteDistance;
            }
          }

          for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line) {
            AssemblyCode assemblyCode = m_assemblyCodeList[line];

            if (assemblyCode.Operator == AssemblyOperator.address_return) {
              int middleAddress = (int) ((BigInteger) assemblyCode[2]);
              int assemblyAddress = middleToAssemblyMap[middleAddress];
              int byteAddress = assemblyToByteMap[assemblyAddress];
              int nextAddress = assemblyToByteMap[line + 1];
              BigInteger byteReturn = byteAddress - nextAddress +
                                      TypeSize.PointerSize;
              assemblyCode[2] = byteReturn;
            }
          }
        }*/

    private void WindowsJumpInfo() {
      IDictionary<int,int> middleToAssemblyMap = new Dictionary<int,int>();

      for (int assemblyIndex = 0;
           assemblyIndex < m_assemblyCodeList.Count; ++assemblyIndex) {
        AssemblyCode assemblyCode = m_assemblyCodeList[assemblyIndex];

        if (assemblyCode.Operator == AssemblyOperator.new_middle_code) {
          int middleIndex = (int) assemblyCode[0];
          middleToAssemblyMap.Add(middleIndex, assemblyIndex);
          assemblyCode.Operator = AssemblyOperator.empty;
        }
      }

      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        if (!(assemblyCode[1] is int) && !(assemblyCode[1] is int) &&
            (assemblyCode.IsRelationNotRegister() ||
             assemblyCode.IsJumpNotRegister())) {
          int middleTarget = (int) assemblyCode[2];
          assemblyCode[1] = middleToAssemblyMap[middleTarget];
        }
      }

      IDictionary<int,int> assemblyToByteMap = new Dictionary<int,int>();

      { int byteSize = 0, line = 0;
        foreach (AssemblyCode assemblyCode in m_assemblyCodeList) {
          assemblyToByteMap.Add(line++, byteSize);
  
          if (!(assemblyCode.IsRelationNotRegister() ||
                assemblyCode.IsJumpNotRegister())) {
            byteSize += assemblyCode.ByteList().Count;
          }
        }

        assemblyToByteMap.Add(m_assemblyCodeList.Count, byteSize);
      }

      while (true) {
        for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line) {
          AssemblyCode thisCode = m_assemblyCodeList[line],
                       nextCode = m_assemblyCodeList[line + 1];

          if (thisCode.IsRelationNotRegister() || 
              thisCode.IsJumpNotRegister()) {
            int assemblyTarget = (int) thisCode[1];
            int byteSource = assemblyToByteMap[line + 1],
                byteTarget = assemblyToByteMap[assemblyTarget];
            int byteDistance = byteTarget - byteSource;
            thisCode[0] = byteDistance;
          }
        }

        bool update = false;
        { int byteSize = 0, line = 0;
          foreach (AssemblyCode objectCode in m_assemblyCodeList) {
            if (assemblyToByteMap[line] != byteSize) {
              assemblyToByteMap[line] = byteSize;
              update = true;
            }
            
            byteSize += objectCode.ByteList().Count;
            ++line;
          }
        }

        if (!update) {
          break;
        }
      }

      foreach (AssemblyCode objectCode in m_assemblyCodeList) {
        if (objectCode.IsRelationNotRegister() ||
            objectCode.IsJumpNotRegister()) {
          int byteDistance = (int) objectCode[0];

          if (byteDistance == 0) {
            objectCode.Operator = AssemblyOperator.empty;
          }
        }
      }

      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {        
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        if (assemblyCode.Operator == AssemblyOperator.return_address) {
          int middleAddress = (int) ((BigInteger) assemblyCode[2]);
          int assemblyAddress = middleToAssemblyMap[middleAddress];
          int byteAddress = assemblyToByteMap[assemblyAddress];
          assemblyCode[2] = (BigInteger) byteAddress;
        }
      }
    }
    
/*
      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode assemblyCode = m_assemblyCodeList[line];

        if (assemblyCode.Operator == AssemblyOperator.address_return) {
          int middleAddress = (int) ((BigInteger) assemblyCode[2]);
          int assemblyAddress = middleToAssemblyMap[middleAddress];
          int byteAddress = assemblyToByteMap[assemblyAddress];
          int nextAddress = assemblyToByteMap[line + 1];
          BigInteger byteReturn = byteAddress - nextAddress +
                                  TypeSize.PointerSize;
          assemblyCode[2] = byteReturn;
        }
      }
*/
    // Text List
    
    public static void LinuxTextList(IList<AssemblyCode> assemblyCodeList,
                                     IList<string> textList,
                                     ISet<string> externSet) {
//      foreach (AssemblyCode assemblyCode in assemblyCodeList) {
      for (int index = 0; index < assemblyCodeList.Count; ++index) {
        AssemblyCode assemblyCode = assemblyCodeList[index];
        AssemblyOperator assemblyOperator = assemblyCode.Operator;
        object operand0 = assemblyCode[0],
               operand1 = assemblyCode[1],
               operand2 = assemblyCode[2];

        if (assemblyOperator == AssemblyOperator.define_value) {
          Sort sort = (Sort) operand0;

          if ((sort != Sort.String) && (operand1 is string)) {
            string name1 = (string) operand1;
               
            if (!name1.Contains(Symbol.SeparatorId)) {
              externSet.Add(name1);
            }
          }
        }
        else if ((assemblyOperator != AssemblyOperator.label) &&
                 (assemblyOperator != AssemblyOperator.comment)) {
          if (operand0 is string) {
            string name0 = (string) operand0;

            if (!name0.Contains(Symbol.SeparatorId)) {
              externSet.Add(name0);
            }
          }

          if (operand1 is string) {
            string name1 = (string) operand1;
               
            if (!name1.Contains(Symbol.SeparatorId)) {
              externSet.Add(name1);
            }
          }

          if (operand2 is string) {
            string name2 = (string) operand2;
            
            if (!name2.Contains(Symbol.SeparatorId)) {
              externSet.Add(name2);
            }
          }
        }

        string text = assemblyCode.ToString();
        if (text != null) {
          textList.Add(text);
        }
      }
    }

    // Target Byte List

    private void WindowsByteList(List<byte> byteList,
                                 IDictionary<int,string> accessMap,
                                 IDictionary<int,string> callMap,
                                 ISet<int> returnSet) {
      foreach (AssemblyCode assemblyCode in m_assemblyCodeList) {
        byteList.AddRange(assemblyCode.ByteList());

        if (!assemblyCode.IsJumpNotRegister() &&
            !assemblyCode.IsRelationNotRegister() &&
            (assemblyCode.Operator != AssemblyOperator.label) &&
            (assemblyCode.Operator != AssemblyOperator.comment) &&
            (assemblyCode.Operator != AssemblyOperator.define_zero_sequence)){
          if (assemblyCode.Operator == AssemblyOperator.define_address) {
            string name = (string) assemblyCode[0];
            accessMap.Add(byteList.Count - TypeSize.PointerSize, name);
          }
          else if (assemblyCode.Operator == AssemblyOperator.define_value) {
            Sort sort = (Sort) assemblyCode[0];
            object value = assemblyCode[1];

            if (sort == Sort.Pointer) {
              if (value is string) {
                accessMap.Add(byteList.Count - TypeSize.PointerSize,
                              (string) value);
              }
              else if (value is StaticAddress) {
                StaticAddress staticAddress = (StaticAddress) value;
                accessMap.Add(byteList.Count - TypeSize.PointerSize,
                              staticAddress.UniqueName);
              }
            }
          }
          else if ((assemblyCode.Operator == AssemblyOperator.call) &&
                   (assemblyCode[0] is string)) {
            string calleeName = (string) assemblyCode[0];
            int address = byteList.Count - TypeSize.PointerSize;
            callMap.Add(address, calleeName);
          }
          else if (assemblyCode.Operator == AssemblyOperator.return_address) {
            int address = byteList.Count - TypeSize.PointerSize;
            returnSet.Add(address);
          }
          else if (assemblyCode[0] is string) { // Add [g + 1], 2
            if (assemblyCode[2] is BigInteger) {
              int size = AssemblyCode.SizeOfValue((BigInteger)assemblyCode[2],
                                                  assemblyCode.Operator);
              int address = byteList.Count - TypeSize.PointerSize - size;
              accessMap.Add(address, (string) assemblyCode[0]);
            }
            else {
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) assemblyCode[0]);
            }
          }
          else if (assemblyCode[1] is string) { // mov ax, [g + 1]; mov ax, g
            int address = byteList.Count - TypeSize.PointerSize;
            accessMap.Add(address, (string) assemblyCode[1]);
          }
          else if (assemblyCode[2] is string) { // Add [bp + 2], g
            int address = byteList.Count - TypeSize.PointerSize;
            accessMap.Add(address, (string) assemblyCode[2]);
          }
        }
      }
    }
  }
}

/*
          else if (assemblyCode[1] is string) {
            if (assemblyCode[2] is int) { // mov ax, [g + 1]
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) assemblyCode[1]);
            }
            else { // mov ax, g
              int address = byteList.Count - TypeSize.PointerSize;
              accessMap.Add(address, (string) assemblyCode[1]);
            }
          }
*/

    /* e = x;     d => ax
       a = b / c; b => ax  
       d = e / f; d => ax
       f = b;     b => ?

       a -> b: yes
       b -> a: no
  
       a -> e: yes
       e -> a: no
  
       b -> d: no
       d -> b: yes
  
       b1: 3ax
       b2: 10, 11
       d1: 8
       d2: 9ax
  
       a: 6ax
       b: 3ax, 10, 11
       d: 8, 9ax
       e: 1, 2, 7ax

       1. mov ?, [x]
       2. mov [e], ?
  
       3. mov ax, [b]    ; b => ax
       4. mov ?, [c]
       5. div ?
       6. empty          ; a => ax
  
       7. empty          ; e => ax
       8. mov ?, [f]
       9. div ?          ; d => ax
  
       10. mov ?, [b]
       11. mov [f], ?

      // int 21       value
      // int ax       GetRegister
      // int [bx]     address
    
      // liveSet: stack  mov ax, [si + 2]; neg ax; int ax
      //          static mov ax, [20]; neg ax
      //        

      // !liveSet: stack  neg [si + 2]
      //           static neg [20]

     1 sbyte: al * 8 => ah:al
     2 bytes: ax * 16 => dx:ax
     4 bytes: eax * 32 => edx:eax

          // b = x;     b => ?
          // ...
          // d = e / f; e => ax
          // ...
          // a = b / c; b => ax

     1 sbyte: ax / 8, div: al, mod: ah
     2 bytes: dx:ax / 16, div: ax, mod: dx
     4 bytes: edx:eax / 32, div eax, mod edx */