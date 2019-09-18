using MicroMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.ViewModels {
    public class MusicEditorViewModel : ObservableObject {
        #region Fields



        #endregion // Fields

        #region Properties

        public DirectoryInfo CurrentDirectory { get; set; }
        public ObservableCollection<TagLib.File> MusicFiles { get; set; } = new ObservableCollection<TagLib.File>();

        #endregion // Properties

        #region Constructor

        public MusicEditorViewModel() {

        }
        public MusicEditorViewModel(DirectoryInfo path) {
            CurrentDirectory = path;
            LoadFiles();

        }
        #endregion // Constructor

        #region Methods

        public void LoadFiles() {
            if (CurrentDirectory == null)
                return;
            try {
                var files = CurrentDirectory.GetFiles("*.mp3");
                Parallel.ForEach(files, file => {
                    try {
                        MusicFiles.Add(TagLib.File.Create(file.FullName));
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                });
            } catch (Exception e) {

            }


        }


        #endregion // Methods

        #region Fields



        #endregion // Fields
    }
}
