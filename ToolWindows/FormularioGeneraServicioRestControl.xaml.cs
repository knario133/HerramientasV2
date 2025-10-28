using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

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
                var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
                var project = await VS.Solutions.GetActiveProjectAsync();
                var csFiles = project.Children.Where(i => i.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

                LinqDisponibles.ItemsSource = csFiles.Select(f => f.Name);
            }
            catch { }
        }

        private async void LinqDisponibles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            try
            {
                var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
                var project = await VS.Solutions.GetActiveProjectAsync();
                var selectedFile = project.Children.FirstOrDefault(i => i.Name == e.AddedItems[0].ToString());

                if (selectedFile == null) return;

                var document = workspace.CurrentSolution.GetDocumentIdsWithFilePath(selectedFile.FullPath).FirstOrDefault();
                if (document == null) return;

                var syntaxRoot = await workspace.CurrentSolution.GetDocument(document).GetSyntaxRootAsync();
                var methods = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();

                Metodos.ItemsSource = methods.Select(m => new CheckBox { Content = m.Identifier.Text });
            }
            catch { }
        }

        private async void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedMethods = Metodos.Items.OfType<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList();
            if (!selectedMethods.Any())
            {
                await VS.MessageBox.ShowWarningAsync("No se seleccionó ningún método.");
                return;
            }

            var project = await VS.Solutions.GetActiveProjectAsync();
            var handlersFolderPath = Path.Combine(Path.GetDirectoryName(project.FullPath), "Handlers");
            Directory.CreateDirectory(handlersFolderPath);

            var workspace = await VS.GetMefServiceAsync<VisualStudioWorkspace>();
            var selectedFile = project.Children.FirstOrDefault(i => i.Name == LinqDisponibles.SelectedItem.ToString());
            var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(selectedFile.FullPath).FirstOrDefault();
            var document = workspace.CurrentSolution.GetDocument(documentId);
            var semanticModel = await document.GetSemanticModelAsync();
            var syntaxRoot = await document.GetSyntaxRootAsync();

            foreach (var methodName in selectedMethods)
            {
                var method = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == methodName);
                if (method == null) continue;

                var handlerPath = Path.Combine(handlersFolderPath, $"Handler_{methodName}.ashx");
                if (File.Exists(handlerPath))
                {
                    await VS.MessageBox.ShowWarningAsync($"El archivo {handlerPath} ya existe.");
                    continue;
                }

                GenerateHandlerFiles(method, semanticModel, project.Name, handlersFolderPath);
                await project.AddExistingFilesAsync(handlerPath, $"{handlerPath}.cs");
                await VS.MessageBox.ShowAsync($"Handler {methodName} creado correctamente.");
            }
        }

        private static void GenerateHandlerFiles(MethodDeclarationSyntax method, SemanticModel semanticModel, string projectName, string handlersFolderPath)
        {
            var handlerName = $"Handler_{method.Identifier.Text}";
            var handlerCode = $@"<%@ WebHandler Language=""C#"" CodeBehind=""{handlerName}.ashx.cs"" Class=""{projectName}.{handlerName}"" %>";
            File.WriteAllText(Path.Combine(handlersFolderPath, $"{handlerName}.ashx"), handlerCode);

            var usings = method.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.ToString()).ToList();
            var classDeclaration = method.Parent as ClassDeclarationSyntax;
            var isStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);
            var parameters = GetParameters(method, semanticModel);
            var returnType = semanticModel.GetSymbolInfo(method.ReturnType).Symbol.ToDisplayString();

            var codeBehind = $@"
using System;
using System.Web;
using Newtonsoft.Json;
{string.Join("\n", usings)}

namespace {projectName}
{{
    public class {handlerName} : IHttpHandler
    {{
        public void ProcessRequest(HttpContext context)
        {{
            context.Response.ContentType = ""application/json"";
            try
            {{
                string jsonBody;
                using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                {{
                    jsonBody = reader.ReadToEnd();
                }}

                var input = JsonConvert.DeserializeObject<Input>(jsonBody);
                var response = new Response();

                try
                {{
                    var result = {(isStatic ? $"{classDeclaration.Identifier.Text}" : $"new {classDeclaration.Identifier.Text}()")}.{method.Identifier.Text}({string.Join(", ", parameters.Select(p => $"input.{p.Name}"))});
                    response.Data = result;
                    response.Success = true;
                }}
                catch (Exception ex)
                {{
                    response.ErrorMessage = ex.Message;
                }}
                context.Response.Write(JsonConvert.SerializeObject(response));
            }}
            catch (Exception ex)
            {{
                context.Response.StatusCode = 500;
                context.Response.Write(JsonConvert.SerializeObject(new {{ ErrorMessage = ex.Message }}));
            }}
        }}

        public bool IsReusable => false;

        public class Input
        {{
            {string.Join("\n            ", parameters.Select(p => $"public {p.Type} {p.Name} {{ get; set; }}"))}
        }}

        public class Response
        {{
            public bool Success {{ get; set; }} = false;
            public string ErrorMessage {{ get; set; }}
            public {returnType} Data {{ get; set; }}
        }}
    }}
}}
/*
// Ejemplo de cómo consumir este servicio con JavaScript:
async function consumeService() {{
    const url = '/Handlers/{handlerName}.ashx';
    const data = {{
        {string.Join(",\n        ", parameters.Select(p => $"{p.Name}: 'valor'"))}
    }};

    try {{
        const response = await fetch(url, {{
            method: 'POST',
            headers: {{
                'Content-Type': 'application/json'
            }},
            body: JSON.stringify(data)
        }});

        if (!response.ok) {{
            throw new Error(`HTTP error! status: ${{response.status}}`);
        }}

        const result = await response.json();
        console.log(result);
    }} catch (error) {{
        console.error('Error al consumir el servicio:', error);
    }}
}}
*/
";
            File.WriteAllText(Path.Combine(handlersFolderPath, $"{handlerName}.ashx.cs"), codeBehind);
        }

        private static List<(string Type, string Name)> GetParameters(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            return method.ParameterList.Parameters
                .Select(p => (Type: semanticModel.GetSymbolInfo(p.Type).Symbol.ToDisplayString(), Name: p.Identifier.Text))
                .ToList();
        }

        private void RellenaComboClases_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RellenaComboClases();
        }
    }
}
