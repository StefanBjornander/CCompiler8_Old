using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class StaticSymbolWindows : StaticSymbol {
    private List<byte> m_byteList;
    private IDictionary<int,string> m_accessMap, m_callMap;
    private ISet<int> m_returnSet;

    public StaticSymbolWindows() {
      // Empty.
    }

    public StaticSymbolWindows(string uniqueName, List<byte> byteList = null,
                        IDictionary<int,string> accessMap = null)
     :base(uniqueName) {
      m_byteList = byteList;
      m_accessMap = accessMap;
      m_callMap = new Dictionary<int,string>();
      m_returnSet = new HashSet<int>();
    }

    public StaticSymbolWindows(string uniqueName, List<byte> byteList, // Windows function definition
                        IDictionary<int,string> accessMap,
                        IDictionary<int,string> callMap, ISet<int> returnSet)
     :base(uniqueName) {
      m_byteList = byteList;
      m_accessMap = accessMap;
      m_callMap = callMap;
      m_returnSet = returnSet;
    }

    public List<byte> ByteList {
      get { return m_byteList; }
    }
  
    public IDictionary<int,string> AccessMap {
      get { return m_accessMap; }
    }
  
    public IDictionary<int,string> CallMap {
      get { return m_callMap; }
    }  
  
    public ISet<int> ReturnSet {
      get { return m_returnSet; }
    }
    
    public override void Write(BinaryWriter outStream) {
      base.Write(outStream);

      outStream.Write(m_byteList.Count);
      foreach (sbyte b in m_byteList) {
        outStream.Write(b);
      }

      outStream.Write(m_accessMap.Count);
      foreach (KeyValuePair<int,string> entry in m_accessMap) {
        outStream.Write(entry.Key);
        outStream.Write(entry.Value);
      }
    
      outStream.Write(m_callMap.Count);
      foreach (KeyValuePair<int,string> entry in m_callMap) {
        outStream.Write(entry.Key);
        outStream.Write(entry.Value);
      }

      outStream.Write(m_returnSet.Count);
      foreach (int address in m_returnSet) {
        outStream.Write(address);
      }
    }

    public override void Read(BinaryReader inStream) {
      base.Read(inStream);

      { m_byteList = new List<byte>();
        int byteListSize = inStream.ReadInt32();

        for (int index = 0; index < byteListSize; ++index) {
          byte b = inStream.ReadByte();
          m_byteList.Add(b);
        }
      }
    
      { m_accessMap = new Dictionary<int,string>();
        int accessMapSize = inStream.ReadInt32();
        for (int index = 0; index < accessMapSize; ++index) {
          int address = inStream.ReadInt32();
          string name = inStream.ReadString();
          m_accessMap.Add(address, name);
        }
      }
    
      { m_callMap = new Dictionary<int,string>();
        int callMapSize = inStream.ReadInt32();
        for (int index = 0; index < callMapSize; ++index) {
          int address = inStream.ReadInt32();
          string name = inStream.ReadString();
          m_callMap.Add(address, name);
        }
      }
    
      { m_returnSet = new HashSet<int>();
        int returnSetSize = inStream.ReadInt32();
        for (int index = 0; index < returnSetSize; ++index) {
          int address = inStream.ReadInt32();
          m_returnSet.Add(address);
        }
      }
    }

/*    public override int GetHashCode() {
      return m_uniqueName.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is StaticSymbolWindows) {
         StaticSymbolWindows s = (StaticSymbolWindows) obj;
         if ((m_accessMap.Count == 0) && (s.m_accessMap.Count == 0) &&
             (m_callMap.Count == 0) && (s.m_callMap.Count == 0) &&
             (m_returnSet.Count == 0) && (s.m_returnSet.Count == 0) &&
             (m_byteList.Count == s.m_byteList.Count)) {
          for (int index = 0; index < m_byteList.Count; ++index) {
            if (m_byteList[index] != s.m_byteList[index]) {
              return false;
            }
          }

          return true;
        }
      }
    }*/

    /*public override bool Equals(object obj) {
      if (obj is StaticSymbolWindows) {
         StaticSymbolWindows s = (StaticSymbolWindows) obj;

         if ((m_accessMap.Count == 0) && (s.m_accessMap.Count == 0) &&
             (m_callMap.Count == 0) && (s.m_callMap.Count == 0) &&
             (m_returnSet.Count == 0) && (s.m_returnSet.Count == 0) &&
             (m_byteList.Count == s.m_byteList.Count)) {
          for (int index = 0; index < m_byteList.Count; ++index) {
            if (m_byteList[index] != s.m_byteList[index]) {
              return false;
            }
          }

          return true;
        }
      }

      return false;
    }*/
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