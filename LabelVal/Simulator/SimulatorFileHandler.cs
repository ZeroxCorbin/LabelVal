using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Simulator
{
    internal class SimulatorFileHandler
    {
        public string SimulatorImageDirectory => App.Settings.GetValue<string>(nameof(SimulatorImageDirectory));

        public bool SimulatorImageDirectoryExists => Directory.Exists(SimulatorImageDirectory);

        public List<string> Images { get; set; } = new List<string>();

        public bool HasImages { get { UpdateImageList(); return Images.Count > 0; } }

        public void UpdateImageList()
        {
            Images.Clear();

            if (SimulatorImageDirectoryExists)
            {
                foreach (string file in Directory.GetFiles(SimulatorImageDirectory))
                {
                    string ext = Path.GetExtension(file);

                    if (ext.Equals(".bmp") ||
                        ext.Equals(".png") ||
                        ext.Equals(".tif") ||
                        ext.Equals(".tiff") ||
                        ext.Equals(".jpg") ||
                        ext.Equals(".webp"))

                        Images.Add(file);
                }
            }

        }

        public bool DeleteAllImages()
        {
            if (HasImages)
            {
                bool ok = true;
                foreach (string file in Images)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        ok = false;
                    }
                }
                return ok;
            }
            return true;
        }

        public bool CopyImage(string file, string prepend)
        {

            if (SimulatorImageDirectoryExists)
            {
                File.Copy(file, Path.Combine(SimulatorImageDirectory, prepend + Path.GetFileName(file)));
                return true;
            }
            else
                return false;
        }

        public bool SaveImage(string fileName, byte[] imageData)
        {

            if (SimulatorImageDirectoryExists)
            {
                File.WriteAllBytes(Path.Combine(SimulatorImageDirectory, fileName), imageData);
                return true;
            }
            else
                return false;
        }
    }
}
