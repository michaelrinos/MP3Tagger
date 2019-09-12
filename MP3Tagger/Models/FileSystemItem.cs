using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroMVVM;

namespace MP3Tagger.Models {

    public class FakeFileSystemItem : FileSystemItem {
        public FakeFileSystemItem() : base(new DirectoryInfo("FakeDirectory")) {

        }
    }
    public class FileSystemItem : ObservableObject {

        #region Fields

        private ObservableCollection<FileSystemItem> _Children;


        #endregion // Fields

        #region Properties

        //public ObservableCollection<FileSystemItem> Children { get => _Children; set => Set(ref _Children, value); }
        public FileSystemInfo Information { get; set; }
        public DriveInfo Parent { get; set; }

        #endregion // Properties

        #region Constructor

        public FileSystemItem(FileSystemInfo info) {
            Information = info;
            if (info is DirectoryInfo) {

            } else if (info is FileInfo) {

            } else {

            }

        }

        #endregion // Constructor

        #region Methods



        #endregion // Methods


    }
}
