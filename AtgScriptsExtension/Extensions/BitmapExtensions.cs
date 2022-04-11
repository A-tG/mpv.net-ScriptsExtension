using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AtgScriptsExtension.Extensions
{
    static internal class BitmapExtensions
    {
        static internal void ReadRgbFromRgb0(this Bitmap bmp, IntPtr data, int lenBytes)
        {
            var format = bmp.PixelFormat;
            lenBytes /= 4; // r, g, b, X
            if (data == IntPtr.Zero) throw new ArgumentException("data is Zero pointer");
            if (lenBytes == 0) throw new ArgumentException("lenBytes is 0");
            if (format != PixelFormat.Format24bppRgb) throw new InvalidOperationException("PixelFormat have to be Format24bppRgb");
            if ((bmp.Width * bmp.Height) != lenBytes) throw new ArgumentException("Bitmap dimensions mismatch");

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            var bmData = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);

            var readPtr = data;
            var writePtr = bmData.Scan0;
            Parallel.For(0, lenBytes, (i) =>
            {
                var rPtr = readPtr + 4 * i;
                var wPtr = writePtr + 3 * i;
                const int gOfs = 1;
                const int bOfs = 2;
                Marshal.WriteByte(wPtr, Marshal.ReadByte(rPtr));
                Marshal.WriteByte(wPtr, gOfs, Marshal.ReadByte(rPtr + gOfs));
                Marshal.WriteByte(wPtr, bOfs, Marshal.ReadByte(rPtr + bOfs));
            });
            bmp.UnlockBits(bmData);
        }
    }
}
