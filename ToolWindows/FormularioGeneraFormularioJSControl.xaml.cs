using HerramientasV2.ToolWindows.Models;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace HerramientasV2
{
    public partial class FormularioGeneraFormularioJSControl : UserControl
    {
        public ObservableCollection<DataRow> DatosTabla { get; set; }
        public ObservableCollection<BotonRow> BotonesLista { get; set; }
        public string NombreFuncionJS { get; set; }

        public FormularioGeneraFormularioJSControl()
        {
            InitializeComponent();
            this.Loaded += UserControl_Loaded;
            BotonesLista = new ObservableCollection<BotonRow>();
            BotonesFormulario.ItemsSource = BotonesLista;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var documentView = await VS.Documents.GetActiveDocumentViewAsync();
            if (documentView?.TextView is IWpfTextView textView && textView.Selection.SelectedSpans.Any())
            {
                var selection = textView.Selection.SelectedSpans[0].GetText();
                NombreFuncionJS = ExtractFunctionName(selection);
                FormularioTextBox.Text = NombreFuncionJS;
                CargaCamposEnTabla(ExtractParameters(selection));
            }
        }

        private void CargaCamposEnTabla(string[] campos)
        {
            DatosTabla = new ObservableCollection<DataRow>(campos.Select((campo, i) => new DataRow
            {
                Id = i,
                NombreCampo = campo,
                TipoCampo = "SingleLine",
                EsRequerido = true,
                OpcionesComboBox = campos.ToList(),
                CampoDestino = campo
            }));
            dgFormulario.ItemsSource = DatosTabla;
        }

        private void AgregarBotonAFormulario_Click(object sender, RoutedEventArgs e)
        {
            BotonesLista.Add(new BotonRow
            {
                Id = BotonesLista.Count + 1,
                NombreCampo = NombreNuevoBoton.Text,
                TipoCampo = ClaseNuevoBoton.Text,
                LlamaJS = LlamaAFuncionJS.IsChecked == true,
                LlamaJSSeleccionado = LlamaAFuncionJSSeleccionado.IsChecked == true,
                swall = Swall.Text
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var html = CodeGenerator.GenerateHtml(
                FormularioTextBox.Text,
                checkBox8.IsChecked == true,
                (comboBox3.SelectedItem as ComboBoxItem)?.Content.ToString(),
                checkBox4.IsChecked == true,
                checkBox5.IsChecked == true,
                checkBox6.IsChecked == true,
                checkBox7.IsChecked == true,
                int.Parse(textBox2.Text),
                DatosTabla.ToList(),
                BotonesLista.ToList()
            );
            textBox5.Text = html;
            VS.MessageBox.Show("Generación Completa!", "Ahora presiona boton el copiar y luego selecciona donde quieras pegar el codigo generado.");
        }

        private static string ExtractFunctionName(string jsFunction) => Regex.Match(jsFunction, @"function\s+(\w+)\s*\(").Groups[1].Value;
        private static string[] ExtractParameters(string jsFunction) => Regex.Match(jsFunction, @"\(([^)]*)\)").Groups[1].Value.Split(',').Select(p => p.Trim()).ToArray();
    }
}
