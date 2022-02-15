using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  class TypeSize {
    public static int PointerSize;
    public static int SignedIntegerSize;

    private static IDictionary<int, BigInteger>
      m_maskMap = new Dictionary<int, BigInteger>() {
        {1, (BigInteger) 0x000000FF},
        {2, (BigInteger) 0x0000FFFF},
        {4, (BigInteger) 0xFFFFFFFF}};

    public static IDictionary<Sort,int> m_sizeMap = new Dictionary<Sort,int>();

    public static IDictionary<int,Sort>
      m_signedMap = new Dictionary<int,Sort>(),
      m_unsignedMap = new Dictionary<int,Sort>();

    private static IDictionary<Sort,BigInteger>
      m_minValueMap = new Dictionary<Sort,BigInteger>(),
      m_maxValueMap = new Dictionary<Sort,BigInteger>();
    
    /*private static IDictionary<Sort,decimal>
      m_minValueFloatMap = new Dictionary<Sort,decimal>(),
      m_maxValueFloatMap = new Dictionary<Sort,decimal>();*/
  
    static TypeSize() {
      if (Start.Linux) {
        PointerSize = 8;
        SignedIntegerSize = 4;

        m_sizeMap.Add(Sort.Void, 0);
        m_sizeMap.Add(Sort.Function, PointerSize);
        m_sizeMap.Add(Sort.Logical, 1);
        m_sizeMap.Add(Sort.Pointer, 8);
        m_sizeMap.Add(Sort.Array, 8);
        m_sizeMap.Add(Sort.String, 8); 
        m_sizeMap.Add(Sort.SignedChar, 1);
        m_sizeMap.Add(Sort.UnsignedChar, 1);
        m_sizeMap.Add(Sort.SignedShortInt, 2);
        m_sizeMap.Add(Sort.UnsignedShortInt, 2);
        m_sizeMap.Add(Sort.SignedInt, 4);
        m_sizeMap.Add(Sort.UnsignedInt, 4);
        m_sizeMap.Add(Sort.SignedLongInt, 8);
        m_sizeMap.Add(Sort.UnsignedLongInt, 8);
        m_sizeMap.Add(Sort.Float, 4);
        m_sizeMap.Add(Sort.Double, 8);
        m_sizeMap.Add(Sort.LongDouble, 8);

        m_signedMap.Add(1, Sort.SignedChar);
        m_signedMap.Add(2, Sort.SignedShortInt);
        m_signedMap.Add(4, Sort.SignedInt);
        m_signedMap.Add(8, Sort.SignedLongInt);

        m_unsignedMap.Add(1, Sort.UnsignedChar);
        m_unsignedMap.Add(2, Sort.UnsignedShortInt);
        m_unsignedMap.Add(4, Sort.UnsignedInt);
        m_unsignedMap.Add(8, Sort.UnsignedLongInt);

        m_minValueMap.Add(Sort.Logical, 0);
        m_minValueMap.Add(Sort.SignedChar, -128);
        m_minValueMap.Add(Sort.UnsignedChar, 0);
        m_minValueMap.Add(Sort.SignedShortInt, -32768);
        m_minValueMap.Add(Sort.UnsignedShortInt, 0);
        m_minValueMap.Add(Sort.SignedInt, -2147483648);
        m_minValueMap.Add(Sort.UnsignedInt, 0);
        m_minValueMap.Add(Sort.Array, 0);
        m_minValueMap.Add(Sort.Pointer, 0);
        m_minValueMap.Add(Sort.SignedLongInt, -9223372036854775808);
        m_minValueMap.Add(Sort.UnsignedLongInt, 0);

        m_maxValueMap.Add(Sort.Logical, 1);
        m_maxValueMap.Add(Sort.SignedChar, 127);
        m_maxValueMap.Add(Sort.UnsignedChar, 255);
        m_maxValueMap.Add(Sort.SignedShortInt, 32767);
        m_maxValueMap.Add(Sort.UnsignedShortInt, 65535);
        m_maxValueMap.Add(Sort.SignedInt, 2147483647);
        m_maxValueMap.Add(Sort.UnsignedInt, 4294967295);
        m_maxValueMap.Add(Sort.Array, 4294967295);
        m_maxValueMap.Add(Sort.Pointer, 4294967295);
        m_maxValueMap.Add(Sort.SignedLongInt, 9223372036854775807);
        m_maxValueMap.Add(Sort.UnsignedLongInt, 18446744073709551615);

        /*m_minValueFloatMap.Add(Sort.Float, decimal.
                                 Parse("1.2E-38", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));

        m_maxValueFloatMap.Add(Sort.Float, decimal.
                               Parse("3.4E+38", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));*/
      }
      if (Start.Windows) {
        PointerSize = 2;
        SignedIntegerSize = 2;

        m_sizeMap.Add(Sort.Void, 0);
        m_sizeMap.Add(Sort.Function, PointerSize);
        m_sizeMap.Add(Sort.Logical, 1);
        m_sizeMap.Add(Sort.Array, 2);
        m_sizeMap.Add(Sort.Pointer, 2);
        m_sizeMap.Add(Sort.String, 2);
        m_sizeMap.Add(Sort.SignedChar, 1);
        m_sizeMap.Add(Sort.UnsignedChar, 1);
        m_sizeMap.Add(Sort.SignedShortInt, 1);
        m_sizeMap.Add(Sort.UnsignedShortInt, 1);
        m_sizeMap.Add(Sort.SignedInt, 2);
        m_sizeMap.Add(Sort.UnsignedInt, 2);
        m_sizeMap.Add(Sort.SignedLongInt, 4);
        m_sizeMap.Add(Sort.UnsignedLongInt, 4);
        m_sizeMap.Add(Sort.Float, 4);
        m_sizeMap.Add(Sort.Double, 8);
        m_sizeMap.Add(Sort.LongDouble, 8);

        m_signedMap.Add(1, Sort.SignedChar);
        m_signedMap.Add(2, Sort.SignedInt);
        m_signedMap.Add(4, Sort.SignedLongInt);

        m_unsignedMap.Add(1, Sort.UnsignedChar);
        m_unsignedMap.Add(2, Sort.UnsignedInt);
        m_unsignedMap.Add(4, Sort.UnsignedLongInt);

        m_minValueMap.Add(Sort.Logical, 0);
        m_minValueMap.Add(Sort.SignedChar, -128);
        m_minValueMap.Add(Sort.UnsignedChar, 0);
        m_minValueMap.Add(Sort.SignedShortInt, -128);
        m_minValueMap.Add(Sort.UnsignedShortInt, 0);
        m_minValueMap.Add(Sort.SignedInt, -32768);
        m_minValueMap.Add(Sort.UnsignedInt, 0);
        m_minValueMap.Add(Sort.Array, 0);
        m_minValueMap.Add(Sort.Pointer, 0);
        m_minValueMap.Add(Sort.SignedLongInt, -2147483648);
        m_minValueMap.Add(Sort.UnsignedLongInt, 0);

        m_maxValueMap.Add(Sort.Logical, 1);
        m_maxValueMap.Add(Sort.SignedChar, 127);
        m_maxValueMap.Add(Sort.UnsignedChar, 255);
        m_maxValueMap.Add(Sort.SignedShortInt, 127);
        m_maxValueMap.Add(Sort.UnsignedShortInt, 255);
        m_maxValueMap.Add(Sort.SignedInt, 32767);
        m_maxValueMap.Add(Sort.UnsignedInt, 65535);
        m_maxValueMap.Add(Sort.Array, 65535);
        m_maxValueMap.Add(Sort.Pointer, 65535);
        m_maxValueMap.Add(Sort.SignedLongInt, 2147483647);
        m_maxValueMap.Add(Sort.UnsignedLongInt, 4294967295);

        /*m_minValueFloatMap.Add(Sort.Float, decimal.
                                 Parse("1.2E-38", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));
        m_minValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("2.3E-308", NumberStyles.Float));

        m_maxValueFloatMap.Add(Sort.Float, decimal.
                               Parse("3.4E+38", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));
        m_maxValueFloatMap.Add(Sort.Long_Double, decimal.
                               Parse("1.7E+308", NumberStyles.Float));*/
      }
    }

    public static BigInteger GetMinValue(Sort sort) {
      return m_minValueMap[sort];
    }

    public static BigInteger GetMaxValue(Sort sort) {
      return m_maxValueMap[sort];
    }

    public static BigInteger GetMask(Sort sort) {
      return m_maskMap[m_sizeMap[sort]];
    }

    public static Type SizeToSignedType(int size) {
      return new Type(m_signedMap[size]);
    }

    public static Type SizeToUnsignedType(int size) {
      return new Type(m_unsignedMap[size]);
    }

    public static int Size(Sort sort) {
      return m_sizeMap[sort];
    }
  }
}
