using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HexWide
{
    public partial class FilePickerWindow : Window
    {
        public string? SelectedFile { get; private set; }
        public string InitialDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public string Filter { get; set; } = "*.exe";

        public FilePickerWindow()
        {
            InitializeComponent();
            Loaded += FilePickerWindow_Loaded;
        }

        private void FilePickerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDrives();
            if (Directory.Exists(InitialDirectory))
            {
                SelectPath(InitialDirectory);
            }
        }

        private void PopulateDrives()
        {
            folderTree.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var item = new TreeViewItem { Header = drive.Name, Tag = drive.RootDirectory.FullName };
                item.Items.Add(null);
                item.Expanded += Folder_Expanded;
                folderTree.Items.Add(item);
            }
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem item) return;
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                try
                {
                    var path = (string)item.Tag;
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var sub = new TreeViewItem { Header = System.IO.Path.GetFileName(dir), Tag = dir };
                        sub.Items.Add(null);
                        sub.Expanded += Folder_Expanded;
                        item.Items.Add(sub);
                    }
                }
                catch { }
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (folderTree.SelectedItem is TreeViewItem tvi && tvi.Tag is string path)
            {
                SelectPath(path);
            }
        }

        private void SelectPath(string path)
        {
            txtCurrentPath.Text = path;
            fileList.Items.Clear();
            try
            {
                var files = Directory.GetFiles(path, Filter).Select(f => new FileItem(f));
                foreach (var fi in files)
                    fileList.Items.Add(fi);
            }
            catch { }
        }

        private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (fileList.SelectedItem is FileItem fi)
            {
                SelectedFile = fi.FullPath;
                DialogResult = true;
                Close();
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var cur = txtCurrentPath.Text;
            if (string.IsNullOrWhiteSpace(cur)) return;
            var parent = Directory.GetParent(cur);
            if (parent != null) SelectPath(parent.FullName);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (fileList.SelectedItem is FileItem fi)
            {
                SelectedFile = fi.FullPath;
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private class FileItem
        {
            public string Name { get; }
            public string FullPath { get; }
            public string SizeText { get; }

            public FileItem(string path)
            {
                FullPath = path;
                Name = System.IO.Path.GetFileName(path);
                try
                {
                    var len = new FileInfo(path).Length;
                    SizeText = len >= 1024 ? $"{len / 1024} KB" : $"{len} B";
                }
                catch { SizeText = "-"; }
            }
        }
    }
}
