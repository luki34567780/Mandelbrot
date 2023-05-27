using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Mandelbrot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int height = 1080;
            int width = 1920;
            //int height = 2160;
            //int width = 3840;

            var queue = new Queue<(int index, DirectBitmap bitmap)>(100);

            bool exit = false;
            var saverTask = new Thread(() => 
            {
                while (!exit)
                {
                    int? index = null;
                    DirectBitmap? bitmap = null;
                    lock (queue)
                    {
                        if (queue.Count > 0)
                        {
                            var item = queue.Dequeue();
                            index = item.index;
                            bitmap = item.bitmap;
                        }
                    }

                    if (bitmap != null)
                    {
                        bitmap.Bitmap.Save($"{index}.bmp");
                        bitmap.Dispose();

                        bitmap = null;
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            });
            saverTask.Start();

            using var brot = new Mandelbrot(height, width);

            for (int i = 0; i < 10000; i++)
            {
                try
                {
                    File.Delete($"{i}.bmp");
                }
                catch { }
            }

            const int Iterations = 2400;

            var points = new List<(double X, double Y)>()
            {
                (-0.743643887037151,  0.131825904205330),
                //(-0.7746806106269039,-0.1374168856037867),
                //(-0.207107867093967732893764544285894983866865721506089742782655, 1.12275706363259748461604158116265882079904682664638092967742)
            };

            var bm = new DirectBitmap(width, height);

            int imageCounter = 0;
            foreach(var point in points)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        lock (queue)
                        {
                            Console.Write($"\rImages: {imageCounter}, queue length:{ queue.Count}, queue capacity: {GetQueueCapacity(queue)}                    ");
                        }

                        brot.Calculate();

                        var newBm = new DirectBitmap(width, height);
                        bm.LoadFrom(brot);

                        Enqueue(queue, (imageCounter, bm));

                        bm = newBm;

                        brot.Zoom(point.X, point.Y, (j % 2 == 0 ? 1.015 : 0.970873786407767));
                        imageCounter++;
                    }
                }
                brot.ResetCoordinates();
            }

            Console.WriteLine();

            bm.Dispose();

            bool keepWaiting = true;
            while (keepWaiting)
            {
                lock(queue)
                {
                    keepWaiting = queue.Count > 0;
                    Console.Write($"\r{queue.Count}");
                }
                Thread.Sleep(1000);

            }
            exit = true;
        }

        public static void Enqueue<T>(Queue<T> queue, T item)
        {
            queue.Enqueue(item);
            return;

            int count;
            lock (queue) 
            { 
                count = queue.Count;
                if (count < 99)
                {
                    queue.Enqueue(item);
                    return;
                }
            }

            while (queue.Count > 99)
            {
                Thread.Sleep(10);
                lock (queue) { count = queue.Count; }
            }

            lock (queue)
            {
                queue.Enqueue(item);
            }
        }

        static int GetQueueCapacity<T>(Queue<T> queue)
        {
            FieldInfo arrayField = typeof(Queue<T>).GetField("_array", BindingFlags.NonPublic | BindingFlags.Instance);
            T[] array = (T[])arrayField.GetValue(queue);
            return array.Length;
        }
    }
}