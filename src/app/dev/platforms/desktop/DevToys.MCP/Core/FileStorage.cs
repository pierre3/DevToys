using DevToys.Api;
using DevToys.Core;

namespace DevToys.MCP.Core;

[Export(typeof(IFileStorage))]
internal sealed class FileStorage : IFileStorage
{
    [ImportingConstructor]
    internal FileStorage()
    {
        AppCacheDirectory = Constants.AppCacheDirectory;
    }

    public string AppCacheDirectory { get; }

    public bool FileExists(string relativeOrAbsoluteFilePath)
    {
        if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
        {
            relativeOrAbsoluteFilePath = Path.Combine(AppCacheDirectory, relativeOrAbsoluteFilePath);
        }

        return File.Exists(relativeOrAbsoluteFilePath);
    }

    public FileStream OpenReadFile(string relativeOrAbsoluteFilePath)
    {
        if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
        {
            relativeOrAbsoluteFilePath = Path.Combine(AppCacheDirectory, relativeOrAbsoluteFilePath);
        }

        if (!File.Exists(relativeOrAbsoluteFilePath))
        {
            throw new FileNotFoundException("Unable to find the indicated file.", relativeOrAbsoluteFilePath);
        }

        return new FileStream(relativeOrAbsoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, SandboxedFileReader.BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public FileStream OpenWriteFile(string relativeOrAbsoluteFilePath, bool replaceIfExist)
    {
        if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
        {
            relativeOrAbsoluteFilePath = Path.Combine(AppCacheDirectory, relativeOrAbsoluteFilePath);
        }

        if (File.Exists(relativeOrAbsoluteFilePath) && replaceIfExist)
        {
            File.Delete(relativeOrAbsoluteFilePath);
        }

        string parentDirectory = Path.GetDirectoryName(relativeOrAbsoluteFilePath)!;
        if (!Directory.Exists(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        return File.OpenWrite(relativeOrAbsoluteFilePath);
    }

    public FileInfo CreateSelfDestroyingTempFile(string? desiredFileExtension = null)
    {
        return FileHelper.CreateTempFile(Constants.AppTempFolder, desiredFileExtension);
    }

    // Interactive file/folder pickers are not supported over MCP: the process's standard input and output
    // are reserved for the MCP protocol channel, so prompting the user via the console is impossible.
    // Tools that need file access must receive an explicit path through their options instead.

    public ValueTask<FileStream?> PickSaveFileAsync(params string[] fileTypes)
    {
        return new ValueTask<FileStream?>((FileStream?)null);
    }

    public ValueTask<SandboxedFileReader?> PickOpenFileAsync(params string[] fileTypes)
    {
        return new ValueTask<SandboxedFileReader?>((SandboxedFileReader?)null);
    }

    public ValueTask<SandboxedFileReader[]> PickOpenFilesAsync(params string[] fileTypes)
    {
        return new ValueTask<SandboxedFileReader[]>(Array.Empty<SandboxedFileReader>());
    }

    public ValueTask<string?> PickFolderAsync()
    {
        return new ValueTask<string?>((string?)null);
    }
}
