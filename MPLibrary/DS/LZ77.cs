using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLibrary.DS
{
    public class LZ77
    {   
        public static byte[] Decompress(byte[] source)
        {
            int length = (int)source[1] | (int)source[2] << 8 | (int)source[3] << 16;
            byte[] numArray = new byte[length];
            int index1 = 4;
            int num1 = 0;
            while (length > 0)
            {
                byte num2 = source[index1++];
                if (num2 != (byte)0)
                {
                    for (int index2 = 0; index2 < 8; ++index2)
                    {
                        if (((int)num2 & 128) != 0)
                        {
                            int num3 = (int)source[index1] << 8 | (int)source[index1 + 1];
                            index1 += 2;
                            int num4 = (num3 >> 12) + 3;
                            int num5 = num3 & 4095;
                            int num6 = num1 - num5 - 1;
                            for (int index3 = 0; index3 < num4; ++index3)
                            {
                                numArray[num1++] = numArray[num6++];
                                --length;
                                if (length == 0)
                                    return numArray;
                            }
                        }
                        else
                        {
                            numArray[num1++] = source[index1++];
                            --length;
                            if (length == 0)
                                return numArray;
                        }
                        num2 <<= 1;
                    }
                }
                else
                {
                    for (int index2 = 0; index2 < 8; ++index2)
                    {
                        numArray[num1++] = source[index1++];
                        --length;
                        if (length == 0)
                            return numArray;
                    }
                }
            }
            return numArray;
        }
    }
}
