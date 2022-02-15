using System;
using System.IO;
using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

namespace Calculator {
  class MainX {
    public static Dictionary<string,double> VariableMap =
      new Dictionary<string,double>();
          
    static void Main(string[] args) {
      if (args.Length != 1) {
        Console.Out.WriteLine("Usage: calculator inputfile");
      }

      { System.Globalization.CultureInfo customCulture =
         (System.Globalization.CultureInfo)
         System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";
        System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
      }

      VariableMap["pi"] = Math.PI;
      VariableMap["e"] = Math.E;

      string s = "abcd", t = "\"\"";
      string u = s.Substring(1, s.Length - 2);
      string v = s.Substring(1, t.Length - 2);

      //string text = Console.In.ReadLine();

      try {
        FileStream file = new FileStream(args[0], FileMode.Open);
        Scanner scanner = new Scanner(file);
        Parser parser = new Parser(scanner);
        parser.Parse();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.ToString());
      }

//      Console.In.ReadLine();
    }
  }

  public partial class Parser :
         QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}