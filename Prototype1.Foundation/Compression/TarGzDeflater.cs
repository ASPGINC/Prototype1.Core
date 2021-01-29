using System.Collections.Generic;
using System.IO;
using Prototype1.Foundation.Compression.GZip;
using Prototype1.Foundation.Compression.Tar;

namespace Prototype1.Foundation.Compression
{
    public class TarGzDeflater
    {
        public static List<string> Deflate(string tarGzFilePath, string destinationFolder)
        {
            using(Stream fileStream = File.OpenRead(@"d:\Panera\FDGGWS_Certificate_WS1001318285._.1.tar.gz"))
            {
                using (var gZipInputStream = new GZipInputStream(fileStream))
                {
                    using (var archive = TarArchive.CreateInputTarArchive(gZipInputStream))
                    {
                        return archive.ExtractContents(destinationFolder);
                    }
                }
            }
        }
    }
}
