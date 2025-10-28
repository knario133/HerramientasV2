using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace HerramientasV2.Services
{
    internal static class SolutionExplorerService
    {
        public static async Task<Project?> GetActiveProjectAsync(CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            ThreadHelper.ThrowIfNotOnUIThread();
            return await VS.Solutions.GetActiveProjectAsync();
        }

        public static async Task<IReadOnlyList<SolutionItem>> GetProjectItemsAsync(SolutionItem root, CancellationToken cancellationToken = default)
        {
            if (root is null)
            {
                return Array.Empty<SolutionItem>();
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            ThreadHelper.ThrowIfNotOnUIThread();

            var items = new List<SolutionItem>();
            Traverse(root, items);
            return items;
        }

        public static async Task<IReadOnlyList<SolutionItem>> GetActiveProjectItemsAsync(CancellationToken cancellationToken = default)
        {
            var project = await GetActiveProjectAsync(cancellationToken);
            if (project is null)
            {
                return Array.Empty<SolutionItem>();
            }

            return await GetProjectItemsAsync(project, cancellationToken);
        }

        private static void Traverse(SolutionItem parent, ICollection<SolutionItem> items)
        {
            foreach (var child in parent.Children)
            {
                items.Add(child);

                if (child.Children.Any())
                {
                    Traverse(child, items);
                }
            }
        }
    }
}
