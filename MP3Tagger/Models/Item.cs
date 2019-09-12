using MicroMVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.Models {
    public class Item : ObservableObject {
        #region Fields

        internal bool _IsExpanded;
        internal bool _IsSelected;

        #endregion // Fields

        #region Properties

        public string Name { get; set; }
        public string Path { get; set; }
        public virtual bool IsSelected { get { return _IsSelected; } set { SetField(ref _IsSelected, value); } }
        public virtual bool IsExpanded { get { return _IsExpanded; } set { SetField(ref _IsExpanded, value); } }

        #endregion // Properties

    }
}
