using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;
using System.Text;
using System.Threading.Tasks;

namespace LVS_ImageTransfer.Controllers
{
    internal class FTPController
    {
        public string ImageSourcePath { get => App.Settings.GetValue("ImageSourcePath", "");  }

        public string Host { get => App.Settings.GetValue("FTPHost", ""); }
        public string RootPath { get => App.Settings.GetValue("FTPRootPath", "/*.*"); }
        public string UserName { get => App.Settings.GetValue("FTPUserName", ""); }
        public string Password { get => App.Settings.GetValue("FTPPassword", "");  }


        public void OpenWrite(string localFilePath, string fileName)
        {
            using (FtpClient conn = new FtpClient())
            {
                conn.Host = Host;
                conn.Credentials = new NetworkCredential(UserName, Password);

                using (Stream ostream = conn.OpenWrite(RootPath + fileName))
                {
                    try
                    {
                        using (Stream istream = File.OpenRead(localFilePath))
                        {
                            istream.CopyTo(ostream);
                        }
                    }
                    finally
                    {
                        ostream.Close();
                    }
                }
            }
        }
        //public void OpenRead()
        //{
        //    using (FtpClient conn = new FtpClient())
        //    {
        //        conn.Host = Host;
        //        conn.Credentials = new NetworkCredential(UserName, Password);

        //        using (Stream istream = conn.OpenRead(RootPath + Path))
        //        {
        //            try
        //            {
        //                // istream.Position is incremented accordingly to the reads you perform
        //                // istream.Length == file size if the server supports getting the file size
        //                // also note that file size for the same file can vary between ASCII and Binary
        //                // modes and some servers won't even give a file size for ASCII files! It is
        //                // recommended that you stick with Binary and worry about character encodings
        //                // on your end of the connection.
        //            }
        //            finally
        //            {
        //                Console.WriteLine();
        //                istream.Close();
        //            }
        //        }
        //    }
        //}

    }
}
