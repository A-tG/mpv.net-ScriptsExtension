using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtgScriptsExtension.Helpers
{
    unsafe internal class MarHelper
    {
        static internal void CopyCharStrBuffToByteStrBuff(char* frombuff, byte* toBuff, int lenWithNull)
        {
            var lenWithouNull = lenWithNull - 1;
            for (int i = 0; i < lenWithouNull; i++)
            {
                unchecked
                {
                    toBuff[i] = (byte)frombuff[i];
                }
            }
            if (lenWithouNull > 0)
            {
                toBuff[lenWithouNull] = 0; // add null character
            }
        }

        static internal void CopyStrToByteStrBuff(string str, byte* toBuff)
        {
            fixed (char* c = str)
            {
                CopyCharStrBuffToByteStrBuff(c, toBuff, str.Length + 1); // to account null character
            }
        }
    }
}
