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
using System.Diagnostics;
using System.Xml.Linq;


namespace HerramientasV2
{
    public partial class FormularioGeneraMetodosEnLinqControl : UserControl
    {
        public FormularioGeneraMetodosEnLinqControl()
        {
            InitializeComponent();

            RellenaComboClases();
            RellenaComboStringConnections();
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
                var archivosLinqConSP = (from x in archivosProyectoActual where x.Text.Contains(".dbml") == true select x).ToArray();

                LinqDisponibles.Items.Clear();
                foreach (var linEncontrada in archivosLinqConSP)
                {
                    LinqDisponibles.Items.Add(linEncontrada.FullPath.Replace(System.IO.Path.GetDirectoryName(projectoActual.Result.FullPath), ""));
                }
                Metodos.ItemsSource = null;
            }
            catch (Exception e1)
            {
                ActivityLog.LogInformation("Herramientas V2", "Mensaje:" + e1.Message + "---Stack:" + e1.StackTrace);
            }
        }

        private async void RellenaComboStringConnections()
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
                // Limpiar la lista antes de llenarla
                CadenasDeConexion.Items.Clear();

                // Obtiene todos los archivos web.config
                var archivosWebConfig = archivosProyectoActual.Where(x => x.Name.Contains("web.config")).ToArray();

