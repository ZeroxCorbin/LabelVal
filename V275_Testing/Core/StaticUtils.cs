#if NEWTONSOFT
using Newtonsoft.Json;
using System;
using System.IO;
#endif

namespace V275_Testing.Core
{
    public static class StaticUtils
    {
        public static class Regex
        {
            public static bool CheckValidIP(string str)
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^((0|1[0-9]{0,2}|2[0-9]?|2[0-4][0-9]|25[0-5]|[3-9][0-9]?)\.){3}(0|1[0-9]{0,2}|2[0-9]?|2[0-4][0-9]|25[0-5]|[3-9][0-9]?)$");
                return regex.IsMatch(str);
            }
            public static bool CheckValidPort(string str)
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");
                return regex.IsMatch(str);
            }
            public static bool CheckFloat(string str)
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)$");
                return regex.IsMatch(str);
            }

            public static bool CheckAlphaNumeric(string str)
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9]+$");
                return regex.IsMatch(str);
            }
            public static bool CheckNumeric(string str)
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[0-9]+$");
                return regex.IsMatch(str);
            }
        }

        //public static class Directory
        //{
        //    private static bool CheckWriteAccess(string folderPath)
        //    {
        //        try
        //        {
        //            // Attempt to get a list of security permissions from the folder. 
        //            // This will raise an exception if the path is read only or do not have access to view the permissions. 
        //            System.Security.AccessControl.DirectorySecurity ds = System.IO.Directory.GetAccessControl(folderPath);
        //            return true;
        //        }
        //        catch (System.UnauthorizedAccessException)
        //        {
        //            return false;
        //        }
        //    }
        //}

#if NEWTONSOFT
        public static class Json
        {
            public static void WriteObjectToFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
                try
                {
                    using (TextWriter writer = new StreamWriter(filePath, append))
                    {
                        writer.Write(contentsToWriteToFile + Environment.NewLine);
                    }
                }
                catch { }
            }

            public static T ReadObjectFromFile<T>(string filePath) where T : new()
            {
                TextReader reader = null;
                try
                {
                    reader = new StreamReader(filePath);
                    var fileContents = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(fileContents);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }

            public static string ReadStringFromFile<T>(string filePath) where T : new()
            {
                TextReader reader = null;
                try
                {
                    reader = new StreamReader(filePath);
                    var fileContents = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(fileContents).ToString();
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }

            private static StreamReader _Reader = null;

            public static void OpenFile(string filePath)
            {
                if (_Reader == null) _Reader = new StreamReader(filePath);
            }

            public static T ReadLineFromFile<T>() where T : new()
            {
                if (_Reader == null) return default(T);
                try
                {
                    if (_Reader.EndOfStream) return default(T);
                    var fileContents = _Reader.ReadLine();
                    return JsonConvert.DeserializeObject<T>(fileContents);
                }
                finally
                {
                }
            }

            public static void CloseFile()
            {
                if (_Reader != null)
                    _Reader.Close();

                _Reader = null;
            }
        }
#endif
    }
}