using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Globalization;
using System.Security.Cryptography;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Xml.Linq;
using Microsoft.IO;

using EnvDTE80;

namespace HerramientasV2
{
    public partial class ObtieneUltimaCDNWindowControl : UserControl
    {
        public class MyItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsSelected { get; set; }
        }

        public ObtieneUltimaCDNWindowControl()
        {
            InitializeComponent();
            List<MyItem> items = new List<MyItem>
        {
                 new MyItem { Name = "chart.js -", Value = "chart.js" },
            new MyItem { Name = "FontAwesome-Free -", Value = "@fortawesome/fontawesome-free" },
            new MyItem { Name = "Normalize.css -", Value = "normalize.css" },
            new MyItem { Name = "Toastr.js -", Value = "toastr" },
            new MyItem { Name = "JS jQuery -", Value = "jquery" },
         //   new MyItem { Name = "CSS Bootstrap -", Value = "bootstrap" },
            new MyItem { Name = "JS Bootstrap Bundle -", Value = "bootstrap" },
         //   new MyItem { Name = "JS Popper.js -", Value = "@popperjs/core" },
         //   new MyItem { Name = "JS Bootstrap -", Value = "bootstrap" },
            new MyItem { Name = "Boostrap -", Value = "bootstrap" },
            new MyItem { Name = "JS SweetAlert2 -", Value = "sweetalert2" },
            new MyItem { Name = "JS jQuery UI -", Value = "jquery-ui" },
            new MyItem { Name = "JS DataTables -", Value = "datatables.net" },
            new MyItem { Name = "JS DataTables Buttons -", Value = "datatables.net-buttons" },
            new MyItem { Name = "JS DataTables Select -", Value = "datatables.net-select" }
        };
            ListaReferencias.ItemsSource = items;
        }

        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            // Obtén las bibliotecas seleccionadas
            var selectedLibraries = ListaReferencias.Items
    .Cast<MyItem>()
    .Where(item => item.IsSelected)
    .ToList();

            if (selectedLibraries.Count == 0)
            {
                VS.MessageBox.Show("CdnWindowControl", "No se seleccionó ninguna biblioteca");
                return;
            }

            // Obtén las últimas versiones y URLs de las bibliotecas seleccionadas
            List <string[]> libraryUrls = await GetLatestLibraryUrls(selectedLibraries);

            // Descarga las bibliotecas y crea una carpeta "Librerias" en el proyecto activo
            await DownloadLibrariesAndCreateFolder(libraryUrls);

