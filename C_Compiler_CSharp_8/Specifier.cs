using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  public class Specifier {
    private bool m_externalLinkage;
    private Storage? m_storage;
    private Type m_type;

    private static IDictionary<int, Sort> m_maskToSortMap =
      new Dictionary<int, Sort>() {  
        {(int) Mask.Void, Sort.Void},
        {(int) Mask.Char, Sort.SignedChar},
        {(int) Mask.SignedChar, Sort.SignedChar},
        {(int) Mask.UnsignedChar, Sort.UnsignedChar},
        {(int) Mask.Short, Sort.SignedShortInt},
        {(int) Mask.ShortInt, Sort.SignedShortInt},
        {(int) Mask.SignedShort, Sort.SignedShortInt},
        {(int) Mask.SignedShortInt, Sort.SignedShortInt},
        {(int) Mask.UnsignedShort, Sort.UnsignedShortInt},
        {(int) Mask.UnsignedShortInt, Sort.UnsignedShortInt},
        {(int) Mask.Int, Sort.SignedInt},
        {(int) Mask.Signed, Sort.SignedInt},
        {(int) Mask.SignedInt, Sort.SignedInt},
        {(int) Mask.Unsigned, Sort.UnsignedInt},
        {(int) Mask.UnsignedInt, Sort.UnsignedInt},
        {(int) Mask.Long, Sort.SignedLongInt},
        {(int) Mask.LongInt, Sort.SignedLongInt},
        {(int) Mask.SignedLong, Sort.SignedLongInt},
        {(int) Mask.SignedLongInt, Sort.SignedLongInt},
        {(int) Mask.UnsignedLong, Sort.UnsignedLongInt},
        {(int) Mask.UnsignedLongInt, Sort.UnsignedLongInt},
        {(int) Mask.Float, Sort.Float},
        {(int) Mask.Double, Sort.Double},
        {(int) Mask.LongDouble, Sort.LongDouble}
      };

    public Specifier(bool externalLinkage, Storage? storage, Type type) {
      m_externalLinkage = externalLinkage;
      m_storage = storage;
      m_type = type;
    }

    public bool ExternalLinkage {
      get { return m_externalLinkage; }
    }
 
    public Storage Storage {
      get { Error.ErrorXXX(m_storage != null);
            return m_storage.Value; }
    }
 
    public Type Type {
      get { return m_type; }
    }

    public static Specifier SpecifierList(List<object> specifierList) {
      int totalMaskValue = 0;
      Type compoundType = null;
    
      foreach (object obj in specifierList) {
        if (obj is Mask) {
          int maskValue = (int) obj;

          if ((maskValue & totalMaskValue) != 0) {
            Error.Report(MaskToString(maskValue),
                         Message.Keyword_defined_twice);
          }

          totalMaskValue |= maskValue;
        }
        else {
          if (compoundType != null) {
            Error.Report(MaskToString(totalMaskValue),
                         Message.Invalid_specifier_sequence);
          }

          compoundType = (Type) obj;
        }
      }

      Storage? storage = null;
      { int totalStorageValue = totalMaskValue & ((int) Mask.StorageMask);

        if (totalStorageValue != 0) {
          Error.Check(Enum.IsDefined(typeof(Mask), totalStorageValue),
                       MaskToString(totalStorageValue),
                       Message.Invalid_specifier_sequence);
          storage = (Storage) totalStorageValue;
        }
      }

      bool externalLinkage = (SymbolTable.CurrentTable.Scope == Scope.Global)
                             && ((storage == null) ||
                                 (storage == CCompiler.Storage.Extern));

      if (storage == null) {
        if (SymbolTable.CurrentTable.Scope == Scope.Global) {
          storage = Storage.Static;
        }
        else {
          storage = Storage.Auto;
        }
      }

      if (SymbolTable.CurrentTable.Scope == Scope.Parameter) {
        Error.Check((storage == Storage.Auto) || 
                     (storage == Storage.Register), storage, Message.
            Only_auto_or_register_storage_allowed_in_parameter_declaration);
      }
      else if ((SymbolTable.CurrentTable.Scope == Scope.Struct) ||
               (SymbolTable.CurrentTable.Scope == Scope.Union)) {
          Error.Check((storage == Storage.Auto) ||
                       (storage == Storage.Register), storage, Message.
            Only_auto_or_register_storage_allowed_for_struct_or_union_scope);  
      }
      else if (SymbolTable.CurrentTable.Scope == Scope.Global) {
          Error.Check((storage == Storage.Extern) ||
                       (storage == Storage.Static) ||
                       (storage == Storage.Typedef), storage, Message.
        Only_extern____static____or_typedef_storage_allowed_in_global_scope);
      }

      if ((compoundType != null) && (compoundType.IsEnumerator())) {        
        if (storage == Storage.Typedef) {
          compoundType = Type.SignedIntegerType;
          storage = (SymbolTable.CurrentTable.Scope == Scope.Global)
                    ? Storage.Static : Storage.Auto;
        }

        foreach (Symbol itemSymbol in compoundType.EnumeratorItemSet){
          itemSymbol.Storage = storage.Value;

          switch (itemSymbol.Storage) {
            case CCompiler.Storage.Static:
              SymbolTable.StaticSet.Add(ConstantExpression.
                                         Value(itemSymbol));
              break;

            case CCompiler.Storage.Auto:
            case CCompiler.Storage.Register:
              SymbolTable.CurrentTable.SetOffset(itemSymbol);
              break;

            case CCompiler.Storage.Extern: {
                Error.Check(!itemSymbol.InitializedEnum,
                              itemSymbol + " = " + itemSymbol.Value,
                  Message.Extern_enumeration_item_cannot_be_initialized);
              }
              break;
          }
        }
      }

      { bool isConstant = (totalMaskValue & ((int) Mask.Constant)) != 0;
        bool isVolatile = (totalMaskValue & ((int) Mask.Volatile)) != 0;

        if ((isConstant || isVolatile) && (compoundType != null) &&
             compoundType.IsStructOrUnion() /*&& compoundType.HasTag()*/) {
          compoundType = new Type(compoundType.Sort, compoundType.MemberMap,
                                  compoundType.MemberList);
        }

        Sort? sort = null;
        int sortMaskValue = totalMaskValue & ((int) Mask.SortMask);

        if (sortMaskValue != 0) {
          if (!m_maskToSortMap.ContainsKey(sortMaskValue)) {
            Error.Report(MaskToString(sortMaskValue),
                         Message.Invalid_specifier_sequence);
          }

          sort = m_maskToSortMap[sortMaskValue];
        }

        Type type = null;
        if ((compoundType != null) && (sort == null)) {
          compoundType.Constant = (compoundType.Constant || isConstant);
          compoundType.Volatile = (compoundType.Volatile || isVolatile);
          type = compoundType;
        }
        else if ((compoundType == null) && (sort != null)) {
          type = new Type(sort.Value);
          type.Constant = isConstant;
          type.Volatile = isVolatile;
        }
        else if ((compoundType == null) && (sort == null)) {
          type = new Type(Sort.SignedInt);
          type.Constant = isConstant;
          type.Volatile = isVolatile;
        }
        else {
          Error.Report(MaskToString((int) sortMaskValue), Message.
                       Invalid_specifier_sequence_together_with_type);
        }

        if (type.IsFunction()) {
          storage = Storage.Extern;
        }

        return (new Specifier(externalLinkage, storage, type));
      }
    }

    public static Type QualifierList(List<Mask> qualifierList) {
      int totalMaskValue = 0;
    
      foreach (Mask mask in qualifierList) {
        int maskValue = (int) mask;

        if ((maskValue & totalMaskValue) != 0) {
          Error.Report(MaskToString(maskValue),
                       Message.Keyword_defined_twice);
        }

        totalMaskValue |= maskValue;
      }

      Type type = Type.VoidPointerType;
      type.Constant = (totalMaskValue & ((int) Mask.Constant)) != 0;
      type.Volatile = (totalMaskValue & ((int) Mask.Volatile)) != 0; 
      return type;
    }
 
    private static string MaskToString(int totalMaskValue) {
      StringBuilder maskBuffer = new StringBuilder();

      for (int maskValue = 1; maskValue != 0; maskValue <<= 1) {
        if ((maskValue & totalMaskValue) != 0) {
          string maskName = Enum.GetName(typeof(Mask), maskValue).ToLower();
          maskBuffer.Append(((maskBuffer.Length > 0) ? " " : "") + maskName);
        }
      }

      return maskBuffer.ToString();
    }
  }
}
