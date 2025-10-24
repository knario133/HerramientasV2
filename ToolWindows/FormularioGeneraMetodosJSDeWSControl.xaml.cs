using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Package;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
namespace HerramientasV2
{
    public partial class FormularioGeneraMetodosJSDeWSControl : UserControl
    {
        public FormularioGeneraMetodosJSDeWSControl()
        {
            InitializeComponent();
            RellenaComboClases();
        }
        private async void RellenaComboClases()
        {
            try
            {
                string Respuesta = "";
                //   var Solucion = VS.Solutions.GetCurrentSolution;
                // Get the workspace.
                Workspace workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();

                // Get the active Visual Studio document.
                DocumentView? documentView = await VS.Documents.GetActiveDocumentViewAsync();
                //   obtener el proyecto activo 
                var projectoActual = VS.Solutions.GetActiveProjectAsync();
                //Obtengo la carpeta raíz del proyecto
                var carpetaRaiz = projectoActual.Result;
                //Llamada al método recursivo para obtener todos los archivos del proyecto
                var archivosProyectoActual = ObtenerArchivosProyecto(carpetaRaiz);

                //ahora filtro los dbml para poder ver si puedo obtener sus clases y metodos
                var ArchivosLinqConSP = (from x in archivosProyectoActual where x.Text.Contains(".asmx") == true select x).ToArray();
                LinqDisponibles.Items.Clear();
                foreach (var linEncontrada in ArchivosLinqConSP)
                {
                    LinqDisponibles.Items.Add(linEncontrada.FullPath.Replace(System.IO.Path.GetDirectoryName(projectoActual.Result.FullPath), ""));
                }
                Metodos.ItemsSource = null;
            }
            catch { }
        }
        private List<SolutionItem> ObtenerArchivosProyecto(SolutionItem carpeta)
        {
            var archivosProyecto = new List<SolutionItem>();
            foreach (var item in carpeta.Children)
            {
                if (item is PhysicalFolder subCarpeta)
                {
                    //Si es una subcarpeta, llamamos al método recursivamente
                    archivosProyecto.AddRange(ObtenerArchivosProyecto(subCarpeta));
                }
                else if (item.GetType() == typeof(PhysicalFile))
                {
                    archivosProyecto.Add((SolutionItem)item);
                }
            }
            return archivosProyecto;
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("Generación de metodos JS en base a WS.net", "Button clicked");
        }

        private async void LinqDisponibles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string Respuesta = "";

            string LinqSeleccionada = "";
            try
            {
                if (!(LinqDisponibles.SelectedValue is null))
                {
                    LinqSeleccionada = LinqDisponibles.SelectedValue.ToString();
                }
                else
                {
                    return;
                }
            }
            catch
            {
                return;
            }
            //   var Solucion = VS.Solutions.GetCurrentSolution;
            // Get the workspace.
            Workspace workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();

            // Get the active Visual Studio document.
            DocumentView? documentView = await VS.Documents.GetActiveDocumentViewAsync();
            //   obtener el proyecto activo 
            var projectoActual = VS.Solutions.GetActiveProjectAsync();
            //Obtengo la carpeta raíz del proyecto
            var carpetaRaiz = projectoActual.Result;
            //Obtengo los hijos o archivos asociados al proyecto
            List<SolutionItem> ArchivosProyectoActual = ObtenerArchivosProyecto(carpetaRaiz);
            //ahora filtro los dbml para poder ver si puedo obtener sus clases y metodos
            SolutionItem?[] ArchivosLinqConSP = (from x in ArchivosProyectoActual where (x.FullPath is null ? "" : x.FullPath).Contains(LinqSeleccionada) == true select x).ToArray();

