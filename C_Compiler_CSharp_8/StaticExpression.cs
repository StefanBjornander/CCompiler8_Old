using System.Numerics;

namespace CCompiler {
  public class StaticExpression {
    public static Expression Binary(MiddleOperator middleOp,
                                    Expression leftExpression,
                                    Expression rightExpression) {
      Symbol leftSymbol = leftExpression.Symbol,
             rightSymbol = rightExpression.Symbol;
      Type leftType = leftSymbol.Type, rightType = rightSymbol.Type;
      object leftValue = leftSymbol.Value, rightValue = rightSymbol.Value;

      switch (middleOp) {
        case MiddleOperator.Add:
          if ((leftValue is StaticAddress) && (rightValue is BigInteger)) { // &i + 2
            return GenerateAddition(leftSymbol, (BigInteger) rightValue);
          }
          else if ((leftValue is BigInteger) && (rightValue is StaticAddress)) {// 2 + &i
            return GenerateAddition(rightSymbol, (BigInteger) leftValue);
          }
          else if (leftSymbol.IsExternOrStaticArray() && (rightValue is BigInteger)) { // a + 2
            return GenerateAddition(leftSymbol, (BigInteger)rightValue);
          }
          else if ((leftValue is BigInteger) && rightSymbol.IsExternOrStaticArray()) { // 2 + a
            return GenerateAddition(rightSymbol, (BigInteger) leftValue);
          }
          break;

        case MiddleOperator.Subtract:
          if ((leftValue is StaticAddress) && (rightValue is BigInteger)) { // &i - 2
            return GenerateAddition(leftSymbol, -((BigInteger) rightValue));
          }
          if (leftSymbol.IsExternOrStaticArray() && (rightValue is BigInteger)) { // a - 2
            return GenerateAddition(leftSymbol, -((BigInteger) rightValue));
          }
          break;

        /*case MiddleOperator.Index:
          if ((leftValue is StaticAddress)  && (rightValue is BigInteger)) { // a[2]
            return GenerateIndex(leftSymbol, (BigInteger) rightValue);
          }
          else if ((leftValue is BigInteger) && (rightValue is StaticAddress)) {
            return GenerateIndex(rightSymbol, (BigInteger) leftValue);
          }
          else if (leftSymbol.IsExternOrStaticArray() && (rightValue is BigInteger)) { // a[2]
            return GenerateIndex(leftSymbol, (BigInteger) rightValue);
          }
          else if ((leftValue is BigInteger) && rightSymbol.IsExternOrStaticArray()) {
            return GenerateIndex(rightSymbol, (BigInteger) leftValue);
          }
          break;*/

        case MiddleOperator.Dot:
          if (leftSymbol.IsExternOrStatic()) {
            object resultValue =
              new StaticValue(leftSymbol.UniqueName,
                              rightSymbol.Offset); // s.i
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            return (new Expression(resultSymbol, null, null));
          }
          break;
      }
    
      return null;
    }  

    private static Expression GenerateAddition(Symbol symbol,
                                               BigInteger value) {
      int offset = ((int) value) * symbol.Type.PointerOrArrayType.Size();
      StaticAddress resultValue;

      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        resultValue = new StaticAddress(staticAddress.UniqueName,
                                        staticAddress.Offset + offset);
      }
      else {
        resultValue = new StaticAddress(symbol.UniqueName, offset);
      }

      Symbol resultSymbol = new Symbol(symbol.Type, resultValue);
      return (new Expression(resultSymbol, null, null));
    }

    /*private static Expression GenerateIndex(Symbol symbol,
                                            BigInteger value) {
      int offset = ((int) value) * symbol.Type.ArrayType.Size();
      StaticValue resultValue;

      if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        resultValue = new StaticValue(staticAddress.UniqueName,
                                      staticAddress.Offset + offset);
      }
      else {
        resultValue = new StaticValue(symbol.UniqueName, offset);
      }

      Symbol resultSymbol = new Symbol(symbol.Type, resultValue);
      return (new Expression(resultSymbol, null, null));
    }*/

    public static Expression Unary(MiddleOperator middleOp, Expression expression) {
      Symbol symbol = expression.Symbol;
    
      switch (middleOp) {
        case MiddleOperator.Address:
          if (symbol.Value is StaticValue) { // &a[i], &s.i
            StaticValue staticValue = (StaticValue) symbol.Value;
            StaticAddress staticAddress =
              new StaticAddress(staticValue.UniqueName, staticValue.Offset);
            Symbol resultSymbol =
              new Symbol(new Type(symbol.Type), staticAddress);
            return (new Expression(resultSymbol, null, null));
          }
          else if (symbol.IsExternOrStatic()) { // &i
            StaticAddress staticAddress =
              new StaticAddress(symbol.UniqueName, 0);
            Symbol resultSymbol =
              new Symbol(new Type(symbol.Type), staticAddress);
            return (new Expression(resultSymbol, null, null));
          }
          break;

        case MiddleOperator.Dereference:
          if (symbol.Value is StaticAddress) {
            StaticAddress staticAddress = (StaticAddress) symbol.Value;
            StaticValue staticValue =
              new StaticValue(staticAddress.UniqueName, staticAddress.Offset);
            Symbol resultSymbol =
              new Symbol(new Type(symbol.Type), staticValue);
            return (new Expression(resultSymbol, null, null));
          }
          break;
      }

      return null;
    }
  }
}