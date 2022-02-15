using System.Collections.Generic;

namespace CCompiler {
  public class Statement {
    private List<MiddleCode> m_codeList;
    private ISet<MiddleCode> m_nextSet;
  
    public Statement(List<MiddleCode> codeList = null,
                     ISet<MiddleCode> nextSet = null) {
      m_codeList = codeList ?? (new List<MiddleCode>());
      m_nextSet = nextSet ?? (new HashSet<MiddleCode>());
    }
  
    public List<MiddleCode> CodeList {
      get { return m_codeList; }
    }
  
    public ISet<MiddleCode> NextSet {
      get { return m_nextSet; }
    }
  }
}
