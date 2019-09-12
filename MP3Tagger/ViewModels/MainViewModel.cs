using MicroMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Tagger.ViewModels {
    class MainViewModel : ObservableObject {


        #region Fields 

        private FileExplorerViewModel _fevm;

        #endregion // Fields

        #region Properties 

        public FileExplorerViewModel fevm { get => _fevm ?? (_fevm = new FileExplorerViewModel()); set => SetField(ref _fevm, value); }

        #endregion // Properties

        #region Constructor 

        public MainViewModel() {

        }

        #endregion // Constructor

        #region Methods 

        #endregion // Methods



    }
}
