using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCodeGenerator {
    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList,
                               MiddleOperator op, object operand0 = null,
                               object operand1 = null, object operand2 = null) {
      MiddleCode middleCode = new MiddleCode(op, operand0, operand1, operand2);
      codeList.Add(middleCode);
      return middleCode;
    }

    public static void Backpatch(ISet<MiddleCode> sourceSet,
                                 List<MiddleCode> list) {
      Backpatch(sourceSet, GetFirst(list));
    }

    public static void Backpatch(ISet<MiddleCode> sourceSet,
                                 MiddleCode target) {
      if (sourceSet != null) {
        foreach (MiddleCode source in sourceSet) {
          Error.ErrorXXX(source[0] == null);
          source[0] = target;
        }
      }
    }

    private static MiddleCode GetFirst(List<MiddleCode> list) {
      if (list.Count == 0) {
        AddMiddleCode(list, MiddleOperator.Empty);
      }
    
      return list[0];
    }
  
    // ---------------------------------------------------------------------------------------------------------------------

    public static void FunctionHeader(Specifier specifier,
                                      Declarator declarator) {
      Type returnType;
      bool externalLinkage;

      if (specifier != null) {
        externalLinkage = specifier.ExternalLinkage;
        returnType = specifier.Type;
      }
      else {
        externalLinkage = true;
        returnType = Type.SignedIntegerType;
      }

      declarator.Add(returnType);
      Error.Check(declarator.Type.IsFunction(), declarator.Name,
                   Message.Not_a_function);
      Error.Check(declarator.Name != null,
                   Message.Unnamed_function_definition);

      SymbolTable.CurrentFunction=new Symbol(declarator.Name, externalLinkage,
                                             Storage.Static, declarator.Type);
      SymbolTable.CurrentTable.AddSymbol(SymbolTable.CurrentFunction);

      if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
        Error.Check(returnType.IsVoid() || returnType.IsInteger(), "main",
                     Message.Function_main_must_return_void_or_integer);
      }

      SymbolTable.CurrentTable =
        new SymbolTable(SymbolTable.CurrentTable, Scope.Function);
    }

    public static void FunctionDefinition() {
      Type funcType = SymbolTable.CurrentFunction.Type;

      if (funcType.IsOldStyle()) {
        List<string> nameList = funcType.NameList;
        IDictionary<string,Symbol> entryMap =
          SymbolTable.CurrentTable.EntryMap;

        Error.Check(nameList.Count == entryMap.Count,
                     SymbolTable.CurrentFunction.Name, Message. 
          Unmatched_number_of_parameters_in_old__style_function_definition);

        int offset = SymbolTable.FunctionHeaderSize;
        foreach (string name in nameList) {
          Symbol symbol;

          if (!entryMap.TryGetValue(name, out symbol)) {
            Error.Report(name, Message.
                      Undefined_parameter_in_old__style_function_definition);
          }

          symbol.Offset = offset;
          offset += symbol.Type.SizeAddress();
        }
      }
      else {
        Error.Check(SymbolTable.CurrentTable.EntryMap.Count == 0,
          Message.New_and_old_style_mixed_function_definition);

        foreach (Symbol symbol in funcType.ParameterList) {
          SymbolTable.CurrentTable.AddSymbol(symbol);
        }
      }

      if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
        AssemblyCodeGenerator.InitializationCodeList();
        List<Type> typeList =
          SymbolTable.CurrentFunction.Type.TypeList;
        if ((typeList != null) && (typeList.Count == 2)) {
          Error.Check(typeList[0].IsInteger() &&
                       typeList[1].IsPointer() &&
                       typeList[1].PointerType.IsPointer() &&
                       typeList[1].PointerType.PointerType.IsChar(),
                       "main", Message.Invalid_parameter_list);
          AssemblyCodeGenerator.ArgumentCodeList();
        }
        else {
          Error.Check((typeList == null) || (typeList.Count == 0),
                       "main", Message.Invalid_parameter_list);
        }
      }
    }

    public static void FunctionEnd(Statement statement) {
      MiddleCode nextCode =
        AddMiddleCode(statement.CodeList, MiddleOperator.Empty);
      Backpatch(statement.NextSet, nextCode);

      if (SymbolTable.CurrentFunction.Type.ReturnType.IsVoid()) {
        AddMiddleCode(statement.CodeList, MiddleOperator.Return);

        if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
          AddMiddleCode(statement.CodeList, MiddleOperator.Exit);
        }
      }