            if (documentView is not null)
            {
                foreach (SolutionItem? x in ArchivosLinqConSP)
                {
                    try
                    {
                        // Get the Roslyn document for that file. Tomo como ejemplo la unica clase linq del proyecto
                        //luego agrego el codigo en base a la seleccion para buscar la mas parecida
                        DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.Children.ToArray()[0].FullPath).FirstOrDefault();
                        var mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
                        if (id is not null)
                        {
                            Document roslynDocument = workspace.CurrentSolution.GetDocument(id);

                            SyntaxNode root = await roslynDocument.GetSyntaxRootAsync();
                            SemanticModel model = await roslynDocument.GetSemanticModelAsync();
                            SyntaxTree tree = await roslynDocument.GetSyntaxTreeAsync();
                            CompilationUnitSyntax CUS = tree.GetCompilationUnitRoot();
                            var ClasesEncontradas = CUS.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
                            MethodDeclarationSyntax[] SPLinqEncontrados = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
                            string metodos = "";
                            string retorno = "";
                            List<string[]> ParametrosEntrada = new List<string[]>();

                            List<string> MetodosEncontrados = new List<string>();
                            //MetodosEncontrados = (from c in SPLinqEncontrados
                            //                      where c.Identifier.Text != "OnCreated"
                            //                      select "_"+c.Identifier.Text).ToList();

                            Metodos.ItemsSource = (from c in SPLinqEncontrados
                                                   where c.Identifier.Text != "OnCreated"
                                                   select new CheckBox() { Content = "_" + c.Identifier.Text }).ToList();

                            //   Metodos.ItemsSource = MetodosEncontrados;
                        }
                    }
                    catch { }
                }
            }
        }
        private class Metodo
        {
            public string Nombre;
            public string ObjetoRetorno;
            public List<string[]> ParametrosEntrada;
            public Metodo()
            {
                ParametrosEntrada = new List<string[]>();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RellenaComboClases();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            GeneraCodigo();
        }

        private async void GeneraCodigo()
        {

            string Respuesta = "";

            string LinqSeleccionada = "";
            try
            {
                if (!(LinqDisponibles.SelectedValue is null))
                {
                    LinqSeleccionada = LinqDisponibles.SelectedValue.ToString();
                    if (LinqSeleccionada.StartsWith("/"))
                    {
                        LinqSeleccionada = LinqSeleccionada.Substring(1);
                    }
                }
                else
                {
                    return;
                }
            }
            catch
            {
                return;
            }
            //   var Solucion = VS.Solutions.GetCurrentSolution;
            // Get the workspace.
            Workspace workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();

            // Get the active Visual Studio document.
            DocumentView? documentView = await VS.Documents.GetActiveDocumentViewAsync();
            //   obtener el proyecto activo 
            var projectoActual = VS.Solutions.GetActiveProjectAsync();
            var carpetaRaiz = projectoActual.Result;
            //Obtengo los hijos o archivos asociados al proyecto
            List<SolutionItem> ArchivosProyectoActual = ObtenerArchivosProyecto(carpetaRaiz);
            //ahora filtro los dbml para poder ver si puedo obtener sus clases y metodos
            SolutionItem?[] ArchivosLinqConSP = (from x in ArchivosProyectoActual where (x.FullPath is null ? "" : x.FullPath).Contains(LinqSeleccionada) == true select x).ToArray();
            foreach (SolutionItem? x in ArchivosLinqConSP)
            {
                try
                {
                    // Get the Roslyn document for that file. Tomo como ejemplo la unica clase linq del proyecto
                    //luego agrego el codigo en base a la seleccion para buscar la mas parecida
                    DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.Children.ToArray()[0].FullPath).FirstOrDefault();
                    var mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
                    if (id is not null)
                    {
                        Document roslynDocument = workspace.CurrentSolution.GetDocument(id);

                        SyntaxNode root = await roslynDocument.GetSyntaxRootAsync();
                        SemanticModel model = await roslynDocument.GetSemanticModelAsync();
                        SyntaxTree tree = await roslynDocument.GetSyntaxTreeAsync();
                        CompilationUnitSyntax CUS = tree.GetCompilationUnitRoot();
                        var ClasesEncontradas = CUS.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
                        MethodDeclarationSyntax[] SPLinqEncontrados = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
                        string metodos = "";
                        string retorno = "";
                        List<string[]> ParametrosEntrada = new List<string[]>();
                        List<Metodo> MetodosAsociados = new List<Metodo>();

                        List<string> metodosChequeados = Metodos.Items
         .OfType<CheckBox>()
         .Where(cb => cb.IsChecked == true)
         .Select(cb => cb.Content.ToString().Substring(1))
         .ToList();
                        string RutaCarpetaoClase;
                        try
                        {
                            RutaCarpetaoClase = System.IO.Path.GetDirectoryName(x.FullPath).Replace(System.IO.Path.GetDirectoryName(projectoActual.Result.FullPath), "").Substring(1).Replace('\\', '.').Trim();
                        }
                        catch
                        {
                            RutaCarpetaoClase = "";
                        }



                        SPLinqEncontrados = SPLinqEncontrados.Where(z => metodosChequeados.Contains(z.Identifier.Text)).ToArray();

                        foreach (MethodDeclarationSyntax Metodo in SPLinqEncontrados)
                        {
                            Metodo MetodoAux = new Metodo();
                            metodos = Metodo.Identifier.Text;
                            if (!metodos.Equals("OnCreated"))
                            {
                                MetodoAux.Nombre = metodos;
                                retorno = Metodo.ReturnType.ToString();
                                if (retorno.Contains("ISingleResult<"))
                                {
                                    retorno = retorno.Replace("ISingleResult<", "");
                                    retorno = retorno.Replace(">", "");
                                    retorno = retorno + "[] ";
                                    MetodoAux.ObjetoRetorno = retorno;
                                }
                                else
                                {
                                    MetodoAux.ObjetoRetorno = retorno;
                                }
                                Console.WriteLine(retorno);
                                var parametros = Metodo.ParameterList;
                                if (parametros.Parameters.Count > 0)
                                {
                                    foreach (var param in parametros.Parameters)
                                    {
                                        string[] aux = new string[2];
                                        string atributo = param.ToFullString().Trim();
                                        aux = atributo.Split(' ');
                                        aux[0] = aux[0].Replace("System.Nullable<", "");
                                        aux[0] = aux[0].Replace(">", "");
                                        MetodoAux.ParametrosEntrada.Add(aux);
                                    }
                                }
                                MetodosAsociados.Add(MetodoAux);
                            }
                        }
                        if (MetodosAsociados.Count > 0)
                        {

                            foreach (Metodo met in MetodosAsociados)
                            {
                                Respuesta += "function  FN_WS_" + met.Nombre + "(";
                                if ((bool)CamposEntrada.IsChecked)
                                {
                                    Respuesta += String.Join(",",
                                        (from y in met.ParametrosEntrada select "CMP_" + y[1]).ToArray()
                                        );
                                }
                                Respuesta += ")\r\n{\r\n";
                                if (!(bool)CamposEntrada.IsChecked)
                                {
                                    Respuesta += String.Join(" ;\n",
                                        (from y in met.ParametrosEntrada select "//" + y[0] + " " + y[1] + "\nvar CMP_" + y[1]).ToArray()
                                        ) + ";\r\n";
                                }
                                Respuesta += "$.ajax({\r\n";
                                Respuesta += "type: 'POST',\r\n";
                                Respuesta += "url: '" + LinqSeleccionada.Replace("\\", "/") + "/" + met.Nombre + "',\r\n";

                                Respuesta += "data: JSON.stringify({ " + String.Join(",", (from y in met.ParametrosEntrada select y[1] + ": CMP_" + y[1]).ToArray()) + " }),\r\n";
                                Respuesta += "contentType: 'application/json; charset=utf-8',\r\n";
                                Respuesta += "dataType: 'json'\r\n";
                                Respuesta += "}).done(function (data) {\r\n";
                                Respuesta += "console.log('Respuesta del servidor: ');\r\n";
                                Respuesta += "console.log(data.d);\r\n";
                                Respuesta += "}).fail(function (xhr, status, error) {\r\n";
                                Respuesta += "console.error('Error en la llamada al servidor: ');\r\n";
                                Respuesta += "console.error(xhr);\r\n";
                                Respuesta += "console.error(status);\r\n";
                                Respuesta += "console.error(error);\r\n";
                                Respuesta += "});\r\n}";

                            }
                            if (tagScript.IsChecked == true)
                            {
                                Respuesta = "\n<script type=\"application/javascript\">\n" + Respuesta + "\n</script>\n";
                            }
                        }
                    }
                }
                catch { }
            }

            // object value = await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null) return;
            SnapshotPoint position = docView.TextView.Caret.Position.BufferPosition;
            docView.TextBuffer?.Replace(documentView.TextView.Selection.SelectedSpans.FirstOrDefault(), Respuesta);

            await VS.MessageBox.ShowWarningAsync("GeneraClasesLinq", "Se ha generado el codigo solicitado.\nFavor no olvidar descomentar el tag [System.Web.Script.Services.ScriptService]  ");
        }
    }

}
