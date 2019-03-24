using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Oracle.ManagedDataAccess.Client;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers.DbFileWriters;

namespace DocumentRepositoryOnline.DocumentRepository
{
    class FilesTraverse
    {
        private Dictionary<FileInfo, int> _filesQueue = new Dictionary<FileInfo, int>();
        private String _path;

        public String Path
        {
            get => _path;
            set => _path = value;
        }

        public Dictionary<FileInfo, int> FilesQueue
        {
            get { return _filesQueue; }
        }

        public IFileHandler CreateHandlerType(FileInfo file)
        {
            IFileHandler fileHandler = null;
            if (file.Extension == ".txt" || file.Extension == ".html")
            {
                fileHandler = new TextHandler(file);
            }
            else if (file.Extension == ".pdf")
            {
                fileHandler = new PdfHandler(file);
            }
            else if (file.Extension == ".docx" || file.Extension == ".xlsx" || file.Extension == ".pptx")
            {
                fileHandler = new OfficeHandler(file);
            }

            return fileHandler;
        }

        public bool DoUnitOfWork()
        {
            if (FilesQueue.Count <= 0) return false;
            Write(CreateHandlerType(FilesQueue.First().Key), FilesQueue.First().Value);
            FilesQueue.Remove(FilesQueue.First().Key);
            return true;
        }

        public static void Write(IFileHandler fileHandler, int? folderId)
        {
            try
            {
                OracleConnection conn = DbSingleton.Conn;
                if (fileHandler != null)
                {
                    fileHandler.ExtractContent();
                    OracleCommand cmd = new OracleCommand {Connection = conn};
                    fileHandler.WriteToDb(new DbFileWriter(cmd, folderId));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public
            FilesTraverse(String path,
                int option) // 1 - files in folder, files in subfolders, 0 - only files in current folder
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            string extensionList = DbSingleton.GetFileTypes();

            int folderId = DbSingleton.WriteFolder(path, null, 1);
            foreach (var file in directory.GetFiles())
            {
                if (extensionList.Contains(file.Extension))
                {
                    FilesQueue.Add(file, folderId);
                }
            }

            if (option == 1)
            {
                foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
                {
                    folderId = DbSingleton.WriteFolder(dir.Name, 1, 1);
                    foreach (var file in dir.GetFiles())
                    {
                        if (extensionList.Contains(file.Extension))
                        {
                            FilesQueue.Add(file, folderId);
                        }
                    }
                }
            }

            DbSingleton.CloseConnection();
        }

        public FilesTraverse(String[] list)
        {
            string extensionList = DbSingleton.GetFileTypes();
            FileInfo fi = new FileInfo(list[0]);
            String fullPath = fi.FullName;
            String path = "";

            for (int i = fullPath.Length - 1; i > 0; i--)
            {
                if (fullPath[i] == '\\')
                {
                    path = fullPath.Substring(0, i);
                    break;
                }
            }

            var folderId = DbSingleton.WriteFolder(path, null, 0);

            foreach (String file in list)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Exists && extensionList.Contains(fileInfo.Extension))
                {
                    _filesQueue.Add(fileInfo, folderId);
                }
            }
        }
    }
}