/*       if (SymbolTable.CurrentFunction.Type.ReturnType.IsVoid()) {
        if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
          AddMiddleCode(statement.CodeList, MiddleOperator.Exit);
        }
        else {
          AddMiddleCode(statement.CodeList, MiddleOperator.Return);
        }
      }*/

      AddMiddleCode(statement.CodeList, MiddleOperator.FunctionEnd,
                    SymbolTable.CurrentFunction);

      if (SymbolTable.CurrentFunction.Name.Equals("setlocale")) {
        string name = @"C:\Users\Stefa\Documents\vagrant\homestead\code\code\" +
                      SymbolTable.CurrentFunction.Name + ".middlebefore";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.CodeList.Count; ++index) {
          MiddleCode middleCode = statement.CodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      MiddleCodeOptimizer middleCodeOptimizer =
        new MiddleCodeOptimizer(statement.CodeList);
      middleCodeOptimizer.Optimize();

      if (SymbolTable.CurrentFunction.Name.Equals("setlocale")) {
        string name = @"C:\Users\Stefa\Documents\vagrant\homestead\code\code\" +
                      SymbolTable.CurrentFunction.Name + ".middleafter";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.CodeList.Count; ++index) {
          MiddleCode middleCode = statement.CodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AssemblyCodeGenerator.GenerateAssembly(statement.CodeList,
                                             assemblyCodeList);

      if (Start.Linux) {
        List<string> textList = new List<string>();

        if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
          textList.AddRange(SymbolTable.InitSymbol.TextList);

          if (SymbolTable.ArgsSymbol != null) {
            textList.AddRange(SymbolTable.ArgsSymbol.TextList);
          }
        }
        else {
          textList.Add("section .text");
        }

        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                                            externSet);
        StaticSymbol staticSymbol =
          new StaticSymbolLinux(SymbolTable.CurrentFunction.UniqueName,
                                textList, externSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      if (Start.Windows) {
        List<byte> byteList = new List<byte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        IDictionary<int,string> callMap = new Dictionary<int,string>();
        ISet<int> returnSet = new HashSet<int>();
        AssemblyCodeGenerator.GenerateTargetWindows
          (assemblyCodeList, byteList, accessMap, callMap, returnSet);
        StaticSymbol staticSymbol =
          new StaticSymbolWindows(SymbolTable.CurrentFunction.UniqueName,
                                  byteList, accessMap, callMap, returnSet);
        SymbolTable.StaticSet.Add(staticSymbol);
      }

      SymbolTable.CurrentTable = SymbolTable.CurrentTable.ParentTable;
      SymbolTable.CurrentFunction = null;
    }

    private static Stack<Type> m_structOrUnionTypeStack = new Stack<Type>();

    public static void StructUnionHeader(Sort sort, string optionalName) {
      Type type = new CCompiler.Type(sort, null, null);

      if (optionalName != null) {
        type = SymbolTable.CurrentTable.AddTag(optionalName, type);
      }

      m_structOrUnionTypeStack.Push(type);
      SymbolTable.CurrentTable =
        new SymbolTable(SymbolTable.CurrentTable, (Scope) sort);
    }

    public static Type StructUnionSpecifier() {
      Type type = m_structOrUnionTypeStack.Pop();
      type.MemberMap = SymbolTable.CurrentTable.EntryMap;
      type.MemberList = SymbolTable.CurrentTable.EntryList;
      SymbolTable.CurrentTable =
        SymbolTable.CurrentTable.ParentTable;
      return type;
    }

    public static Type StructUnionLookup(Sort sort, string name) {
      Type type = SymbolTable.CurrentTable.LookupTag(name, sort);

      if (type == null) {
        type = new Type(sort, null, null);
        SymbolTable.CurrentTable.AddTag(name, type);
      }

      return type;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Stack<BigInteger> m_enumerationStack =
      new Stack<BigInteger>();

    public static void EnumumerationHeader() {
      m_enumerationStack.Push(BigInteger.Zero);
    }

    public static Type EnumumerationSpecifier(string optionalName,
                                              ISet<Symbol> enumerationItemSet)
    { Type enumerationType = new Type(enumerationItemSet);

      if (optionalName != null) {
        SymbolTable.CurrentTable.AddTag(optionalName, enumerationType);
      }

      m_enumerationStack.Pop();
      return enumerationType;
    }

    public static Type EnumumerationLookup(string name) {
      Type type = SymbolTable.CurrentTable.LookupTag(name, Sort.SignedInt);
      Error.Check(type != null, name, Message.Tag_not_found); 
      return type;
    }

    public static Symbol EnumerationItem(string itemName,
                                         Symbol optionalInitSymbol) {
      Type itemType = new Type(Sort.SignedInt);      
      itemType.Constant = true;

      BigInteger value;
      if (optionalInitSymbol != null) {
        Error.Check(optionalInitSymbol.Type.IsIntegral(), itemName,
                      Message.Non__integral_enum_value);
        Error.Check(optionalInitSymbol.Value != null, itemName,
                      Message.Non__constant_enum_value);
        m_enumerationStack.Pop();
        value = (BigInteger) optionalInitSymbol.Value;
      }
      else {
        value = m_enumerationStack.Pop();
      }
    
      Symbol itemSymbol =
        new Symbol(itemName, false, Storage.Auto, itemType, value);
      if (optionalInitSymbol != null) {
        itemSymbol.InitializedEnum = true;
      }

      SymbolTable.CurrentTable.AddSymbol(itemSymbol);
      m_enumerationStack.Push(value + 1);
      return itemSymbol;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static List<MiddleCode> Declarator(Specifier specifier,
                                   Declarator declarator, object initializer,
                                   Symbol bitsSymbol) {
      if (bitsSymbol != null) {
        Error.Check((SymbolTable.CurrentTable.Scope == Scope.Struct) ||
                    (SymbolTable.CurrentTable.Scope == Scope.Union), bitsSymbol,
                    Message.Bitfields_only_allowed_in_structs_or_unions);
      }

      Type type;
      Storage storage;

      if (declarator != null) {
        if (specifier != null) {
          declarator.Add(specifier.Type);          
          storage = specifier.Storage;
        }
        else {
          declarator.Add(Type.SignedIntegerType);
          storage = Storage.Auto;
        }

        type = declarator.Type;
      }
      else {
        type = specifier.Type;
        storage = Storage.Auto;
      }

      if (type.IsFunction()) {
        Error.Check((storage == Storage.Static) ||
                    (storage == Storage.Extern),  storage, Message.
        Only_extern_or_static_storage_allowed_for_functions);
          storage = Storage.Extern;
      }

      if (initializer != null) {
        Error.Check(!type.IsFunction() &&
                    (storage != Storage.Extern) &&
                    (storage != Storage.Typedef) &&
                    (SymbolTable.CurrentTable.Scope != Scope.Struct) &&
                    (SymbolTable.CurrentTable.Scope != Scope.Union),
                    declarator.Name, Message.Invalid_initialization);
      }

      Symbol symbol = new Symbol(declarator.Name, specifier.ExternalLinkage,
                                 storage, type);
      List<MiddleCode> codeList = new List<MiddleCode>();

      if (initializer != null) {
        SymbolTable.CurrentTable.AddSymbol(symbol, false);

        if (storage == Storage.Static) {
          List<MiddleCode> middleCodeList =
            GenerateStaticInitializer.GenerateStatic(type, initializer);
          StaticSymbol staticSymbol =
            ConstantExpression.Value(symbol.UniqueName, type, middleCodeList);
          SymbolTable.StaticSet.Add(staticSymbol);
        }
        else {
          GenerateAutoInitializer.GenerateAuto(symbol, initializer,
                                                0, codeList);
          SymbolTable.CurrentTable.CurrentOffset += symbol.Type.Size();
        }
      }
      else if (bitsSymbol != null) {
        Error.Check(type.IsIntegral(), type,
                     Message.Non__integral_bits_expression);
        int bits = (int)((BigInteger)bitsSymbol.Value);
        Error.Check((bits >= 1) && (bits <= (8 * type.Size())),
                     bits, Message.Bits_value_out_of_range);
        type.SetBitfieldMask(bits);

        if (declarator != null) {
          SymbolTable.CurrentTable.AddSymbol(symbol);
        }
      }
      else {
        SymbolTable.CurrentTable.AddSymbol(symbol);

        if (symbol.IsStatic()) {
          SymbolTable.StaticSet.Add(ConstantExpression.Value(symbol));
        }
      }

      return codeList;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Declarator PointerDeclarator(List<Type> typeList,
                                                Declarator declarator) {
      if (declarator == null) {
        declarator = new Declarator(null);
      }

      foreach (Type type in typeList) {
        Type pointerType = new Type((Type) null);
        pointerType.Constant = type.Constant;
        pointerType.Volatile = type.Volatile;
        declarator.Add(pointerType);
      }
      return declarator;

    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Declarator ArrayType(Declarator declarator,
                                        Expression optionalSizeExpression) {
        if (declarator == null) {
        declarator = new Declarator(null);
      }

      int arraySize;
      if (optionalSizeExpression != null) {
        arraySize = (int) ((BigInteger) optionalSizeExpression.Symbol.Value);
        Error.Check(arraySize > 0, arraySize,
                      Message.Non__positive_array_size);
      }
      else {
        arraySize = 0;
      }

      Type arrayType = new Type(arraySize, null);
      declarator.Add(arrayType);
      return declarator;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Declarator OldFunctionDeclaration(Declarator declarator,
                                                    List<string> nameList) {
      declarator.Add(new Type(nameList));
      return declarator;
    }

    public static Declarator NewFunctionDeclaration(Declarator declarator,
                                                    List<Symbol> parameterList,
                                                    bool variadic) {
      if (parameterList.Count == 0) {
        Error.Check(!variadic, "...",
            Message.An_variadic_function_must_have_at_least_one_parameter);
      }
      else if ((parameterList.Count == 1) && parameterList[0].Type.IsVoid()) {
        Error.Check(parameterList[0].Name == null,
                      parameterList[0].Name,
                      Message.A_void_parameter_cannot_be_named);
        Error.Check(!variadic, "...", Message.
                      An_variadic_function_cannot_have_a_void_parameter);
        parameterList.Clear();
      }
      else {
        foreach (Symbol symbol in parameterList) {
          Error.Check(!symbol.Type.IsVoid(),
                        Message.Invalid_void_parameter);
        }
      }
      declarator.Add(new Type(parameterList, variadic));
      return declarator;
    }

    public static Symbol Parameter(Specifier specifier,
                                    Declarator declarator) {
      string name;
      Type type;

      if (declarator != null) {
        name = declarator.Name;
        declarator.Add(specifier.Type);
        type = declarator.Type;
      }
      else {
        name = null;
        type = specifier.Type;
      }

      if (type.IsArray()) {
        type = new Type(type.ArrayType);
      }
      else if (type.IsFunction()) {
        type = new Type(type);
      }

      Symbol symbol = new Symbol(name, false, specifier.Storage, type);
      symbol.Parameter = true;
      return symbol;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    /*
      * if (x) {
      *   y();
      * }
      * else {
      * }
      * */

    /*public static Statement IfStatement(Expression expression,
                                        Statement innerStatement) {
      expression = TypeCast.ToLogical(expression);
      List<MiddleCode> longList = expression.LongList;
//      AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);

      Backpatch(expression.Symbol.TrueSet, innerStatement.CodeList);    
      longList.AddRange(innerStatement.CodeList);
      MiddleCode nextCode = AddMiddleCode(longList, MiddleOperator.Jump); // XXX
      
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(innerStatement.NextSet);
      nextSet.UnionWith(expression.Symbol.FalseSet);
      nextSet.Add(nextCode);
    
      return (new Statement(longList, nextSet));
    }*/

    public static Statement IfElseStatement(Expression testExpression,
                                            Statement trueStatement,
                                            Statement falseStatement) {
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(trueStatement.NextSet);
      nextSet.UnionWith(falseStatement.NextSet);

      Symbol voidSymbol = new Symbol(new Type(Sort.Void));
      Expression trueExpression =
        new Expression(voidSymbol, trueStatement.CodeList),
                 falseExpression =
        new Expression(voidSymbol, falseStatement.CodeList);

      Expression conditionExpression =
        ConditionExpression(testExpression, trueExpression, falseExpression);
      return (new Statement(conditionExpression.ShortList, nextSet));
    }

    public static Statement IfElseStatementX(Expression testExpression,
                                            Statement trueStatement,
                                            Statement falseStatement) {
      testExpression = TypeCast.ToLogical(testExpression);
      List<MiddleCode> longList = testExpression.LongList;
//      AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);

      Backpatch(testExpression.Symbol.TrueSet, trueStatement.CodeList);
      Backpatch(testExpression.Symbol.FalseSet, falseStatement.CodeList);

      longList.AddRange(trueStatement.CodeList);
      MiddleCode jumpTrue = AddMiddleCode(longList, MiddleOperator.Jump);
      longList.AddRange(falseStatement.CodeList);
      MiddleCode jumpFalse = AddMiddleCode(longList, MiddleOperator.Jump);

      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(trueStatement.NextSet);
      nextSet.UnionWith(falseStatement.NextSet);
      nextSet.Add(jumpTrue);
      nextSet.Add(jumpFalse);

      return (new Statement(longList, nextSet));
    }

    private static Stack<IDictionary<BigInteger, MiddleCode>> m_caseMapStack =
      new Stack<IDictionary<BigInteger, MiddleCode>>();
    private static Stack<MiddleCode> m_defaultStack = new Stack<MiddleCode>();
    private static Stack<ISet<MiddleCode>> m_breakSetStack =
      new Stack<ISet<MiddleCode>>();

    public static void SwitchHeader() {
      m_caseMapStack.Push(new Dictionary<BigInteger,MiddleCode>());
      m_defaultStack.Push(null);
      m_breakSetStack.Push(new HashSet<MiddleCode>());
    }

    public static Statement SwitchStatement(Expression switchExpression,
                                            Statement statement) {
      switchExpression = TypeCast.LogicalToIntegral(switchExpression);
      List<MiddleCode> codeList = switchExpression.LongList;

      Type switchType = switchExpression.Symbol.Type;
      foreach (KeyValuePair<BigInteger,MiddleCode> pair
               in m_caseMapStack.Pop()) {
        BigInteger caseValue = pair.Key;
        MiddleCode caseTarget = pair.Value;
        Symbol caseSymbol = new Symbol(switchType, caseValue);
        AddMiddleCode(codeList, MiddleOperator.Case, caseTarget,
                      switchExpression.Symbol, caseSymbol);
      }
      AddMiddleCode(codeList, MiddleOperator.CaseEnd,
                    switchExpression.Symbol);
    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      MiddleCode defaultCode = m_defaultStack.Pop();

      if (defaultCode != null) {
        AddMiddleCode(codeList, MiddleOperator.Jump, defaultCode);
      }
      else {
        nextSet.Add(AddMiddleCode(codeList, MiddleOperator.Jump));      
      }

      codeList.AddRange(statement.CodeList);
      nextSet.UnionWith(statement.NextSet);
      nextSet.UnionWith(m_breakSetStack.Pop());
      return (new Statement(codeList, nextSet));
    }

    public static Statement CaseStatement(Expression expression,
                                          Statement statement) {
      Error.Check(m_caseMapStack.Count > 0, Message.Case_without_switch);
      expression = TypeCast.LogicalToIntegral(expression);
      Error.Check(expression.Symbol.Value != null, expression.Symbol.Name,
                   Message.Non__constant_case_value);
      Error.Check(expression.Symbol.Value is BigInteger, expression.Symbol.Name,
                   Message.Non__integral_case_value);
      BigInteger caseValue = (BigInteger)expression.Symbol.Value;
      IDictionary<BigInteger, MiddleCode> caseMap = m_caseMapStack.Peek();
      Error.Check(!caseMap.ContainsKey(caseValue), caseValue,
                   Message.Repeated_case_value);
      caseMap.Add(caseValue, GetFirst(statement.CodeList));
      return statement;
    }
  
    public static Statement DefaultStatement(Statement statement) {
      Error.Check(m_defaultStack.Count > 0, Message.Default_without_switch);
      Error.Check(m_defaultStack.Pop() == null, Message.Repeted_default);
      m_defaultStack.Push(GetFirst(statement.CodeList));
      return statement;
    }

    public static Statement BreakStatement() {
      Error.Check(m_breakSetStack.Count > 0,
                   Message.Break_without_switch____while____do____or____for);
      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode breakCode = AddMiddleCode(codeList, MiddleOperator.Jump);
      m_breakSetStack.Peek().Add(breakCode);
      return (new Statement(codeList));
    }

    private static Stack<ISet<MiddleCode>> m_continueSetStack =
      new Stack<ISet<MiddleCode>>();
  
    public static void LoopHeader() {
      m_breakSetStack.Push(new HashSet<MiddleCode>());
      m_continueSetStack.Push(new HashSet<MiddleCode>());
    }

    public static Statement DoStatement(Statement innerStatement,
                                        Expression expression) {
      List<MiddleCode> codeList = innerStatement.CodeList;
      Backpatch(innerStatement.NextSet, codeList);

      //AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);
      codeList.AddRange(expression.LongList);

      Backpatch(expression.Symbol.TrueSet, codeList);
      Backpatch(m_continueSetStack.Pop(), codeList);    

      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(expression.Symbol.FalseSet);
      nextSet.UnionWith(m_breakSetStack.Pop());
      return (new Statement(codeList, nextSet));
    }

    public static Statement ForStatement(Expression initializerExpression, Expression testExpression,
                                         Expression nextExpression, Statement innerStatement) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
    
      if (initializerExpression != null) {
        codeList.AddRange(initializerExpression.ShortList);
      }

      MiddleCode testTarget = AddMiddleCode(codeList, MiddleOperator.Empty);
    
      if (testExpression != null) {
        testExpression = TypeCast.ToLogical(testExpression);
        codeList.AddRange(testExpression.LongList);
        //AddMiddleCode(codeList, MiddleOperator.CheckTrackMapFloatStack);
        Backpatch(testExpression.Symbol.TrueSet, innerStatement.CodeList);
        nextSet.UnionWith(testExpression.Symbol.FalseSet);
      }

      codeList.AddRange(innerStatement.CodeList);
      MiddleCode nextTarget = AddMiddleCode(codeList, MiddleOperator.Empty);
      Backpatch(innerStatement.NextSet, nextTarget);
    
      if (nextExpression != null) {
        codeList.AddRange(nextExpression.ShortList);
      }

      AddMiddleCode(codeList, MiddleOperator.Jump, testTarget);
      Backpatch(m_continueSetStack.Pop(), nextTarget);
      nextSet.UnionWith(m_breakSetStack.Pop());
    
      return (new Statement(codeList, nextSet));
    }

    public static Statement WhileStatement(Expression expression,
                                           Statement statement) {
      return ForStatement(null, expression, null, statement);
    }
 
    public static Statement ContinueStatement() {
      Error.Check(m_continueSetStack.Count > 0,
                   Message.Continue_without_while____do____or____for);
      List<MiddleCode> codeList = new List<MiddleCode>();
      MiddleCode continueCode = AddMiddleCode(codeList, MiddleOperator.Jump);
      m_continueSetStack.Peek().Add(continueCode);
      return (new Statement(codeList));
    }

    public static IDictionary<string, MiddleCode> m_labelMap =
      new Dictionary<string, MiddleCode>();
    public static IDictionary<string, ISet<MiddleCode>> m_gotoSetMap =
      new Dictionary<string, ISet<MiddleCode>>();

    public static Statement LabelStatement(string labelName,
                                           Statement statement) {
      Error.Check(!m_labelMap.ContainsKey(labelName),
                   labelName, Message.Defined_twice);
      m_labelMap.Add(labelName, GetFirst(statement.CodeList));
      return statement;
    }

    public static Statement GotoStatement(string labelName) {
      List<MiddleCode> gotoList = new List<MiddleCode>();
      MiddleCode gotoCode = AddMiddleCode(gotoList, MiddleOperator.Jump);

      if (m_gotoSetMap.ContainsKey(labelName)) {
        ISet<MiddleCode> gotoSet = m_gotoSetMap[labelName];
        gotoSet.Add(gotoCode);
      }
      else {
        ISet<MiddleCode> gotoSet = new HashSet<MiddleCode>();
        gotoSet.Add(gotoCode);
        m_gotoSetMap.Add(labelName, gotoSet);
      }

      return (new Statement(gotoList));
    }

    public static void BackpatchGoto() {
      foreach (KeyValuePair<string,ISet<MiddleCode>> entry in m_gotoSetMap) {
        string labelName = entry.Key;
        ISet<MiddleCode> gotoSet = entry.Value;

        MiddleCode labelCode;
        Error.Check(m_labelMap.TryGetValue(labelName, out labelCode),
                     labelName, Message.Missing_goto_address);
        Backpatch(gotoSet, labelCode);
      }
    }

    public static Statement ReturnStatement(Expression expression) {
      List<MiddleCode> codeList;

      if (expression != null) {
        Error.Check(!SymbolTable.CurrentFunction.Type.ReturnType.IsVoid(),
                     Message.Non__void_return_from_void_function);
        expression = TypeCast.ImplicitCast(expression,
                              SymbolTable.CurrentFunction.Type.ReturnType);
        codeList = expression.LongList;
        AddMiddleCode(codeList, MiddleOperator.SetReturnValue);
        AddMiddleCode(codeList, MiddleOperator.Return,
                      null, expression.Symbol);
      }
      else {
        Error.Check(SymbolTable.CurrentFunction.Type.ReturnType.IsVoid(),
                     Message.Void_returned_from_non__void_function);
        codeList = new List<MiddleCode>();
        AddMiddleCode(codeList, MiddleOperator.Return);
      }

      if (SymbolTable.CurrentFunction.UniqueName.Equals("main")) {
        AddMiddleCode(codeList, MiddleOperator.Exit);
      }

      return (new Statement(codeList));
    }

    public static Statement ExpressionStatement(Expression expression) {
      if (expression != null) {
        return (new Statement(expression.ShortList));
      }
      else {    
        return (new Statement());
      }
    }

    public static Statement JumpRegisterStatement(Register register) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      //Register register = (Register) Enum.Parse(typeof(Register), registerName);
      AddMiddleCode(codeList, MiddleOperator.JumpRegister, register);
      return (new Statement(codeList));
    }

    public static Statement InterruptStatement(Expression expression) {
      List<MiddleCode> codeList = new List<MiddleCode>();
      AddMiddleCode(codeList, MiddleOperator.Interrupt, expression.Symbol.Value);
      return (new Statement(codeList));
    }

    public static Statement SyscallStatement() {
      List<MiddleCode> codeList = new List<MiddleCode>();
      AddMiddleCode(codeList, MiddleOperator.SysCall);
      return (new Statement(codeList));
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Expression CommaExpression(Expression leftExpression,
                                             Expression rightExpression) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);
    
      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.ShortList); // Obs: shortList
      longList.AddRange(rightExpression.LongList);
        
      return (new Expression(rightExpression.Symbol, shortList, longList));
    }

    public static Expression AssignmentExpression(MiddleOperator middleOp,
                                                  Expression leftExpression,
                                                  Expression rightExpression) {
      if (middleOp == MiddleOperator.Assign) {
        return Assignment(leftExpression, rightExpression, true);
      }
      else {
        rightExpression =
          ArithmeticExpression(middleOp, leftExpression, rightExpression);
        return Assignment(leftExpression, rightExpression, false);
      }
    }

    public static Expression Assignment(Expression leftExpression,
                                        Expression rightExpression,
                                        bool simpleAssignment) {
      rightExpression = TypeCast.ImplicitCast(rightExpression,
                                              leftExpression.Symbol.Type); // XXX
      Register? register = leftExpression.Register;

      if (register != null) {
        Symbol rightSymbol = rightExpression.Symbol;
        Error.Check(AssemblyCode.SizeOfRegister(register.Value) ==
                     rightExpression.Symbol.Type.Size(),
                     Message.Unmatched_register_size);
        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(rightExpression.LongList);
        AddMiddleCode(longList, MiddleOperator.AssignRegister,
                      register.Value, rightExpression.Symbol);
        return (new Expression(rightExpression.Symbol, longList, longList));
      }
      else {
        Error.Check(leftExpression.Symbol.IsAssignable(),
                     leftExpression, Message.Not_assignable);
        List<MiddleCode> longList = new List<MiddleCode>();

        if (simpleAssignment) {
          longList.AddRange(leftExpression.LongList);
  
          if (leftExpression.Symbol.Type.IsFloating()) {
            AddMiddleCode(longList, MiddleOperator.PopEmpty);
          }
        }

        longList.AddRange(rightExpression.LongList);

        if (leftExpression.Symbol.Type.IsFloating()) {
          List<MiddleCode> shortList = new List<MiddleCode>();
          shortList.AddRange(longList);

          AddMiddleCode(longList, MiddleOperator.TopFloat,
                        leftExpression.Symbol);
          AddMiddleCode(shortList, MiddleOperator.PopFloat,
                        leftExpression.Symbol);
          return (new Expression(leftExpression.Symbol, shortList, longList));
        }
        else {
          if (rightExpression.Symbol.Type.IsStructOrUnion()) {
            AddMiddleCode(longList, MiddleOperator.AssignInitSize,
                          leftExpression.Symbol, rightExpression.Symbol);
          }

          AddMiddleCode(longList, MiddleOperator.Assign,
                        leftExpression.Symbol, rightExpression.Symbol);

          if (leftExpression.Symbol.Type.IsBitfield()) {
            Symbol maskSymbol = new Symbol(leftExpression.Symbol.Type,
                                    leftExpression.Symbol.Type.BitfieldMask);
            AddMiddleCode(longList, MiddleOperator.BitwiseAnd,
                          leftExpression.Symbol, leftExpression.Symbol,
                          maskSymbol);
          }

          return (new Expression(leftExpression.Symbol, longList, longList));
        }
      }
    }

    public static Expression ConditionExpression(Expression testExpression,
                                                 Expression trueExpression,
                                                 Expression falseExpression) {
      testExpression = TypeCast.ToLogical(testExpression);
/*      if (ConstantExpression.IsConstant(testExpression)) {
        return ConstantExpression.IsTrue(testExpression)
               ? trueExpression : falseExpression;
      }*/

      Type maxType = TypeCast.MaxType(trueExpression.Symbol.Type,
                                      falseExpression.Symbol.Type);
      trueExpression = TypeCast.ImplicitCast(trueExpression, maxType);
      falseExpression = TypeCast.ImplicitCast(falseExpression, maxType);

      MiddleCode emptyTrueCode = new MiddleCode(MiddleOperator.Empty);
      trueExpression.ShortList.Insert(0, emptyTrueCode);
      trueExpression.LongList.Insert(0, emptyTrueCode);
      Backpatch(testExpression.Symbol.TrueSet, emptyTrueCode);

      MiddleCode emptyFalseCode = new MiddleCode(MiddleOperator.Empty);
      falseExpression.ShortList.Insert(0, emptyFalseCode);
      falseExpression.LongList.Insert(0, emptyFalseCode);
      Backpatch(testExpression.Symbol.FalseSet, emptyFalseCode);

      Symbol resultSymbol = new Symbol(maxType);

      if (maxType.IsFloating()) {
        AddMiddleCode(trueExpression.LongList,
                      MiddleOperator.DecreaseStack);
      }
      else {
        AddMiddleCode(trueExpression.LongList, MiddleOperator.Assign,
                      resultSymbol, trueExpression.Symbol);
        AddMiddleCode(falseExpression.LongList, MiddleOperator.Assign,
                      resultSymbol, falseExpression.Symbol);
      }

      MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Jump, targetCode);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(testExpression.LongList); // Obs: LongList
      shortList.AddRange(trueExpression.ShortList);
      shortList.Add(jumpCode);
      shortList.AddRange(falseExpression.ShortList);
      shortList.Add(targetCode);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(testExpression.LongList);
      longList.AddRange(trueExpression.LongList);
      longList.Add(jumpCode);
      longList.AddRange(falseExpression.LongList);
      longList.Add(targetCode);

      return (new Expression(resultSymbol, shortList, longList));
    }

    /*private static void ReplaceSymbol(List<MiddleCode> codeList,
                                      Symbol fromSymbol, Symbol toSymbol) {
      foreach (MiddleCode middleCode in codeList) {
        if (middleCode[0] == fromSymbol) {
          middleCode[0] = toSymbol;
        }
      }
    }*/

    public static Expression ConstantIntegralExpression(Expression expression) 
    { expression = ConstantExpression.ConstantCast(expression,
                                                  Type.SignedLongIntegerType);
      Error.Check(expression != null, expression,
                   Message.Non__constant_expression);
      Error.Check(expression.Symbol.Type.IsIntegralOrPointer(),
                   expression.Symbol, Message.Non__integral_expression);
      return expression;
    }

    public static Expression LogicalExpression(MiddleOperator middleOp, 
                                               Expression leftExpression,
                                               Expression rightExpression) {
      Expression constantExpression =
        ConstantExpression.Logical(middleOp, leftExpression, rightExpression);

      if (constantExpression != null) {
        return constantExpression;
      }

      leftExpression = TypeCast.ToLogical(leftExpression);
      rightExpression = TypeCast.ToLogical(rightExpression);

      Symbol resultSymbol;
      if (middleOp == MiddleOperator.LogicalOr) {
        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
        trueSet.UnionWith(leftExpression.Symbol.TrueSet);
        trueSet.UnionWith(rightExpression.Symbol.TrueSet);

        Backpatch(leftExpression.Symbol.FalseSet, rightExpression.LongList);
        resultSymbol = new Symbol(trueSet, rightExpression.Symbol.FalseSet);
      }
      else {
        ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
        falseSet.UnionWith(leftExpression.Symbol.FalseSet);
        falseSet.UnionWith(rightExpression.Symbol.FalseSet);

        Backpatch(leftExpression.Symbol.TrueSet, rightExpression.LongList);
        resultSymbol = new Symbol(rightExpression.Symbol.TrueSet, falseSet);
      }

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);

      return (new Expression(resultSymbol, shortList, longList));
    }

    public static Expression RelationalExpression(MiddleOperator middleOp,
                                                  Expression leftExpression,
                                                  Expression rightExpression){
      Error.Check(!leftExpression.Symbol.Type.IsStructOrUnion(),
                    leftExpression,
                    Message.Invalid_type_in_expression);
      Error.Check(!rightExpression.Symbol.Type.IsStructOrUnion(),
                    rightExpression,
                    Message.Invalid_type_in_expression);

      Expression constantExpression =
        ConstantExpression.Relation(middleOp, leftExpression,
                                    rightExpression);
      if (constantExpression != null) {
        return constantExpression;
      }

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);

      leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
      rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);

      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(leftExpression.ShortList);
      shortList.AddRange(rightExpression.ShortList);

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(leftExpression.LongList);
      longList.AddRange(rightExpression.LongList);
    
      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                       falseSet = new HashSet<MiddleCode>();
      trueSet.Add(AddMiddleCode(longList, middleOp, null,
                                leftExpression.Symbol, rightExpression.Symbol));
      falseSet.Add(AddMiddleCode(longList, MiddleOperator.Jump));

      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, shortList, longList));
    }
    
    public static Expression ArithmeticExpression(MiddleOperator middleOp, Expression leftExpression,
                                                  Expression rightExpression) {
      Expression constantExpression =
        ConstantExpression.Arithmetic(middleOp, leftExpression, rightExpression);
      if (constantExpression != null) {
        return constantExpression;
      }

      Expression staticExpression =
        StaticExpression.Binary(middleOp, leftExpression, rightExpression);
      if (staticExpression != null) {
        return staticExpression;
      }

      Type leftType = leftExpression.Symbol.Type,
           rightType = rightExpression.Symbol.Type;

      switch (middleOp) {
        case MiddleOperator.Add:
          if (leftType.IsPointerArrayOrString()) {
            Error.Check((leftType.PointerArrayOrStringType.Size() > 0) &&
                        rightType.IsIntegral(),
                        null, Message.Invalid_arithmetic_expression);
          }
          else if (rightType.IsPointerArrayOrString()) {
            Error.Check(leftType.IsIntegral() &&
                        (rightType.PointerArrayOrStringType.Size() > 0),
                        null, Message.Invalid_arithmetic_expression);
          }
          else {
            Error.Check(leftType.IsArithmetic() && rightType.IsArithmetic(),
                        null, Message.Invalid_arithmetic_expression);
          }
          break;

        case MiddleOperator.Subtract:
          if (leftType.IsPointerArrayOrString() &&
              rightType.IsPointerArrayOrString()) {
            int leftSize = leftType.PointerArrayOrStringType.Size();
            Error.Check((leftSize > 0) && (leftSize ==
                         rightType.PointerArrayOrStringType.Size()),
                        null, Message.Invalid_arithmetic_expression);
          }
          else if (leftType.IsPointerArrayOrString()) {
            Error.Check((leftType.PointerArrayOrStringType.Size() > 0) &&
                        rightType.IsIntegral(), null,
                        Message.Invalid_arithmetic_expression);
          }
          else {
            Error.Check(leftType.IsArithmetic() && rightType.IsArithmetic(),
                        null, Message.Invalid_arithmetic_expression);
          }
          break;

        case MiddleOperator.Multiply:
        case MiddleOperator.Divide:
          Error.Check(leftType.IsArithmetic() && rightType.IsArithmetic(),
                      null, Message.Invalid_arithmetic_expression);
          break;

        default: // modulo, shift, and, or, xor
          Error.Check(leftType.IsIntegral() && rightType.IsIntegral(),
                      null, Message.Invalid_arithmetic_expression);
          break;
      }

      if (leftType.IsPointerArrayOrString() &&
          rightType.IsPointerArrayOrString()) {
        List<MiddleCode> shortList = new List<MiddleCode>();
        shortList.AddRange(leftExpression.ShortList);
        shortList.AddRange(rightExpression.ShortList);

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);

        Symbol subtractSymbol = new Symbol(leftType);
        AddMiddleCode(longList, MiddleOperator.Subtract, subtractSymbol,
                      leftExpression.Symbol, rightExpression.Symbol);
        Expression subtractExpression =
          new Expression(subtractSymbol, shortList, longList);
        Expression integerExpression =
          TypeCast.ExplicitCast(subtractExpression, Type.SignedIntegerType);

        int typeSize = leftType.PointerArrayOrStringType.Size();
        if (typeSize > 1) {
          Symbol sizeSymbol = new Symbol(Type.SignedIntegerType,
                                         new BigInteger(typeSize));
          return ArithmeticExpression(MiddleOperator.Divide, integerExpression,
                                      new Expression(sizeSymbol));
        }

        return integerExpression;
      }
      else {
        if (rightType.IsPointerArrayOrString()) {
          Expression tempExpression = leftExpression;
          leftExpression = rightExpression;
          rightExpression = tempExpression;
        }

        Symbol resultSymbol;

        if (leftType.IsPointerArrayOrString()) {
          int typeSize = leftExpression.Symbol.Type.PointerArrayOrStringType.Size();

          if (typeSize > 1) {
            Symbol sizeSymbol =
              new Symbol(rightExpression.Symbol.Type, new BigInteger(typeSize));
            rightExpression = ArithmeticExpression(MiddleOperator.Multiply, rightExpression,
                                                   new Expression(sizeSymbol));
          }

          rightExpression = TypeCast.ImplicitCast(rightExpression, leftType);
          resultSymbol = new Symbol(leftType);
        }
        else if (MiddleCode.IsShift(middleOp)) {
          rightExpression = TypeCast.ImplicitCast(rightExpression, Type.UnsignedCharType);
          resultSymbol = new Symbol(leftType);
        }
        else {
          Type maxType = TypeCast.MaxType(leftType, rightType);
          resultSymbol = new Symbol(maxType);
          leftExpression = TypeCast.ImplicitCast(leftExpression, maxType);
          rightExpression = TypeCast.ImplicitCast(rightExpression, maxType);
        }

        List<MiddleCode> shortList = new List<MiddleCode>();
        shortList.AddRange(leftExpression.ShortList);
        shortList.AddRange(rightExpression.ShortList);

        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(leftExpression.LongList);
        longList.AddRange(rightExpression.LongList);

        AddMiddleCode(longList, middleOp, resultSymbol,
                      leftExpression.Symbol, rightExpression.Symbol);
        return (new Expression(resultSymbol, shortList, longList));
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    public static Type TypeName(Specifier specifier, Declarator declarator)
    { Type specifierType = specifier.Type;

      if (declarator != null) {
        declarator.Add(specifierType);
        return declarator.Type;
      }
      else {
        return specifierType;
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Expression UnaryExpression(MiddleOperator middleOp,
                                             Expression expression) {
      expression = TypeCast.LogicalToIntegral(expression);
      if (middleOp == MiddleOperator.BitwiseNot) {
        Error.Check(expression.Symbol.Type.IsIntegral(),
                     expression, Message.Invalid_unary_expression);
      }
      else {
        Error.Check(expression.Symbol.Type.IsArithmetic(),
                     expression, Message.Invalid_unary_expression);
      }

      Expression constantExpression =
        ConstantExpression.Arithmetic(middleOp, expression);    
      if (constantExpression != null) {
        return constantExpression;
      }

      Symbol resultSymbol = new Symbol(expression.Symbol.Type);
      AddMiddleCode(expression.LongList, middleOp,
                    resultSymbol, expression.Symbol);
      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression LogicalNotExpression(Expression expression) {    
      Expression constantExpression =
        ConstantExpression.LogicalNot(expression);
      if (constantExpression != null) {
        return constantExpression;
      }

      expression = TypeCast.ToLogical(expression);
      Symbol notSymbol =
        new Symbol(expression.Symbol.FalseSet, expression.Symbol.TrueSet);
      return (new Expression(notSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression SizeOfType(Type type) {
      Error.Check(!type.IsVoid() && !type.IsFunction() &&
                   !type.IsBitfield(), type,
                   Message.Invalid_sizeof_expression);
      Symbol symbol =
        new Symbol(Type.SignedIntegerType, (BigInteger)type.Size());
      return (new Expression(symbol, new List<MiddleCode>(),
                             new List<MiddleCode>()));
    }

    public static Expression SizeOfExpression(Expression expression) {
      expression = TypeCast.LogicalToIntegral(expression);
      Error.Check(!expression.Symbol.IsRegister(),
                   expression, Message.Invalid_sizeof_expression);
      return SizeOfType(expression.Symbol.Type);
    }

    public static Expression AddressExpression(Expression expression) {
      Symbol symbol = expression.Symbol;
      Error.Check(!symbol.IsRegister() && !symbol.Type.IsBitfield(),
                   expression,  Message.Not_addressable);

      Expression staticExpression =
        StaticExpression.Unary(MiddleOperator.Address, expression);
      if (staticExpression!= null) {
        return staticExpression ;
      }
    
      if (expression.Symbol.Type.IsFloating()) {
        AddMiddleCode(expression.LongList, MiddleOperator.PopEmpty);
      }

      Symbol resultSymbol = new Symbol(new Type(expression.Symbol.Type));
      AddMiddleCode(expression.LongList, MiddleOperator.Address,
                    resultSymbol, expression.Symbol);
      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    /*
     *p = 1;
     a[1] = 2;
     a[i] = 3;
     s.i = 4;
     p->i = 5;
    */

    //int *p = &i;
    //int *p = &a[3];
    //int *p = a + 2;
    public static Expression DereferenceExpression(Expression expression) {
      Error.Check(expression.Symbol.Type.IsPointerArrayOrString(),
                   Message.Invalid_dereference_of_non__pointer);

      Expression staticExpression =
        StaticExpression.Unary(MiddleOperator.Dereference, expression);
      if (staticExpression != null) {
        return staticExpression;
      }

      Symbol resultSymbol =
        new Symbol(expression.Symbol.Type.PointerArrayOrStringType);
      resultSymbol.AddressSymbol = expression.Symbol;
      resultSymbol.AddressOffset = 0;
      AddMiddleCode(expression.LongList, MiddleOperator.Dereference,
                    resultSymbol, expression.Symbol, 0);

      if (resultSymbol.Type.IsFloating()) {
        AddMiddleCode(expression.LongList, MiddleOperator.PushFloat,
                      resultSymbol);
      }

      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    // p->m <=> (*p).m 
    public static Expression ArrowExpression(Expression expression,
                                             string memberName) {
      return DotExpression(DereferenceExpression(expression), memberName);
    }

    // a[i] <=> *(a + i)
    public static Expression IndexExpression(Expression leftExpression,
                                             Expression rightExpression) {
      Expression addExpression =
        ArithmeticExpression(MiddleOperator.Add, leftExpression, rightExpression);
      return DereferenceExpression(addExpression);
    }

    public static Expression DotExpression(Expression expression,
                                           string memberName) {
      Symbol parentSymbol = expression.Symbol;
      Error.Check(parentSymbol.Type.IsStructOrUnion(), expression,
                   Message.Not_a_struct_or_union_in_dot_expression);
      Error.Check(parentSymbol.Type.MemberMap != null, expression,
                   Message.Member_access_of_uncomplete_struct_or_union);
      Symbol memberSymbol;
      Error.Check(parentSymbol.Type.MemberMap.
                   TryGetValue(memberName, out memberSymbol),
                   memberName, Message.Unknown_member_in_dot_expression);

      string name = parentSymbol.Name + Symbol.SeparatorDot + memberSymbol.Name;
      Symbol resultSymbol = new Symbol(name, parentSymbol.ExternalLinkage,
                                       parentSymbol.Storage, memberSymbol.Type);

      /*if (parentSymbol.AddressSymbol != null) {
        resultSymbol = new Symbol(name, parentSymbol.ExternalLinkage,
                                  parentSymbol.Storage, memberSymbol.Type);
      }
      else {
        resultSymbol = new Symbol(memberSymbol.Type);
        resultSymbol.Name = parentSymbol.Name + Symbol.SeparatorDot + memberName;
        resultSymbol.Storage = parentSymbol.Storage;
      }*/

      resultSymbol.Parameter = parentSymbol.Parameter;
      resultSymbol.UniqueName = parentSymbol.UniqueName;
      resultSymbol.Offset = parentSymbol.Offset + memberSymbol.Offset;
      resultSymbol.AddressSymbol = parentSymbol.AddressSymbol;
      resultSymbol.AddressOffset = parentSymbol.AddressOffset + memberSymbol.Offset;

      /*resultSymbol.Assignable = !parentSymbol.Type.Constant &&
                                !memberSymbol.Type.IsConstantRecursive() &&
                                !memberSymbol.Type.IsArrayOrFunction();*/
      /*resultSymbol.Addressable = !parentSymbol.IsRegister() &&
                                  !memberSymbol.Type.IsBitfield();*/

      return (new Expression(resultSymbol, expression.ShortList,
                             expression.LongList));
    }

    public static Expression CastExpression(Type type, Expression expression) {
      Expression constantExpression = ConstantExpression.ConstantCast(expression, type);
      if (constantExpression != null) {
        return constantExpression;
      }
    
      return TypeCast.ExplicitCast(expression, type);
    }
  
    // ++i <=> i += 1
    // --i <=> i -= 1
    public static Expression PrefixIncrementExpression
                             (MiddleOperator middleOp, Expression expression){
      if (expression.Symbol.Type.IsFloating()) {
        Symbol oneSymbol = new Symbol(expression.Symbol.Type);
        List<MiddleCode> longList = new List<MiddleCode>();
        AddMiddleCode(longList, MiddleOperator.PushOne);
        Expression oneExpression = new Expression(oneSymbol, null, longList);
        return AssignmentExpression(middleOp, expression, oneExpression);
      }
      else {
        Symbol oneSymbol =
          new Symbol(expression.Symbol.Type, BigInteger.One);
        return AssignmentExpression(middleOp, expression,
                                    new Expression(oneSymbol));
      }
    }

    // i++ <=> i += 1
    // i-- <=> i -= 1
    public static Expression PostfixIncrementExpression
                             (MiddleOperator middleOp, Expression expression){
      List<MiddleCode> shortList = PrefixIncrementExpression(middleOp, expression).ShortList,
                       longList = new List<MiddleCode>();

      Symbol resultSymbol = new Symbol(expression.Symbol.Type);
      Expression resultExpression = new Expression(resultSymbol, longList, longList);

      if (expression.Symbol.Type.IsFloating()) {
        longList.AddRange(expression.LongList);
        AddMiddleCode(longList, MiddleOperator.PushFloat, expression.Symbol);
        AddMiddleCode(longList, MiddleOperator.PushOne);
        Symbol oneSymbol = new Symbol(expression.Symbol.Type, (decimal) 1);
        AddMiddleCode(longList, middleOp, expression.Symbol, expression.Symbol, oneSymbol);
        AddMiddleCode(longList, MiddleOperator.PopFloat, expression.Symbol);
      }
      else {
        longList.AddRange(Assignment(resultExpression, expression, false).ShortList);
        longList.AddRange(PrefixIncrementExpression(middleOp, expression).ShortList);
      }
    
      return resultExpression;
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Stack<List<Type>> m_typeListStack = new Stack<List<Type>>();
    public static Stack<int> m_parameterOffsetStack = new Stack<int>();

    public static void CallHeader(Expression expression) {
      Type type = expression.Symbol.Type;
      Error.Check(type.IsFunction() ||
                   type.IsPointer() && type.PointerType.IsFunction(),
                   expression.Symbol, Message.Not_a_function);
      Type functionType = type.IsFunction() ? type : type.PointerType;
      m_typeListStack.Push(functionType.TypeList);
      m_parameterOffsetStack.Push(0);

      AddMiddleCode(expression.LongList, MiddleOperator.PreCall,
                    SymbolTable.CurrentTable.CurrentOffset);
    }

    public static Expression CallExpression(Expression functionExpression,
                                            List<Expression> argumentList){
      m_typeListStack.Pop();
      m_parameterOffsetStack.Pop();

      int totalOffset = 0;
      foreach (int currentOffset in m_parameterOffsetStack) {
        if (currentOffset > 0) {
          totalOffset += (SymbolTable.FunctionHeaderSize + currentOffset);
        }
      }

      Type functionType = functionExpression.Symbol.Type.IsPointer() ?
                      functionExpression.Symbol.Type.PointerType :
                      functionExpression.Symbol.Type;

      List<Type> typeList = functionType.TypeList;
      Error.Check((typeList == null) ||
                   (argumentList.Count >= typeList.Count),
                   functionExpression,
                   Message.Too_few_actual_parameters_in_function_call);
      Error.Check(functionType.IsVariadic() || (typeList == null) ||
                   (argumentList.Count == typeList.Count),
                   functionExpression,
                   Message.Too_many_parameters_in_function_call);
    
      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(functionExpression.LongList);

      int index = 0, offset = SymbolTable.FunctionHeaderSize, extra = 0;
      foreach (Expression argumentExpression in argumentList) {
        Type parameterType;
        if ((typeList != null) && (index < typeList.Count)) {
          parameterType = typeList[index++];
        }
        else {
          parameterType = ParameterType(argumentExpression.Symbol);
          extra += parameterType.Size();
        }

        Expression parameterExpression = TypeCast.ImplicitCast(argumentExpression, parameterType);
        longList.AddRange(parameterExpression.LongList);

        if (parameterType.IsStructOrUnion()) {
          AddMiddleCode(longList, MiddleOperator.ParameterInitSize,
                        SymbolTable.CurrentTable.CurrentOffset + totalOffset +
                        offset, parameterType, parameterExpression.Symbol);
        }

        AddMiddleCode(longList, MiddleOperator.Parameter,
                      SymbolTable.CurrentTable.CurrentOffset + totalOffset +
                      offset, parameterType, parameterExpression.Symbol);
        offset += parameterType.Size();
      }

      Symbol functionSymbol = functionExpression.Symbol;
      AddMiddleCode(longList, MiddleOperator.Call,
                    SymbolTable.CurrentTable.CurrentOffset + totalOffset,
                    functionSymbol, extra);
      AddMiddleCode(longList, MiddleOperator.PostCall,
                    SymbolTable.CurrentTable.CurrentOffset + totalOffset);
    
      Type returnType = functionType.ReturnType;
      Symbol returnSymbol = new Symbol(returnType);
    
      List<MiddleCode> shortList = new List<MiddleCode>();
      shortList.AddRange(longList);
    
      if (!returnType.IsVoid()) {
        if (returnType.IsStructOrUnion()) {
          Type pointerType = new Type(returnType);
          Symbol addressSymbol = new Symbol(pointerType);
          returnSymbol.AddressSymbol = addressSymbol;
        }
      
        AddMiddleCode(longList, MiddleOperator.GetReturnValue,
                      returnSymbol);
      
        if (returnType.IsFloating()) {
          AddMiddleCode(shortList, MiddleOperator.PopEmpty);
        }
      }

      return (new Expression(returnSymbol, shortList, longList));
    }

    public static Expression ArgumentExpression(int index,
                                                Expression expression) {
      List<Type> typeList = m_typeListStack.Peek();

      if ((typeList != null) && (index < typeList.Count)) {
        expression = TypeCast.ImplicitCast(expression, typeList[index]);
      }
      else {
        expression = TypeCast.TypePromotion(expression);
      }

      int offset = m_parameterOffsetStack.Pop();
      m_parameterOffsetStack.Push(offset +
                                ParameterType(expression.Symbol).Size());
      return (new Expression(expression.Symbol, expression.LongList,
                             expression.LongList));
                                                }
 
    private static Type ParameterType(Symbol symbol) {
      switch (symbol.Type.Sort) {
        case Sort.Array:
          return (new Type(symbol.Type.ArrayType));

        case Sort.Function:
          return (new Type(symbol.Type));

        case Sort.String:
          return (new Type(new Type(Sort.SignedChar)));

        case Sort.Logical:
          return Type.SignedIntegerType;

        default:
          return symbol.Type;
      }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    public static Expression ValueExpression(Symbol symbol) {
      List<MiddleCode> longList = new List<MiddleCode>();

      if (symbol.Type.IsFloating()) {
        AddMiddleCode(longList, MiddleOperator.PushFloat, symbol);
      }
      /*else if (symbol.Type.IsString()) {
        StaticSymbol staticSymbol = ConstantExpression.Value(symbol.UniqueName, symbol.Type, symbol.Value);
        SymbolTable.StaticSet.Add(staticSymbol);
      }*/

      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }

    public static Expression NameExpression(string name) {
      Symbol symbol = SymbolTable.CurrentTable.LookupSymbol(name);
      Error.Check(symbol != null, name, Message.Unknown_name);

      /*if (symbol == null) {
        Type type = new Type(Type.SignedIntegerType, null, false);
        symbol = new Symbol(name, true, Storage.Extern, type);
        SymbolTable.CurrentTable.AddSymbol(symbol);
      }*/

      //symbol.Used = true;
      List<MiddleCode> shortList = new List<MiddleCode>(),
                       longList = new List<MiddleCode>(); 

      if (symbol.Type.IsFloating()) {
        //AddMiddleCode(shortList, MiddleOperator.PushFloat, symbol);
        AddMiddleCode(longList, MiddleOperator.PushFloat, symbol);
      }

      return (new Expression(symbol, shortList, longList));
    }

    public static Expression RegisterExpression(Register register) {
      List<MiddleCode> longList = new List<MiddleCode>();
      int size = AssemblyCode.SizeOfRegister(register);
      Type type = TypeSize.SizeToUnsignedType(size); 
      Symbol symbol = new Symbol(type);
      AddMiddleCode(longList, MiddleOperator.InspectRegister, symbol, register);
      return (new Expression(symbol, new List<MiddleCode>(), longList, register));
    }

    public static Expression CarryFlagExpression() {
      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                       falseSet = new HashSet<MiddleCode>();
      List<MiddleCode> longList = new List<MiddleCode>();
      trueSet.Add(AddMiddleCode(longList, MiddleOperator.Carry));
      falseSet.Add(AddMiddleCode(longList, MiddleOperator.Jump));
      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }

    public static Expression StackTopExpression() {
      List<MiddleCode> longList = new List<MiddleCode>();
      Symbol symbol = new Symbol(new Type(Type.UnsignedCharType));
      AddMiddleCode(longList, MiddleOperator.StackTop, symbol);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }
  }
}

/* 1. if a = b goto 2
   2. goto 3
   3. ...  
  
   a += b; => t = a + b; a = t;
 
 x += y;
  
 1. x += y
    
 1. t1 = x + y
 2. x = t1

 (i + 1) ? (s + 1) : (t + 1)
    
 1.  t1 = i + 1
 2.  if t1 != 0 goto 4
 3.  goto 6
 4.  t2 = s + 1
 5.  goto 10
 6.  t3 = t + 1
 7.  t4 = short_to_int t3
 8.  t5 = t4
 9.  goto 12
 10. t6 = short_to_int t2
 11. t5 = t6
 12. ...

 1.  t1 = i + 1
 2.  if t1 == 0 goto 7
 3.  t2 = s + 1
 4.  t3 = short_to_int t2
 5.  t4 = t3
 6.  goto 10
 7.  t5 = t + 1
 8.  t6 = short_to_int t5
 9.  t4 = t6
 10. ...
  
 1.  t1 = i + 1
 2.  if t1 == 0 goto 5
 3.  t2 = s + 1
 4.  goto 9
 5.  t3 = t + 1  
 6.  t4 = short_to_int t3
 7.  t5 = t4
 8.  goto 11
 9.  t6 = short_to_int t2
 10. t5 = t6
 11. ...

 1.  t1 = i + 1
 2.  if t1 != 0 goto 4
 3.  goto 8
 4.  t2 = s + 1
 5.  t3 = short_to_int t2
 6.  t4 = t3
 7.  goto 11
 8.  t5 = t + 1
 9.  t6 = short_to_int t5
 10. t4 = t6
 11. ...

 1.  t1 = i + 1
 2.  if t1 == 0 goto 8
 3.  nop
 4.  t2 = s + 1
 5.  t3 = short_to_int t2
 6.  t4 = t3
 7.  goto 11
 8.  t5 = t + 1
 9.  t6 = short_to_int t5
 10. t4 = t6
 11. ...

 1.  t1 = i + 1
 2.  if t1 == 0 goto 7, soft
 3.  t2 = s + 1
 4.  t3 = short_to_int t2
 5.  t4 = t3
 6.  goto 10, soft
 7.  t5 = t + 1
 8.  t6 = short_to_int t5
 9.  t4 = t6
 10. ...
*/