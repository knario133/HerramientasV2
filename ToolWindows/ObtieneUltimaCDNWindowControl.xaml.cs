using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;

namespace HerramientasV2
{
    public class Library
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsSelected { get; set; }
        public List<string> Files { get; set; }
        public bool IsDatatable { get; set; }
        public List<Dependency> Dependencies { get; set; }
    }

    public class Dependency
    {
        public string Value { get; set; }
        public List<string> Files { get; set; }
        public bool IsDatatable { get; set; }
    }

    public partial class ObtieneUltimaCDNWindowControl : UserControl
    {
        private readonly List<Library> libraries;

        public ObtieneUltimaCDNWindowControl()
        {
            InitializeComponent();
            libraries = LoadLibrariesFromResource();
            ListaReferencias.ItemsSource = libraries;
        }

        private static List<Library> LoadLibrariesFromResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "HerramientasV2.Resources.libraries.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<List<Library>>(json);
            }
        }

        private async void button1_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedLibraries = libraries.Where(item => item.IsSelected).ToList();

            if (!selectedLibraries.Any())
            {
                await VS.MessageBox.ShowAsync("No se seleccionó ninguna biblioteca.", buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                return;
            }

            var libraryUrls = await GetLatestLibraryUrlsAsync(selectedLibraries);
            await DownloadLibrariesAsync(libraryUrls);

            await VS.MessageBox.ShowAsync("Bibliotecas descargadas e incluidas en el proyecto.", buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
        }

        private static async Task<List<string[]>> GetLatestLibraryUrlsAsync(IEnumerable<Library> selectedLibraries)
        {
            var result = new List<string[]>();
            using (var client = new HttpClient())
            {
                foreach (var library in selectedLibraries)
                {
                    await AddLibraryUrlsAsync(library, result, client);
                    if (library.Dependencies != null)
                    {
                        foreach (var dependency in library.Dependencies)
                        {
                            await AddDependencyUrlsAsync(dependency, library.Name, result, client);
                        }
                    }
                }
            }
            return result.Distinct().ToList();
        }

        private static async Task AddLibraryUrlsAsync(Library library, ICollection<string[]> result, HttpClient client)
        {
            var version = await GetLatestVersionAsync(library.Value, library.IsDatatable, client);
            var folder = ConvertToPascalCase(library.Name);
            foreach (var file in library.Files)
            {
                result.Add(new[] { folder, GetCdnUrl(library.Value, version, file, library.IsDatatable) });
            }
        }

        private static async Task AddDependencyUrlsAsync(Dependency dependency, string libraryName, ICollection<string[]> result, HttpClient client)
        {
            var version = await GetLatestVersionAsync(dependency.Value, dependency.IsDatatable, client);
            var folder = ConvertToPascalCase(libraryName);
            foreach (var file in dependency.Files)
            {
                result.Add(new[] { folder, GetCdnUrl(dependency.Value, version, file, dependency.IsDatatable) });
            }
        }

        private static async Task<string> GetLatestVersionAsync(string packageName, bool isDatatable, HttpClient client)
        {
            var url = isDatatable ? "https://api.datatables.net/versions/feed" : $"https://data.jsdelivr.com/v1/package/npm/{packageName}";
            var response = await client.GetStringAsync(url);
            var json = Newtonsoft.Json.Linq.JObject.Parse(response);

            if (isDatatable)
            {
                var componentName = packageName.Split('-').Last();
                return json[componentName]["release"]["version"].ToString();
            }

            return json["tags"]["latest"].ToString();
        }

        private static string GetCdnUrl(string packageName, string version, string file, bool isDatatable)
        {
            return isDatatable
                ? $"https://cdn.datatables.net/{version}/{file}"
                : $"https://cdn.jsdelivr.net/npm/{packageName}@{version}/{file}";
        }

        private static async Task DownloadLibrariesAsync(IEnumerable<string[]> libraryUrls)
        {
            var activeProject = await VS.Solutions.GetActiveProjectAsync();
            var baseFolderPath = Path.Combine(Path.GetDirectoryName(activeProject.FullPath), "Librerias");
            Directory.CreateDirectory(baseFolderPath);

            using (var client = new HttpClient())
            {
                foreach (var libraryUrl in libraryUrls)
                {
                    var subDirectory = libraryUrl[0];
                    var url = libraryUrl[1];
                    var subDirectoryPath = Path.Combine(baseFolderPath, subDirectory);
                    Directory.CreateDirectory(subDirectoryPath);

                    var fileName = Path.GetFileName(url);
                    var filePath = Path.Combine(subDirectoryPath, fileName);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        File.WriteAllBytes(filePath, fileBytes);
                        await activeProject.AddExistingFilesAsync(filePath);
                    }
                }
            }
        }

        private static string ConvertToPascalCase(string text)
        {
            var cleanText = Regex.Replace(text, @"[^a-zA-Z0-9]+", " ");
            var textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(cleanText.ToLower()).Replace(" ", "");
        }
    }
}
