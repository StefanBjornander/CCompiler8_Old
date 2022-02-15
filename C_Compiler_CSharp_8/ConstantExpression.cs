using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class ConstantExpression {
    public static bool IsConstant(Expression expression) {
      Symbol symbol = expression.Symbol;

      if (symbol.Type.IsLogical()) {
        return (symbol.TrueSet.Count == 0) ||
               (symbol.FalseSet.Count == 0);
      }
      else {
        return (symbol.Value is BigInteger) ||
               (symbol.Value is decimal);
      }
    }

    public static bool IsTrue(Expression expression) {
      Symbol symbol = expression.Symbol;

      if (symbol.Type.IsLogical()) {
        return (expression.Symbol.TrueSet.Count > 0);
      }
      else {
        return !symbol.Value.Equals(BigInteger.Zero) &&
               !symbol.Value.Equals((decimal) 0);
      }
    }

    private static Expression LogicalToIntegral(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return ConstantCast(expression, Type.SignedIntegerType);
      }

      return expression;
    }

    private static Expression LogicalToFloating(Expression expression) {
      if (expression.Symbol.Type.IsLogical()) {
        return ConstantCast(expression, Type.DoubleType);
      }

      return expression;
    }

    private static Expression ToLogical(Expression expression) {
      if (!expression.Symbol.Type.IsLogical()) {
        return ConstantCast(expression, Type.LogicalType);
      }

      return expression;
    }

    public static Expression Relation(MiddleOperator middleOp,
                                      Expression leftExpression,
                                      Expression rightExpression) {
      if (!IsConstant(leftExpression) || !IsConstant(rightExpression)) {
        return null;
      }

      leftExpression = LogicalToIntegral(leftExpression);
      rightExpression = LogicalToIntegral(rightExpression);
                
      Type leftType = leftExpression.Symbol.Type,
           rightType = rightExpression.Symbol.Type;

      object leftValue = leftExpression.Symbol.Value,
             rightValue = rightExpression.Symbol.Value;

      int compareTo = 0;
      if (leftType.IsFloating() || rightType.IsFloating()) {
        decimal leftDecimal, rightDecimal;

        if (leftValue is BigInteger) {
          leftDecimal = (decimal) ((BigInteger) leftValue);
          rightDecimal = (decimal) rightValue;
        }
        else if (rightValue is BigInteger) {
          leftDecimal = (decimal) leftValue;
          rightDecimal = (decimal) ((BigInteger) rightValue);
        }
        else {
          leftDecimal = (decimal) leftValue;
          rightDecimal = (decimal) rightValue;
        }

        compareTo = leftDecimal.CompareTo(rightDecimal);
      }
      else {
        BigInteger leftBigInteger = (BigInteger) leftExpression.Symbol.Value,
                   rightBigInteger = (BigInteger) rightExpression.Symbol.Value;
        compareTo = leftBigInteger.CompareTo(rightBigInteger);
      }

      bool resultValue = false;
      switch (middleOp) {
        case MiddleOperator.Equal:
          resultValue = (compareTo == 0);
          break;

        case MiddleOperator.NotEqual:
          resultValue = (compareTo != 0);
          break;

        case MiddleOperator.LessThan:
          resultValue = (compareTo < 0);
          break;

        case MiddleOperator.LessThanEqual:
          resultValue = (compareTo <= 0);
          break;

        case MiddleOperator.GreaterThan:
          resultValue = (compareTo > 0);
          break;

        case MiddleOperator.GreaterThanEqual:
          resultValue = (compareTo >= 0);
          break;
      }

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Jump);
      longList.Add(jumpCode);

      ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
      jumpSet.Add(jumpCode);

      Symbol resultSymbol = new Symbol(null, null);
      if (resultValue) {
        resultSymbol.TrueSet = jumpSet;
      }
      else {
        resultSymbol.FalseSet = jumpSet;
      }

      return (new Expression(resultSymbol, null, longList));
    }

    public static Expression Logical(MiddleOperator middleOp,
                                     Expression leftExpression,
                                     Expression rightExpression) {
      if (!IsConstant(leftExpression) || !IsConstant(rightExpression)) {
        return null;
      }

      bool resultValue = false;
      switch (middleOp) {
        case MiddleOperator.LogicalAnd:
          resultValue = IsTrue(leftExpression) && IsTrue(rightExpression);
          break;

        case MiddleOperator.LogicalOr:
          resultValue = IsTrue(leftExpression) || IsTrue(rightExpression);
          break;
      }

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCode jumpCode = new MiddleCode(MiddleOperator.Jump);
      longList.Add(jumpCode);

      ISet<MiddleCode> jumpSet = new HashSet<MiddleCode>();
      jumpSet.Add(jumpCode);

      Symbol resultSymbol = new Symbol(null, null);
      if (resultValue) {
        resultSymbol.TrueSet = jumpSet;
      }
      else {
        resultSymbol.FalseSet = jumpSet;
      }

      return (new Expression(resultSymbol, null, longList));
    }

    public static Expression Arithmetic(MiddleOperator middleOp,
                                        Expression leftExpression,
                                        Expression rightExpression) {
      if (!IsConstant(leftExpression) || !IsConstant(rightExpression)) {
        return null;
      }
      else if (leftExpression.Symbol.Type.IsFloating() ||
               rightExpression.Symbol.Type.IsFloating()) {
        return ArithmeticFloating(middleOp, leftExpression, rightExpression);
      }
      else {
        return ArithmeticIntegral(middleOp, leftExpression, rightExpression);
      }
    }

    private static Expression ArithmeticIntegral(MiddleOperator middleOp,
                                                 Expression leftExpression,
                                                 Expression rightExpression){
      leftExpression = LogicalToIntegral(leftExpression);
      rightExpression = LogicalToIntegral(rightExpression);
      Symbol symbol = ArithmeticIntegral(middleOp, leftExpression.Symbol,
                                         rightExpression.Symbol);
      return (new Expression(symbol, null, null));
    }

    public static Symbol ArithmeticIntegral(MiddleOperator middleOp,
                                            Symbol leftSymbol,
                                            Symbol rightSymbol) {
      Type leftType = leftSymbol.Type,
           rightType = rightSymbol.Type;

      BigInteger leftValue = (BigInteger) leftSymbol.Value,
                 rightValue = (BigInteger) rightSymbol.Value,
                 resultValue = 0;

      switch (middleOp) {
        case MiddleOperator.Add:
          if (leftType.IsPointerOrArray()) {
            resultValue = leftValue +
                          (rightValue * leftType.PointerOrArrayType.Size());
          }
          else if (leftType.IsPointerOrArray()) {
            resultValue = (leftValue * rightType.PointerOrArrayType.Size()) +
                          rightValue;
          }
          else {
            resultValue = leftValue + rightValue;
          }
          break;

        case MiddleOperator.Subtract:
          if (leftType.IsPointerOrArray() && rightType.IsPointerOrArray()) {
            resultValue = (leftValue - rightValue) /
                          leftType.PointerOrArrayType.Size();
          }
          else if (leftType.IsPointerOrArray()) {
            resultValue = leftValue -
                          (rightValue * leftType.PointerOrArrayType.Size());
          }
          else {
            resultValue = leftValue - rightValue;
          }
          break;

        case MiddleOperator.Multiply:
          resultValue = leftValue * rightValue;
          break;
        
        case MiddleOperator.Divide:
          resultValue = leftValue / rightValue;
          break;
        
        case MiddleOperator.Modulo:
          resultValue = leftValue % rightValue;
          break;

        case MiddleOperator.BitwiseOr:
          resultValue = leftValue | rightValue;
          break;
        
        case MiddleOperator.BitwiseXOr:
          resultValue = leftValue ^ rightValue;
          break;
        
        case MiddleOperator.BitwiseAnd:
          resultValue = leftValue & rightValue;
          break;

        case MiddleOperator.ShiftLeft:
          resultValue = leftValue << ((int) rightValue);
          break;
        
        case MiddleOperator.ShiftRight:
          resultValue = leftValue >> ((int) rightValue);
          break;
      }

      Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
      return (new Symbol(maxType, resultValue));
    }

    private static Expression ArithmeticFloating(MiddleOperator middleOp,
                                                 Expression leftExpression,
                                                 Expression rightExpression) {
      leftExpression = TypeCast.LogicalToFloating(leftExpression);
      rightExpression = TypeCast.LogicalToFloating(rightExpression);

      Type maxType = TypeCast.MaxType(leftExpression.Symbol.Type,
                                      rightExpression.Symbol.Type);
      leftExpression = ConstantCast(leftExpression, maxType);
      rightExpression = ConstantCast(rightExpression, maxType);

      decimal leftDecimal = (decimal) leftExpression.Symbol.Value,
              rightDecimal = (decimal) rightExpression.Symbol.Value,
              resultDecimal = 0;

      switch (middleOp) {
        case MiddleOperator.Add:
          resultDecimal = leftDecimal + rightDecimal;
          break;
        
        case MiddleOperator.Subtract:
          resultDecimal = leftDecimal - rightDecimal;
          break;
        
        case MiddleOperator.Multiply:
          resultDecimal = leftDecimal * rightDecimal;
          break;
        
        case MiddleOperator.Divide:
          resultDecimal = leftDecimal / rightDecimal;
          break;
      }

      Symbol resultSymbol = new Symbol(maxType, resultDecimal);
      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCodeGenerator.AddMiddleCode(longList, MiddleOperator.PushFloat,
                                        resultSymbol);
      return (new Expression(resultSymbol, null, longList));
    }

    public static Expression LogicalNot(Expression expression) {
      if (IsConstant(expression)) {
        expression = ToLogical(expression);
        Symbol resultSymbol =
          new Symbol(expression.Symbol.FalseSet, expression.Symbol.TrueSet);
        return (new Expression(resultSymbol, null, expression.LongList));
      }

      return null;
    }

    public static Expression Arithmetic(MiddleOperator middleOp,
                                        Expression expression) {
      if (!IsConstant(expression)) {
        return null;
      }
      else if (expression.Symbol.Type.IsFloating()) {
        return ArithmeticFloating(middleOp, expression);
      }
      else {
        return ArithmeticIntegral(middleOp, expression);
      }
    }

    private static Expression ArithmeticIntegral(MiddleOperator middleOp,
                                                 Expression expression) {
      expression = TypeCast.LogicalToIntegral(expression);
      BigInteger value = (BigInteger) expression.Symbol.Value;

      Symbol resultSymbol = null;
      switch (middleOp) {
        case MiddleOperator.Plus:
          resultSymbol = new Symbol(expression.Symbol.Type, value);
          break;

        case MiddleOperator.Minus:
          resultSymbol = new Symbol(expression.Symbol.Type, -value);
          break;

        case MiddleOperator.BitwiseNot:
          resultSymbol = new Symbol(expression.Symbol.Type, ~value);
          break;
      }

      return (new Expression(resultSymbol, null, null));
    }

    private static Expression ArithmeticFloating(MiddleOperator middleOp,
                                                 Expression expression) {
      expression = LogicalToFloating(expression);
      decimal value = (decimal) expression.Symbol.Value;
      Symbol resultSymbol = null;

      switch (middleOp) {
        case MiddleOperator.Plus:
          resultSymbol = new Symbol(expression.Symbol.Type, value);
          break;
        
        case MiddleOperator.Minus:
          resultSymbol = new Symbol(expression.Symbol.Type, -value);
          break;
      }

      List<MiddleCode> longList = new List<MiddleCode>();
      MiddleCodeGenerator.AddMiddleCode(longList, MiddleOperator.PushFloat,
                                        resultSymbol);
      return (new Expression(resultSymbol, null, longList));
    }

    public static Expression ConstantCast(Expression sourceExpression,
                                          Type targetType) {
      if (!IsConstant(sourceExpression)) {
        return null;
      }
      
      Symbol sourceSymbol = sourceExpression.Symbol, targetSymbol;
      Type sourceType = sourceSymbol.Type;
      object sourceValue = sourceSymbol.Value;
      List<MiddleCode> longList = new List<MiddleCode>();

      if (sourceType.IsIntegralOrPointer() && targetType.IsFloating()) {
        decimal targetValue = ((decimal) ((BigInteger) sourceValue));
        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsFloating() && targetType.IsIntegralOrPointer()) {
        BigInteger targetValue = ((BigInteger)((decimal)sourceValue));
        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsLogical() && targetType.IsArithmeticOrPointer()) {
        bool isTrue = (sourceSymbol.TrueSet.Count > 0);
        object targetValue;

        if (targetType.IsIntegralOrPointer()) {
          targetValue = isTrue ? BigInteger.One : BigInteger.Zero;
        }
        else {
          targetValue = isTrue ? decimal.One : decimal.Zero;
        }

        targetSymbol = new Symbol(targetType, targetValue);
      }
      else if (sourceType.IsArithmeticOrPointer() && targetType.IsLogical()) {
        bool isTrue = !sourceValue.Equals(BigInteger.Zero) &&
                      !sourceValue.Equals(decimal.Zero);

        MiddleCode gotoCode = new MiddleCode(MiddleOperator.Jump);
        longList.Add(gotoCode);

        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                         falseSet = new HashSet<MiddleCode>();
        if (isTrue) {
          trueSet.Add(gotoCode);
        }
        else {
          falseSet.Add(gotoCode);
        }

        targetSymbol = new Symbol(trueSet, falseSet);
      }
      else {
        targetSymbol = new Symbol(targetType, sourceValue);
      }

      if (targetType.IsFloating()) {
        longList.Add(new MiddleCode(MiddleOperator.PushFloat, targetSymbol));
      }

      return (new Expression(targetSymbol, null, longList));
    }
    
    public static StaticSymbol Value(Symbol symbol) {
      return Value(symbol.UniqueName, symbol.Type, symbol.Value);
    }

    public static StaticSymbol Value(string uniqueName, Type type,
                                     object value) {
      List<MiddleCode> middleCodeList;

      if (value is List<MiddleCode>) {
        middleCodeList = (List<MiddleCode>) value;
      }
      else {
        middleCodeList = new List<MiddleCode>();
      
        if (value != null) {
          middleCodeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                            type.Sort, value));
        }
        else {
          middleCodeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            type.Size()));
        }
      }

      List<AssemblyCode> assemblyCodeList = new List<AssemblyCode>();
      AssemblyCodeGenerator.GenerateAssembly(middleCodeList,
                                             assemblyCodeList);

      if (Start.Linux) {
        List<string> textList = new List<string>();
        textList.Add("section .data");
        textList.Add("\n" + uniqueName + ":");
        ISet<string> externSet = new HashSet<string>();
        AssemblyCodeGenerator.LinuxTextList(assemblyCodeList, textList,
                                            externSet);
        return (new StaticSymbolLinux(uniqueName, textList, externSet));
      }

      if (Start.Windows) {
        List<byte> byteList = new List<byte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        AssemblyCodeGenerator.
          GenerateTargetWindows(assemblyCodeList, byteList,
                                accessMap, null, null);
        return (new StaticSymbolWindows(uniqueName, byteList, accessMap));
      }
      
      return null;
    }
  }
}
