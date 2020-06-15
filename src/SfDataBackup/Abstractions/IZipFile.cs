namespace SfDataBackup.Abstractions
{
    public interface IZipFile
    {
        void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles);

        void CreateFromDirectory (string sourceDirectoryName, string destinationArchiveFileName);
    }
}