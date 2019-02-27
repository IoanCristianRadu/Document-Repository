﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.ComponentModel;
using Oracle.ManagedDataAccess.Client;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers;

namespace DocumentRepositoryOnline.DocumentRepository
{
    class FilesTraverse
    {
        private Dictionary<FileInfo, int> listaFisiere = new Dictionary<FileInfo, int>();
        String path;

        public String Path
        {
            get { return path; }
            set { path = value; }
        }

        public Dictionary<FileInfo, int> ListaFisiere
        {
            get
            {
                return listaFisiere;
            }
        }

        public TextHandler createHandlerType(FileInfo file)
        {
            TextHandler textHandler = null;
            if (file.Extension == ".txt" || file.Extension == ".html")
            {
                textHandler = new TextHandler(file);
            }
            else if (file.Extension == ".pdf")
            {
                textHandler = new PdfHandler(file);
            }
            else if (file.Extension == ".docx" || file.Extension == ".xlsx" || file.Extension == ".pptx")
            {
                textHandler = new OfficeHandler(file);
            }
            return textHandler;
        }

        public bool doUnitOfWork()
        {
            if (ListaFisiere.Count > 0)
            {
                Write(createHandlerType(ListaFisiere.First().Key), ListaFisiere.First().Value);
                ListaFisiere.Remove(ListaFisiere.First().Key);
                return true;
            }
            return false;
        }

        public static void Write(TextHandler textHandler, int? folderId)
        {
            try
            {
                OracleConnection Conn = DBSingleton.Conn;
                if (textHandler != null)
                {
                    textHandler.extractContent();
                    OracleCommand cmd = new OracleCommand();
                    cmd.Connection = Conn;
                    textHandler.writeToDB(cmd, folderId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public FilesTraverse(String path, int option) // 1 - files in folder, files in subfolders, 0 - only files in current folder
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            string extensionList = DBSingleton.getFiletypes();

            int folderId = DBSingleton.writeFolder(path, null, 1);
            foreach (var file in directory.GetFiles())
            {
                if (extensionList.Contains(file.Extension))
                {
                    ListaFisiere.Add(file, folderId);
                }
            }

            if (option == 1)
            {
                foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
                {
                    folderId = DBSingleton.writeFolder(dir.Name, 1, 1);
                    foreach (var file in dir.GetFiles())
                    {
                        if (extensionList.Contains(file.Extension))
                        {
                            ListaFisiere.Add(file, folderId);
                        }
                    }
                }
            }
            DBSingleton.CloseConnection();
        }

        public FilesTraverse(String[] list)
        {
            string extensionList = DBSingleton.getFiletypes();
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
            int folderId = DBSingleton.writeFolder(path, null, 0);

            foreach (String file in list)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Exists && extensionList.Contains(fileInfo.Extension))
                {
                    listaFisiere.Add(fileInfo, folderId);
                }
            }
        }
    }
}


