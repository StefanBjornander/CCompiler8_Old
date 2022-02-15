namespace CCompiler {
  public class Pair<FirstType,SecondType> {
    private FirstType m_first;
    private SecondType m_second;

    public Pair(FirstType first, SecondType second) {
      m_first = first;
      m_second = second;
    }

    public void Add(FirstType first, SecondType second) {
      m_first = first;
      m_second = second;
    }

/*    public void Add(FirstType first) {
      m_first = first;
    }

    public void Add(SecondType second) {
      m_second = second;
    }*/

    public FirstType First {
      get { return m_first; }
      set { m_first = value; }
    }

    public SecondType Second {
      get { return m_second; }
      set { m_second = value; }
    }

    public override int GetHashCode() {
      return m_first.GetHashCode() + m_second.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is Pair<FirstType,SecondType>) {
        Pair<FirstType, SecondType> pair = (Pair<FirstType, SecondType>) obj;
        return m_first.Equals(pair.m_first) && m_second.Equals(m_second);
      }
    
      return false;
    }
  }
}