using System.IO;

namespace CCompiler {
  public abstract class StaticSymbol {
    private string m_uniqueName;

    public StaticSymbol() {
      // Empty.
    }

    public StaticSymbol(string uniqueName) {
      m_uniqueName = uniqueName;
    }

    public string UniqueName {
      get { return m_uniqueName; }
    }

    public override bool Equals(object obj) {
      if (obj is StaticSymbol) {
        StaticSymbol staticSymbol = (StaticSymbol) obj;
        return m_uniqueName.Equals(staticSymbol.m_uniqueName);
      }

      return false;
    }

    public override int GetHashCode() {
      return m_uniqueName.GetHashCode();
    }

    public virtual void Write(BinaryWriter outStream) {
      outStream.Write(m_uniqueName);
    }

    public virtual void Read(BinaryReader inStream) {
      m_uniqueName = inStream.ReadString();
    }
  }
}

    // global space:
    //   int i;        => external linkage
    //   extern int i; => external linkage
    //   static int i; => no external linkage
  
    //   int f(int i);        => external linkage
    //   extern int f(int i); => external linkage
    //   static int f(int i); => no external linkage

    //   int f(int i) {}        => external linkage
    //   extern int f(int i) {} => external linkage
    //   static int f(int i) {} => no external linkage

    //   int x = 1;        => external linkage
    //   static int x = 1; => external linkage
    //   extern int x = 1; => external linkage
  
    // int  f() {}
    // extern int  f() {}
    // static int  f() {}

    // int i;
    // extern int i;      
    // static int i;