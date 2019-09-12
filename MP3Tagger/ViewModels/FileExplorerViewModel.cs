using MicroMVVM;
using MP3Tagger.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MP3Tagger.ViewModels {
     public class FileExplorerViewModel : ObservableObject {


        #region Fields 
        private IList<Item> _directories;
        private ICommand _folderChanged;

        #endregion // Fields

        #region Properties 

        public IList<Item> Directories { get => _directories ?? (_directories = new ObservableCollection<Item>()); set => SetField(ref _directories, value); }
        public ICommand FolderChanged { get { return _folderChanged ?? (_folderChanged = new RelayCommand<object>(FolderChangedExecute)); } private set { SetField(ref _folderChanged, value); } }


        #endregion // Properties

        #region Constructor 

        public FileExplorerViewModel() {
            //var envHome = RuntimeInformation.InOSPlatform(OSPlatform.Windows) ? "HOMEPATH" : "HOME";

            Directories = ItemProvider.GetDrives();

        }

        #endregion // Constructor

        #region Methods 

        private void FolderChangedExecute(object obj) {
        }

        #endregion // Methods


    }
}
