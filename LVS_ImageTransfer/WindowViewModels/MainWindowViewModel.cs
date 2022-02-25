using LVS_ImageTransfer.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LVS_ImageTransfer.WindowViewModele
{
    public class MainWindowViewModel : Core.BaseViewModel
    {
        public bool Started
        {
            get => started;
            set { SetProperty(ref started, value); OnPropertyChanged("NotStarted"); }
        }
        private bool started = false;

        public bool NotStarted => !started;

        public string ImageSourcePath { get => App.Settings.GetValue("ImageSourcePath", ""); set => App.Settings.SetValue("ImageSourcePath", value); }
        public string Host { get => App.Settings.GetValue("FTPHost", ""); set => App.Settings.SetValue("FTPHost", value); }
        public string RootPath { get => App.Settings.GetValue("FTPRootPath", "/"); set => App.Settings.SetValue("FTPRootPath", value); }
       //public string Path { get => App.Settings.GetValue("FTPPath", ""); set => App.Settings.SetValue("FTPPath", value); }
        public string UserName { get => App.Settings.GetValue("FTPUserName", ""); set => App.Settings.SetValue("FTPUserName", value); }
        public string Password { get => App.Settings.GetValue("FTPPassword", ""); set => App.Settings.SetValue("FTPPassword", value); }

        public ICommand Start { get; }
        public ICommand Stop { get; }

        private FTPController FTPController { get; } = new FTPController();
        private FolderWatcher FolderWatcher { get; } = new FolderWatcher();

        public MainWindowViewModel()
        {
            Start = new Core.RelayCommand(StartAction, c => true);
            Stop = new Core.RelayCommand(StopAction, c => true);
        }

        private void StartAction(object parameter)
        {
            if (Directory.Exists(ImageSourcePath))
            {
                Started = true;
                FolderWatcher.Start(ImageSourcePath);

                FolderWatcher.OnNewImage -= FolderWatcher_OnNewImage;
                FolderWatcher.OnNewImage += FolderWatcher_OnNewImage;
            }
        }

        private void FolderWatcher_OnNewImage(string path)
        {
            Thread.Sleep(100);

            string ext = Path.GetExtension(path);
            string name = Path.GetFileName(path).Replace(ext, string.Empty);
            
            string dir = Path.GetDirectoryName(path);
            string newFile = $"{App.SettingsFileRootDir}\\Temp\\{name}.jpg";

            Directory.CreateDirectory($"{App.SettingsFileRootDir}\\Temp\\");

            new PNGProcess().Resize(path, newFile, 0.8);

            FTPController.Host = Host;
            FTPController.RootPath = RootPath;
            FTPController.UserName = UserName;
            FTPController.Password = Password;

            FTPController.OpenWrite(newFile, Path.GetFileName(newFile));

            File.Delete(newFile);
        }

        private void StopAction(object parameter)
        {
            Started = false;
            FolderWatcher.Stop();
        }

    }
}
