namespace CCompiler {
  public enum Storage {Auto     = (int) Mask.Auto,
                       Register = (int) Mask.Register,
                       Static   = (int) Mask.Static,
                       Extern   = (int) Mask.Extern,
                       Typedef  = (int) Mask.Typedef};
}