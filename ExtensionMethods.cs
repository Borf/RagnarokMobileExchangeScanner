using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RomExchangeScanner
{
    public static class ExtensionMethods
    {
        public static void Swap<T>(this List<T> list, int index1, int index2)
        {
            T temp = list[index1];
            list[index1] = list[index2];
            list[index2] = temp;
        }


        public static Vector3 Hsv(this Rgba32 input)
        {
            float r = input.R / 255.0f;
            float g = input.G / 255.0f;
            float b = input.B / 255.0f;

            float max = MathF.Max(r, MathF.Max(g, b));
            float min = MathF.Min(r, MathF.Min(g, b));
            float chroma = max - min;
            float h = 0F;
            float s = 0F;
            float l = (max + min) / 2F;

            if (MathF.Abs(chroma) < 0.0001)
                return new Vector3(0F, s, l);

            if (MathF.Abs(r - max) < 0.0001)
                h = (g - b) / chroma;
            else if (MathF.Abs(g - max) < 0.0001)
                h = 2F + (b - r) / chroma;
            else if (MathF.Abs(b - max) < 0.0001)
                h = 4F + (r - g) / chroma;

            h *= 60F;
            if (h < 0F)
                h += 360F;

            if (l <= .5F)
                s = chroma / (max + min);
            else
                s = chroma / (2F - max - min);

            return new Vector3(h, s, l);
        }


        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for cancellation.</param>
        /// <param name="cancellationToken">A cancellation token. If invoked, the task will return 
        /// immediately as canceled.</param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Process_Exited(object sender, EventArgs e)
            {
                tcs.TrySetResult(true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                process.Exited -= Process_Exited;
            }
        }
    }
}
