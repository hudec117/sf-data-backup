namespace SfDataBackup
{
    public class SfResult
    {
        public bool Success { get; set; }

        public SfResult(bool success)
        {
            Success = success;
        }
    }
}