                foreach (var archivoWebConfig in archivosWebConfig)
                {
                    // Obtiene la ruta completa del archivo
                    string rutaArchivo = archivoWebConfig.FullPath;

                    // Carga el archivo
                    XDocument configXml = XDocument.Load(rutaArchivo);

                    // Encuentra las cadenas de conexión
                    var connectionStrings = configXml.Descendants()
                        .Where(e => e.Name.LocalName == "add" && e.Parent != null && e.Parent.Name.LocalName == "connectionStrings");

                    foreach (var connectionString in connectionStrings)
                    {
                        // Agrega la cadena de conexión a la lista de cadenas de conexión
                        CadenasDeConexion.Items.Add(connectionString.Attribute("name")?.Value);
                    }
                    if (CadenasDeConexion.Items.Count > 0)
                    {
                        CadenasDeConexion.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception e1)
            {
                ActivityLog.LogInformation("Herramientas V2", "Mensaje:" + e1.Message + "---Stack:" + e1.StackTrace);
            }
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
            VS.MessageBox.Show("FormularioGeneraMetodosEnLinqControl", "Button clicked");
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
                        DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.Children.ToArray()[1].FullPath).FirstOrDefault();
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
                    catch (Exception e1)
                    {
                        ActivityLog.LogInformation("Herramientas V2", "Mensaje:" + e1.Message + "---Stack:" + e1.StackTrace);
                    }
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
            try
            {
                RellenaComboClases();
                RellenaComboStringConnections();
            }
            catch (Exception e1)
            {
                ActivityLog.LogInformation("Herramientas V2", "Mensaje Herramientas V2:" + e1.Message + "---Stack:" + e1.StackTrace);
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                GeneraCodigo();
            }
            catch (Exception e1)
            {
                ActivityLog.LogInformation("Herramientas V2", "Mensaje Herramientas V2:" + e1.Message + "---Stack:" + e1.StackTrace);
            }
        }

        private async void GeneraCodigo_old()
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
            var carpetaRaiz = projectoActual.Result;
            //Obtengo los hijos o archivos asociados al proyecto
            List<SolutionItem> ArchivosProyectoActual = ObtenerArchivosProyecto(carpetaRaiz);
            //ahora filtro los dbml para poder ver si puedo obtener sus clases y metodos
            SolutionItem?[] ArchivosLinqConSP = (from x in ArchivosProyectoActual where (x.FullPath is null ? "" : x.FullPath).Contains(LinqSeleccionada) == true select x).ToArray();
            foreach (SolutionItem? x in ArchivosLinqConSP)
            {
                // Get the Roslyn document for that file. Tomo como ejemplo la unica clase linq del proyecto
                //luego agrego el codigo en base a la seleccion para buscar la mas parecida
                DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.Children.ToArray()[1].FullPath).FirstOrDefault();
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
                                    string atributo = param.ToFullString().Replace(param.AttributeLists[0].ToString(), "").Trim();
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
                            Respuesta += "public class Rsp_" + met.Nombre + "{\r\n";
                            Respuesta += "public bool Error;\r\n";
                            Respuesta += "public string Message;\r\n";
                            Respuesta += "public string StackTrace;\r\n";
                            Respuesta += "public " + RutaCarpetaoClase + (RutaCarpetaoClase.Equals("") ? "" : ".") + met.ObjetoRetorno + " Resultado;\r\n}\r\n";
                            if (webmethod.IsChecked == true)
                            {
                                Respuesta += "[WebMethod]\r\n";
                            }
                            Respuesta += "public " + "Rsp_" + met.Nombre + " " + met.Nombre + "(";
                            Respuesta += String.Join(",",
                                (from y in met.ParametrosEntrada select y[0] + " " + y[1]).ToArray()
                                );
                            Respuesta += ")\r\n{\r\n";
                            Respuesta += "Rsp_" + met.Nombre + " RespuestaMetodo = new Rsp_" + met.Nombre + "();\r\n";
                            Respuesta += "using(" + RutaCarpetaoClase + (RutaCarpetaoClase.Equals("") ? "" : ".") + roslynDocument.Name.Split('.')[0] + "DataContext linkbd = new " + RutaCarpetaoClase + (RutaCarpetaoClase.Equals("") ? "" : ".") + roslynDocument.Name.Split('.')[0] + "DataContext())\r\n{";
                            Respuesta += "try{\r\n";
                            Respuesta += "RespuestaMetodo.Resultado=linkbd." + met.Nombre + "(";
                            Respuesta += String.Join(",",
                                (from y in met.ParametrosEntrada select y[1]).ToArray()
                                );
                            Respuesta += (met.ObjetoRetorno.Contains("[]") ? ").ToArray()" : ")") + ";\r\n";
                            Respuesta += "RespuestaMetodo.Error=false;\r\n";
                            Respuesta += "RespuestaMetodo.Message=\"\";\r\n";
                            Respuesta += "RespuestaMetodo.StackTrace=\"\";\r\n";
                            Respuesta += "}\r\n";
                            Respuesta += "catch(Exception e1){\r\n";
                            Respuesta += "RespuestaMetodo.Error=true;\r\n";
                            Respuesta += "RespuestaMetodo.Message=e1.Message;\r\n";
                            Respuesta += "RespuestaMetodo.StackTrace=e1.StackTrace;\r\n";
                            Respuesta += "}\r\n";
                            Respuesta += "}\r\n";
                            Respuesta += "return RespuestaMetodo;\r\n";
                            Respuesta += "}\r\n";



                        }
                    }
                }
            }

            // object value = await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null) return;
            SnapshotPoint position = docView.TextView.Caret.Position.BufferPosition;
            docView.TextBuffer?.Replace(documentView.TextView.Selection.SelectedSpans.FirstOrDefault(), Respuesta);

            await VS.MessageBox.ShowWarningAsync("Herramienta V2", "Se ha generado el codigo solicitado... Buen día!");
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
                }
                else
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error al seleccionar Linq: {e}");
                return;
            }

            Workspace workspace;
            DocumentView? documentView;
            SolutionItem? carpetaRaiz;
            try
            {
                workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
                documentView = await VS.Documents.GetActiveDocumentViewAsync();
                carpetaRaiz = await VS.Solutions.GetActiveProjectAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error al obtener workspace, documentView o carpetaRaiz: {e}");
                return;
            }

            if (documentView == null || carpetaRaiz == null)
            {
                Debug.WriteLine("documentView o carpetaRaiz son null");
                return;
            }

            List<SolutionItem> ArchivosProyectoActual = ObtenerArchivosProyecto(carpetaRaiz);
            SolutionItem?[] ArchivosLinqConSP = (from x in ArchivosProyectoActual where (x.FullPath is null ? "" : x.FullPath).Contains(LinqSeleccionada) == true select x).ToArray();
            foreach (SolutionItem? x in ArchivosLinqConSP)
            {
                try
                {
                    DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.Children.ToArray()[1].FullPath).FirstOrDefault();
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
                            RutaCarpetaoClase = System.IO.Path.GetDirectoryName(x.FullPath).Replace(System.IO.Path.GetDirectoryName(carpetaRaiz.FullPath), "").Substring(1).Replace('\\', '.').Trim();
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
                                        string atributo = param.ToFullString().Replace(param.AttributeLists[0].ToString(), "").Trim();
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
                                Respuesta += "public class Rsp_" + met.Nombre + "{\r\n";
                                Respuesta += "public bool Error;\r\n";
                                Respuesta += "public string Message;\r\n";
                                Respuesta += "public string StackTrace;\r\n";
                                Respuesta += "public int SqlErrorCode;\r\n";
                                Respuesta += "public " + RutaCarpetaoClase + (RutaCarpetaoClase.Equals("") ? "" : ".") + met.ObjetoRetorno + " Resultado;\r\n}\r\n";
                                if (webmethod.IsChecked == true)
                                {
                                    Respuesta += "[WebMethod]\r\n";
                                }
                                else
                                {
                                    if (webmethodSesion.IsChecked == true)
                                    {
                                        Respuesta += "[WebMethod(EnableSession = true)]\r\n";
                                    }

                                }
                                Respuesta += "public " + "Rsp_" + met.Nombre + " " + met.Nombre + "(";
                                Respuesta += String.Join(",",
                                    (from y in met.ParametrosEntrada select y[0] + " " + y[1]).ToArray()
                                    );
                                Respuesta += ")\r\n{\r\n";
                                Respuesta += "/**Inicio Codigo Previo**/\r\n";
                                Respuesta += TextoPrevio.Text+"\r\n";
                                Respuesta += "/**Fin Codigo Previo**/\r\n";

                                Respuesta += "Rsp_" + met.Nombre + " RespuestaMetodo = new Rsp_" + met.Nombre + "();\r\n";
                                Respuesta += "string cadenaConexion = System.Configuration.ConfigurationManager.ConnectionStrings[\"" + CadenasDeConexion.SelectedItem + "\"].ConnectionString;\r\n";
                                Respuesta += "using(" + RutaCarpetaoClase + (RutaCarpetaoClase.Equals("") ? "" : ".") + roslynDocument.Name.Split('.')[0] + "DataContext linkbd = new " + RutaCarpetaoClase + (RutaCarpetaoClase.Equals("") ? "" : ".") + roslynDocument.Name.Split('.')[0] + "DataContext(cadenaConexion))\r\n{";
                                Respuesta += "try{\r\n";
                               
                                Respuesta += "RespuestaMetodo.Resultado=linkbd." + met.Nombre + "(";
                                Respuesta += String.Join(",",
                                    (from y in met.ParametrosEntrada select y[1]).ToArray()
                                    );
                                Respuesta += (met.ObjetoRetorno.Contains("[]") ? ").ToArray()" : ")") + ";\r\n";
                                Respuesta += "RespuestaMetodo.Error=false;\r\n";
                                Respuesta += "RespuestaMetodo.Message=\"\";\r\n";
                                Respuesta += "RespuestaMetodo.StackTrace=\"\";\r\n";
                                /*   Respuesta += "}\r\n";
                                   Respuesta += "catch(Exception e1){\r\n";
                                   Respuesta += "RespuestaMetodo.Error=true;\r\n";
                                   Respuesta += "RespuestaMetodo.Message=e1.Message;\r\n";
                                   Respuesta += "RespuestaMetodo.StackTrace=e1.StackTrace;\r\n";
                                   Respuesta += "}\r\n";
                                   Respuesta += "}\r\n";
                                   Respuesta += "return RespuestaMetodo;\r\n";
                                   Respuesta += "}\r\n";*/
                                Respuesta += "} catch(System.Data.SqlClient.SqlException sqlEx){\r\n";
                                Respuesta += "RespuestaMetodo.Error=true;\r\n";
                                Respuesta += "RespuestaMetodo.Message=sqlEx.Message;\r\n";
                                Respuesta += "RespuestaMetodo.SqlErrorCode=sqlEx.Number;\r\n";
                                Respuesta += "RespuestaMetodo.StackTrace=sqlEx.StackTrace;\r\n";
                                Respuesta += "} catch(NullReferenceException nullRefEx){\r\n";
                                Respuesta += "RespuestaMetodo.Error=true;\r\n";
                                Respuesta += "RespuestaMetodo.Message=nullRefEx.Message;\r\n";
                                Respuesta += "RespuestaMetodo.StackTrace=nullRefEx.StackTrace;\r\n";
                                Respuesta += "} catch(InvalidCastException castEx){\r\n";
                                Respuesta += "RespuestaMetodo.Error=true;\r\n";
                                Respuesta += "RespuestaMetodo.Message=castEx.Message;\r\n";
                                Respuesta += "RespuestaMetodo.StackTrace=castEx.StackTrace;\r\n";
                                Respuesta += "} catch(Exception e1){\r\n";
                                Respuesta += "RespuestaMetodo.Error=true;\r\n";
                                Respuesta += "RespuestaMetodo.Message=e1.Message;\r\n";
                                Respuesta += "RespuestaMetodo.StackTrace=e1.StackTrace;\r\n";
                                Respuesta += "}\r\n";
                                Respuesta += "return RespuestaMetodo;\r\n";
                                Respuesta += "}\r\n";
                                Respuesta += "}\r\n";
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Si algo sale mal, escribe el error en el log.
                    Debug.WriteLine($"Error : {e}");
                }
            }
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null) return;
            SnapshotPoint position = docView.TextView.Caret.Position.BufferPosition;
            try
            {
                // Intenta reemplazar el texto en el buffer.
                docView.TextBuffer?.Replace(documentView.TextView.Selection.SelectedSpans.FirstOrDefault(), Respuesta);
            }
            catch (Exception e)
            {
                // Si algo sale mal, escribe el error en el log.
                Debug.WriteLine($"Error replacing text in buffer: {e}");
            }

            try
            {
                // Intenta mostrar la caja de mensaje.
                await VS.MessageBox.ShowWarningAsync("Herramienta V2", "Se ha generado el codigo solicitado... Buen día!");
            }
            catch (Exception e)
            {
                // Si algo sale mal, escribe el error en el log.
                Debug.WriteLine($"Error showing message box: {e}");
            }
        }






    }

}
