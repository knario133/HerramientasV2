using System.Collections.Generic;
using System.ComponentModel;

namespace HerramientasV2.ToolWindows.Models
{
    public class DataRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        private string _nombreCampo;
        private string _tipoCampo;
        private bool _esRequerido;
        private string _campoDestino;
        private List<string> _opcionesComboBox;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }
        public string NombreCampo
        {
            get => _nombreCampo;
            set { _nombreCampo = value; OnPropertyChanged(nameof(NombreCampo)); }
        }
        public string TipoCampo
        {
            get => _tipoCampo;
            set { _tipoCampo = value; OnPropertyChanged(nameof(TipoCampo)); }
        }
        public bool EsRequerido
        {
            get => _esRequerido;
            set { _esRequerido = value; OnPropertyChanged(nameof(EsRequerido)); }
        }
        public string CampoDestino
        {
            get => _campoDestino;
            set { _campoDestino = value; OnPropertyChanged(nameof(CampoDestino)); }
        }
        public List<string> OpcionesComboBox
        {
            get => _opcionesComboBox;
            set { _opcionesComboBox = value; OnPropertyChanged(nameof(OpcionesComboBox)); }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
