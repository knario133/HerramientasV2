using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Package;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System.Text.RegularExpressions;
using System.Windows.Markup;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Collections.ObjectModel;
using System.Globalization;

namespace HerramientasV2
{
    public partial class FormularioGeneraFormularioJSControl : UserControl
    {
        public string[] CamposSeleccion;
        public string NombreFuncionJS;
        public FormularioGeneraFormularioJSControl()
        {
            InitializeComponent();
            this.Loaded += UserControl_Loaded;

        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // El código aquí se ejecutará cada vez que el UserControl se cargue.
            RellenaComboClases();
        }
        private async void RellenaComboClases()
        {
            BotonesLista = new ObservableCollection<BotonRow>();
            try
            {
                //   var Solucion = VS.Solutions.GetCurrentSolution;
                // Get the workspace.
                Workspace workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();

                // Get the active Visual Studio document.

                DocumentView? documentView = await VS.Documents.GetActiveDocumentViewAsync();
                if (documentView != null)
                {
                    IWpfTextView textView = documentView.TextView;
                    var selectedSpan = textView.Selection.SelectedSpans;

                    if (selectedSpan.Count > 0)
                    {
                        if (selectedSpan[0].GetText().Equals(""))
                        {
                            //    VS.MessageBox.Show("Excepcion de funcionamiento", "Recuerda seleccionar la declaración de funcion con sus atributos ejemplo:\n function prueba(campo1)");
                        }
                        else
                        {
                            CamposSeleccion = ExtractParameters(selectedSpan[0].GetText());
                            FormularioTextBox.Text = ExtractFunctionName(selectedSpan[0].GetText());
                            NombreFuncionJS = ExtractFunctionName(selectedSpan[0].GetText());
                            CargaCamposEnTabla(CamposSeleccion);
                        }
                        // Haz algo con el texto seleccionado
                    }
                    else
                    {
                        VS.MessageBox.Show("Excepcion de funcionamiento", "Recuerda seleccionar la declaración de funcion con sus atributos ejemplo:\n function prueba(campo1)");
                    }
                }


            }
            catch (Exception e1)
            {

                VS.MessageBox.Show("Excepcion de funcionamiento", "Recuerda seleccionar la declaración de funcion con sus atributos ejemplo:\n function prueba(campo1)");
            }
        }
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

