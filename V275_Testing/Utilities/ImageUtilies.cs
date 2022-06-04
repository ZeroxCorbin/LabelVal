using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.Utilities
{
    public static class ImageUtilities
    {
        public static string ImageUID(byte[] image)
        {
            try
            {
                using (SHA256 md5 = SHA256.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(image)).Replace("-", String.Empty);
                }

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

    }
}
