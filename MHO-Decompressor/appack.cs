//
// aPLib compression library  -  the smaller the better :)
//
// C# example
//
// Copyright (c) 1998-2004 by Joergen Ibsen / Jibz
// All Rights Reserved
//
// http://www.ibsensoftware.com/
//

using System;
using System.IO;

using IbsenSoftware.aPLib;

class appack
{
    public static int ShowProgress(int length, int slen, int dlen, int cbparam)
    {
        Console.Write("{0} -> {1}\r", slen, dlen);
        return 1;
    }

    public static void CompressStream(Stream from, Stream to)
    {
        byte[] src = new byte[from.Length];

        // read file
        if (from.Read(src, 0, src.Length) == src.Length)
        {
            int dstSize = DllInterface.aP_max_packed_size(src.Length);
            int wrkSize = DllInterface.aP_workmem_size(src.Length);

            // allocate mem
            byte[] dst = new byte[dstSize];
            byte[] wrk = new byte[wrkSize];

            // compress data
            int packedSize = DllInterface.aPsafe_pack(
                src,
                dst,
                src.Length,
                wrk,
                new DllInterface.CompressionCallback(ShowProgress),
                0
            );

            // write compressed data
            to.Write(dst, 0, packedSize);

            Console.WriteLine("compressed to {0} bytes", packedSize);
        }
    }

    public static byte[] DecompressStream(Stream from)
    {
        byte[] src = new byte[from.Length];

        // read file
        if (from.Read(src, 0, src.Length) == src.Length)
        {
            int dstSize = DllInterface.aPsafe_get_orig_size(src);

            // allocate mem
            byte[] dst = new byte[dstSize];

            // decompress data
            int depackedSize = DllInterface.aPsafe_depack(src, src.Length, dst, dstSize);

            Console.WriteLine("decompressed to {0} bytes", depackedSize);

            return dst;
        }

        return null;
    }

}
