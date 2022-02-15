using System.Linq;
using System.Collections.Generic;

namespace CCompiler {
  public class Macro {
    private int m_parameters;
    private List<Token> m_tokenList;
    private IDictionary<int,int> m_indexToParamMap;
  
    public Macro(int parameters, List<Token> tokenList, IDictionary<int,int> indexToParamMap) {
      m_parameters = parameters;
      m_tokenList = new List<Token>(tokenList);
      m_indexToParamMap = indexToParamMap;
    }

    public int Parameters {
      get {return m_parameters;}
    }
  
    public List<Token> TokenList {
      get {return m_tokenList;}
    }

    public IDictionary<int,int> IndexToParamMap {
      get {return m_indexToParamMap;}
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }
  
    public override bool Equals(object obj) {
      if (obj is Macro) {
        Macro macro = (Macro) obj;
        return (m_parameters == macro.m_parameters) &&
               (m_tokenList.SequenceEqual(macro.m_tokenList));
      }

      return false;
    }
  }
}