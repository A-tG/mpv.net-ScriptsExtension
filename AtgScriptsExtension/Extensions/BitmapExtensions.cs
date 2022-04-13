using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AtgScriptsExtension.Extensions
{
    static internal class BitmapExtensions
    {
        unsafe public static void Read32RgbFromPaddedRgb0(this Bitmap bmp, long strideB, IntPtr data, long lenB)
        {
            const int bpp = 4; // r, g, b, X - 4 bytes
            const PixelFormat preferredFormat = PixelFormat.Format32bppRgb;

            var format = bmp.PixelFormat;
            var paddingB = strideB - bmp.Width * bpp;
            var outOfBoundB = paddingB * bmp.Height;

            if (data == IntPtr.Zero) throw new ArgumentException("data is Zero pointer");
            if (lenB == 0) throw new ArgumentException("lenBytes is 0");
            if (((lenB - outOfBoundB) / bpp) != bmp.Width * bmp.Height)
            {
                throw new ArgumentException("Bitmap dimensions mismatch");
            }
            if (format != preferredFormat) throw new InvalidOperationException($"PixelFormat have to be {preferredFormat}");

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmData = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);

            var readPtr = (byte*)data;
            var writePtr = (byte*)bmData.Scan0;
            var pixelsNumber = (lenB - paddingB) / bpp; // remove padding in the last line of pixels
            var strideP = strideB / bpp;
            var widthP = bmp.Width;

            Parallel.For(0, pixelsNumber, (pIndex) =>
            {
                var y = pIndex / strideP;
                bool isOutOfBound = ((pIndex % strideP) >= widthP) ||
                    (y == 0) && (pIndex >= widthP);
                if (isOutOfBound) return;

                var ofs = bpp * pIndex;
                for (int i = 0; i < 3; i++)
                {
                    var outOfBoundOfs = paddingB * y;
                    writePtr[ofs + i - outOfBoundOfs] = readPtr[ofs + i];
                }
            });

            bmp.UnlockBits(bmData);
        }
    }
}
