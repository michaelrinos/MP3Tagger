using MicroMVVM;
using MP3Tagger.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MP3Tagger.ViewModels {

    #region Enums

    public enum State {
        Undefined,
        Open,
        Closed
    }
    public enum Type {
        Drive,
        Folder,
        File
    }
    public enum IconSize : short {
        Small,
        Large
    }
    public enum FileAttribute : uint {
        Directory = 16,
        File = 256
    }

    [Flags]
    public enum ShellAttribute : uint {
        LargeIcon = 0,              // 0x000000000
        SmallIcon = 1,              // 0x000000001
        OpenIcon = 2,               // 0x000000002
        ShellIconSize = 4,          // 0x000000004
        Pidl = 8,                   // 0x000000008
        UseFileAttributes = 16,     // 0x000000010
        AddOverlays = 32,           // 0x000000020
        OverlayIndex = 64,          // 0x000000040
        Others = 128,               // Not defined, really?
        Icon = 256,                 // 0x000000100  
        DisplayName = 512,          // 0x000000200
        TypeName = 1024,            // 0x000000400
        Attributes = 2048,          // 0x000000800
        IconLocation = 4096,        // 0x000001000
        ExeType = 8192,             // 0x000002000
        SystemIconIndex = 16384,    // 0x000004000
        LinkOverlay = 32768,        // 0x000008000 
        Selected = 65536,           // 0x000010000
        AttributeSpecified = 131072 // 0x000020000
    }

    #endregion // Enums

    public class FakeFileSystemItemViewModel : FileSystemItemViewModel {
        public FakeFileSystemItemViewModel() : base(new DirectoryInfo("FakeFileSystemItemViewModel")) {

        }
    }
    public class FileSystemItemViewModel : ObservableObject {
        #region Fields 

        private string _Name;
        private string _Path;

        private ObservableCollection<FileSystemItemViewModel> _Items;
        private bool _IsExpanded;
        private bool _IsSelected;

        #endregion // Fields

        #region Properties 

        public ImageSource Image { get; private set; }

        public FileSystemItem Info { get; private set; }
        public string Name { get => _Name ; set => Set(ref _Name, value); }
        public string Path { get => _Path; set => Set(ref _Path, value); }
        public FileSystemItemViewModel Parent { get; internal set; }
        public ObservableCollection<FileSystemItemViewModel> Items { get => _Items ?? (_Items = new ObservableCollection<FileSystemItemViewModel>()); set => Set(ref _Items, value); }
        public bool IsExpanded { get => _IsExpanded; set => Set(ref _IsExpanded, value); }
        public bool IsSelected { get => _IsSelected; set => Set(ref _IsSelected, value); }

        #endregion // Properties

        #region Constructor 
        public FileSystemItemViewModel(DriveInfo info) : this(info.RootDirectory) {
            //Image = 
        }

        public FileSystemItemViewModel(FileSystemInfo info) {
            Name = info.Name;
            if (this is FakeFileSystemItemViewModel) {
                return;
            }
            Info = new FileSystemItem(info);
            Path = info.FullName;
            if (info is DirectoryInfo) {
                Image = FolderManager.GetImageSource(info.FullName, State.Closed);
                Items.Add(new FakeFileSystemItemViewModel());
            } else if (info is FileInfo) {
                Image = FileManager.GetImageSource(info.FullName);
            }

            base.PropertyChanged += HandlePropertyChanged;

        }



        #endregion // Constructor

        #region Methods 

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName.Equals("IsExpanded")) {
                // Remove the fake item to remove the carat 
                if (Items.FirstOrDefault() is FakeFileSystemItemViewModel) { Items.Remove(Items.First()); }
                if (Info.Information is DirectoryInfo) { // We are a directory 
                    InspectDirectory();
                }
            }
        }

        private void InspectDirectory() {
            foreach( var dir in ((DirectoryInfo)Info.Information).GetDirectories()) {
                Items.Add(new FileSystemItemViewModel(dir) { Parent = this });
            }
            foreach (var file in ((DirectoryInfo)Info.Information).GetFiles()){
                Items.Add(new FileSystemItemViewModel(file) { Parent = this });
            }
        }


        #endregion // Methods


    }
    public class ShellManager {
        public static Icon GetIcon(string path, Type type, IconSize iconSize, State state) {
            var attributes = (uint)(type == Type.Folder ? FileAttribute.Directory : FileAttribute.File);
            var flags = (uint)(ShellAttribute.Icon | ShellAttribute.UseFileAttributes);

            if (type == Type.Folder && state == State.Open) {
                flags = flags | (uint)ShellAttribute.OpenIcon;
            }
            if (iconSize == IconSize.Small) {
                flags = flags | (uint)ShellAttribute.SmallIcon;
            } else {
                flags = flags | (uint)ShellAttribute.LargeIcon;
            }

            var fileInfo = new ShellFileInfo();
            var size = (uint)Marshal.SizeOf(fileInfo);
            var result = Interop.SHGetFileInfo(path, attributes, out fileInfo, size, flags);

            if (result == IntPtr.Zero) {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            try {
                return (Icon)Icon.FromHandle(fileInfo.hIcon).Clone();
            } catch {
                throw;
            } finally {
                Interop.DestroyIcon(fileInfo.hIcon);
            }
        }
    }
    public static class Interop {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string path,
            uint attributes,
            out ShellFileInfo fileInfo,
            uint size,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr pointer);
    }



    public static class FolderManager {
        public static ImageSource GetImageSource(string directory,State folderType) {
            using (var icon = ShellManager.GetIcon(directory, Type.Folder, IconSize.Large, folderType)) {
                return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                   System.Windows.Int32Rect.Empty,
                   BitmapSizeOptions.FromWidthAndHeight(16,16));

            }
        }
    }
    public static class FileManager {
        public static ImageSource GetImageSource(string filename) {
            return GetImageSource(filename, new Size(16, 16));
        }

        public static ImageSource GetImageSource(string filename, Size size) {
            using (var icon = ShellManager.GetIcon(Path.GetExtension(filename), Type.File, IconSize.Small, State.Undefined)) {
                return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(size.Width, size.Height));
            }
        }
    }



}
