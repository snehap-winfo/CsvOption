namespace CsvOption.Models
{
    public class FileUpload
    {
        public string UploadFile { get; set; } = "Uploads";
    }

    public class CsvRow
    {
        public Dictionary<string, string> Fields { get; set; } = new();
    }
}
