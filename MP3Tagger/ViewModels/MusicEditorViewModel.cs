using MP3Tagger.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Linq.Dynamic.Core;
using Data;
using System.Configuration;

namespace MP3Tagger.ViewModels
{
    public class MusicEditorViewModel : ObservableObject
    {
        #region Fields

        private CheckableObservableCollection<string> _Options =
            new CheckableObservableCollection<string>() { "This", "is", "a", "test" };

        #endregion // Fields

        #region Properties
        public string ExportPath { get; set; } = Properties.Settings.Default.ExportPath;

        public CheckableObservableCollection<string> Options { get => _Options; set => Set(ref _Options, value); }
        public DirectoryInfo CurrentDirectory { get; set; }
        public ObservableCollection<TagLib.File> MusicFiles { get; set; } = new ObservableCollection<TagLib.File>();


        #endregion // Properties

        #region Constructor

        public MusicEditorViewModel()
        {

        }
        public MusicEditorViewModel(DirectoryInfo path)
        {
            CurrentDirectory = path;

            var t = typeof(TagLib.Tag).GetProperties().Select(x => x.Name).ToList();
            t.AddRange(typeof(TagLib.File).GetProperties().Select(x => x.Name).ToList());

            _Options = new CheckableObservableCollection<string>(t);
            _Options.SetCheck("Title", true);
            _Options.SetCheck("FirstPerformer", true);
            LoadFiles();

        }
        #endregion // Constructor

        #region Methods

        public void LoadFiles()
        {
            if (CurrentDirectory == null)
                return;
            try
            {
                var fileTypes = new string[] { "*.mp3", "*.m4a" };
                foreach (var types in fileTypes)
                {
                    var files = CurrentDirectory.GetFiles(types);
                    foreach (var file in files)
                    {
                        try
                        {
                            var item = TagLib.File.Create(file.FullName);
                            if (item != null && item != default(TagLib.File))
                                MusicFiles.Add(item);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                    }
                }
                /*
                Parallel.ForEach(files, file => {
                    try {
                        var item = TagLib.File.Create(file.FullName);
                        if (item != null)
                            MusicFiles.Add(item);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                });
                */
            }
            catch (Exception e)
            {
            }
        }
        private static void FindAudioFiles()
        {

        }

        public void RemoveDuplicates()
        {
            var CWOptions = Options.CheckedItems.Cast<CheckWrapper<string>>().ToList();

            var options = CWOptions.Select(q => typeof(TagLib.File).GetProperties().Any(w => w.Name.Contains(q.Value)) ? q.Value : string.Format("Tag.{0}", q.Value));
            string dynamicLinqGroupByKeySelector =
                "new (" + String.Join(", ", options) + ")";


            var tempQuery = ((System.Linq.Dynamic.Core.DynamicQueryableExtensions
                            .GroupBy(MusicFiles.AsQueryable(), dynamicLinqGroupByKeySelector)
                            .Where("x => x.Count() > 1")));
            var newQuery = ((System.Linq.Dynamic.Core.DynamicQueryableExtensions
                .GroupBy(MusicFiles.AsQueryable(), dynamicLinqGroupByKeySelector)
                .Where("x => x.Count() > 1")
                .Select("x => x.First()")
                ) as IEnumerable<TagLib.File>).ToList()
                ;


            /*
            //Original Query
            var query = MusicFiles.
                GroupBy(x => new { x.Tag.Title, x.Tag.FirstPerformer })
                .Where(g => g.Count() > 1)
                .Select(x => x.First())
                .ToList()
                //.Select(h => h.)
                //.Cast<TagLib.Tag>()
                ;

            Console.WriteLine(query);
            // */

            var dupes = newQuery;
            if (!Directory.Exists(ExportPath))
            {
                Directory.CreateDirectory(ExportPath);
            }

            RemoveDuplicates(dupes);
            Console.WriteLine("Done");

        }
        public void writeToFile()
        {
            var filePath = CurrentDirectory.ToString() + "\\Files.txt";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            using (StreamWriter w = File.AppendText(filePath))
            {
                foreach (var item in MusicFiles)
                {
                    w.WriteLine(string.Format("{0}\t\t\t\t\t\t{1}\t\t\t\t{2}", item.Tag.Title, item.Tag.FirstPerformer, item.Tag.FirstPerformer));
                }
            }

            var t = ConfigurationManager.AppSettings["Temp"];
            var ds = new DataSource("Server=localhost;Database=Music;Trusted_Connection=True;");
            foreach (var item in MusicFiles)
            {
                try
                {
                    ds.DataManager.Parameters.Clear();
                    ds.DataManager.Parameters.Add("Name", item.Tag.Title);
                    ds.DataManager.Parameters.Add("Artist", item.Tag.FirstPerformer);
                    ds.DataManager.Parameters.Add("Album", item.Tag.Album);
                    ds.DataManager.Parameters.Add("Location", item.Name);

                    ds.DataManager.RunQuery("MusicFile_Create", QueryType.QT_Sproc);
                }catch (Exception ex)
                {
                    ex = ex;
                }
            }



        }

        private void RemoveDuplicates(List<TagLib.File> dupes)
        {
            Parallel.ForEach(dupes, x =>
            {
                try
                {
                    if (x == null)
                        return;
                    try
                    {
                        File.Move(x.Name, ExportPath + "\\" + Path.GetFileName(x.Name));
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.ToString());
                        e = e;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }


        #endregion // Methods

        #region Fields



        #endregion // Fields
    }
}
