using System;
using System.IO;
using System.Collections.Generic;

namespace CCompiler {
  public class StaticSymbolLinux : StaticSymbol {
    private List<String> m_textList;
    private ISet<string> m_externSet;

    public StaticSymbolLinux(string uniqueName, List<string> textList,
                             ISet<string> externSet)
     :base(uniqueName) {
      m_textList = textList;
      m_externSet = externSet;
    }

    public List<string> TextList {
      get { return m_textList; }
    }

    public ISet<string> ExternSet {
      get { return m_externSet; }
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