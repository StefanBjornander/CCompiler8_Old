using System.Collections.Generic;

namespace CCompiler {
  public class GenerateStaticInitializer {
    public static List<MiddleCode> GenerateStatic(Type toType,
                                                  object fromInitializer) {
      List<MiddleCode> codeList = new List<MiddleCode>();

      if (fromInitializer is Expression) {
        Expression fromExpression = (Expression) fromInitializer;
        Symbol fromSymbol = fromExpression.Symbol;
        Error.Check(fromSymbol.IsExternOrStatic(), fromSymbol,
                     Message.Non__static_initializer);
        Type fromType = fromSymbol.Type;

        if (toType.IsArray() && toType.ArrayType.IsChar() &&
            fromType.IsString()) {
          string text = (string) fromSymbol.Value;
 
          if (toType.ArraySize == 0) {
            toType.ArraySize = text.Length + 1;
          }
          else {
            Error.Check(text.Length < toType.ArraySize, toType,
                         Message.Too_many_initializers_in_array);
          }

          codeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                      fromSymbol.Type.Sort, text));
        }
        else if (toType.IsPointer() && fromType.IsArrayFunctionOrString()) {
          Error.Check((fromType.IsString() && toType.PointerType.IsChar()) ||
                       (fromType.IsArray() &&
                        fromType.ArrayType.Equals(toType.PointerType)) ||
                       (fromType.IsFunction() &&
                        fromType.Equals(toType.PointerType)),
                       Message.Invalid_type_cast);
                    StaticAddress staticAddress =
            new StaticAddress(fromSymbol.UniqueName, 0);
          codeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                      toType.Sort, staticAddress));
        }
        else {
          Expression toExpression =
            TypeCast.ImplicitCast(fromExpression, toType);
          Symbol toSymbol = toExpression.Symbol;
          Error.Check(toSymbol.Value != null, toSymbol,
                       Message.Non__constant_expression);
          codeList.Add(new MiddleCode(MiddleOperator.Initializer,
                                      toSymbol.Type.Sort, toSymbol.Value));
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

              foreach (object value in fromList) {
                codeList.AddRange(GenerateStatic(toType.ArrayType, value));
              }

              int restSize = toType.Size() -
                             (fromList.Count * toType.ArrayType.Size());
              if (restSize > 0) {
                codeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            restSize));
              }
            }
            break;
          
          case Sort.Struct: {
              List<Symbol> memberList = toType.MemberList;
              Error.Check(fromList.Count <= memberList.Count, toType,
                           Message.Too_many_initializers_in_struct);

              int initSize = 0;
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberList[index];
                codeList.AddRange(GenerateStatic(memberSymbol.Type,
                                                 fromList[index]));
                initSize += memberSymbol.Type.Size();
              }

              int restSize = toType.Size() - initSize;
              if (restSize > 0) {
                codeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            restSize));
              }
            }
            break;
          
          case Sort.Union: {
              List<Symbol> memberList = toType.MemberList;
              Error.Check(fromList.Count == 1, toType,
                           Message.Only_one_Initlizer_allowed_in_unions);

              Symbol memberSymbol = memberList[0];
              codeList.AddRange(GenerateStatic(memberSymbol.Type,
                                               fromList[0]));

              int restSize = toType.Size() - memberSymbol.Type.Size();
              if (restSize > 0) {
                codeList.Add(new MiddleCode(MiddleOperator.InitializerZero,
                                            restSize));
              }
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
