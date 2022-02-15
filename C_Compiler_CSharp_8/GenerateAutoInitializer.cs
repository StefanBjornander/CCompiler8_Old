using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  class GenerateAutoInitializer {
    public static List<MiddleCode> GenerateAuto(Symbol toSymbol,
                                                object fromInitializer, int extraOffset,
                                                List<MiddleCode> codeList) {
      Type toType = toSymbol.Type;

      if (fromInitializer is Expression) {
        Expression fromExpression = (Expression) fromInitializer;

        if (toType.IsArray() && toType.ArrayType.IsChar() &&
            fromExpression.Symbol.Type.IsString()) {
          string text = ((string) fromExpression.Symbol.Value) + "\0";
          List<object> list = new List<object>();

          foreach (char c in text) {
            Symbol charSymbol =
              new Symbol(toType.ArrayType, (BigInteger) ((int) c));
            Expression charExpression = new Expression(charSymbol, null, null);
            list.Add(charExpression);
          }

          return GenerateAuto(toSymbol, list, extraOffset, codeList);
        }
        else {
          fromExpression = TypeCast.ImplicitCast(fromExpression, toType);

          foreach (MiddleCode middleCode in fromExpression.LongList) {
            switch (middleCode.Operator) {
              case MiddleOperator.PreCall:
              case MiddleOperator.ParameterInitSize:
              case MiddleOperator.Parameter:
              case MiddleOperator.Call:
              case MiddleOperator.PostCall:
                middleCode[0] = ((int) middleCode[0]) + extraOffset;
                break;
            }
          }

          codeList.AddRange(fromExpression.LongList);
      
          if (toSymbol.Type.IsFloating()) {
            codeList.Add(new MiddleCode(MiddleOperator.PopFloat, toSymbol));
          }
          else {
            if (fromExpression.Symbol.Type.IsStructOrUnion()) {
              codeList.Add(new MiddleCode(MiddleOperator.AssignInitSize,
                                          toSymbol, fromExpression.Symbol));
            }

            codeList.Add(new MiddleCode(MiddleOperator.Assign, toSymbol,
                                        fromExpression.Symbol));
          }
        }
      }
      else {
        List<object> fromList = (List<object>) fromInitializer;

        switch (toType.Sort) {
          case Sort.Array: {
              fromList = ModifyInitializer.ModifyArray(toType, fromList);

              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }
              else {
                Error.Check(fromList.Count <= toType.ArraySize,
                             toType, Message.Too_many_initializers_in_array);
              }

              for (int index = 0; index < fromList.Count; ++index) {
                Symbol indexSymbol = new Symbol(toType.ArrayType);
                indexSymbol.Offset = toSymbol.Offset +
                                    (index * toType.ArrayType.Size());
                indexSymbol.Name = toSymbol.Name + "[" + index + "]";
                GenerateAuto(indexSymbol, fromList[index], extraOffset, codeList);
                extraOffset += toType.ArrayType.Size();
              }
            }
            break;

          case Sort.Struct: {
            List<Symbol> memberList = toType.MemberList; 
              Error.Check(fromList.Count <= memberList.Count, toType,
                           Message.Too_many_initializers_in_struct);

              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberList[index];
                Symbol subSymbol = new Symbol(memberList[index].Type); 
                subSymbol.Name = toSymbol.Name + "." + memberSymbol.Name;
                subSymbol.Offset = toSymbol.Offset + memberSymbol.Offset;
                GenerateAuto(subSymbol, fromList[index], extraOffset, codeList);
                extraOffset += memberSymbol.Type.Size();
              }
            }
            break;

          case Sort.Union: {
              List<Symbol> memberList = toType.MemberList;
              Error.Check(fromList.Count == 1, toType,
                           Message.Only_one_Initlizer_allowed_in_unions);
              Symbol memberSymbol = memberList[0];
              Symbol subSymbol = new Symbol(memberSymbol.Type); 
              subSymbol.Name = toSymbol.Name + "." + memberSymbol.Name;
              subSymbol.Offset = toSymbol.Offset;
              GenerateAuto(subSymbol, fromList[0], extraOffset, codeList);
            }
            break;

          default:
            Error.Report(toType, Message.
                Only_array_struct_or_union_can_be_initialized_by_a_list);
            break;
        }
      }

      return codeList;
    }
  }
}