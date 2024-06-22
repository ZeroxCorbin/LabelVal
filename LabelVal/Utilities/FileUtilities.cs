using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabelVal.Utilities
{
    public static class FileUtilities
    {
        public class LoadFileDialogFilter
        {
            public string Description { get; set; }
            public List<string> Extensions { get; set; } = [];
        }

        public class LoadFileDialogSettings
        {
            public List<LoadFileDialogFilter> Filters { get; set; } = new List<LoadFileDialogFilter>();
            public string FilterString { get => filterString ?? GenerateFilterString(Filters); set => filterString = value; }
            private string filterString = null;
            public string Title { get; set; }
            public string Description { get; set; }
            public bool Multiselect { get; set; }

            public int SelectedFilterIndex { get; set; }
            public string SelectedFile { get; set; }
            public List<string> SelectedFiles { get; set; }
        }

        public static bool LoadFileDialog(LoadFileDialogSettings settings)
        {
            Microsoft.Win32.OpenFileDialog diag = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = settings.FilterString,
                Title = settings.Title,
                Multiselect = settings.Multiselect,
                FileName = settings.SelectedFile
            };

            if (diag.ShowDialog() == true)
            {
                settings.SelectedFilterIndex = diag.FilterIndex;

                if (settings.Multiselect)
                    settings.SelectedFiles = diag.FileNames.ToList();
                else
                    settings.SelectedFile = diag.FileName;

                return true;
            }
            else
                return false;
        }

        public static string GenerateFilterString(List<LoadFileDialogFilter> filterEntries)
        {
            var filterBuilder = new StringBuilder();
            foreach (var entry in filterEntries)
            {
                if (filterBuilder.Length > 0)
                    filterBuilder.Append("|");

                var extensions = string.Join(";", entry.Extensions.Select(ext => $"*.{ext}"));
                filterBuilder.Append($"{entry.Description}|{extensions}");
            }
            return filterBuilder.ToString();
        }

        public static string LoadFileDialog(string fileName = "", string filter = "All Files|*.*", string title = "Select a file.")
        {
            var settings = new LoadFileDialogSettings
            {
                FilterString = filter,
                Title = title,
                SelectedFile = fileName
            };

            if (LoadFileDialog(settings))
                return settings.SelectedFile;
            else
                return null;
        }

        /// <summary>
        /// 
        /// Image Files|*.png;*.bmp|PNG Files|*.png|BMP Files|*.bmp
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static List<string> LoadFileDialog(string filter = "All Files|*.*", string title = "Select image files.")
        {
            var settings = new LoadFileDialogSettings
            {
                FilterString = filter,
                Title = title,
                Multiselect = true
            };

            if (LoadFileDialog(settings))
                return settings.SelectedFiles;
            else
                return null;
        }


        public class SaveFileDialogSettings
        {
            public List<LoadFileDialogFilter> Filters { get; set; } = new List<LoadFileDialogFilter>();
            public string FilterString { get => filterString ?? GenerateFilterString(Filters); set => filterString = value; }
            private string filterString = null;
            public string Title { get; set; }
            public string InitialFileName { get; set; }
            public string SelectedFileName { get; set; }
            public int SelectedFilterIndex { get; set; }
        }

        public static bool SaveFileDialog(SaveFileDialogSettings settings)
        {
            Microsoft.Win32.SaveFileDialog diag = new()
            {
                Filter = settings.FilterString,
                Title = settings.Title,
                FileName = settings.InitialFileName
            };

            if (diag.ShowDialog() == true)
            {
                settings.SelectedFilterIndex = diag.FilterIndex;
                settings.SelectedFileName = diag.FileName;
                return true;
            }
            else
                return false;
        }

        public static string GetSaveFilePath(string fileName = "", string filter = "All Files|*.*", string title = "Save a file.")
        {
            SaveFileDialogSettings settings = new SaveFileDialogSettings
            {
                FilterString = filter,
                Title = title,
                InitialFileName = fileName
            };

            if (FileUtilities.SaveFileDialog(settings))
                return settings.SelectedFileName;
            else
                return null;

        }

    }
}
