using System;


namespace Xms.OCR
{
    class Program
    {
        static void Main(string[] args)
        {
            Invoice i = Util.OCR_Invoice(@"D:\test\fapiao.pdf", @"F:\VS\PaddleOCR-release-2.1\config.txt", false);
            if (i.Stream != null)
                i.Stream.Dispose();
            string json = JsonHelper.SerializeObject(i);
            Console.WriteLine(json);
        }
    }
}
