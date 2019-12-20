using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
                        var item = TagLib.File.Create(file.FullName);

                        MusicFiles.Add(item);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                });
            } catch (Exception e) {
            }


        }

        public void RemoveDuplicates()
        {
            var query = MusicFiles.GroupBy(x => new { x?.Tag.FirstArtist, x?.Tag.Title })
                .Where(g => g.Count() > 1)
                .ToList();
            Console.WriteLine(query);
            var t = query.Select(x => x.First());
            Parallel.ForEach(t, x => {
                try {
                    if (x == null)
                        return;
                    File.Copy(x.Name, @"H:\Music\Copies\" + Path.GetFileName(x.Name));
                }catch(Exception e) { Console.WriteLine(e); }
            });

            
        }


        #endregion // Methods

        #region Fields



        #endregion // Fields
    }
}
