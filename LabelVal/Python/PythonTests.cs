using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Python
{
    public static class PythonTests
    {
        public static void RunPythonTests(byte[] imageUnderTest)
        {
            var imageut = $"{App.PythonWorkingDirectory}\\imageUnderTest.png";

            File.WriteAllBytes(imageut, imageUnderTest);
            string pythonPath = @"C:\\Users\\Jack.Bowling\\AppData\\Local\\Programs\\Python\\Python313\\python.exe"; // Adjust the path to your Python installation
            string scriptPath = $"{App.PythonRootDirectory}\\Contamination\\bad_pixel_detect.py"; // Adjust the path to your Python script
            string arguments = App.PythonWorkingDirectory; // Adjust the arguments as needed
            var pythonExecutor = new PythonExecutor(pythonPath, scriptPath, arguments);
            pythonExecutor.Execute();

            scriptPath = $"{App.PythonRootDirectory}\\Contamination\\dirtdetect.py"; // Adjust the path to your Python script
            var pythonExecutor2 = new PythonExecutor(pythonPath, scriptPath, arguments);
            pythonExecutor2.Execute();
        }
    }
}
