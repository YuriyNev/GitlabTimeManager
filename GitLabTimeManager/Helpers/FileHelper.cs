using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Catel.IoC;
using Catel.Services;

namespace GitLabTimeManager.Helpers;

public static class FileHelper
{
    public static async Task<string> SaveDialog(string defaultName, string extension)
    {
        var dependencyResolver = IoCConfiguration.DefaultDependencyResolver.GetDependencyResolver();
        var saveFileService = dependencyResolver.Resolve<ISaveFileService>();

        var context = new DetermineSaveFileContext
        {
            Filter = $"Excel Document |*.{extension}",
            FileName = $"{defaultName}.{extension}",
            ValidateNames = true,
            AddExtension = true,
        };

        var result = await saveFileService.DetermineFileAsync(context).ConfigureAwait(false);
        return result.Result ? result.FileName : null;
    }

    public static async Task<string> OpenDialog(string defaultName, string extension)
    {
        var dependencyResolver = IoCConfiguration.DefaultDependencyResolver.GetDependencyResolver();
        var openFileService = dependencyResolver.Resolve<IOpenFileService>();

        var context = new DetermineOpenFileContext
        {
            Filter = $"Excel Document |*.{extension}",
            FileName = $"{defaultName}.{extension}"
        };

        var result = await openFileService.DetermineFileAsync(context).ConfigureAwait(false);
        return result.Result ? result.FileName : null;
    }

    public static void OpenFileDirectory(string filePath)
    {
        try
        {
            var argument = $"/select, \"{filePath}\"";
            Process.Start("explorer.exe", argument);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}