        public ObservableCollection<DataRow> DatosTabla;
        private void CargaCamposEnTabla(string[] camposSeleccion)
        {
            DatosTabla = new ObservableCollection<DataRow>();

            for (int i = 0; i < camposSeleccion.Length; i++)
            {
                DataRow Fila = new DataRow
                {
                    Id = i,
                    NombreCampo = camposSeleccion[i],
                    TipoCampo = "SingleLine",
                    EsRequerido = true,
                    OpcionesComboBox = camposSeleccion.ToList(),
                    CampoDestino = camposSeleccion[i]
                };

                DatosTabla.Add(Fila);
            }

            dgFormulario.ItemsSource = DatosTabla;
        }
        public class BotonRow
        {
            public int Id { get; set; }
            public string NombreCampo { get; set; }
            public string TipoCampo { get; set; }
            public string swall { get; set; }
            public bool LlamaJS { get; set; }
            public bool LlamaJSSeleccionado { get; set; }
        }
        public ObservableCollection<BotonRow> BotonesLista;
        public int IdCounter = 1;
        private void AgregarBotonAFormulario_Click(object sender, RoutedEventArgs e)
        {
            BotonRow nuevaFila = new BotonRow
            {
                Id = IdCounter,
                NombreCampo = NombreNuevoBoton.Text,
                TipoCampo = ClaseNuevoBoton.Text,
                LlamaJS = LlamaAFuncionJS.IsChecked.Value,
                LlamaJSSeleccionado = LlamaAFuncionJSSeleccionado.IsChecked.Value,
                swall = Swall.Text
            };
            if (BotonesLista == null)
            {
                BotonesLista = new ObservableCollection<BotonRow>();
            }

            BotonesLista.Add(nuevaFila);
            BotonesFormulario.ItemsSource = null;
            BotonesFormulario.ItemsSource = BotonesLista;
            IdCounter++;
        }
        private void EliminarBoton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                BotonRow item = button.DataContext as BotonRow;
                BotonesLista.Remove(item);
                BotonesFormulario.ItemsSource = null;
                BotonesFormulario.ItemsSource = BotonesLista;
            }
            catch { }
        }

        public static string ExtractFunctionName(string jsFunction)
        {
            // La expresión regular busca el nombre de la función después de "function" y antes de los paréntesis
            var match = Regex.Match(jsFunction, @"function\s+(\w+)\s*\(", RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return null; // Retorna null si no encuentra el nombre
        }
        public static string[] ExtractParameters(string jsFunction)
        {
            // La expresión regular busca los parámetros entre paréntesis y considera espacios y saltos de línea
            var match = Regex.Match(jsFunction, @"function\s+\w+\s*\(\s*([^)]+)\s*\)", RegexOptions.Singleline);
            if (match.Success)
            {
                // Separa los parámetros por coma y quita espacios en blanco
                return match.Groups[1].Value.Split(',').Select(p => p.Trim()).ToArray();
            }

            return new string[] { }; // Retorna un array vacío si no encuentra parámetros
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                var item = button.DataContext; // Aquí obtienes la fila relacionada al botón.
                dgFormulario.ItemsSource = null; // Desenlazar
                DatosTabla.Remove((DataRow)item); // Asumiendo que DatosTabla es una List<DataRow>
                dgFormulario.ItemsSource = DatosTabla; // Volver a enlazar
            }
            catch (Exception ex)
            {
                // Puedes manejar el error aquí
            }
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("FormularioGeneraFormularioJSControl", "Button clicked");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DataRow Fila = new DataRow();
            if (DatosTabla == null)
            {
                DatosTabla = new ObservableCollection<DataRow>();
            }
            Fila.Id = DatosTabla.Count + 1;

            Fila.NombreCampo = NombreDeCampo.Text;
            Fila.TipoCampo = TipoDeCampo.Text;
            Fila.EsRequerido = false;
            if (DatosTabla.Count > 0)
            {
                Fila.OpcionesComboBox = DatosTabla[0].OpcionesComboBox;
            }
            Fila.CampoDestino = "";
            DatosTabla.Add(Fila);
            dgFormulario.ItemsSource = DatosTabla;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            String Respuesta = "";
            Respuesta += "<div>" + Environment.NewLine + "\t";
            Respuesta += "<h1 class=\"display-4\">" + FormularioTextBox.Text + "</h1>" + Environment.NewLine + "\t";
            if ((bool)checkBox8.IsChecked)
            {
                Respuesta += "<div class=\"table-responsive-" + comboBox3.SelectedValue.ToString() + "\">" + Environment.NewLine + "\t" + "\t";
            }
            string ClasesTabla = "table ";
            if ((bool)checkBox4.IsChecked)
            {
                ClasesTabla += "table-bordered ";
            }
            if ((bool)checkBox5.IsChecked)
            {
                ClasesTabla += "table-striped ";
            }
            if ((bool)checkBox6.IsChecked)
            {
                ClasesTabla += "table-hover ";
            }
            if ((bool)checkBox7.IsChecked)
            {
                ClasesTabla += "table-sm ";
            }

            Respuesta += "<table class=\"" + ClasesTabla + "\">" + Environment.NewLine + "\t" + "\t";
            Respuesta += "<tbody>" + Environment.NewLine + "\t" + "\t" + "\t";
            int Columnas = int.Parse(this.textBox2.Text);
            int CantidadDeFilas = ((float.Parse(DatosTabla.Count + "") / float.Parse(Columnas + "")) % 1.0 > 0) ? (DatosTabla.Count / Columnas) + 1 : (DatosTabla.Count / Columnas);
            List<DataRow> ListaCampos = new List<DataRow>();
            ListaCampos = DatosTabla.ToList();
            for (int i = 0; i < CantidadDeFilas; i++)
            {
                Respuesta += "<tr>" + Environment.NewLine + "\t" + "\t" + "\t" + "\t"; ;
                for (int j = 0; j < Columnas; j++)
                {
                    if (ListaCampos.Count > 0)
                    {

                        DataRow Aux = ListaCampos[0];
                        Respuesta += "<td>" + Environment.NewLine + "\t" + "\t" + "\t" + "\t" + "\t";
                        Respuesta += " <asp:Label ID=\"LB_" + FormateaCadena(Aux.NombreCampo, true, false) + "\" runat=\"server\" AssociatedControlID=\"TB_" + FormateaCadena(Aux.NombreCampo, true, false) + "\"  class=\"col-form-label\" Text=\"" + FormateaCadena(Aux.NombreCampo, false, true) + "\"></asp:Label>" + "\t" + "\t" + "\t" + "\t";
                        Respuesta += Environment.NewLine + "\t" + "\t" + "\t" + "\t" + "\t" + "</td>" + Environment.NewLine + "\t" + "\t" + "\t" + "\t" + "<td>" + Environment.NewLine + "\t" + "\t" + "\t" + "\t" + "\t";
                        Respuesta += "<asp:TextBox ID=\"TB_" + FormateaCadena(Aux.NombreCampo, true, false) + "\" CssClass=\"form-control\"  TextMode=\"" + Aux.TipoCampo + "\"  runat=\"server\" " + (Aux.EsRequerido ? "readonly" : "") + "></asp:TextBox>" + Environment.NewLine + "\t" + "\t" + "\t" + "\t" + "\t";
                        Respuesta += "<asp:HiddenField ID=\"HF_" + FormateaCadena(Aux.NombreCampo, true, false) + "\" runat=\"server\" />" + Environment.NewLine + "\t" + "\t" + "\t" + "\t";
                        Respuesta += "</td>" + Environment.NewLine + "\t" + "\t" + "\t";
                        ListaCampos.RemoveAt(0);
                    }
                    else
                    {
                        Respuesta += "<td></td><td></td>" + Environment.NewLine;
                    }
                }
                Respuesta += "</tr>" + Environment.NewLine + "\t" + "\t" + "\t"; ;
            }
            Respuesta += Environment.NewLine + "\t" + "\t" + "</tbody>" + Environment.NewLine + "\t";
            Respuesta += "</table>" + Environment.NewLine;
            if ((bool)checkBox8.IsChecked)
            {
                Respuesta += "</div>" + Environment.NewLine;
            }
            Respuesta += "<div style=\"text-align: right;\">" + Environment.NewLine;
            List<BotonRow> botonesFormulario = new List<BotonRow>();
            botonesFormulario = BotonesLista.ToList();
            foreach (BotonRow boton in botonesFormulario)
            {
                if (boton.LlamaJS || boton.LlamaJSSeleccionado)
                {

                    Respuesta += "<input type=\"button\"  ID=\"BTN_" + FormateaCadena(boton.NombreCampo, true, false) + "\"  class=\"" + boton.TipoCampo + "\" onclick=\"FN_" + FormateaCadena(boton.NombreCampo, true, false) + "_Click()\" value=\"" + FormateaCadena(boton.NombreCampo, true, true) + "\" />" + Environment.NewLine;

                }
                else
                {
                    Respuesta += "<asp:Button ID=\"BTN_" + FormateaCadena(boton.NombreCampo, true, false) + "\" runat=\"server\" CssClass=\"" + boton.TipoCampo + "\" OnClientClick=\"\" Text=\"" + FormateaCadena(boton.NombreCampo, true, true) + "\" />" + Environment.NewLine;

                }
            }

            Respuesta += "</div>" + Environment.NewLine;
            Respuesta += "<script type=\"application/javascript\">" + Environment.NewLine;
            botonesFormulario.Clear();
            botonesFormulario = BotonesLista.Where(boton => (boton.LlamaJS == true || boton.LlamaJSSeleccionado)).ToList();
            foreach (BotonRow boton in botonesFormulario)
            {
                Respuesta += "function FN_" + FormateaCadena(boton.NombreCampo, true, false) + "_Click()" + Environment.NewLine;
                Respuesta += "{" + Environment.NewLine + "\t";
                //botones comentados
                Respuesta += "/*" + Environment.NewLine + "\t";
                foreach (DataRow campo in DatosTabla.ToList())
                {
                    Respuesta += "TB_" + FormateaCadena(campo.NombreCampo, true, false) + Environment.NewLine + "\t";
                }
                Respuesta += "*/" + Environment.NewLine + "\t";
                //fin botones comentados
                foreach (DataRow campo in DatosTabla.ToList())
                {
                    Respuesta += "var " + FormateaCadena(campo.NombreCampo, true, false) + "=  document.getElementById('<%=TB_" + FormateaCadena(campo.NombreCampo, true, false) + ".ClientID%>').value;" + Environment.NewLine + "\t";
                }
                if (!(boton.LlamaJS || boton.LlamaJSSeleccionado))
                {
                    foreach (DataRow campo in DatosTabla.ToList())
                    {
                        Respuesta += "<%=HF_" + FormateaCadena(campo.NombreCampo, true, false) + ".ClientID %>.value=" + FormateaCadena(campo.NombreCampo, true, false) + ";" + Environment.NewLine + "\t";
                    }
                }
                if (boton.swall == "Dialogo Confirmación")
                {
                    Respuesta += @"Swal.fire({
  title: ""Titulo"",
  text: ""Texto"",
  icon: ""warning"",
  showCancelButton: true,
  confirmButtonColor: ""#3085d6"",
  cancelButtonColor: ""#d33"",
  confirmButtonText: ""TextoBotonConfirmar""
}).then((result) => {
  if (result.isConfirmed) {
/*LLAMADA FUNCION JS*/
   " + Environment.NewLine;
                }
                if (boton.LlamaJSSeleccionado)
                {
                    Respuesta += Environment.NewLine + "\t" + NombreFuncionJS + "(" + Environment.NewLine + "\t";
                    Respuesta += string.Join(",", (from DataRow x in DatosTabla select FormateaCadena(x.NombreCampo, true, false)));
                    Respuesta += Environment.NewLine + "\t"  + ");" + Environment.NewLine + "\t";
                }
                if (boton.swall == "Dialogo Confirmación")
                {
                    Respuesta += @" }
});" + Environment.NewLine;
                }
                if (boton.swall == "Alerta Post")
                {
                    Respuesta += "Swal.fire({  title: \"Confirmacion\",  text: \"Mensaje\",  icon: \"success\"});" + Environment.NewLine;
                }
                if (!(boton.LlamaJS || boton.LlamaJSSeleccionado))
                {
                    Respuesta += "return false;" + Environment.NewLine;
                }
                Respuesta += "}" + Environment.NewLine;
            }
            Respuesta += "</script>" + Environment.NewLine;
            Respuesta += "</div>" + Environment.NewLine;
            textBox5.Text = Respuesta;
            VS.MessageBox.Show("Generación Completa!", "Ahora presiona boton el copiar y luego selecciona donde quieras pegar el codigo generado.");
        }
        private string FormateaCadena(string cadena, bool pascalCase, bool espacios)
        {
            cadena = cadena.ToLower().Replace("_", " ");
            cadena = Regex.Replace(cadena, @"'[^']+'(?=!\w+)", "");
            TextInfo info = CultureInfo.CurrentCulture.TextInfo;
            cadena = info.ToTitleCase(cadena);
            if (!espacios)
            {
                cadena = cadena.Replace(" ", string.Empty);
            }
            if (pascalCase)
            {
                cadena = Regex.Replace(cadena, "[^0-9A-Za-z]", "", RegexOptions.None);
            }
            return cadena;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(textBox5.Text);
            VS.MessageBox.Show("Generación Completa!", "Ahora  tu eliges donde quieras pegar el codigo generado. ");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            String Respuesta = "";
            Respuesta += "<div style=\"text-align: right;\">" + Environment.NewLine;
            List<BotonRow> botonesFormulario = new List<BotonRow>();
            botonesFormulario = BotonesLista.ToList();
            foreach (BotonRow boton in botonesFormulario)
            {
                if (boton.LlamaJS || boton.LlamaJSSeleccionado)
                {

                    Respuesta += "<input type=\"button\"  ID=\"BTN_" + FormateaCadena(boton.NombreCampo, true, false) + "\"  class=\"" + boton.TipoCampo + "\" onclick=\"FN_" + FormateaCadena(boton.NombreCampo, true, false) + "_Click()\" value=\"" + FormateaCadena(boton.NombreCampo, true, true) + "\" />" + Environment.NewLine;

                }
                else
                {
                    Respuesta += "<asp:Button ID=\"BTN_" + FormateaCadena(boton.NombreCampo, true, false) + "\" runat=\"server\" CssClass=\"" + boton.TipoCampo + "\" OnClientClick=\"\" Text=\"" + FormateaCadena(boton.NombreCampo, true, true) + "\" />" + Environment.NewLine;

                }
            }

            Respuesta += "</div>" + Environment.NewLine;
            Respuesta += "<script type=\"application/javascript\">" + Environment.NewLine;
            botonesFormulario.Clear();
            botonesFormulario = BotonesLista.Where(boton => (boton.LlamaJS == true || boton.LlamaJSSeleccionado)).ToList();
            foreach (BotonRow boton in botonesFormulario)
            {
                Respuesta += "function FN_" + FormateaCadena(boton.NombreCampo, true, false) + "_Click()" + Environment.NewLine;
                Respuesta += "{" + Environment.NewLine + "\t";
                //botones comentados
                Respuesta += "/*" + Environment.NewLine + "\t";
                foreach (DataRow campo in DatosTabla.ToList())
                {
                    Respuesta += "TB_" + FormateaCadena(campo.NombreCampo, true, false) + Environment.NewLine + "\t";
                }
                Respuesta += "*/" + Environment.NewLine + "\t";
                //fin botones comentados
                foreach (DataRow campo in DatosTabla.ToList())
                {
                    Respuesta += "var " + FormateaCadena(campo.NombreCampo, true, false) + "=  document.getElementById('<%=TB_" + FormateaCadena(campo.NombreCampo, true, false) + ".ClientID%>').value;" + Environment.NewLine + "\t";
                }
                if (!(boton.LlamaJS || boton.LlamaJSSeleccionado))
                {
                    foreach (DataRow campo in DatosTabla.ToList())
                    {
                        Respuesta += "<%=HF_" + FormateaCadena(campo.NombreCampo, true, false) + ".ClientID %>.value=" + FormateaCadena(campo.NombreCampo, true, false) + ";" + Environment.NewLine + "\t";
                    }
                }
                if (boton.swall == "Dialogo Confirmación")
                {
                    Respuesta += @"Swal.fire({
  title: ""Titulo"",
  text: ""Texto"",
  icon: ""warning"",
  showCancelButton: true,
  confirmButtonColor: ""#3085d6"",
  cancelButtonColor: ""#d33"",
  confirmButtonText: ""TextoBotonConfirmar""
}).then((result) => {
  if (result.isConfirmed) {
/*LLAMADA FUNCION JS*/
   " + Environment.NewLine;
                }
                if (boton.LlamaJSSeleccionado)
                {
                    Respuesta += Environment.NewLine + "\t" + NombreFuncionJS + "(" + Environment.NewLine + "\t";
                    Respuesta += string.Join(",", (from DataRow x in DatosTabla select FormateaCadena(x.NombreCampo, true, false)));
                    Respuesta += Environment.NewLine + "\t" + ");" + Environment.NewLine + "\t";
                }
                if (boton.swall == "Dialogo Confirmación")
                {
                    Respuesta += @" }
});" + Environment.NewLine;
                }
                if (boton.swall == "Alerta Post")
                {
                    Respuesta += "Swal.fire({  title: \"Confirmacion\",  text: \"Mensaje\",  icon: \"success\"});" + Environment.NewLine;
                }
                if (!(boton.LlamaJS || boton.LlamaJSSeleccionado))
                {
                    Respuesta += "return false;" + Environment.NewLine;
                }
                Respuesta += "}" + Environment.NewLine;
            }
            Respuesta += "</script>" + Environment.NewLine;
            Respuesta += "</div>" + Environment.NewLine;
            textBox5.Text = Respuesta;
            VS.MessageBox.Show("Generación Completa!", "Ahora presiona boton el copiar y luego selecciona donde quieras pegar el codigo generado.");

        }
    }
}