            VS.MessageBox.Show("CdnWindowControl", "Bibliotecas descargadas e incluidas en el proyecto");

        }
        protected async Task DownloadLibrariesAndCreateFolder(List<string[]> libraryUrls)
        {
            // Obtener la carpeta "Librerias" en el proyecto activo
            Community.VisualStudio.Toolkit.Project activeProject = await VS.Solutions.GetActiveProjectAsync();
            string baseFolderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(activeProject.FullPath) + "\\", "Librerias");

            if (!System.IO.Directory.Exists(baseFolderPath))
            {
                System.IO.Directory.CreateDirectory(baseFolderPath);
            }

            // Descargar cada archivo de la lista
            using (HttpClient client = new HttpClient())
            {
                foreach (string[] libraryUrl in libraryUrls)
                {
                    string subDirectory = libraryUrl[0];
                    string url = libraryUrl[1];

                    // Crear la ruta del subdirectorio
                    string subDirectoryPath = System.IO.Path.Combine(baseFolderPath, subDirectory);

                    // Crear el subdirectorio si no existe
                    if (!System.IO.Directory.Exists(subDirectoryPath))
                    {
                        System.IO.Directory.CreateDirectory(subDirectoryPath);
                    }

                    // Obtener el nombre del archivo desde la URL
                    string fileName = System.IO.Path.GetFileName(url);
                    string filePath = System.IO.Path.Combine(subDirectoryPath, fileName);

                    // Verificar si el archivo ya existe y eliminarlo si es necesario
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Descargar el archivo y guardarlo en el subdirectorio correspondiente
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                        System.IO.File.WriteAllBytes(filePath, fileBytes);

                        // Agregar el archivo descargado al proyecto activo
                        var addedFiles = await activeProject.AddExistingFilesAsync(filePath);

                        // Obtener la instancia de DTE para acceder a las propiedades del archivo
                        DTE2 dte = await VS.GetServiceAsync<DTE, DTE2>();

                        foreach (var file in addedFiles)
                        {
                            // Buscar el archivo en el proyecto
                            ProjectItem? projectItem = dte.Solution.FindProjectItem(file.FullPath);

                            if (projectItem != null)
                            {
                                // Establecer la acción de compilación como "Contenido"
                                projectItem.Properties.Item("BuildAction").Value = 2; // 2 = Contenido

                                // Establecer "Copiar en el directorio de salida" como "Copiar siempre"
                                projectItem.Properties.Item("CopyToOutputDirectory").Value = 1; // 1 = Copiar siempre
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error downloading {url}: {response.StatusCode}");
                    }
                }
            }
        }

        //protected async Task DownloadLibrariesAndCreateFolder(List<string[]> libraryUrls)
        //{
        //    // Obtener la carpeta "Librerias" en el proyecto activo
        //    Community.VisualStudio.Toolkit.Project activeProject = await VS.Solutions.GetActiveProjectAsync();
        //    string folderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(activeProject.FullPath) + "\\", "Librerias");

        //    if (!System.IO.Directory.Exists(folderPath))
        //    {
        //        System.IO.Directory.CreateDirectory(folderPath);
        //    }

        //    // Descargar cada archivo de la lista
        //    using (HttpClient client = new HttpClient())
        //    {
        //        foreach (string[] libraryUrl in libraryUrls)
        //        {
        //            string fileName = System.IO.Path.GetFileName(libraryUrl);
        //            string filePath = System.IO.Path.Combine(folderPath, fileName);

        //            // Verificar si el archivo ya existe y eliminarlo si es necesario
        //            if (System.IO.File.Exists(filePath))
        //            {
        //                System.IO.File.Delete(filePath);
        //            }

        //            // Descargar el archivo y guardarlo en la carpeta "Librerias"
        //            HttpResponseMessage response = await client.GetAsync(libraryUrl);
        //            if (response.IsSuccessStatusCode)
        //            {
        //                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
        //                System.IO.File.WriteAllBytes(filePath, fileBytes);
        //                await activeProject.AddExistingFilesAsync(filePath);
        //                //   SolutionItem?[] ArchivosProyectoActual = activeProject.Result.Children.ToArray();

        //                // EnvDTE.ProjectItems projectItems = activeProject.ProjectItems.Item("Librerias").ProjectItems;
        //            }
        //            else
        //            {
        //                Console.WriteLine($"Error downloading {libraryUrl}: {response.StatusCode}");
        //            }
        //        }
        //    }

        //}
        public static async Task<string> ObtieneCodigoUltimaVersion(string value)
        {
            if (value.Contains("datatables"))
            {
                string baseUrl = "https://api.datatables.net/versions/feed";  // Cambiado a la API de versiones de DataTables
                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(baseUrl);
                string content = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(content);
                string latestVersion;
                if (value.Contains("buttons"))
                {
                    latestVersion = json["Buttons"]["release"]["version"].ToString();
                }
                else
                {
                    if (value.Contains("select"))
                    {
                        latestVersion = json["Select"]["release"]["version"].ToString();
                    }
                    else
                    {
                        latestVersion = json["DataTables"]["release"]["version"].ToString();  // Ruta modificada para acceder a la versión de lanzamiento
                    }
                }
                return latestVersion;
            }
            else
            {
                List<string> libraries = new List<string> { "jszip", "pdfmake", "vfs_fonts" };
                if (libraries.Any(lib => value.Contains(lib)))
                {
                    string baseUrl = "https://api.cdnjs.com/libraries/"+value;
                    using HttpClient client = new HttpClient();
                    HttpResponseMessage response = await client.GetAsync(baseUrl);
                    string content = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(content);
                    string latestVersion = json["version"].ToString();
                    return latestVersion;
                }
                else
                {
                    string baseUrl = "https://data.jsdelivr.com/v1/package/npm/";
                    using HttpClient client = new HttpClient();
                    HttpResponseMessage response = await client.GetAsync(baseUrl + value);
                    string content = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(content);
                    string latestVersion = json["tags"]["latest"].ToString();
                    return latestVersion;
                }
            }
        }
        static string ConvertirAPascalCase(string texto)
        {
            // Reemplazar todo lo que no sea letra o número por espacios
            string textoLimpio = Regex.Replace(texto, @"[^a-zA-Z0-9]+", " ");

            // Convertir a título (PascalCase)
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            string resultado = textInfo.ToTitleCase(textoLimpio.ToLower()).Replace(" ", "");

            return resultado;
        }
        public static async Task<List<string[]>> GetLatestLibraryUrls(List<MyItem> selectedLibraries)
        {
            List <string[]> Resultado = new List<string[]>();
            string baseUrl = "https://data.jsdelivr.com/v1/package/npm/";
            using HttpClient client = new HttpClient();
            foreach (MyItem library in selectedLibraries)
            {
                string libraryUrl;
                HttpResponseMessage response = await client.GetAsync(baseUrl + library.Value);
                if (response.IsSuccessStatusCode)
                {
                    string Carpeta = "";
                    string content = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(content);
                    string latestVersion = json["tags"]["latest"].ToString();
                    switch (library.Name)
                    {
                        case "chart.js -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/chart.umd.min.js";
                            break;
                        case "FontAwesome-Free -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/js/all.min.js";
                            Resultado.Add(new string[] { Carpeta,libraryUrl});
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/css/fontawesome.min.css";
                            break;
                        case "Toastr.js -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/toastr.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/toastr.min.css";
                            break;
                        case "Normalize.css -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/normalize.min.css";
                            break;

                        case "JS SweetAlert2 -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/sweetalert2.all.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/sweetalert2.min.css";
                            break;
                        case "JS jQuery -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/jquery.min.js";
                            break;
                        //case "CSS Bootstrap -":
                        //    libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/css/{library.Value}.min.css";
                        //    break;
                        case "JS Bootstrap Bundle -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"bootstrap"}@{await ObtieneCodigoUltimaVersion("bootstrap")}/dist/css/{library.Value}.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/js/{library.Value}.bundle.min.js";
                            break;
                        //case "JS Popper.js -":
                        //    libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/umd/popper.min.js";
                        //    break;
                        //case "JS Bootstrap -":
                        //    libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/js/{library.Value}.min.js";
                        //    break;
                        case "Boostrap -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"bootstrap"}@{await ObtieneCodigoUltimaVersion("bootstrap")}/dist/css/{library.Value}.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"@popperjs/core"}@{await ObtieneCodigoUltimaVersion("@popperjs/core")}/dist/umd/popper.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"bootstrap"}@{await ObtieneCodigoUltimaVersion("bootstrap")}/dist/js/{library.Value}.min.js";
                            break;
                        case "JS jQuery UI -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            //libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/jquery-ui.min.js";
                            //break;
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"jquery"}@{await ObtieneCodigoUltimaVersion("jquery")}/dist/jquery.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/jquery-ui.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/themes/base/theme.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            break;
                        case "JS DataTables -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"jquery"}@{await ObtieneCodigoUltimaVersion("jquery")}/dist/jquery.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/{await ObtieneCodigoUltimaVersion("datatables.net")}/js/jquery.dataTables.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/{await ObtieneCodigoUltimaVersion("datatables.net")}/css/jquery.dataTables.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            break;
                        case "JS DataTables Buttons -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"jquery"}@{await ObtieneCodigoUltimaVersion("jquery")}/dist/jquery.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/{await ObtieneCodigoUltimaVersion("datatables.net")}/js/jquery.dataTables.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/{await ObtieneCodigoUltimaVersion("datatables.net")}/css/jquery.dataTables.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/buttons/{await ObtieneCodigoUltimaVersion("datatables.net-buttons")}/js/dataTables.buttons.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/buttons/{await ObtieneCodigoUltimaVersion("datatables.net-buttons")}/js/dataTables.bootstrap5.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/buttons/{await ObtieneCodigoUltimaVersion("datatables.net-buttons")}/css/buttons.dataTables.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/buttons/{await ObtieneCodigoUltimaVersion("datatables.net-buttons")}/js/buttons.html5.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdnjs.cloudflare.com/ajax/libs/{"jszip"}/{await ObtieneCodigoUltimaVersion("jszip")}/jszip.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdnjs.cloudflare.com/ajax/libs/{"pdfmake"}/{await ObtieneCodigoUltimaVersion("pdfmake")}/pdfmake.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdnjs.cloudflare.com/ajax/libs/{"pdfmake"}/{await ObtieneCodigoUltimaVersion("pdfmake")}/vfs_fonts.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });

                            break;
                        case "JS DataTables Select -":
                            Carpeta = ConvertirAPascalCase(library.Name);
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{"jquery"}@{await ObtieneCodigoUltimaVersion("jquery")}/dist/jquery.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/{await ObtieneCodigoUltimaVersion("datatables.net")}/js/jquery.dataTables.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/{await ObtieneCodigoUltimaVersion("datatables.net")}/css/jquery.dataTables.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/select/{await ObtieneCodigoUltimaVersion("datatables.net-select")}/js/dataTables.select.min.js";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            libraryUrl = $"https://cdn.datatables.net/select/{await ObtieneCodigoUltimaVersion("datatables.net-select")}/css/select.dataTables.min.css";
                            Resultado.Add(new string[] { Carpeta, libraryUrl });
                            break;
                        default:
                            libraryUrl = $"https://cdn.jsdelivr.net/npm/{library.Value}@{latestVersion}/dist/{library.Value}.min.js";
                            break;
                    }

                    Resultado.Add(new string[] { Carpeta, libraryUrl });
                }
                else
                {
                    Console.WriteLine($"Error fetching {library.Name}: {response.StatusCode}");
                }
            }
            Resultado = Resultado.Distinct().ToList();
            return Resultado;
        }
    }
}
