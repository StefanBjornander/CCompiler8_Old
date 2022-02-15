// -rebuild -print Main Malloc CType ErrNo Locale Math SetJmp Signal File Temp Scanf Printf StdLib Time String PrintTest CharacterTest FloatTest LimitsTest AssertTest StringTest LocaleTest SetJmpTest MathTest FileTest StdIOTest SignalTest StackTest MallocTest StdLibTest TimeTest

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Start {
    public static bool Linux = false, Windows;
    public static string SourcePath = @"C:\Users\Stefa\Documents\vagrant\homestead\code\code\",
                         TargetPath = @"C:\D\";

    public static void Main(string[] args) {
      Windows = !Linux;

      if (Start.Windows) {
        ObjectCodeTable.Initializer();
      }

      System.Threading.Thread.CurrentThread.CurrentCulture =
        CultureInfo.InvariantCulture;

      Error.Check(args.Length > 0, Message.Usage_compiler_filename);

      List<string> argList = new List<string>(args);
      bool rebuild = argList.Remove("-rebuild"),
           print = argList.Remove("-print");

      try {
        if (Start.Linux) {
          foreach (string arg in argList) {
            FileInfo file = new FileInfo(SourcePath + arg);

            if (rebuild || !IsGeneratedFileUpToDate(file, ".asm")) {
              if (print) {
                Console.Out.WriteLine("Compiling \"" +
                                      file.FullName + ".c\".");  
              }

              CompileSourceFile(file);
            }
          }

          GenerateMakeFile(argList);
        }

        if (Start.Windows) {
          bool doLink = false;

          foreach (string arg in argList) {
            FileInfo file = new FileInfo(SourcePath + arg);

            if (rebuild || !IsGeneratedFileUpToDate(file, ".obj")) {
              if (print) {
                Console.Out.WriteLine("Compiling \"" + file.FullName + ".c\"."); 
              }

              CompileSourceFile(file);
              doLink = true;
            }
          }

          if (doLink) {
            FileInfo targetFile =
              new FileInfo(TargetPath + argList[0] + ".com");
            Linker linker = new Linker();

            CCompiler_Main.Scanner.Path = null;
            foreach (string arg in argList) {
              FileInfo file = new FileInfo(SourcePath + arg);

              if (print) {
                Console.Out.WriteLine("Loading \"" + file.FullName +
                                      ".obj\".");  
              }
          
              ReadObjectFile(file, linker);
            }

            linker.Generate(targetFile);
          }
          else if (print) {
            Console.Out.WriteLine(SourcePath + argList[0] +
                                  ".com is up-to-date.");
          }
        }
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Error.Report(exception.Message, Message.Parse_error);
      }
    }

    private static void GenerateMakeFile(List<string> argList) {
      StreamWriter makeStream = new StreamWriter(SourcePath + "makefile");

      makeStream.Write("main:");
      foreach (string arg in argList) {
        makeStream.Write(" " + arg.ToLower() + ".o");
      }
      makeStream.WriteLine();

      makeStream.Write("\tld -o main");
      foreach (string arg in argList) {
        makeStream.Write(" " + arg.ToLower() + ".o");
      }
      makeStream.WriteLine();
      makeStream.WriteLine();

      foreach (string arg in argList) {
        makeStream.WriteLine(arg.ToLower() + ".o: " + arg.ToLower() + ".asm");
        makeStream.WriteLine("\tnasm -f elf64 -o " + arg.ToLower() + ".o "
                           + arg.ToLower() + ".asm");
        makeStream.WriteLine();
      }

      makeStream.WriteLine("clear:");
      foreach (string arg in argList) {
        makeStream.WriteLine("\trm " + arg.ToLower() + ".o");
      }

      makeStream.WriteLine("\trm main");
      makeStream.Close();
    }

    public static void ReadObjectFile(FileInfo file, Linker linker) {
      FileInfo objectFile = new FileInfo(file.FullName + ".obj");

      try {
        BinaryReader dataInputStream =
          new BinaryReader(File.OpenRead(objectFile.FullName));

        int linkerSetSize = dataInputStream.ReadInt32();
        for (int count = 0; count < linkerSetSize; ++count) {
          StaticSymbolWindows staticSymbol = new StaticSymbolWindows();
          staticSymbol.Read(dataInputStream);
          linker.Add(staticSymbol);
        }

        dataInputStream.Close();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Error.Report(exception.Message);      
      }
    }
  
    public static void CompileSourceFile(FileInfo file) {
      FileInfo sourceFile = new FileInfo(file.FullName + ".c");
      Preprocessor preprocessor = new Preprocessor(sourceFile);
      GenerateIncludeFile(file, preprocessor.IncludeSet);

      FileInfo pFile = new FileInfo(file.FullName + ".p");
      StreamWriter pWriter = new StreamWriter(pFile.FullName);
      pWriter.Write(preprocessor.PreprocessedCode);
      pWriter.Close();

      byte[] byteArray =
        Encoding.ASCII.GetBytes(preprocessor.PreprocessedCode);
      MemoryStream memoryStream = new MemoryStream(byteArray);
      CCompiler_Main.Scanner scanner =
        new CCompiler_Main.Scanner(memoryStream);

      try {
        SymbolTable.CurrentTable = new SymbolTable(null, Scope.Global);
        //CCompiler_Main.Scanner.Path = sourceFile;
        CCompiler_Main.Scanner.Line = 2000; 
        CCompiler_Main.Parser parser = new CCompiler_Main.Parser(scanner);
        Error.Check(parser.Parse(), Message.Syntax_error);
      }
      catch (IOException ioException) {
        Error.Check(false, ioException.StackTrace, Message.Syntax_error);
      }

      if (Start.Linux) {
        ISet<string> totalExternSet = new HashSet<string>();

        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          StaticSymbolLinux staticSymbolLinux =
            (StaticSymbolLinux) staticSymbol;
          totalExternSet.UnionWith(staticSymbolLinux.ExternSet);
        }

        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          totalExternSet.Remove(staticSymbol.UniqueName);
        }

        FileInfo assemblyFile = new FileInfo(file.FullName + ".asm");
        File.Delete(assemblyFile.FullName);
        StreamWriter streamWriter = new StreamWriter(assemblyFile.FullName);

        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          if (!staticSymbol.UniqueName.Contains(Symbol.SeparatorId) &&
              !staticSymbol.UniqueName.Contains(Symbol.NumberId)) {
            streamWriter.WriteLine("\tglobal " + staticSymbol.UniqueName);
          }
        }
        streamWriter.WriteLine();

        foreach (string externName in totalExternSet) {
          streamWriter.WriteLine("\textern " + externName);
        }

        if (SymbolTable.InitSymbol != null) {
          streamWriter.WriteLine("\tglobal _start");
          streamWriter.WriteLine("\tglobal " + Linker.StackStart);
        }
        else {
          streamWriter.WriteLine("\textern " + Linker.StackStart);
        }
        streamWriter.WriteLine();

        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          StaticSymbolLinux staticSymbolLinux =
            (StaticSymbolLinux) staticSymbol;

          streamWriter.WriteLine();
          foreach (string line in staticSymbolLinux.TextList) {
            streamWriter.WriteLine(line);
          }
        }

        if (SymbolTable.InitSymbol != null) {
          streamWriter.WriteLine();
          streamWriter.WriteLine("section .data");
          streamWriter.WriteLine(Linker.StackStart + ":\ttimes 1048576 db 0");
        }

        streamWriter.Close();
      }

      if (Start.Windows) {
        FileInfo objectFile = new FileInfo(file.FullName + ".obj");
        BinaryWriter binaryWriter =
          new BinaryWriter(File.Open(objectFile.FullName, FileMode.Create));

        binaryWriter.Write(SymbolTable.StaticSet.Count);    
        foreach (StaticSymbol staticSymbol in SymbolTable.StaticSet) {
          staticSymbol.Write(binaryWriter);
        }

        binaryWriter.Close();
      }
    }

    private static void GenerateIncludeFile(FileInfo file,
                                            ISet<FileInfo> includeSet) {
      FileInfo dependencySetFile = new FileInfo(file.FullName + ".dependency");
      StreamWriter dependencyWriter =
        new StreamWriter(File.Open(SourcePath + dependencySetFile.Name, FileMode.Create));

      dependencyWriter.Write(file.Name + ".c");
      foreach (FileInfo includeFile in includeSet) {
        dependencyWriter.Write(" " + includeFile.Name);
      }

      dependencyWriter.Close();
    }

    public static bool IsGeneratedFileUpToDate(FileInfo file, string suffix) {
      FileInfo generatedFile = new FileInfo(file.FullName + suffix), 
               dependencySetFile = new FileInfo(file.FullName + ".dependency");

      if (!generatedFile.Exists || !dependencySetFile.Exists) {
        return false;
      }

      if (dependencySetFile.Exists) {
        try {
          StreamReader dependencySetReader =
            new StreamReader(File.OpenRead(dependencySetFile.FullName));
          string dependencySetText = dependencySetReader.ReadToEnd();
          dependencySetReader.Close();

          if (dependencySetText.Length > 0) {
            string[] dependencyNameArray = dependencySetText.Split(' ');

            foreach (string dependencyName in dependencyNameArray)  {
              FileInfo dependencyFile =
                new FileInfo(SourcePath + dependencyName);

              if (dependencyFile.LastWriteTime > generatedFile.LastWriteTime) {
                return false;
              }
            }
          }
        }
        catch (IOException ioException) {
          Console.Out.WriteLine(ioException.StackTrace);
          return false;
        }
      }

      return true;
    }
  }
}