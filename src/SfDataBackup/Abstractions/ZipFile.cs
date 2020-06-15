namespace SfDataBackup.Abstractions
{
    public class ZipFile : IZipFile
    {
        public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
        }

        public void CreateFromDirectory (string sourceDirectoryName, string destinationArchiveFileName)
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
        }
    }
}