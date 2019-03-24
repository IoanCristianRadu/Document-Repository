using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnidecodeSharpFork;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class TextHandler
    {
        public List<string> Content = new List<string>();
        public DateTime LastModified;
        public DateTime DateCreated;
        public long FileSize = 0;
        public string Extension;
        public string Author;
        public string Title;
        public int Pages;
        public string Path;
        public string Name;

        public TextHandler(FileInfo f)
        {
            this.LastModified = f.LastWriteTime;
            this.DateCreated = f.CreationTime;
            this.FileSize = f.Length / 1024; //bytes to kb
            this.Author = System.IO.File.GetAccessControl(f.FullName)
                .GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
            this.Extension = f.Extension;
            this.Title = f.Name;
            this.Pages = 1;
            this.Path = f.FullName;
            this.Name = f.Name;
            if (this.FileSize == 0)
            {
                this.FileSize = 1;
            }
        }

        public virtual void ExtractContent()
        {
            int splitLength = 3000;
            if (Extension == ".txt" || Extension == ".html")
            {
                string fileContent = System.IO.File.ReadAllText(Path);
                for (int index = 0; index < fileContent.Length; index = index + splitLength)
                {
                    if (fileContent.Length - index > splitLength)
                    {
                        Content.Add(fileContent.Substring(index, splitLength));
                    }
                    else
                    {
                        Content.Add(fileContent.Substring(index));
                    }
                }
            }
        }

        public void WriteToDb(OracleCommand cmd, int? folderId)
        {
            cmd.CommandText = "Insert into file_details VALUES (default, TO_DATE('" + this.LastModified +
                              "' , 'mm/dd/yyyy HH:MI:SS AM') , TO_DATE('" + this.DateCreated +
                              "' , 'mm/dd/yyyy HH:MI:SS AM' ) , " + this.FileSize + ",'" + this.Extension + "','" +
                              this.Author + "','" + this.Title + "'," + this.Pages + "," +
                              (folderId.GetValueOrDefault() == 0 ? "null" : folderId.ToString()) + ")";
            int rowsUpdated = cmd.ExecuteNonQuery();

            var fileDetailsId = DBSingleton.GetCurrentSeqValue("file_details_id.currval");

            int i = 1;
            foreach (String s in this.Content)
            {
                String page = s.Unidecode();
                page = page.Replace("'", "''");
                cmd.CommandText = "Insert into content_data values(default,'" + page + "'," + i + "," +
                                  (folderId.GetValueOrDefault() == 0 ? "null" : folderId.ToString()) + "," +
                                  fileDetailsId + ")";
                cmd.ExecuteNonQuery();
                i++;
            }
        }
    }
}