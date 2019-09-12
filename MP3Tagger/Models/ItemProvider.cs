using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.Models {
    public static class ItemProvider {


        public static List<Item> GetDrives() {
            var items = new List<Item>();

            foreach (var s in Directory.GetLogicalDrives())
                items.Add(new DirectoryItem { Name = s, Path = s });

            return items;
        }

        public static List<Item> GetItems(string path) {
            var items = new List<Item>();
            try {
                foreach (var s in Directory.GetFiles(path) ) {
                    var dirInfo = new FileInfo(s);
                    items.Add(new FileItem { Name = dirInfo.Name, Path = dirInfo.FullName });
                }

                foreach (var s in Directory.GetDirectories(path) ) {
                    var dirInfo = new DirectoryInfo(s);
                    items.Add(new DirectoryItem { Name = dirInfo.Name, Path = dirInfo.FullName });
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            return items;
        }
    }
}
