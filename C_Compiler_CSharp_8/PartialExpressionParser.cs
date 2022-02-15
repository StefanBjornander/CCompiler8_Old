using System.Collections.Generic;

namespace CCompiler_Exp {
  public partial class Parser :
         QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public static IDictionary<string,CCompiler.Macro> m_macroMap;

    public Parser(Scanner scanner,
                  IDictionary<string,CCompiler.Macro> macroMap)
     :base(scanner) {
      m_macroMap = macroMap;
    }
  }
}
