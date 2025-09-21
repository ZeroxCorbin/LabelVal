using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V430.Extensions;
public static class StringExtensions
{
    public static byte[] FromHEX(this string hexString, int size = 0)
    {
        if (hexString == null)
            return new byte[size];

        int hexLen = hexString.Length / 2;
        if (size == 0 || size < hexLen)
            size = hexLen;

        byte[] data = new byte[size];

        if (hexString.Length % 2 != 0)
        {
            int i = 0;
            foreach (byte c in Encoding.ASCII.GetBytes(hexString).ToArray())
            {
                if (i >= size)
                    break;

                data[i++] = c;
            }
            return data;
        }


        try
        {

            for (int index = 0; index < hexLen; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            data = new byte[size];
            int i = 0;
            foreach (byte c in Encoding.ASCII.GetBytes(hexString).ToArray())
            {
                if (i >= size)
                    break;

                data[i++] = c;
            }
            return data;
        }

        return data;
    }
}
