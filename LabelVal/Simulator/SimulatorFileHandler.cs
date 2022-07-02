using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Simulator
{
    internal class SimulatorFileHandler
    {
        public string SimulatorImageDirectory { get => App.Settings.GetValue("Simulator_ImageDirectory", "D:\\V275_Watson_BV_Beta\\Product\\v275\\data\\images\\simulation"); set { App.Settings.SetValue("Simulator_ImageDirectory", value); } }

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
                try
                {
                    foreach (string file in Images)
                        File.Delete(file);
                }
                catch
                {
                    return false;
                }

            }
            return true;
        }

        public bool CopyImage(string file)
        {
            try
            {
                if (SimulatorImageDirectoryExists)
                {
                    File.Copy(file, Path.Combine(SimulatorImageDirectory, Path.GetFileName(file)));
                    return true;
                }
                else
                    return false;
 
            }
            catch
            {
                return false;
            }

        }


        public bool SaveImage(string file, byte[] imageData)
        {
            try
            {
                if (SimulatorImageDirectoryExists)
                {
                    File.WriteAllBytes(Path.Combine(SimulatorImageDirectory, Path.GetFileName(file)), imageData);
                    return true;
                }
                else
                    return false;

            }
            catch
            {
                return false;
            }

        }
    }
}
