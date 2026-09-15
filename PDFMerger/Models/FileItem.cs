//FileItem.cs
using PDFMerger.Infrastructure;

namespace PDFMerger.Models
{
    public class FileItem : ObservableObject
    {

        private string? _filePath;
        public string FilePath
        {
            get => _filePath ?? string.Empty;
            set => SetProperty(ref _filePath, value);
        }

        private string? _fileName;
        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => SetProperty(ref _fileName, value);
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        private long _fileSize;
        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }
        public string FileSizeDisplay
        {
            get
            {
                if (FileSize <= 0)
                    return "Unknown";

                string[] units = { "B", "KB", "MB", "GB", "TB" };
                double size = FileSize;
                int unitIndex = 0;

                while (size >= 1024 && unitIndex < units.Length - 1)
                {
                    size /= 1024;
                    unitIndex++;
                }

                return unitIndex == 0
                    ? $"{size:F0} {units[unitIndex]}"
                    : $"{size:0.##} {units[unitIndex]}";
            }
        }
        private string? _author;
        public string Author
        {
            get => _author ?? string.Empty;
            set => SetProperty(ref _author, value);
        }

        private FileType _type = FileType.Pdf;
        public FileType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public bool IsEncrypted { get; set; } = false;
        public string IsEncryptedDisplay => IsEncrypted ? "✅" : "";
        public string ? Password { get; set; } = null;
        public bool IsImage => Type == FileType.Image;
       

    }
  
}
