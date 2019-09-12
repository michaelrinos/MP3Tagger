using MicroMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.ViewModels {
    class MainViewModel : ObservableObject {


        #region Fields 

        private FileSystemItemViewModel _fevm;
        private ObservableCollection<FileSystemItemViewModel> _Items;
        private FileSystemItemViewModel _SelectedLocation;

        #endregion // Fields

        #region Properties 

        public string SearchText { get; set; } = "Search";

        public ObservableCollection<FileSystemItemViewModel> Items { get => _Items ?? (_Items = new ObservableCollection<FileSystemItemViewModel>()); set => Set(ref _Items,value); }
        public FileSystemItemViewModel SelectedLocation { get => _SelectedLocation; set => Set(ref _SelectedLocation, value); }

        #endregion // Properties

        #region Constructor 

        public MainViewModel() {
            foreach (var di in DriveInfo.GetDrives()) {
                Items.Add(new FileSystemItemViewModel(di));
            }

        }

        #endregion // Constructor

        #region Methods 

        #endregion // Methods



    }
}
