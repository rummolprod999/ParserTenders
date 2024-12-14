#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

#endregion

namespace ParserTenders
{
    public class WorkWithFtp
    {
        private readonly int _bufferSize = 1024;
        private readonly string _password;
        private string _uri;
        private readonly string _userName;
        public bool Binary = true;
        public bool EnableSsl = false;
        public bool Hash = false;
        public bool Passive = true;

        public WorkWithFtp(string uri, string userName, string password)
        {
            _uri = uri;
            _userName = userName;
            _password = password;
        }

        public string ChangeWorkingDirectory(string path)
        {
            _uri = Combine(_uri, path);

            return PrintWorkingDirectory();
        }

        public string DeleteFile(string fileName)
        {
            var request = CreateRequest(Combine(_uri, fileName), WebRequestMethods.Ftp.DeleteFile);

            return GetStatusDescription(request);
        }

        public string DownloadFile(string source, string dest)
        {
            var request = CreateRequest(Combine(_uri, source), WebRequestMethods.Ftp.DownloadFile);

            var buffer = new byte[_bufferSize];

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var fs = new FileStream(dest, FileMode.OpenOrCreate))
                    {
                        var readCount = stream.Read(buffer, 0, _bufferSize);

                        while (readCount > 0)
                        {
                            if (Hash)
                            {
                                Console.Write("#");
                            }

                            fs.Write(buffer, 0, readCount);
                            readCount = stream.Read(buffer, 0, _bufferSize);
                        }
                    }
                }

                return response.StatusDescription;
            }
        }

        public DateTime GetDateTimestamp(string fileName)
        {
            var request = CreateRequest(Combine(_uri, fileName), WebRequestMethods.Ftp.GetDateTimestamp);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                return response.LastModified;
            }
        }

        public long GetFileSize(string fileName)
        {
            var request = CreateRequest(Combine(_uri, fileName), WebRequestMethods.Ftp.GetFileSize);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                return response.ContentLength;
            }
        }

        public List<string> ListDirectory()
        {
            var list = new List<string>();

            var request = CreateRequest(WebRequestMethods.Ftp.ListDirectory);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                    }
                }
            }

            return list;
        }

        public List<string> ListDirectoryFull()
        {
            var list = new List<string>();

            var request = CreateRequest(WebRequestMethods.Ftp.ListDirectoryDetails);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            var file = reader.ReadLine();
                            //var size = GetFileSize(file);
                            Console.WriteLine(file);
                            //Console.WriteLine(size);
                            Console.WriteLine();
                            list.Add(file);
                        }
                    }
                }
            }

            return list;
        }

        public string[] ListDirectoryDetails()
        {
            var list = new List<string>();

            var request = CreateRequest(WebRequestMethods.Ftp.ListDirectoryDetails);

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                    }
                }
            }

            return list.ToArray();
        }

        public string MakeDirectory(string directoryName)
        {
            var request = CreateRequest(Combine(_uri, directoryName), WebRequestMethods.Ftp.MakeDirectory);

            return GetStatusDescription(request);
        }

        public string PrintWorkingDirectory()
        {
            var request = CreateRequest(WebRequestMethods.Ftp.PrintWorkingDirectory);

            return GetStatusDescription(request);
        }

        public string RemoveDirectory(string directoryName)
        {
            var request = CreateRequest(Combine(_uri, directoryName), WebRequestMethods.Ftp.RemoveDirectory);

            return GetStatusDescription(request);
        }

        public string Rename(string currentName, string newName)
        {
            var request = CreateRequest(Combine(_uri, currentName), WebRequestMethods.Ftp.Rename);

            request.RenameTo = newName;

            return GetStatusDescription(request);
        }

        public string UploadFile(string source, string destination)
        {
            var request = CreateRequest(Combine(_uri, destination), WebRequestMethods.Ftp.UploadFile);

            using (var stream = request.GetRequestStream())
            {
                using (var fileStream = File.Open(source, FileMode.Open))
                {
                    int num;

                    var buffer = new byte[_bufferSize];

                    while ((num = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (Hash)
                        {
                            Console.Write("#");
                        }

                        stream.Write(buffer, 0, num);
                    }
                }
            }

            return GetStatusDescription(request);
        }

        public string UploadFileWithUniqueName(string source)
        {
            var request = CreateRequest(WebRequestMethods.Ftp.UploadFileWithUniqueName);

            using (var stream = request.GetRequestStream())
            {
                using (var fileStream = File.Open(source, FileMode.Open))
                {
                    int num;

                    var buffer = new byte[_bufferSize];

                    while ((num = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (Hash)
                        {
                            Console.Write("#");
                        }

                        stream.Write(buffer, 0, num);
                    }
                }
            }

            using (var response = (FtpWebResponse)request.GetResponse())
            {
                return Path.GetFileName(response.ResponseUri.ToString());
            }
        }

        private FtpWebRequest CreateRequest(string method)
        {
            return CreateRequest(_uri, method);
        }

        private FtpWebRequest CreateRequest(string uri, string method)
        {
            var r = (FtpWebRequest)WebRequest.Create(uri);

            r.Credentials = new NetworkCredential(_userName, _password);
            r.Method = method;
            r.UseBinary = Binary;
            r.EnableSsl = EnableSsl;
            r.UsePassive = Passive;

            return r;
        }

        private string GetStatusDescription(FtpWebRequest request)
        {
            using (var response = (FtpWebResponse)request.GetResponse())
            {
                return response.StatusDescription;
            }
        }

        public string Combine(string path1, string path2)
        {
            return path1 + path2;
        }
    }
}