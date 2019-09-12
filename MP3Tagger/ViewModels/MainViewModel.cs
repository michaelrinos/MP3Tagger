using MicroMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MP3Tagger.ViewModels {
    class MainViewModel : ObservableObject {


        #region Fields 

        private FileSystemItemViewModel _fevm;
        private ObservableCollection<FileSystemItemViewModel> _Items;
        private FileSystemItemViewModel _SelectedLocation;
        private ICollectionView _SearchResults;
        private string _TreeText;

        #endregion // Fields

        #region Properties 

        public string SearchText { get; set; } = "Search";
        public ICollectionView SearchResults { get => _SearchResults; set => Set(ref _SearchResults, value); }

        public ObservableCollection<FileSystemItemViewModel> Items { get => _Items ?? (_Items = new ObservableCollection<FileSystemItemViewModel>()); set => Set(ref _Items,value); }
        public FileSystemItemViewModel SelectedLocation { get => _SelectedLocation;
            set => Set(ref _SelectedLocation, value); }
        public string TreeText {
            get => _TreeText;
            internal set {
                if (Set(ref _TreeText, value)) {
                    FindNode();
                }
            }
        }


        #endregion // Properties

        #region Constructor 

        public MainViewModel() {
            foreach (var di in DriveInfo.GetDrives()) {
                Items.Add(new FileSystemItemViewModel(di));
            }

        }

        public void Search(FileSystemInfo info, string searchterm) {
            StringBuilder pattern = new StringBuilder();
            pattern.Append("(?i)");
            foreach ( var letter in searchterm) {
                pattern.Append("[^"); 
                pattern.Append(letter);
                pattern.Append("]*");
                pattern.Append(letter);
            }
            Console.WriteLine(pattern.ToString());
            Regex reg = new Regex(pattern.ToString());
            IEnumerable<string> items;
                
            var results = new List<FileInfo>();
            SearchResults = CollectionViewSource.GetDefaultView(results);
            if (info is FileInfo) { // We are given a file
                if (reg.IsMatch(info.Name)) {
                    results.Add(info as FileInfo);
                } else {
                }
            }
            var dirInfo = info as DirectoryInfo;
            foreach(var dir in dirInfo.GetDirectories()) { // We are given a directory, check its children directories
                Search(dir, reg, results);
            }
            results.AddRange(dirInfo.GetFiles().Where(path => reg.IsMatch(path.Name)));
        }

        public void Search(DirectoryInfo info, Regex reg, List<FileInfo> results) {
            try {
                foreach (var dir in info.GetDirectories()) { // We are given a directory, check its children directories
                    Search(dir, reg, results);
                }
                results.AddRange(info.GetFiles().Where(path => reg.IsMatch(path.Name)));
            } catch (Exception e) {
                e = e;
            }
        }

        #endregion // Constructor

        #region Methods 

        private void FindNode() {
            
        }

        #endregion // Methods



    }
}
