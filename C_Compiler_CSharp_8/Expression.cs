using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class Expression {
    private Symbol m_symbol;
    private List<MiddleCode> m_shortList;
    private List<MiddleCode> m_longList;
    private Register? m_register;
  
    public Expression(Symbol symbol, List<MiddleCode> shortList = null,
                      List<MiddleCode> longList = null, Register? register = null) {
      m_symbol = symbol;
      m_shortList = shortList ?? (new List<MiddleCode>());
      m_longList = longList ?? (new List<MiddleCode>());
      m_register = register;
    }
  
    public Symbol Symbol {
      get { return m_symbol; }
    }

    public List<MiddleCode> ShortList {
      get { return m_shortList; }
    }

    public List<MiddleCode> LongList {
      get { return m_longList; }
    }

    public Register? Register {
      get { return m_register; }
    }

    public override string ToString() {
      return m_symbol.ToString();
    }
  }
}