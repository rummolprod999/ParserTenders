#region

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

#endregion

namespace ParserTenders
{
    public class Unzipped
    {
        public static string Unzip(string filea)
        {
            var fileInf = new FileInfo(filea);
            if (fileInf.Exists)
            {
                fileInf.Refresh();
                if (fileInf.Length == 0)
                {
                    Log.Logger("File archive has size 0!!!", filea);
                }

                var rPoint = filea.LastIndexOf('.');
                var lDir = filea.Substring(0, rPoint);
                Directory.CreateDirectory(lDir);
                try
                {
                    ZipFile.ExtractToDirectory(filea, lDir);
                    fileInf.Delete();
                    return lDir;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось извлечь файл", e, filea);
                    try
                    {
                        var myProcess = new Process
                            { StartInfo = new ProcessStartInfo("unzip", $"-B {filea} -d {lDir}") };
                        myProcess.Start();
                        myProcess.WaitForExit();
                        Log.Logger("Извлекли файл альтернативным методом", filea);
                        return lDir;
                    }
                    catch (Exception exception)
                    {
                        Log.Logger("Не удалось извлечь файл альтернативным методом", exception, filea);
                        return lDir;
                    }
                }
            }

            Log.Logger("File does not exist", filea);

            return "";
        }
    }
}