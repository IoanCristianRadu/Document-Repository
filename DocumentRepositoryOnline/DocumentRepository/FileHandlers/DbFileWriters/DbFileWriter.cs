using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using UnidecodeSharpFork;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class DbFileWriter : IDbFileWriter
    {
        public OracleCommand Cmd { get; }
        public int? FolderId { get; }

        public DbFileWriter(OracleCommand cmd, int? folderId)
        {
            this.Cmd = cmd;
            this.FolderId = folderId;
        }

        public void WriteToDb(FileDetails fileData)
        {
            Cmd.CommandText = "Insert into file_details VALUES (default, TO_DATE('" + fileData.LastModified +
                              "' , 'mm/dd/yyyy HH:MI:SS AM') , TO_DATE('" + fileData.DateCreated +
                              "' , 'mm/dd/yyyy HH:MI:SS AM' ) , " + fileData.FileSize + ",'" + fileData.Extension + "','" +
                              fileData.Author + "','" + fileData.Title + "'," + fileData.Pages + "," +
                              (FolderId.GetValueOrDefault() == 0 ? "null" : FolderId.ToString()) + ")";
            int rowsUpdated = Cmd.ExecuteNonQuery();

            var fileDetailsId = DbSingleton.GetCurrentSeqValue("file_details_id.currval");

            int i = 1;
            foreach (String s in fileData.Content)
            {
                String page = s.Unidecode();
                page = page.Replace("'", "''");
                Cmd.CommandText = "Insert into content_data values(default,'" + page + "'," + i + "," +
                                  (FolderId.GetValueOrDefault() == 0 ? "null" : FolderId.ToString()) + "," +
                                  fileDetailsId + ")";
                Cmd.ExecuteNonQuery();
                i++;
            }
        }
    }
}