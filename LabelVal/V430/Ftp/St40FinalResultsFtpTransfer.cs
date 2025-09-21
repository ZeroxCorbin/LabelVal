using Logging.lib;
using System.IO;
using V430_REST_Lib.FTP;

namespace LabelVal.V430.Ftp;
public class St40FinalResultsFtpTransfer : IDisposable
{
    private FluentFTP.FtpClient? _ftpClient;

    private const string ftpPath = "/sd0:0/Config/Mfg/St40";

    public bool Connect()
    {
        _ftpClient = new FluentFTP.FtpClient("192.168.188.2");
        _ftpClient.Credentials = new System.Net.NetworkCredential("target", "password"); // Use appropriate credentials

        try
        {
            _ftpClient.Connect();
            _ftpClient.CreateDirectory(ftpPath); // Ensure the directory exists
            _ftpClient.SetWorkingDirectory(ftpPath); // Change to the target directory
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
        }

    public void Disconnect()
    {
        _ftpClient?.Disconnect();
        _ftpClient?.Dispose(); // Dispose of the FTP client to free resources
        _ftpClient = null; // Set to null to avoid using a disposed object
    }

    public bool UploadFile(byte[] data, string remoteFileName)
    {
        if (_ftpClient == null || !_ftpClient.IsConnected)
            return false;
        
        try
        {
            _ftpClient.UploadBytes(data, remoteFileName);
            Logger.Info($"File '{remoteFileName}' uploaded successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
    }

    public void Dispose() => _ftpClient?.Disconnect(); // Ensure the FTP client is disconnected when disposing of this class
}
