namespace CCompiler {
  class IfElseChain {
    bool m_formerStatus, m_currentStatus, m_elseStatus;

    public IfElseChain(bool formerStatus, bool currentStatus, bool elseStatus){
      m_formerStatus = formerStatus;
      m_currentStatus = currentStatus;
      m_elseStatus = elseStatus;
    }

    public bool FormerStatus {
      get { return m_formerStatus; }
    }

    public bool CurrentStatus {
      get { return m_currentStatus; }
    }

    public bool ElseStatus {
      get { return m_elseStatus; }
    }
  }
}
