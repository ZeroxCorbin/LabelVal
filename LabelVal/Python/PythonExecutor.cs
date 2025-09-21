using Logging.lib;

namespace LabelVal.Python;

public class IronPythonExecutor
{
    public string ScriptPath { get; set; }
    public string Arguments { get; set; }
    public IronPythonExecutor(string scriptPath, string arguments)
    {
        ScriptPath = scriptPath;
        Arguments = arguments;
    }
    public void Execute()
    {
        Microsoft.Scripting.Hosting.ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();
        Microsoft.Scripting.Hosting.ScriptScope scope = engine.CreateScope();
        Microsoft.Scripting.Hosting.ScriptSource script = engine.CreateScriptSourceFromFile(ScriptPath);
        _ = new IronPython.Runtime.PythonDictionary();

        try
        {
            script.Execute();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        finally
        {
            // Clean up resources if needed
        }
    }
}
    public class PythonExecutor
    {
        public string PythonPath { get; set; }
        public string ScriptPath { get; set; }
        public string Arguments { get; set; }
        public PythonExecutor(string pythonPath, string scriptPath, string arguments)
        {
            PythonPath = pythonPath;
            ScriptPath = scriptPath;
            Arguments = arguments;
        }
        public void Execute()
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = PythonPath,
                Arguments = $"\"{ScriptPath}\" \"{Arguments}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(processInfo);
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
        Logger.Debug($"Python script output:\n{output}");
    }
}
