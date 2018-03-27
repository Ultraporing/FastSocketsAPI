//-----------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets.Serializer
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// The Data Serializers for objects and types
    /// </summary>
    public class DataSerializers
    {
        /// <summary>
        /// Serializes the specified data to byte array.
        /// </summary>
        /// <typeparam name="StructType">The Struct Type</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>Returns the converted byte array</returns>
        public static byte[] Serialize<StructType>(StructType data) where StructType : struct
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, data);
            byte[] arr = stream.ToArray();

            return arr;
        }

        /// <summary>
        /// Deserializes the specified array to a struct.
        /// </summary>
        /// <typeparam name="StructType">The Struct Type</typeparam>
        /// <param name="array">The array.</param>
        /// <returns>Returns deserialized struct</returns>
        public static StructType Deserialize<StructType>(byte[] array) where StructType : struct
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            StructType s = (StructType)formatter.Deserialize(stream);

            return s;
        }

        /// <summary>
        /// Converts objects to byte array.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Returns byte array converted from object</returns>
        public static byte[] ObjectToByteArray(object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Converts Byte Array to object.
        /// </summary>
        /// <param name="arrBytes">The Byte Array.</param>
        /// <returns>Returns object converted from byte array</returns>
        public static object ByteArrayToObject(byte[] arrBytes, out long bytesRead)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Position = 0;
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            object obj = binForm.Deserialize(memStream);
            bytesRead = memStream.Position;
 
            return obj;
        }
    }
}
