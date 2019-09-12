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
        internal string _name;
        internal string _path;

        #endregion // Fields

        #region Properties

        public string Name { get { return _name ?? (_name = string.Empty); } set { SetField(ref _name, value); } }
        public string Path { get { return _path ?? (_path = string.Empty); } set { SetField(ref _path, value); } }
        public virtual bool IsSelected { get { return _IsSelected; } set { SetField(ref _IsSelected, value); } }
        public virtual bool IsExpanded { get { return _IsExpanded; } set { SetField(ref _IsExpanded, value); } }

        #endregion // Properties

    }
}
