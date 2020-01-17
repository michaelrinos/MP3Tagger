using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MP3Tagger.Wrappers {
    public class CheckWrapper<T> : ObservableObject {
        private readonly CheckableObservableCollection<T> _parent;

        public CheckWrapper(CheckableObservableCollection<T> parent) {
            _parent = parent;
        }

        private T _value;

        public T Value {
            get { return _value; }
            set {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        private bool _isChecked;

        public bool IsChecked {
            get { return _isChecked; }
            set {
                _isChecked = value;
                CheckChanged();
                OnPropertyChanged("IsChecked");
            }
        }

        private void CheckChanged() {
            _parent.Refresh();
        }
    }

    public class CheckableObservableCollection<T> : ObservableCollection<CheckWrapper<T>> {
        private ListCollectionView _selected;

        public CheckableObservableCollection() {
            _selected = new ListCollectionView(this);
            _selected.Filter = delegate (object checkObject) {
                return ((CheckWrapper<T>)checkObject).IsChecked;
            };
        }

        public CheckableObservableCollection(IList<T> t) : this() {
            foreach(T item in t) {
                this.Add(new CheckWrapper<T>(this) { Value = item });
            }
        }

        public void Add(T item) {
            this.Add(new CheckWrapper<T>(this) { Value = item });
        }

        public ICollectionView CheckedItems {
            get { return _selected; }
        }

        public bool SetCheck(T thing, bool value) {
            try {
                this.Items.Where(x => x.Value.Equals(thing)).First().IsChecked = value;
                return true;
            } catch (InvalidOperationException) { return false; }
        }

        internal void Refresh() {
            _selected.Refresh();
        }
    }
}
