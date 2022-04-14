using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

namespace TencentExtractor
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Choose your compilation (c) or decompilation (d) method :");
            var method = Console.ReadLine();
            if (method != "c" && method != "d")
            {
                Console.WriteLine("Syntax:  appack <c|d> <input file> <output file>");
                return;
            }

            string inFilename = string.Empty;
            do
            {
                if(!string.IsNullOrEmpty(inFilename))
                    Console.WriteLine("Error: unable to find file '{0}'", inFilename);

                Console.WriteLine("Source file path :");
                inFilename = Console.ReadLine();
            }
            while (!File.Exists(inFilename));

            Console.WriteLine("Destination file path :");
            var outFile = Console.ReadLine();


            try
            {
                var inFileBytes = File.ReadAllBytes(inFilename);
                MemoryStream stream = new MemoryStream(inFileBytes);
                BinaryReader reader = new BinaryReader(stream);
                //HACK : work only for the last version (2.0.11.632)
                //TODO : Change this by the mz header rebuilder
                var mzHeader = reader.ReadBytes(1024);

                long currentPos = reader.BaseStream.Position;
                List<long> compressedPositions = GetCompressedPositions(reader);

                List<byte> finalData = new List<byte>();
                finalData.AddRange(mzHeader);

                for (int i = 0; i < compressedPositions.Count; i++)
                {
                    long position = compressedPositions[i];

                    reader.BaseStream.Seek(position, SeekOrigin.Begin);
                    var apHeader = reader.ReadChars(4);
                    var apheaderSize = reader.ReadInt32();
                    var packedSize = reader.ReadInt32();
                    var packed_crc = reader.ReadUInt32();
                    var origin_size = reader.ReadInt32();
                    var origin_crc = reader.ReadUInt32();

                    reader.BaseStream.Seek(-24, SeekOrigin.Current);

                    var packedBytes = new MemoryStream(reader.ReadBytes(24 + packedSize));

                    var depackedData = appack.DecompressStream(packedBytes);
                    var depacked_crc = Crc32Algorithm.Compute(depackedData, 0, origin_size);

                    if (depacked_crc != origin_crc)
                        throw new InvalidOperationException("Data is not decompressed correctly check if the lib version is correct !");

                    finalData.AddRange(depackedData);
                    long nextPosition = i + 1 < compressedPositions.Count ? compressedPositions[i + 1] : reader.BaseStream.Length;

                    byte[] bytes = reader.ReadBytes((int)(nextPosition - reader.BaseStream.Position));
                    finalData.AddRange(bytes);

                }

                File.WriteAllBytes(outFile, finalData.ToArray());
            }
            catch (Exception e)
            {

                Console.WriteLine("Error: {0}", e.ToString());
            }

            Console.WriteLine("Finished, you can close the program !");
            Console.ReadLine();
        }

        private static List<long> GetCompressedPositions(BinaryReader reader)
        {
            List<long> compressedPositions = new List<long>();
            while (reader.BaseStream.CanRead)
            {
                try
                {
                    if (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
                    {
                        var apHeader = reader.ReadBytes(4);
                        if (Encoding.ASCII.GetString(apHeader) == "AP32")
                            compressedPositions.Add(reader.BaseStream.Position - 4);

                        reader.BaseStream.Seek(-3, SeekOrigin.Current);
                    }
                    else
                        break;
                }
                catch(Exception e)
                {

                }
            }

            return compressedPositions;
        }
    }
}
