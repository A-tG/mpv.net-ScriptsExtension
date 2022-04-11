using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AtgScriptsExtension.Extensions
{
    static internal class BitmapExtensions
    {
        static internal void Read32RgbFromPaddedRgb0(this Bitmap bmp, int strideB, IntPtr data, int lenB)
        {
            const int bpp = 4; // r, g, b, X - 4 bytes
            var format = bmp.PixelFormat;
            var preferredFormat = PixelFormat.Format32bppRgb;
            int paddingB = strideB - bmp.Width * bpp;
            int outOfBoundB = paddingB * bmp.Height;

            if (data == IntPtr.Zero) throw new ArgumentException("data is Zero pointer");
            if (lenB == 0) throw new ArgumentException("lenBytes is 0");
            if (((lenB - outOfBoundB) / bpp) != bmp.Width * bmp.Height)
            {
                throw new ArgumentException("Bitmap dimensions mismatch");
            }
            if (format != preferredFormat) throw new InvalidOperationException($"PixelFormat have to be {preferredFormat}");

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmData = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);

            var readPtr = data;
            var writePtr = bmData.Scan0;
            var pixelsNumber = (lenB - paddingB) / bpp;
            var paddingP = paddingB / bpp;
            var strideP = strideB / bpp;
            var widthP = bmp.Width;
            Parallel.For(0, pixelsNumber, (pIndex) =>
            {
                int y = pIndex / strideP;
                bool isOutOfBound = ((pIndex % strideP) >= widthP) ||
                    (y == 0) && (pIndex >= widthP);
                if (isOutOfBound) return;

                int ofs = bpp * pIndex;
                var rPtr = readPtr + ofs;
                var wPtr = writePtr + ofs - paddingB * y;
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
