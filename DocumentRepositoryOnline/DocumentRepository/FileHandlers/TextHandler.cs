using System.IO;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers.DbFileWriters;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class TextHandler : IFileHandler
    {
        public FileDetails FileData;

        public TextHandler(FileInfo f)
        {
            FileData = new FileDetails(
                f.LastWriteTime,
                f.CreationTime,
                f.Extension,
                System.IO.File.GetAccessControl(f.FullName)
                    .GetOwner(typeof(System.Security.Principal.NTAccount))
                    .ToString(),
                f.Name,
                1,
                f.FullName,
                f.Name,
                f.Length / 1024);

            if (FileData.FileSize == 0)
            {
                FileData.FileSize = 1;
            }
        }

        public void ExtractContent()
        {
            int splitLength = 3000;
            if (FileData.Extension != ".txt" && FileData.Extension != ".html") return;
            string fileContent = System.IO.File.ReadAllText(FileData.Path);
            for (int index = 0; index < fileContent.Length; index = index + splitLength)
            {
                if (fileContent.Length - index > splitLength)
                {
                    FileData.Content.Add(fileContent.Substring(index, splitLength));
                }
                else
                {
                    FileData.Content.Add(fileContent.Substring(index));
                }
            }
        }

        public void WriteToDb(IDbFileWriter dbFileWriter)
        {
            dbFileWriter.WriteToDb(FileData);
        }

        public FileDetails GetFileDetails()
        {
            return FileData;
        }
    }
}