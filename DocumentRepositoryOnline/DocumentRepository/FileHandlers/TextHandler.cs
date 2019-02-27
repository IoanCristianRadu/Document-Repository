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
        public List<string> content = new List<string>();
        public DateTime last_modified;
        public DateTime date_created;
        public long fileSize = 0;
        public string extension;
        public string author;
        public string title;
        public int pages;
        public string path = "";
        public string name;

        public TextHandler(FileInfo f)
        {
            this.last_modified = f.LastWriteTime;
            this.date_created = f.CreationTime;
            this.fileSize = f.Length / 1024; //bytes to kb
            this.author = System.IO.File.GetAccessControl(f.FullName).GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
            this.extension = f.Extension;
            this.title = f.Name;
            this.pages = 1;
            this.path = f.FullName;
            this.name = f.Name;
            if (this.fileSize == 0)
            {
                this.fileSize = 1;
            }
        }

        public virtual void extractContent()
        {
            int splitLength = 3000;
            if (extension == ".txt" || extension == ".html")
            {
                string fileContent = System.IO.File.ReadAllText(path);
                for (int index = 0; index < fileContent.Length; index = index + splitLength)
                {
                    if (fileContent.Length - index > splitLength)
                    {
                        content.Add(fileContent.Substring(index, splitLength));
                    }
                    else
                    {
                        content.Add(fileContent.Substring(index));
                    }
                }
            }
        }

        public void writeToDB(OracleCommand cmd,int? folderId)
        {
            cmd.CommandText = "Insert into file_details VALUES (default, TO_DATE('"+ this.last_modified + "' , 'mm/dd/yyyy HH:MI:SS AM') , TO_DATE('" + this.date_created + "' , 'mm/dd/yyyy HH:MI:SS AM' ) , " + this.fileSize + ",'" + this.extension + "','" + this.author + "','" + this.title + "'," + this.pages + "," + (folderId.GetValueOrDefault() == 0 ? "null" : folderId.ToString()) + ")";
            int rowsUpdated = cmd.ExecuteNonQuery();

            int file_details_id;
            file_details_id = DBSingleton.getCurrentSeqValue("file_details_id.currval");

            int i = 1;
            foreach (String s in this.content)
            {
                String page = s.Unidecode();
                page = page.Replace("'", "''");
                cmd.CommandText = "Insert into content_data values(default,'" + page + "'," + i + "," + (folderId.GetValueOrDefault() == 0 ? "null" : folderId.ToString()) + "," + file_details_id + ")";
                cmd.ExecuteNonQuery();
                i++;
            }
        }
    }
}
