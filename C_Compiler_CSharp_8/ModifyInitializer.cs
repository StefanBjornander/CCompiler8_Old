// int a[2][2][2] = {1,2,3,4,5,6,7,8};
// {{1,2],{3,4},{5,6},{7,8}}
// {{{1,2],{3,4}},{{5,6},{7,8}}}

using System;
using System.IO;
using System.Text; // XXX
using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  class ModifyInitializer {
    public static List<object> ModifyArray(Type type, List<object> list) {
      IDictionary<int,int> dimensionToSizeMap = new Dictionary<int,int>();
      int maxDimension = DimensionToSizeMap(type, dimensionToSizeMap);
      IDictionary<object,int> initializerToDimensionMap =
        new Dictionary<object,int>();
      InitializerToDimensionMap(list, initializerToDimensionMap);

      for (int dimension = 1; dimension < maxDimension; ++dimension) {
        List<object> totalList =
          new List<object>(), currentList = new List<object>();
        int arraySize = dimensionToSizeMap[dimension];
        Error.ErrorXXX(arraySize > 0);

        foreach (object member in list) {
          if (initializerToDimensionMap[member] < dimension) {
            currentList.Add(member);
          }
          else {
            if (currentList.Count > 0) {
              initializerToDimensionMap[currentList] = dimension;
              totalList.Add(currentList);
            }

            totalList.Add(member);
            currentList = new List<object>();
          }

          if (currentList.Count == arraySize) {
            initializerToDimensionMap[currentList] = dimension;
            totalList.Add(currentList);
            currentList = new List<object>();
          }
        }

        if (currentList.Count > 0) {
          initializerToDimensionMap[currentList] = dimension;
          totalList.Add(currentList);
        }

        list = totalList;
      }

      return list;
    }

    private static int DimensionToSizeMap(Type type,
                                  IDictionary<int,int> dimensionToSizeMap) {
      if (type.IsArray()) {
        int dimension =
          DimensionToSizeMap(type.ArrayType, dimensionToSizeMap) + 1;
        dimensionToSizeMap[dimension] = type.ArraySize;
      }

      return 0;
    }

    private static int InitializerToDimensionMap(object initializer,
                          IDictionary<object,int> initializerToDimensionMap) {
      if (initializer is List<object>) {
        List<object> list = (List<object>) initializer;
        int maxDimension = 0;

        foreach (object member in list) {
          int dimension =
            InitializerToDimensionMap(member, initializerToDimensionMap);
          maxDimension = Math.Max(maxDimension, dimension);
        }

        initializerToDimensionMap[list] = maxDimension + 1;
        return maxDimension + 1;
      }

      return 0;
    }

    //public static void PrintList(TextWriter textWriter, object initializer)
    //{
    //  if (initializer is Expression)
    //  {
    //    textWriter.Write(Symbol.SimpleName((((Expression)initializer)).
    //                                       Symbol.UniqueName));
    //  }
    //  else
    //  {
    //    textWriter.Write("[");

    //    List<object> list = (List<object>)initializer;
    //    bool first = true;

    //    foreach (object member in list)
    //    {
    //      textWriter.Write(first ? "" : ",");
    //      PrintList(textWriter, member);
    //      first = false;
    //    }

    //    textWriter.Write("]");
    //  }
    //}
  }
 }