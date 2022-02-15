using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  public class Linker {    
    public static string StackStart = Symbol.SeparatorId + "StackTop";
    private int m_totalSize = 256;
    private IDictionary<string,StaticSymbolWindows> m_globalMap =
      new Dictionary<string,StaticSymbolWindows>();
    private List<StaticSymbolWindows> m_globalList =
      new List<StaticSymbolWindows>();
    private IDictionary<string,int> m_addressMap =
      new Dictionary<string,int>();
  
    public void Add(StaticSymbol staticSymbol) {
      StaticSymbolWindows staticSymbolWindows = (StaticSymbolWindows) staticSymbol;
      string uniqueName = staticSymbolWindows.UniqueName;

      if (!m_globalMap.ContainsKey(uniqueName)) {
        m_globalMap.Add(uniqueName, staticSymbolWindows);
      }
      else {
        Error.Check(uniqueName.EndsWith(Symbol.NumberId),
                     SimpleName(uniqueName), Message.Duplicate_global_name);
      }
    }

    public void Generate(FileInfo targetFile) {
      Error.ErrorXXX(m_globalMap.ContainsKey(AssemblyCodeGenerator.InitializerName));
      StaticSymbolWindows initializerInfo = m_globalMap[AssemblyCodeGenerator.InitializerName];
      m_globalList.Add(initializerInfo);
      m_totalSize += initializerInfo.ByteList.Count;
      m_addressMap.Add(AssemblyCodeGenerator.InitializerName, 0);
      
      StaticSymbolWindows pathNameSymbol = null;
      if (m_globalMap.ContainsKey(AssemblyCodeGenerator.ArgsName)) {
        StaticSymbolWindows argsInfo =
          m_globalMap[AssemblyCodeGenerator.ArgsName];
        m_globalList.Add(argsInfo);
        Console.Out.WriteLine(argsInfo.UniqueName);
        m_totalSize += argsInfo.ByteList.Count;
        m_addressMap.Add(AssemblyCodeGenerator.ArgsName, 0);

        List<byte> byteList = new List<byte>();
        IDictionary<int, string> accessMap = new Dictionary<int, string>();
        pathNameSymbol = (StaticSymbolWindows)
          ConstantExpression.Value(AssemblyCodeGenerator.PathName,
                                   Type.StringType, @"C:\D\Main.com");
        m_globalMap.Add(AssemblyCodeGenerator.PathName, pathNameSymbol);
      }

      StaticSymbolWindows mainInfo;
      Error.Check(m_globalMap.TryGetValue("main", out mainInfo),
                   "non-static main", Message.Function_missing);
      GenerateTrace(mainInfo);
      
      if (pathNameSymbol != null) {
        Error.ErrorXXX(!m_globalList.Contains(pathNameSymbol));
        m_globalList.Add(pathNameSymbol);
        m_addressMap.Add(pathNameSymbol.UniqueName, m_totalSize);
        m_totalSize += (int) pathNameSymbol.ByteList.Count;
      }

      m_addressMap.Add(StackStart, m_totalSize);
    
      foreach (StaticSymbolWindows staticSymbol in m_globalList) {
        List<byte> byteList = staticSymbol.ByteList;
        int startAddress = m_addressMap[staticSymbol.UniqueName];
        GenerateAccess(staticSymbol.AccessMap, byteList);
        GenerateCall(startAddress, staticSymbol.CallMap, byteList);
        GenerateReturn(startAddress, staticSymbol.ReturnSet, byteList);
      }

      { Console.Out.WriteLine("Generating \"" + targetFile.FullName + "\".");
        targetFile.Delete();
        BinaryWriter targetStream =
          new BinaryWriter(File.OpenWrite(targetFile.FullName));

        foreach (StaticSymbolWindows staticSymbol in m_globalList) {
          foreach (sbyte b in staticSymbol.ByteList) {
            targetStream.Write(b);
          }
        }

        targetStream.Close();
      }
    }
 
    private void GenerateTrace(StaticSymbolWindows staticSymbol) {
      if (!m_globalList.Contains(staticSymbol)) {
        m_globalList.Add(staticSymbol);
        m_addressMap.Add(staticSymbol.UniqueName, m_totalSize);
        m_totalSize += (int) staticSymbol.ByteList.Count;
      
        ISet<string> accessNameSet =
          new HashSet<string>(staticSymbol.AccessMap.Values);
        foreach (string accessName in accessNameSet) {
          StaticSymbolWindows accessSymbol;
          Error.Check(m_globalMap.TryGetValue(accessName, out accessSymbol),
                       accessName, Message.Object_missing_in_linking);
          Error.ErrorXXX(accessSymbol != null);
          GenerateTrace(accessSymbol);
        }

        ISet<string> callNameSet =
          new HashSet<string>(staticSymbol.CallMap.Values);
        foreach (string callName in callNameSet) {
          StaticSymbolWindows funcSymbol;
          Error.Check(m_globalMap.TryGetValue(callName, out funcSymbol),
                       callName, Message.Function_missing_in_linking);
          Error.Check(funcSymbol != null, SimpleName(callName), 
                       Message.Missing_external_function);
          GenerateTrace(funcSymbol);
        }      
      }
    }

    private void GenerateAccess(IDictionary<int,string> accessMap,
                                List<byte> byteList) {
      foreach (KeyValuePair<int,string> entry in accessMap) {
        int sourceAddress = entry.Key;
        string name = entry.Value;
        byte lowByte = byteList[sourceAddress],
             highByte = byteList[sourceAddress + 1];
        int targetAddress = ((int) highByte << 8) + lowByte;
        targetAddress += m_addressMap[name];
        byteList[sourceAddress] = (byte) targetAddress;
        byteList[sourceAddress + 1] = (byte) (targetAddress >> 8);
      }
    }

/*    private void GenerateCallX(int callerStartAddress,
                              IDictionary<int,string> callMap,
                              List<byte> byteList) {
      foreach (KeyValuePair<int,string> entry in callMap) {
        int sourceAddress = entry.Key;
        string sourceName = entry.Value;
        int nextAddress = (sourceAddress + 2) + callerStartAddress;
        int calleeAddress = m_addressMap[sourceName];
        int relativeAddress = calleeAddress - nextAddress;
        byteList[sourceAddress] = (byte) ((sbyte) relativeAddress);
        byteList[sourceAddress + 1] = (byte) ((sbyte) (relativeAddress >> 8));
      }
    }*/
  
    private void GenerateReturn(int functionStartAddress, ISet<int> returnSet,
                                List<byte> byteList) {
      foreach (int sourceAddress in returnSet) {
        int lowByte = byteList[sourceAddress],
            highByte = byteList[sourceAddress + 1];
        int targetAddress = (highByte << 8) + lowByte;
        targetAddress += functionStartAddress;
        byteList[sourceAddress] = (byte) targetAddress;
        byteList[sourceAddress + 1] = (byte) (targetAddress >> 8);
      }
    }

    /*private void GenerateCallX(int startAddress, IDictionary<int, string> callMap,
                              List<byte> byteList) {
      const byte NopOperator = -112 + 256;
      const byte ShortJumpOperator = -21 + 256;

      foreach (KeyValuePair<int,string> entry in callMap) {
        int address = entry.Key;
        int callerAddress = startAddress + address + 2;
        int calleeAddress = m_addressMap[entry.Value];
        int relativeAddress = calleeAddress - callerAddress;

        if (relativeAddress == -129) {
          byteList[address - 1] = (byte) ShortJumpOperator;
          byteList[address] = (byte) (-128 + 256); // (byte)((sbyte)-128);
          byteList[address + 1] = (byte) NopOperator;
        }
        else if (relativeAddress >= -128) {
          byteList[address - 1] = (byte) NopOperator;
          byteList[address] = (byte) ShortJumpOperator;
          byteList[address + 1] = (byte) relativeAddress;
        }
        else {
          byteList[address] = (byte) ((sbyte) relativeAddress);
          byteList[address + 1] = (byte) ((sbyte) (relativeAddress >> 8));
        }
      }
    }*/

    private void GenerateCall(int startAddress,
                              IDictionary<int, string> callMap,
                              List<byte> byteList) {
      const byte NopOperator = -112 + 256;
      const byte ShortJumpOperator = -21 + 256;

      foreach (KeyValuePair<int,string> entry in callMap) {
        int address = entry.Key;
        int callerAddress = startAddress + address + 2;
        int calleeAddress = m_addressMap[entry.Value];
        int relativeAddress = calleeAddress - callerAddress;

        if (relativeAddress == -129) {
          byteList[address - 1] = (byte) ShortJumpOperator;
          byteList[address] = (byte) (-128 + 256); // (byte)((sbyte)-128);
          byteList[address + 1] = (byte) NopOperator;
        }
        else if ((relativeAddress >= -128) && // 12849
               ((((startAddress + address) <= 12849) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 11492) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11488) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11449) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11501) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11507) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 12525) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 12522) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) <= 12322) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11790) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11768) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11638) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 11540) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
               //((((startAddress + address) < 10812) && (relativeAddress <= 127)) || (relativeAddress <= 126))) {
          byteList[address - 1] = (byte) NopOperator;
          byteList[address] = (byte) ShortJumpOperator;
          byteList[address + 1] = (byte) relativeAddress;
        }
        else {
          byteList[address] = (byte) ((sbyte) relativeAddress);
          byteList[address + 1] = (byte) ((sbyte) (relativeAddress >> 8));
        }
      }
    }
    public static string SimpleName(string name) {
      int index = name.LastIndexOf(Symbol.SeparatorId);
      return ((index != -1) ? name.Substring(index + 1)
                            : name).Replace(Symbol.NumberId, "");
    }
  }
}