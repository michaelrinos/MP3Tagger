using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.Models {
    class DirectoryItem : Item {

        #region Fields

        private IList<Item> _Items;

        #endregion

        #region Properties

        public IList<Item> Items { get { return _Items ?? (_Items = new List<Item>()); } set { SetField(ref _Items, value); } }

        public DirectoryItem() {
            Items = new List<Item>();
            if (Directory.GetFiles(Path).Length > 0 || Directory.GetDirectories(Path).Length > 0)
                Items.Add(null);
        }

        public override bool IsExpanded {
            get { return base.IsExpanded; }
            set {
                if (SetField(ref _IsExpanded, value)) {
                    Items = ItemProvider.GetItems(Path);
                }
            }
        }

        public override bool IsSelected {
            get { return base.IsSelected; }
            set {
                if (SetField(ref _IsSelected, value)) {
                    Items = ItemProvider.GetItems(Path);
                }
            }
        }

        #endregion // Properties

        #region Methods

        #endregion // Methods
    }
}
