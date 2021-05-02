using Force.Crc32;
using System;
using System.IO;

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
            var outFile = File.Create(Console.ReadLine());


            try
            {
                var inFileBytes = File.ReadAllBytes(inFilename);
                BinaryReader reader = new BinaryReader(new MemoryStream(inFileBytes));
                //HACK : work only for the last version (2.0.11.632)
                //TODO : Change this by the mz header rebuilder
                var mzHeader = reader.ReadBytes(1024);

                var apHeader = reader.ReadChars(4);
                var apheaderSize = reader.ReadInt32();
                var packedSize = reader.ReadInt32();
                var packed_crc = reader.ReadUInt32();
                var origin_size = reader.ReadInt32();
                var origin_crc = reader.ReadUInt32();

                reader.BaseStream.Seek(-24, SeekOrigin.Current);

                var packedBytes = new MemoryStream(reader.ReadBytes(24 + packedSize));
                var leftBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

                var depackedData = appack.DecompressStream(packedBytes);
                var depacked_crc = Crc32Algorithm.Compute(depackedData, 0, origin_size); 

                if (depacked_crc != origin_crc)
                    throw new InvalidOperationException("Data is not decompressed correctly check if the lib version is correct !");

                BinaryWriter writer = new BinaryWriter(outFile);
                writer.Write(mzHeader);
                writer.Write(depackedData);
                writer.Write(leftBytes);
                writer.Flush();
            }
            catch (Exception e)
            {

                Console.WriteLine("Error: {0}", e.ToString());
            }
        }

    }
}
