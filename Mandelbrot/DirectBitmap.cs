using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenCL.Net;

namespace Mandelbrot
{
    public class DirectBitmap : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack =1, Size= 3)]
        public struct Pixel
        {
            public byte B;
            public byte G;
            public byte R;
        }

        public Bitmap Bitmap { get; private set; }
        public Pixel[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Pixel[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, BitsHandle.AddrOfPinnedObject());
        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);

            Bits[index].R = colour.R;
            Bits[index].G = colour.G;
            Bits[index].B = colour.B;
        }

        public unsafe void LoadFrom(Mandelbrot brot)
        {
            var error = Cl.EnqueueReadBuffer(brot.CommandQueue, brot.GpuMem, Bool.True, 0, Width * Height * 3, BitsHandle.AddrOfPinnedObject(), 0, null, out var _);
            if (error != ErrorCode.Success)
                throw new Cl.Exception(error);
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            var i = Bits[index];
            return Color.FromArgb(i.R, i.G, i.B);
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            BitsHandle.Free();
            Bitmap.Dispose();
        }
    }
}
