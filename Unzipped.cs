using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace ParserTenders
{
    public class Unzipped
    {
        public static string Unzip(string filea)
        {
            FileInfo fileInf = new FileInfo(filea);
            if (fileInf.Exists)
            {
                int r_point = filea.LastIndexOf('.');
                string l_dir = filea.Substring(0, r_point);
                Directory.CreateDirectory(l_dir);
                try
                {
                    ZipFile.ExtractToDirectory(filea, l_dir);
                    fileInf.Delete();
                    return l_dir;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось извлечь файл", e, filea);
                    try
                    {
                        var MyProcess = new Process {StartInfo = new ProcessStartInfo("unzip", $"{filea} -d {l_dir}")};
                        MyProcess.Start();
                        Log.Logger("Извлекли файл альтернативным методом", filea);
                        MyProcess.WaitForExit();
                        return l_dir;
                    }
                    catch (Exception exception)
                    {
                        Log.Logger("Не удалось извлечь файл альтернативным методом", exception, filea);
                        return l_dir;
                    }
                }
            }

            return "";
        }
    }
}