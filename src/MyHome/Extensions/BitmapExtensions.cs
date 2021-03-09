using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace MyHome.Extensions
{
    public static class BitmapExtensions
    {
        /// <summary>
        /// Converts bitmap class to bitmap file.
        /// </summary>
        public static byte[] GetBytes(this Bitmap bitmap)
        {
            var inputBuffer = bitmap.GetBitmap();
            var width = (uint)bitmap.Width;
            var height = (uint)bitmap.Height;
            uint outputBufferLength = width * height * 3 + 54;
            var outputBuffer = new byte[outputBufferLength];

            Array.Clear(outputBuffer, 0, outputBuffer.Length);

            Utility.InsertValueIntoArray(outputBuffer, 0, 2, 19778);
            Utility.InsertValueIntoArray(outputBuffer, 2, 4, width * height * 3 + 54);
            Utility.InsertValueIntoArray(outputBuffer, 10, 4, 54);
            Utility.InsertValueIntoArray(outputBuffer, 14, 4, 40);
            Utility.InsertValueIntoArray(outputBuffer, 18, 4, width);
            Utility.InsertValueIntoArray(outputBuffer, 22, 4, (uint)(-height));
            Utility.InsertValueIntoArray(outputBuffer, 26, 2, 1);
            Utility.InsertValueIntoArray(outputBuffer, 28, 2, 24);

            for (int i = 0, j = 54; i < width * height * 4; i += 4, j += 3)
            {
                outputBuffer[j + 0] = inputBuffer[i + 2];
                outputBuffer[j + 1] = inputBuffer[i + 1];
                outputBuffer[j + 2] = inputBuffer[i + 0];
            }

            return outputBuffer;
        }
    }
}
