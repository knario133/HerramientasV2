using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Community.VisualStudio.Toolkit;
using HerramientasV2.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System.Xml.Linq;

namespace HerramientasV2
{
    public partial class FormularioGeneraMetodosEnLinqControl : UserControl
    {
        private IReadOnlyList<SolutionItem> _projectItems = Array.Empty<SolutionItem>();
        private IReadOnlyList<ProjectFileEntry> _linqFiles = Array.Empty<ProjectFileEntry>();

        public FormularioGeneraMetodosEnLinqControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                _projectItems = await SolutionExplorerService.GetActiveProjectItemsAsync();
                await LoadLinqFilesAsync();
                await LoadConnectionStringsAsync();
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("Herramientas V2", $"Error inicializando herramienta: {ex}");
            }
        }

        private async Task LoadLinqFilesAsync()
        {
            try
            {
                var project = await SolutionExplorerService.GetActiveProjectAsync();
                if (project is null)
                {
                    LinqDisponibles.ItemsSource = null;
                    return;
                }

                var projectFolder = Path.GetDirectoryName(project.FullPath) ?? string.Empty;
                _linqFiles = _projectItems
                    .OfType<PhysicalFile>()
                    .Where(file => file.Name.EndsWith(".dbml", StringComparison.OrdinalIgnoreCase))
                    .Select(file => new ProjectFileEntry(file, GetRelativePath(file.FullPath, projectFolder)))
                    .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                LinqDisponibles.ItemsSource = _linqFiles;
                LinqDisponibles.DisplayMemberPath = nameof(ProjectFileEntry.RelativePath);
                Metodos.ItemsSource = null;
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("Herramientas V2", $"Error cargando archivos LINQ: {ex}");
            }
        }

        private async Task LoadConnectionStringsAsync()
        {
            try
            {
                var connectionNames = new List<string>();

                foreach (var configFile in _projectItems.OfType<PhysicalFile>().Where(f => f.Name.Equals("web.config", StringComparison.OrdinalIgnoreCase)))
                {
                    if (string.IsNullOrEmpty(configFile.FullPath) || !File.Exists(configFile.FullPath))
                    {
                        continue;
                    }

                    XDocument configXml = XDocument.Load(configFile.FullPath);
                    var connections = configXml
                        .Descendants()
                        .Where(e => e.Name.LocalName.Equals("add", StringComparison.OrdinalIgnoreCase) &&
                                    e.Parent?.Name.LocalName.Equals("connectionStrings", StringComparison.OrdinalIgnoreCase) == true)
                        .Select(e => e.Attribute("name")?.Value)
                        .Where(name => !string.IsNullOrWhiteSpace(name));

                    connectionNames.AddRange(connections!);
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                CadenasDeConexion.ItemsSource = connectionNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (CadenasDeConexion.Items.Count > 0)
                {
                    CadenasDeConexion.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("Herramientas V2", $"Error cargando cadenas de conexión: {ex}");
            }
        }

        private async void LinqDisponibles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LinqDisponibles.SelectedItem is ProjectFileEntry entry)
            {
                await LoadMethodsForSelectedDbmlAsync(entry);
            }
            else
            {
                Metodos.ItemsSource = null;
            }
        }

        private async Task LoadMethodsForSelectedDbmlAsync(ProjectFileEntry entry)
        {
            try
            {
                var designerPath = await GetDesignerFilePathAsync(entry.File);
                if (string.IsNullOrEmpty(designerPath))
                {
                    return;
                }

                var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
                if (workspace is null)
                {
                    return;
                }

                DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(designerPath).FirstOrDefault();
                if (id is null)
                {
                    return;
                }

                Document? roslynDocument = workspace.CurrentSolution.GetDocument(id);
                if (roslynDocument is null)
                {
                    return;
                }

                var root = await roslynDocument.GetSyntaxRootAsync();
                if (root is null)
                {
                    return;
                }

                var methods = root
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => !string.Equals(method.Identifier.Text, "OnCreated", StringComparison.Ordinal))
                    .Select(method => new CheckBox { Content = "_" + method.Identifier.Text })
                    .ToList();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Metodos.ItemsSource = methods;
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("Herramientas V2", $"Error cargando métodos LINQ: {ex}");
            }
        }

        private async Task<string?> GetDesignerFilePathAsync(PhysicalFile file)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var designerItem = file.Children
                .FirstOrDefault(child => child.FullPath?.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) == true)
                ?? file.Children.FirstOrDefault();

            return designerItem?.FullPath;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await InitializeAsync();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await GenerateCodeAsync();
        }

        private async Task GenerateCodeAsync()
        {
            if (LinqDisponibles.SelectedItem is not ProjectFileEntry entry)
            {
                return;
            }

            try
            {
                var project = await SolutionExplorerService.GetActiveProjectAsync();
                var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
                var documentView = await VS.Documents.GetActiveDocumentViewAsync();

                if (project is null || workspace is null || documentView?.TextView is null)
                {
                    return;
                }

                var designerPath = await GetDesignerFilePathAsync(entry.File);
                if (string.IsNullOrEmpty(designerPath))
                {
                    return;
                }

                DocumentId? id = workspace.CurrentSolution.GetDocumentIdsWithFilePath(designerPath).FirstOrDefault();
                if (id is null)
                {
                    return;
                }

                Document? roslynDocument = workspace.CurrentSolution.GetDocument(id);
                if (roslynDocument is null)
                {
                    return;
                }

                var root = await roslynDocument.GetSyntaxRootAsync();
                if (root is null)
                {
                    return;
                }

                var selectedMethods = Metodos.Items
                    .OfType<CheckBox>()
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Content?.ToString()?.TrimStart('_'))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .ToHashSet(StringComparer.Ordinal);

                if (!selectedMethods.Any())
                {
                    return;
                }

                var methodNodes = root
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => selectedMethods.Contains(method.Identifier.Text))
                    .ToArray();

                if (methodNodes.Length == 0)
                {
                    return;
                }

                string relativeNamespace = BuildNamespace(entry.File, project);
                string dataContextName = Path.GetFileNameWithoutExtension(roslynDocument.Name);

                string generatedCode = BuildCode(methodNodes, relativeNamespace, dataContextName);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var selection = documentView.TextView.Selection.SelectedSpans.FirstOrDefault();
                if (selection.Length > 0)
                {
                    documentView.TextBuffer?.Replace(selection, generatedCode);
                }
                else
                {
                    SnapshotPoint position = documentView.TextView.Caret.Position.BufferPosition;
                    documentView.TextBuffer?.Insert(position, generatedCode);
                }

                await VS.MessageBox.ShowWarningAsync("Herramienta V2", "Se ha generado el codigo solicitado... Buen día!");
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("Herramientas V2", $"Error generando código: {ex}");
            }
        }

        private string BuildCode(IEnumerable<MethodDeclarationSyntax> methods, string relativeNamespace, string dataContextName)
        {
            var builder = new StringBuilder();

            foreach (var metodo in methods.Select(CreateMetodo))
            {
                builder.AppendLine($"public class Rsp_{metodo.Nombre}{{");
                builder.AppendLine("public bool Error;");
                builder.AppendLine("public string Message;");
                builder.AppendLine("public string StackTrace;");
                builder.AppendLine("public int SqlErrorCode;");
                builder.AppendLine($"public {BuildNamespacePrefix(relativeNamespace)}{metodo.ObjetoRetorno} Resultado;\r\n}}");

                if (webmethod.IsChecked == true)
                {
                    builder.AppendLine("[WebMethod]");
                }
                else if (webmethodSesion.IsChecked == true)
                {
                    builder.AppendLine("[WebMethod(EnableSession = true)]");
                }

                builder.Append($"public Rsp_{metodo.Nombre} {metodo.Nombre}(");
                builder.Append(string.Join(",", metodo.ParametrosEntrada.Select(p => $"{p.Tipo} {p.Nombre}")));
                builder.AppendLine(")");
                builder.AppendLine("{");
                builder.AppendLine("/**Inicio Codigo Previo**/");
                builder.AppendLine(TextoPrevio.Text);
                builder.AppendLine("/**Fin Codigo Previo**/");
                builder.AppendLine($"Rsp_{metodo.Nombre} RespuestaMetodo = new Rsp_{metodo.Nombre}();");

                if (CadenasDeConexion.SelectedItem is string cadenaSeleccionada)
                {
                    builder.AppendLine($"string cadenaConexion = System.Configuration.ConfigurationManager.ConnectionStrings[\"{cadenaSeleccionada}\"].ConnectionString;");
                    builder.AppendLine($"using({BuildNamespacePrefix(relativeNamespace)}{dataContextName}DataContext linkbd = new {BuildNamespacePrefix(relativeNamespace)}{dataContextName}DataContext(cadenaConexion))");
                }
                else
                {
                    builder.AppendLine($"using({BuildNamespacePrefix(relativeNamespace)}{dataContextName}DataContext linkbd = new {BuildNamespacePrefix(relativeNamespace)}{dataContextName}DataContext())");
                }

                builder.AppendLine("{");
                builder.AppendLine("try{");
                builder.Append($"RespuestaMetodo.Resultado=linkbd.{metodo.Nombre}(");
                builder.Append(string.Join(",", metodo.ParametrosEntrada.Select(p => p.Nombre)));
                builder.AppendLine(metodo.ObjetoRetorno.EndsWith("[]", StringComparison.Ordinal) ? ").ToArray();" : ");");
                builder.AppendLine("RespuestaMetodo.Error=false;");
                builder.AppendLine("RespuestaMetodo.Message=\"\";");
                builder.AppendLine("RespuestaMetodo.StackTrace=\"\";");
                builder.AppendLine("RespuestaMetodo.SqlErrorCode=0;");
                builder.AppendLine("} catch(System.Data.SqlClient.SqlException sqlEx){");
                builder.AppendLine("RespuestaMetodo.Error=true;");
                builder.AppendLine("RespuestaMetodo.Message=sqlEx.Message;");
                builder.AppendLine("RespuestaMetodo.SqlErrorCode=sqlEx.Number;");
                builder.AppendLine("RespuestaMetodo.StackTrace=sqlEx.StackTrace;");
                builder.AppendLine("} catch(NullReferenceException nullRefEx){");
                builder.AppendLine("RespuestaMetodo.Error=true;");
                builder.AppendLine("RespuestaMetodo.Message=nullRefEx.Message;");
                builder.AppendLine("RespuestaMetodo.StackTrace=nullRefEx.StackTrace;");
                builder.AppendLine("} catch(InvalidCastException castEx){");
                builder.AppendLine("RespuestaMetodo.Error=true;");
                builder.AppendLine("RespuestaMetodo.Message=castEx.Message;");
                builder.AppendLine("RespuestaMetodo.StackTrace=castEx.StackTrace;");
                builder.AppendLine("} catch(Exception e1){");
                builder.AppendLine("RespuestaMetodo.Error=true;");
                builder.AppendLine("RespuestaMetodo.Message=e1.Message;");
                builder.AppendLine("RespuestaMetodo.StackTrace=e1.StackTrace;");
                builder.AppendLine("}");
                builder.AppendLine("return RespuestaMetodo;");
                builder.AppendLine("}");
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private static Metodo CreateMetodo(MethodDeclarationSyntax method)
        {
            var metodo = new Metodo
            {
                Nombre = method.Identifier.Text,
                ObjetoRetorno = NormalizeReturnType(method.ReturnType.ToString())
            };

            foreach (var parameter in method.ParameterList.Parameters)
            {
                metodo.ParametrosEntrada.Add(new MetodoParametro
                {
                    Tipo = NormalizeParameterType(parameter.Type?.ToString() ?? "object"),
                    Nombre = parameter.Identifier.Text
                });
            }

            return metodo;
        }

        private static string NormalizeReturnType(string returnType)
        {
            if (returnType.Contains("ISingleResult<", StringComparison.Ordinal))
            {
                return returnType.Replace("ISingleResult<", string.Empty, StringComparison.Ordinal)
                                 .Replace(">", string.Empty, StringComparison.Ordinal) + "[]";
            }

            return returnType;
        }

        private static string NormalizeParameterType(string parameterType)
        {
            return parameterType.Replace("System.Nullable<", string.Empty, StringComparison.Ordinal)
                                 .Replace(">", string.Empty, StringComparison.Ordinal);
        }

        private static string BuildNamespace(PhysicalFile file, Project project)
        {
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var fileDir = Path.GetDirectoryName(file.FullPath);

            if (string.IsNullOrEmpty(projectDir) || string.IsNullOrEmpty(fileDir))
            {
                return string.Empty;
            }

            if (!fileDir.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var relative = fileDir.Substring(projectDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
        }

        private static string BuildNamespacePrefix(string relativeNamespace)
        {
            if (string.IsNullOrEmpty(relativeNamespace))
            {
                return string.Empty;
            }

            return relativeNamespace + ".";
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
            {
                return fullPath;
            }

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }

            return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private sealed record ProjectFileEntry(PhysicalFile File, string RelativePath);

        private sealed class Metodo
        {
            public string Nombre { get; set; } = string.Empty;
            public string ObjetoRetorno { get; set; } = string.Empty;
            public List<MetodoParametro> ParametrosEntrada { get; } = new();
        }

        private sealed class MetodoParametro
        {
            public string Tipo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
        }
    }
}
