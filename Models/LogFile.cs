namespace ABCRetail.Models
{
    public class LogFile
    {
        public string FileName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}