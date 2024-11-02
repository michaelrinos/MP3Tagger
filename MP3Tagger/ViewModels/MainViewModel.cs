using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MP3Tagger.NewFolder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace MP3Tagger.ViewModels {
    public class MainViewModel : ObservableObject {


        #region Fields 

        private ObservableCollection<FileSystemItemViewModel> _Items;
        private FileSystemItemViewModel _SelectedLocation;
        private ICollectionView _SearchResults;
        private string _TreeText = string.Empty;
        private MusicEditorViewModel _Manager;
        private IServiceProvider serviceProvider;

        #endregion // Fields

        #region Properties 

        public string SearchText { get; set; } = "Search";
        public ICollectionView SearchResults { get => _SearchResults; set => Set(ref _SearchResults, value); }

        public ObservableCollection<FileSystemItemViewModel> Items { get => _Items ?? (_Items = new ObservableCollection<FileSystemItemViewModel>()); set => Set(ref _Items, value); }
        public FileSystemItemViewModel SelectedLocation {
            get => _SelectedLocation;
            set {
                if (Set(ref _SelectedLocation, value)) {
                    if (value.Info.Information is DirectoryInfo ) Manager = new MusicEditorViewModel(serviceProvider.GetRequiredService<IMusicService>(), value.Info.Information as DirectoryInfo);
                }
            }
        }

        public string SelectedLocationStr { get => SelectedLocation?.Path; }

        public MusicEditorViewModel Manager {get => _Manager; set => Set(ref _Manager, value);}

        public string TreeText {
            get => _TreeText;
            internal set {
                if (Set(ref _TreeText, value)) {
                }
                FindNode();
            }
        }


        #endregion // Properties

        #region Constructor 

        public MainViewModel(IServiceProvider serviceProvider) {

            this.serviceProvider = serviceProvider;
            foreach (var di in DriveInfo.GetDrives()) {
                Items.Add(new FileSystemItemViewModel(di));
            }

        }

        public void Search(FileSystemInfo info, string searchterm) {
            // First check that we were not provided a path
            // C:\Users\mrino\Music\Test
            var results = new List<FileInfo>();
            if (Regex.IsMatch(searchterm, @"[.\\/]", RegexOptions.IgnoreCase))
            {
                if (File.Exists(searchterm))
                {
                }else if (Directory.Exists(searchterm))
                {
                    var d = new DirectoryInfo(searchterm);
                    var t = serviceProvider.GetRequiredService<MusicEditorViewModel>();
                    Manager = t;
                    /*
                    Manager.CurrentDirectory = d;
                    Manager.LoadFiles();
                    //results.AddRange( d.GetFiles() );
                    */
                    
                }

            }
            else
            {
                StringBuilder pattern = new StringBuilder();
                // Fuzzy Search
                pattern.Append("(?i)");
                foreach (var letter in searchterm)
                {
                    pattern.Append("[^");
                    pattern.Append(letter);
                    pattern.Append("]*");
                    pattern.Append(letter);
                }
                Console.WriteLine(pattern.ToString());
                Regex reg = new Regex(pattern.ToString());
                IEnumerable<string> items;

                SearchResults = CollectionViewSource.GetDefaultView(results);
                if (info is FileInfo)
                { // We are given a file
                    if (reg.IsMatch(info.Name))
                    {
                        results.Add(info as FileInfo);
                    }
                    else
                    {
                    }
                }
                var dirInfo = info as DirectoryInfo;
                foreach (var dir in dirInfo.GetDirectories())
                { // We are given a directory, check its children directories
                    Search(dir, reg, results);
                }
                results.AddRange(dirInfo.GetFiles().Where(path => reg.IsMatch(path.Name)));
            }
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
            if (string.IsNullOrEmpty(TreeText)) return;
            Debug.WriteLine(string.Format("TreeText \t{0}", TreeText));
            var oldNode = SelectedLocation;
            var node = SelectedLocation?.Parent?.Items.Where(p => p.Name != oldNode.Name && p.Name.ToLower().StartsWith(TreeText.ToLower())).FirstOrDefault();
            if (node != default(FileSystemItemViewModel)) {
                node.IsSelected = true;
            } else {
                node = SelectedLocation.Items.Where(p => p.Name != oldNode.Name && p.Name.ToLower().StartsWith(TreeText.ToLower())).FirstOrDefault();
                if (node != default(FileSystemItemViewModel)) {
                    node.Parent.IsExpanded = true;
                    node.Parent.IsSelected = true;
                    node.IsSelected = true;
                }
            }

        }

        internal void DoShutdown()
        {
            Properties.Settings.Default.Save();
        }

        internal void DoKeyPress(System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key){
                case Key.F5:
                    Items.Clear();
                    foreach (var di in DriveInfo.GetDrives()) {
                        Items.Add(new FileSystemItemViewModel(di));
                    }
                break;
                default:
                    // Do something
                    Console.WriteLine(string.Format("{0}\t{1}", e.Key, e.SystemKey));
                break;
            }
        }

        #endregion // Methods





    }
}
