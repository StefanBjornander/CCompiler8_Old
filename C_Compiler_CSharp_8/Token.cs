using System.Text;

namespace CCompiler {
  public class Token {
    private CCompiler_Pre.Tokens m_id;
    private object m_value;
    private int m_newlineCount;
  
    public Token(CCompiler_Pre.Tokens id, object value) {
      m_id = id;
      m_value = value;
      m_newlineCount = CCompiler_Pre.Scanner.NewlineCount;
      CCompiler_Pre.Scanner.NewlineCount = 0;
    }

    public Token(CCompiler_Pre.Tokens id, object value, int newlineCount) {
      m_id = id;
      m_value = value;
      m_newlineCount = newlineCount;
    }

    public void AddNewlineCount(int newlineCount) {
      m_newlineCount += newlineCount;
    }  

    public object Clone() {
      Token token = new Token(m_id, m_value);
      token.m_newlineCount = 0;
      return token;
    }
  
    public CCompiler_Pre.Tokens Id {
      get { return m_id; }
      set { m_id = value; }
    }
  
    public object Value {
      get { return m_value; }
      set { m_value = value; }
    }
  
    public int GetNewlineCount() {
      return m_newlineCount;
    }
  
    public string ToNewlineString() {
      StringBuilder buffer = new StringBuilder();
      
      if (m_newlineCount > 0) {
        for (int count = 0; count < m_newlineCount; ++count) {
          buffer.Append('\n');
        }
      }
      else {
        buffer.Append(' ');
      }

      return buffer.ToString();
    }
  
    public void ClearNewlineCount() {
      m_newlineCount = 0;
    }
  
    public override int GetHashCode() {
      return base.GetHashCode();
    }
  
    public override bool Equals(object obj) {
      if (obj is Token) {
        Token token = (Token) obj;
        return (m_id == token.m_id) &&
                m_value.Equals(token.m_value);
      }
    
      return false;
    }
  
    public override string ToString() {
      switch (m_id) {
        case CCompiler_Pre.Tokens.NAME_WITH_PARENTHESES:
          return m_value.ToString() + " (";

        default:
          return m_value.ToString();
      }
    }
  }
}