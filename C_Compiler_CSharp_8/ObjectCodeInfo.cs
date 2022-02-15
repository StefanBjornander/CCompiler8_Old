using System;
using System.Collections.Generic;

namespace CCompiler {
  public class ObjectCodeInfo {
    private AssemblyOperator m_operator;
    private object m_value1, m_value2, m_value3;

    public ObjectCodeInfo(AssemblyOperator operatorX, object value1, object value2, object value3) {
      m_operator = operatorX;
      m_value1 = value1;
      m_value2 = value2;
      m_value3 = value3;
    }

    public static int Compare(ObjectCodeInfo info1, ObjectCodeInfo info2) {
      int compareOperator = info1.m_operator.CompareTo(info2.m_operator),
          compare1 = CompareObject(info1.m_value1, info2.m_value1),
          compare2 = CompareObject(info1.m_value2, info2.m_value2),
          compare3 = CompareObject(info1.m_value3, info2.m_value3);

      if (compareOperator == 0) {
        if (compare1 == 0) {
          if (compare2 == 0) {
            return compare3;
          }
          else {
            return compare2;
          }
        }
        else {
          return compare1;
        }
      }
      else {
        return compareOperator;
      }
    }

    // register < integer < null

    private static int CompareObject(object object1, object object2) {
      if ((object1 == null) && (object2 == null)) {
        return 0;
      }
      else if ((object1 == null) && (object2 != null)) {
        return -1;
      }
      else if ((object1 != null) && (object2 == null)) {
        return 1;
      }
      else {
        System.Type type1 = object1.GetType(), type2 = object2.GetType();

        if (type1.Equals(type2)) {
          if (type1.IsEnum) {
            string name1 = Enum.GetName(type1, object1),
                   name2 = Enum.GetName(type2, object2);
            return name1.CompareTo(name2);
          }
          else {
            string name1 = object1.ToString(), name2 = object2.ToString();
            return name1.CompareTo(name2);
          }
        }
        else {
          return type1.Name.CompareTo(type2.Name);
        }
      }
    }
  }
}