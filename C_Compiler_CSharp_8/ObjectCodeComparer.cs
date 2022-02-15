using System.Collections.Generic;

namespace CCompiler {
  class ObjectCodeComparer : IComparer<ObjectCodeInfo> {
    public int Compare(ObjectCodeInfo info1, ObjectCodeInfo info2) {
      return ObjectCodeInfo.Compare(info1, info2);
   }
 }
}
