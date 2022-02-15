using System;

namespace CCompiler {
  public class Error {
    public static void Report(string message) {
      Report(message, null);    
    }

    public static void ErrorXXX(bool test) {
      if (!test) {
        System.Environment.Exit(-1);
      }
    }

    public static void Check(bool test, Message message) {
      if (!test) {
        Report(message, null);      }
    }

    public static void Report(object value, Message message) {
      Check(false, value, message);
    }

    public static void Check(bool test, object value, Message message) {
      if (!test) {
        Report(message, value.ToString());      
      }
    }

    public static void Report(Message message) {
      Report(message, null);   
    }

    private static void Report(Message message, string text) {     
      Report(Enum.GetName(typeof(Message), message).
             Replace("___", ",").Replace("__", "-").
             Replace("_", " "), text);
    }

    private static void Report(string message, string text) {
      Message("Error", message, text);    
      Console.In.ReadLine();
      System.Environment.Exit(-1);
    }

    private static void Message(string type, string message,
                                string text) {
      string funcText;

      if (SymbolTable.CurrentFunction != null) {
        funcText = " in function " + SymbolTable.CurrentFunction.UniqueName;
      }
      else {
        funcText = " in global space";
      }

      string extraText = (text != null) ? (": " + text) : "";
    
      if ((message != null) &&
          (CCompiler_Main.Scanner.Path != null)) {
        Console.Error.WriteLine(type + " at line " +
                CCompiler_Main.Scanner.Line + funcText +
                " in file " + CCompiler_Main.Scanner.Path.Name +
                ". " + message + extraText + ".");
      }
      else if ((message == null) &&
               (CCompiler_Main.Scanner.Path != null)) {
        Console.Error.WriteLine(type + " at line " +
                CCompiler_Main.Scanner.Line + funcText +
                " in file " + CCompiler_Main.Scanner.Path.Name +
                extraText + ".");
      }
      else if ((message != null) &&
               (CCompiler_Main.Scanner.Path == null)) {
        Console.Error.WriteLine(type + ". " + message +
                                extraText + ".");
      }
      else if ((message == null) &&
               (CCompiler_Main.Scanner.Path == null)) {
        Console.Error.WriteLine(type + extraText + ".");
      }
    }
  }
}
