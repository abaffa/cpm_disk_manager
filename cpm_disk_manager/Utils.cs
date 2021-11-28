using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager
{
    public static class Utils
    {
        public static String getStringFromByteArray(byte[] fileBytes2, int start, int max)
        {
            String ret = "";
            for (int i = 0; i < max && fileBytes2[start + i] != 0x00; i++)
                ret += Convert.ToChar(fileBytes2[start + i]);

            return ret.Trim('\0');
        }


        public static String ByteArrayToString(byte[] data)
        {
            string file = "";
            int index = 0;
            int size = data.Length;
            while (index < size)
            {
                file += data[index].ToString("X2");
                index++;
            }

            return file;
        }

        public static bool CompareByteArrays(byte[] data1 ,byte[] data2)
        {
            return data1.SequenceEqual(data1) && data1.LongLength == data2.LongLength;
        }
    }
}
