using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FastSockets.Helper
{
    public class ArrayHelper
    {
        public static int IndexOf(byte[] arrayToSearchThrough, byte[] patternToFind, int beginSearchIdx = 0)
        {
            if (patternToFind.Length > arrayToSearchThrough.Length)
                return -1;
            for (int i = beginSearchIdx; i < arrayToSearchThrough.Length - patternToFind.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < patternToFind.Length; j++)
                {
                    if (arrayToSearchThrough[i + j] != patternToFind[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Reads data into a complete array, throwing an EndOfStreamException
        /// if the stream runs out of data first, or if an IOException
        /// naturally occurs.
        /// </summary>
        /// <param name="reader">The stream to read data from</param>
        /// <param name="data">The array to read bytes into. The array
        /// will be completely filled from the stream, so an appropriate
        /// size must be given.</param>
        public static void ReadWholeArray(StreamReader reader, char[] data)
        {
            var offset = 0;
            var remaining = data.Length;
            while (remaining > 0)
            {
                var read = reader.Read(data, offset, remaining);
                if (read <= 0)
                    throw new EndOfStreamException
                        (String.Format("End of stream reached with {0} bytes left to read", remaining));
                remaining -= read;
                offset += read;
            }
        }
    }
}
