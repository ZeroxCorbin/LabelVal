using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.Controllers
{
    public class FolderWatcher
    {
        public delegate void NewImage(string path);
        public event NewImage OnNewImage;

        private FileSystemWatcher Watcher { get; set; }

        public FolderWatcher()
        {

        }

        public void Start(string path)
        {
            Watcher = new FileSystemWatcher(path);

            Watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            Watcher.Changed += Watcher_Changed; ;
            Watcher.Created += Watcher_Created; ;
            Watcher.Deleted += Watcher_Deleted; ;
            Watcher.Error += Watcher_Error; ;

            Watcher.Filter = "*.png";
            Watcher.IncludeSubdirectories = false;
            Watcher.EnableRaisingEvents = true;

       }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {

        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {

        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Task.Run(() => { OnNewImage?.Invoke(e.FullPath); });
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Created)
                Task.Run(() => { OnNewImage?.Invoke(e.FullPath); });
        }

        public void Stop()
        {
            Watcher.Created -= Watcher_Changed;

            Watcher.Dispose();
            Watcher = null;
        }


    }
}
