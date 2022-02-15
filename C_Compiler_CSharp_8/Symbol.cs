using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Symbol {
    public const string NumberId = "#";
    public const string TemporaryId = "£";
    public const string SeparatorId = "$";
    public const string SeparatorDot = ".";
    public const string FileMarker = "@";

    private string m_name, m_uniqueName;
    private bool m_externalLinkage;
    private Storage m_storage;
    private Type m_type;
    private object m_value;
    private int m_offset;
    private Symbol m_addressSymbol;
    private int m_addressOffset;
    private static int UniqueNameCount = 0, TemporaryNameCount = 0;

    public ISet<MiddleCode> TrueSet {get; set;}
    public ISet<MiddleCode> FalseSet {get; set;}

    public bool Parameter {get; set;}
    public bool InitializedEnum {get; set;}

    public Symbol(string name, bool externalLinkage, Storage storage,
                  Type type, object value = null) {
      m_name = name;
      m_externalLinkage = externalLinkage;
      m_storage = storage;
      m_type = type;

      if (m_externalLinkage) {
        m_uniqueName = m_name;
      }
      else {
        m_uniqueName = Symbol.FileMarker + (UniqueNameCount++) +
                       Symbol.SeparatorId + m_name;
      }

      m_value = CheckValue(m_type, value);
    }

    public Symbol(Type type) {
      m_name = Symbol.TemporaryId + "temporary" + (TemporaryNameCount++);
      m_storage = Storage.Auto;
      m_type = type;
    }

    public Symbol(ISet<MiddleCode> trueSet, ISet<MiddleCode> falseSet) {
      m_name = Symbol.TemporaryId + "logical" + (TemporaryNameCount++);
      m_storage = Storage.Auto;
      m_type = new Type(Sort.Logical);
      TrueSet = trueSet ?? (new HashSet<MiddleCode>());
      FalseSet = falseSet ?? (new HashSet<MiddleCode>());
    }

    public Symbol(Type type, object value) {
      Error.ErrorXXX(value != null);
      Error.ErrorXXX(!(value is bool));

      m_storage = Storage.Static;
      m_type = type;
      m_value = CheckValue(m_type, value);

      if (m_value is string) {
        string text = (string) m_value;
        m_name = "string_" + Slash.CharToHex(text) + Symbol.NumberId;
      }
      else if (m_value is StaticBase) {
        StaticBase staticBase = (StaticBase) m_value;
        m_name = m_value.GetType().Name + "_" + staticBase.UniqueName +
                 "_" + staticBase.Offset + Symbol.NumberId;
      }
      else if (m_type.IsFloating()) {
        m_name = "floating" + m_type.Size() + Symbol.SeparatorId
                 + m_value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }
      else {
        m_name = "integral" + m_type.SizeAddress() + Symbol.SeparatorId
                 + m_value.ToString().Replace("-", "minus") + Symbol.NumberId;
      }

      m_uniqueName = Symbol.FileMarker + (UniqueNameCount++) + m_name;
    }

    private static object CheckValue(Type type, object value) {
      if (value is BigInteger) {
        BigInteger bigValue = (BigInteger) value;

        if (type.IsUnsigned() && (bigValue < 0)) {
          bigValue += TypeSize.GetMaxValue(type.Sort) + 1;
        }

        Error.Check((bigValue >= TypeSize.GetMinValue(type.Sort)) &&
                     (bigValue <= TypeSize.GetMaxValue(type.Sort)),
                     type + ": " + value, Message.Value_overflow);
        return bigValue;
      }

      return value;
    }

    public string Name {
      get { return m_name; }
      set { m_name = value; }
    }

    public string UniqueName {
      get { return m_uniqueName; }
      set { m_uniqueName = value; }
    }

    public bool ExternalLinkage {
      get { return m_externalLinkage; }
    }

    public Storage Storage {
      get { return m_storage; }
      set { m_storage = value; }
    }

    public Type Type {
      get { return m_type; }
      set { m_type = value; }
    }

    public int Offset {
      get { return m_offset; }
      set { m_offset = value; }
    }

    public bool IsExtern() {
      return (m_storage == Storage.Extern);
    }
  
    public bool IsStatic() {
      return (m_storage == Storage.Static);
    }
  
    public bool IsExternOrStatic() {
      return IsExtern() || IsStatic();
    }
  
    public bool IsExternOrStaticArray() {
      return IsExternOrStatic() && m_type.IsArray();
    }

    public bool IsTypedef() {
      return (m_storage == Storage.Typedef);
    }
  
    public bool IsAuto() {
      return (m_storage == Storage.Auto);
    }

    public bool IsRegister() {
      return (m_storage == Storage.Register);
    }

    public bool IsAutoOrRegister() {
      return IsAuto() || IsRegister();
    }

    public object Value {
      get { return m_value; }
      set { m_value = value; }
    }
  
    public Symbol AddressSymbol {
      get { return m_addressSymbol; }
      set { m_addressSymbol = value; }
    }  

    public int AddressOffset {
      get { return m_addressOffset; }
      set { m_addressOffset = value; }
    }

    public bool IsValue() {
      return (m_name != null) && m_name.Contains(NumberId);
    }

    public bool IsTemporary() {
      return (m_name != null) && m_name.Contains(TemporaryId) &&
             (m_addressSymbol == null);             
    }

    public bool IsAssignable() {
      return !IsValue() && !m_type.IsConstantRecursive() &&
             !m_type.IsArrayFunctionOrString();
    }

    public override string ToString() {
      if (m_name != null) {
        return m_name;
      }
      else if (m_addressSymbol != null) {
        return m_addressSymbol.ToString();
      }

      return "";
    }

    /*public override string ToString() {
      if (m_value is String) {
        return "\"" + m_value.ToString().Replace("\n", "\\n") + "\"";
      }
      else if (m_value != null) {
        return m_value.ToString();
      }
      else if (m_name != null) {
        if (m_addressSymbol != null) {
          return m_name + " -> " + m_addressSymbol.ToString();
        }
        else {
          return m_name;
        }
      }
      else {
        if (m_addressSymbol != null) {
          return m_addressSymbol.ToString();
        }
        else {
          return "";
        }
      }
    }*/
  }
}

    /* global space:
       int i;        => external linkage
       extern int i; => external linkage
       static int i; => no external linkage
  
       int f(int i);        => external linkage
       extern int f(int i); => external linkage
       static int f(int i); => no external linkage

       int f(int i) {}        => external linkage
       extern int f(int i) {} => external linkage
       static int f(int i) {} => no external linkage

       int x = 1;        => external linkage
       static int x = 1; => external linkage
       extern int x = 1; => external linkage
  
       int  f() {}
       extern int  f() {}
       static int  f() {}

       int i;
       extern int i;      
       static int i;*/