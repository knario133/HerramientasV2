using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;

namespace HerramientasV2
{
    public partial class FormularioGeneraServicioRestControl : UserControl
    {

        public FormularioGeneraServicioRestControl()
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
                var ArchivosLinqConSP = (from x in archivosProyectoActual where x.Text.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true select x).ToArray();
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
                        DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.FullPath).FirstOrDefault();
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
                                                       //    where c.Identifier.Text != "OnCreated"
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

            public bool esEstatico { get; set; }
            public string NombreDeClase { get; set; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RellenaComboClases();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //   GeneraCodigo();
            AddGenericHandlerWithNamespaceAsync();
        }


        public async void AddGenericHandlerWithNamespaceAsync()
        {
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
            // Obtener el proyecto activo
            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            if (activeProject == null) return;

            // Obtener el namespace base del proyecto
            string projectName = Path.GetFileNameWithoutExtension(activeProject.FullPath);
            string baseNamespace = projectName.Replace(" ", "_"); // Por si hay espacios en el nombre

            // Definir las rutas de los archivos
            string projectDir = Path.GetDirectoryName(activeProject.FullPath) ?? string.Empty;
            //string handlerPath = Path.Combine(projectDir, "Handler1.ashx");
            //string codeBehindPath = Path.Combine(projectDir, "Handler1.ashx.cs");

            // Definir la ruta de la carpeta Handlers
            string handlersFolderPath = Path.Combine(projectDir, "Handlers");

            // Crear la carpeta Handlers si no existe
            if (!Directory.Exists(handlersFolderPath))
            {
                Directory.CreateDirectory(handlersFolderPath);
            }

            List<SolutionItem> archivosProyectoActual = ObtenerArchivosProyecto(activeProject);
            SolutionItem? archivoLinq = archivosProyectoActual.FirstOrDefault(x => (x.FullPath ?? string.Empty).Contains(LinqSeleccionada));
            if (archivoLinq != null)
            {
                // Obtener el workspace y el documento Roslyn del archivo seleccionado
                var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
                var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(archivoLinq.FullPath).FirstOrDefault();
                var document = workspace.CurrentSolution.GetDocument(documentId);

                if (document != null)
                {
                    var root = await document.GetSyntaxRootAsync();
                    var metodos = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    // Lista para almacenar los métodos encontrados
                    List<Metodo> metodosAsociados = new List<Metodo>();

                    //obtengo los metodos chequeados
                    List<string> metodosChequeados = Metodos.Items
                    .OfType<CheckBox>()
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Content.ToString().Substring(1))
                    .ToList();

                    // Iterar sobre los métodos encontrados
                    foreach (var metodo in metodos)
                    {
                        if (!metodosChequeados.Contains(metodo.Identifier.Text))
                        {
                            continue; // Saltamos este método si no fue chequeado
                        }

                        var claseContenedora = metodo.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                        string nombreClase = claseContenedora?.Identifier.Text ?? "ClaseDesconocida";
                        Metodo metodoAux = new Metodo
                        {
                            Nombre = metodo.Identifier.Text,
                            ObjetoRetorno = metodo.ReturnType.ToString(),
                            esEstatico = metodo.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
                            NombreDeClase = nombreClase
                        };

                        // Obtener los parámetros de entrada
                        foreach (var parametro in metodo.ParameterList.Parameters)
                        {
                            string[] paramInfo = new string[2];
                            paramInfo[0] = parametro.Type.ToString();  // Tipo del parámetro
                            paramInfo[1] = parametro.Identifier.Text;  // Nombre del parámetro

                            metodoAux.ParametrosEntrada.Add(paramInfo);
                        }

                        metodosAsociados.Add(metodoAux);
                    }

                    // Mostrar los métodos encontrados en la consola
                    foreach (var metodo in metodosAsociados)
                    {
                        string handlerPath = Path.Combine(handlersFolderPath, "Handler_" + metodo.Nombre + ".ashx");
                        string codeBehindPath = Path.Combine(handlersFolderPath, "Handler_" + metodo.Nombre + ".ashx.cs");


                        // Verificar si los archivos ya existen
                        if (!File.Exists(handlerPath) && !File.Exists(codeBehindPath))
                        {



                            // Crear el contenido del archivo .ashx
                            string handlerCode = @"<%@ WebHandler Language=""C#"" CodeBehind=""{Handler1}.ashx.cs"" Class=""{baseNamespace}.{Handler1}"" %>";
                            handlerCode = handlerCode.Replace("{Handler1}", "Handler_" + metodo.Nombre);
                            handlerCode = handlerCode.Replace("{baseNamespace}", baseNamespace);
                            string Parametros = "";

                            foreach (var parametro in metodo.ParametrosEntrada)
                            {
                                Parametros += "\n public " + parametro[0] + " " + parametro[1] + " { get; set; }";

                            }
                            string ParametrosRSP = "";

                            ParametrosRSP += "\n public " + metodo.NombreDeClase + "." + metodo.ObjetoRetorno + " Respuesta { get; set; }";
                            string LlamadaMetodo = metodo.esEstatico
    ? $"respuestaServicio.Respuesta = {metodo.NombreDeClase}.{metodo.Nombre}({string.Join(", ", metodo.ParametrosEntrada.Select(p => "\rEntradaServicioRest." + p[1]))});"
    : $"var instancia = new {metodo.NombreDeClase}();\nrespuestaServicio.Respuesta = instancia.{metodo.Nombre}({string.Join(", ", metodo.ParametrosEntrada.Select(p => "\rEntradaServicioRest." + p[1]))});";

                            //   string LlamadaMetodo = "respuestaServicio.Respuesta = " + metodo.Nombre + "(\n " + string.Join(",\r", (from x in metodo.ParametrosEntrada select "EntradaServicioRest." + x[1])) + ");";
                            // Crear el contenido del archivo .ashx.cs
                            string codeBehindCode = @"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace {baseNamespace}
{
    public class {Handler1} : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = ""application/json"";
            try{
            // Leer el cuerpo de la solicitud
            string jsonBody;
            using (var reader = new System.IO.StreamReader(context.Request.InputStream))
            {
                jsonBody = reader.ReadToEnd();
            }
            // Variables de entrada a Mapear
            IN_{Handler1} EntradaServicioRest = JsonConvert.DeserializeObject<IN_{Handler1}>(jsonBody);
            //Llamada a metodo
            
            RSP_{Handler1} respuestaServicio = new RSP_{Handler1}();
            try{
            {LlamadaMetodo}
            respuestaServicio.CodigoRespuesta = ""200"";
            respuestaServicio.GlosaRespuesta = ""Operación realizada con éxito."";
            }
            catch (Exception ex){
                    respuestaServicio.CodigoRespuesta = ""500"";
                    respuestaServicio.GlosaRespuesta = $""Error al procesar el método: {ex.Message}"";
                }
            string jsonString = JsonConvert.SerializeObject(respuestaServicio);
            context.Response.Write(jsonString);
            }
            catch (Exception ex)
                        {
                            context.Response.StatusCode = 500;
                            context.Response.Write(JsonConvert.SerializeObject(new
                            {
                                CodigoRespuesta = ""500"",
                                GlosaRespuesta = $""Error inesperado: {ex.Message}""
                            }));
                        }
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public class IN_{Handler1}
        {
            {parametros}
        }

        public class RSP_{Handler1}
        {
            public string CodigoRespuesta { get; set; }
            public string GlosaRespuesta { get; set; }
            {parametrosRSP}
        }
    }
}";
                            codeBehindCode = codeBehindCode.Replace("{baseNamespace}", baseNamespace);
                            codeBehindCode = codeBehindCode.Replace("{Handler1}", "Handler_" + metodo.Nombre);
                            codeBehindCode = codeBehindCode.Replace("{parametros}", Parametros);
                            codeBehindCode = codeBehindCode.Replace("{parametrosRSP}", ParametrosRSP);
                            codeBehindCode = codeBehindCode.Replace("{LlamadaMetodo}", LlamadaMetodo);
                            // Escribir los archivos
                            File.WriteAllText(handlerPath, handlerCode);
                            File.WriteAllText(codeBehindPath, codeBehindCode);

                            // Agregar los archivos al proyecto
                            await activeProject.AddExistingFilesAsync(handlerPath, codeBehindPath);

                            // Confirmar en la consola de salida
                            await VS.MessageBox.ShowAsync("Resultado de proceso", $"{handlerPath} y {codeBehindPath} agregados correctamente.");
                        }
                        else
                        {
                            await VS.MessageBox.ShowAsync("Resultado de proceso", $"El archivo {handlerPath} ya existe.");
                        }
                    }
                }
                else
                {
                    VS.MessageBox.ShowError("Error", "No se pudo obtener el documento Roslyn.");
                }



            }
        }





    }
}
