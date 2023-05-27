using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenCL.Net;

namespace Mandelbrot
{
    public class Mandelbrot : IDisposable
    {
        public int depth = 10000;
        public double ymax = 1;
        public double xmin = -2.5;
        public double xmax = 1;
        public double ymin = -1;
        public int width = 1920;
        public int height = 1080;

        public IMem GpuMem;
        public Context Context;
        public CommandQueue CommandQueue;
        public Kernel Kernel;
        public OpenCL.Net.Program Program;

        private void ThrowOnError(ErrorCode error)
        {
            if (error != ErrorCode.Success)
                throw new Cl.Exception(error);
        }

        public void ResetCoordinates()
        {
            ymax = 1;
            xmin = -2.5;
            xmax = 1;
            ymin = -1;
    }

        public Mandelbrot(int height, int width) 
        {
            this.height = height;
            this.width = width;

            var platforms = Cl.GetPlatformIDs(out var error);
            ThrowOnError(error);

            var devs = Cl.GetDeviceIDs(platforms[0], DeviceType.All, out error);
            ThrowOnError(error);
            var dev = devs[0];

            Context = Cl.CreateContext("*", DeviceType.All, out error);
            ThrowOnError(error);

            CommandQueue = Cl.CreateCommandQueue(Context, dev, CommandQueueProperties.None, out error);
            ThrowOnError(error);

            GpuMem = Cl.CreateBuffer(Context, MemFlags.ReadWrite | MemFlags.AllocHostPtr, width * height * 3, out error);
            ThrowOnError(error);

            Program = Cl.CreateProgramWithSource(Context, 1, new string[] { File.ReadAllText("mandel.cl") }, null, out error);
            ThrowOnError(error);

            Cl.BuildProgram(Program, 1, new Device[] { dev }, "", null, 0);

            Kernel = Cl.CreateKernel(Program, "CalculatePixels", out error);
            ThrowOnError(error);
        }

        public void Zoom(double centerX, double centerY, double zoomFactor)
        {
            double newWidth = (xmax - xmin) / zoomFactor;
            double newHeight = (ymax - ymin) / zoomFactor;
            xmin = centerX - newWidth / 2;
            xmax = centerX + newWidth / 2;
            ymin = centerY - newHeight / 2;
            ymax = centerY + newHeight / 2;
        }

        public void CalculateCpu(DirectBitmap bm)
        {
            Parallel.For(0, width, (x) =>
            {
                for (int y = 0; y < height; y++)
                {
                    bm.SetPixel(x, y, GetPixel(x, y));
                }
            });
        }


        public unsafe void Calculate()
        {
            Cl.SetKernelArg(Kernel, 0, GpuMem);
            Cl.SetKernelArg(Kernel, 1, width);
            Cl.SetKernelArg(Kernel, 2, height);
            Cl.SetKernelArg(Kernel, 3, xmin);
            Cl.SetKernelArg(Kernel, 4, ymin);
            Cl.SetKernelArg(Kernel, 5, xmax);
            Cl.SetKernelArg(Kernel, 6, ymax);
            Cl.SetKernelArg(Kernel, 7, depth);


            var error = Cl.EnqueueNDRangeKernel(CommandQueue, Kernel, 2, null, new nint[] { width, height }, null, 0, null, out var e);
            ThrowOnError(error);
            //Cl.WaitForEvents(1, new Event[] { e });
        }

        public Color GetPixel(int x, int y)
        {
            double zx, zy, cx, cy;
            zx = zy = 0;
            cx = Map(x, 0, width, xmin, xmax);
            cy = Map(y, 0, height, ymin, ymax);

            int iteration = 0;
            while (zx * zx + zy * zy < 4 && iteration < depth)
            {
                double temp = zx * zx - zy * zy + cx;
                zy = 2 * zx * zy + cy;
                zx = temp;
                iteration++;
            }

            if (iteration == depth)
                return Color.Black;

            double smoothColor = iteration + 1 - Math.Log(Math.Log(Math.Sqrt((double)(zx * zx + zy * zy)))) / Math.Log(2);
            double hue = smoothColor / depth;

            Color color = HsvToRgb(hue, 1, 1);

            return color;
        }

        private static double Map(int value, int inputMin, int inputMax, double outputMin, double outputMax)
        {
            return ((value - inputMin) * (outputMax - outputMin) / (inputMax - inputMin)) + outputMin;
        }

        private static Color HsvToRgb(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue * 6)) % 6;
            double f = (double)(hue * 6 - Math.Floor(hue * 6));
            double p = value * (1 - saturation);
            double q = value * (1 - f * saturation);
            double t = value * (1 - (1 - f) * saturation);

            double r, g, b;
            switch (hi)
            {
                case 0:
                    r = value;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = value;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = value;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = value;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = value;
                    break;
                case 5:
                    r = value;
                    g = p;
                    b = q;
                    break;
                default:
                    r = g = b = 0; // Default to black
                    break;
            }

            int red = (int)(r * 255);
            int green = (int)(g * 255);
            int blue = (int)(b * 255);

            return Color.FromArgb(red, green, blue);
        }

        public void Dispose()
        {
            Kernel.Dispose();
            Program.Dispose();
            GpuMem.Dispose();
            CommandQueue.Dispose();
            Context.Dispose();
        }
    }
}
