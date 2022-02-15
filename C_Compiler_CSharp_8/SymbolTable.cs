using System;
using System.Collections.Generic;

namespace CCompiler {
  public class SymbolTable {
    private Scope m_scope;

    public static int ReturnAddressOffset = 0;
    public static int RegularFrameOffset = TypeSize.PointerSize;
    public static int VariadicFrameOffset = 2 * TypeSize.PointerSize;
    public static int FunctionHeaderSize = 3 * TypeSize.PointerSize;

    private SymbolTable m_parentTable;
    private int m_currentOffset;

    private IDictionary<string,Symbol> m_entryMap =
      new Dictionary<string,Symbol>();
    private List<Symbol> m_entryList = new List<Symbol>();
    private IDictionary<string, Type> m_tagMap = new Dictionary<string, Type>();

    public static SymbolTable CurrentTable = null;
    public static Symbol CurrentFunction = null;
    public static ISet<StaticSymbol> StaticSet;
    public static StaticSymbolLinux InitSymbol, ArgsSymbol;

    public SymbolTable(SymbolTable parentTable, Scope scope) {
      m_parentTable = parentTable;
    
      switch (m_scope = scope) {
        case Scope.Global:
          StaticSet = new HashSet<StaticSymbol>();
          InitSymbol = ArgsSymbol = null; 
          break;

        case Scope.Struct:
        case Scope.Union:
          m_currentOffset = 0;
          break;

        case Scope.Function:
          m_currentOffset = FunctionHeaderSize;
          break;

        case Scope.Block:
          m_currentOffset = m_parentTable.m_currentOffset;
          break;
      }
    }

    public Scope Scope {
      get { return m_scope; }
      set { m_scope = value; }
    }

    public SymbolTable ParentTable {
      get { return m_parentTable; }
    }

    public IDictionary<string,Symbol> EntryMap {
      get { return m_entryMap; }
    }

    public List<Symbol> EntryList {
      get { return m_entryList; }
    }

    public int CurrentOffset {
      get { return m_currentOffset; }
      set { m_currentOffset = value; }
    }

    public void AddSymbol(Symbol newSymbol, bool increaseOffset = true) {
      string name = newSymbol.Name;

      if (name != null) {
        Symbol oldSymbol;

        if (m_entryMap.TryGetValue(name, out oldSymbol)) {
          Error.Check(oldSymbol.IsExtern() || newSymbol.IsExtern(),
                       name, Message.Name_already_defined);
          Error.Check(oldSymbol.Type.Equals(newSymbol.Type),
                       name, Message.Different_types_in_redeclaration);

          newSymbol.UniqueName = oldSymbol.UniqueName;

          if (!newSymbol.IsExtern()) {
            m_entryMap[name] = newSymbol;
          }
        }
        else {
          m_entryMap[name] = newSymbol;
          m_entryList.Add(newSymbol);
        }
      }

      if (!newSymbol.Type.IsFunction()) {
        if (newSymbol.IsAutoOrRegister()) {
          if (m_scope == Scope.Union) {
            newSymbol.Offset = 0;
          }
          else if (!newSymbol.Type.IsEnumerator()) {
            newSymbol.Offset = m_currentOffset;

            if (increaseOffset) {
              m_currentOffset += newSymbol.Type.Size();
            }
          }
        }
      }
    }

    public void SetOffset(Symbol symbol) {
      symbol.Offset = m_currentOffset;
      m_currentOffset += symbol.Type.Size();
    }

    public Symbol LookupSymbol(string name) {
      Symbol symbol;

      if (m_entryMap.TryGetValue(name, out symbol)) {
        return symbol;
      }
      else if (m_parentTable != null) {
        return m_parentTable.LookupSymbol(name);
      }

      return null;
    }

    public Type AddTag(string name, Type newType) {
      Type oldType;

      if (m_tagMap.TryGetValue(name, out oldType)) {
        Error.Check(!oldType.IsEnumerator() &&
                     (oldType.Sort == newType.Sort), name,
                     Message.Name_already_defined);

        if (oldType.MemberMap == null) {
          oldType.MemberMap = newType.MemberMap;
          oldType.MemberList = newType.MemberList;
        }
        else {
          Error.Check(newType.MemberMap == null, name,
                       Message.Name_already_defined);
        }

        return oldType;
      }
      else {
        m_tagMap.Add(name, newType);
        return newType;
      }
    }

    public Type LookupTag(string name, Sort sort) {
      Type type;

      if (m_tagMap.TryGetValue(name, out type) && (type.Sort == sort)) {
        return type;
      }
      else if (m_parentTable != null) {
        return m_parentTable.LookupTag(name, sort);
      }

      return null;
    }
  }
}