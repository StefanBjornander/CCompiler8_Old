namespace CCompiler_Main {
  public partial class Parser :
         QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}