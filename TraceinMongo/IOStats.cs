using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TraceinMongo
{
    class IOStats
    {
        public static bool FileExists(string filename)
        {
            return new FileInfo(filename).Exists;
        }
        public static string GenerateFileName(string dirname, string pattern)
        {
            return String.Format("{0}{1}.xml", pattern,
                new DirectoryInfo(dirname).GetFiles(pattern).Count() + 1);
        }

        public static void WriteData(string filename, byte[] array)
        {
            File.OpenWrite(filename).Write(array, 0, array.Length);
        }
    }
}
