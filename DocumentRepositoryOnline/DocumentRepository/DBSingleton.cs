using System;
using System.Collections.Generic;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.IO;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers;
using DocumentRepositoryOnline.Models;
using FileDetails = DocumentRepositoryOnline.DocumentRepository.FileHandlers.FileDetails;

namespace DocumentRepositoryOnline.DocumentRepository
{
    public class DbSingleton
    {
        private static readonly OracleConnection conn = new OracleConnection(
            "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522))(CONNECT_DATA=(SERVICE_NAME=orcl))); Password=DocRep;User ID = C##DocRep");
        //"Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = orclu))); Password=DocRep;User ID = C##DocRep");
        //DATA SOURCE=DocRep;USER ID=C##DOCREP

        private static int _lastNullFolder;

        public static OracleConnection Conn
        {
            get
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                return conn;
            }
        }

        public static void CloseConnection()
        {
            conn.Close();
        }

        public static List<String> GetDbFolderPaths()
        {
            try
            {
                OracleCommand cmd = new OracleCommand
                {
                    CommandType = CommandType.Text,
                    CommandText = "Select local_path, id from folders where parent_id IS NULL and fullScan = 1",
                    Connection = Conn
                };

                OracleDataReader dr = cmd.ExecuteReader();
                List<string> pathList = new List<string>();
                while (dr.Read())
                {
                    pathList.Add(dr.GetOracleString(0).ToString());
                }

                dr.Close();

                cmd.CommandText =
                    "Select f.local_path ||'\\'|| fd.title from folders f,file_details fd where f.id = fd.folder_id AND f.fullScan = 0";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    pathList.Add(dr.GetOracleString(0).ToString());
                }

                dr.Close();

                Conn.Close();
                return pathList;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<string>();
        }

        public static int WriteFolder(String path, int? parentId, int fullScan)
        {
            try
            {
                OracleCommand cmd;
                if (parentId.HasValue)
                {
                    cmd = new OracleCommand();
                    parentId = GetCurrentSeqValue("folders_id.currval");
                }

                cmd = new OracleCommand
                {
                    Connection = Conn,
                    CommandType = CommandType.Text,
                    CommandText = "Insert into folders (LOCAL_PATH, PARENT_ID,FULLSCAN) VALUES ('" + path + "', " +
                                  (parentId.GetValueOrDefault() == 0 ? "null" : parentId.ToString()) + "," + fullScan +
                                  ")"
                };

                int rowsUpdated = cmd.ExecuteNonQuery();


                int primaryKey = GetCurrentSeqValue("folders_id.currval");

                if (parentId.GetValueOrDefault() == 0)
                {
                    _lastNullFolder = primaryKey;
                }

                return primaryKey;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return -1;
        }

        public static int WriteFolderWeb(String path, int? parentId, int fullScan)
        {
            try
            {
                var cmd = new OracleCommand
                {
                    Connection = Conn,
                    CommandType = CommandType.Text,
                    CommandText = "Insert into folders (LOCAL_PATH, PARENT_ID,FULLSCAN) VALUES ('" + path + "', " +
                                  (parentId.GetValueOrDefault() == 0 ? "null" : parentId.ToString()) + "," + fullScan +
                                  ")"
                };

                int rowsUpdated = cmd.ExecuteNonQuery();

                return rowsUpdated;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return -1;
        }

        public static String GetFileTypes()
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "Select filetype FROM app_settings";
                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                dr.Read();
                String fileTypes = dr.GetString(0);
                return fileTypes;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "";
        }

        public static int GetCurrentSeqValue(string seqName)
        {
            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn, CommandText = "Select " + seqName + " FROM dual", CommandType = CommandType.Text
            };
            OracleDataReader dataReader = cmd.ExecuteReader();
            dataReader = cmd.ExecuteReader();
            dataReader.Read();
            int fileId = dataReader.GetInt32(0);
            cmd.Cancel();
            return fileId;
        }

        public static void DeleteLastFolderAdded()
        {
            OracleCommand oracleCommand = new OracleCommand
            {
                Connection = Conn, CommandText = "Delete from FOLDERS WHERE id=" + _lastNullFolder
            };
            int rowsUpdated = oracleCommand.ExecuteNonQuery();
        }

        public static String CreateCommand(String content, int pagesMin, int pagesMax, String lastModifiedMin,
            String lastModifiedMax, String creationDateMin, String creationDateMax, int fileSizeMin, int fileSizeMax,
            String extension, String title)
        {
            String command = "";
            if (lastModifiedMin != null || lastModifiedMax != null)
            {
                if (lastModifiedMin != "" || lastModifiedMax != "")
                {
                    command = command + " and fd.last_modified between TO_DATE('" + lastModifiedMin +
                              "', 'dd/mm/yyyy') AND TO_DATE('" + lastModifiedMax + "', 'dd/mm/yyyy')";
                }
            }

            if (creationDateMin != null || creationDateMax != null)
            {
                if (creationDateMin != "" || creationDateMax != "")
                {
                    command = command + " and fd.date_created between TO_DATE('" + creationDateMin +
                              "', 'dd/mm/yyyy') AND TO_DATE('" + creationDateMax + "', 'dd/mm/yyyy')";
                }
            }

            if (fileSizeMin != 0 || fileSizeMax != 0)
            {
                command = command + " and fd.fileSize BETWEEN " + fileSizeMin + " AND " + fileSizeMax;
            }

            if (extension != null)
            {
                if (extension != "")
                {
                    command = command + " and fd.extension LIKE('%." + extension + "%')";
                }
            }

            if (title != null)
            {
                if (title != "")
                {
                    command = command + " and fd.title LIKE('%" + title + "%')";
                }
            }

            if (pagesMin != 0 || pagesMax != 0)
            {
                if (pagesMin != 0 || pagesMax != int.MaxValue)
                {
                    command = command + " and fd.pages BETWEEN " + pagesMin + " AND " + pagesMax;
                }
            }

            return command;
        }

        public static List<Record> SearchContent(String content)
        {
            List<Record> instance = new List<Record>();
            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn,
                CommandType = CommandType.Text,
                CommandText =
                    "SELECT cd.continut,cd.part_number,fd.last_modified,fd.date_created,fd.fileSize,fd.extension,fd.author,fd.title,fd.pages,f.local_path" +
                    " FROM content_data cd, file_details fd, folders f WHERE cd.file_details_id = fd.id AND f.id = fd.folder_id " +
                    " and cd.continut LIKE('%" + content + "%')"
            };
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Record r = new Record(dr.GetOracleString(0).ToString(), dr.GetInt32(1),
                    dr.GetOracleString(2).ToString(), dr.GetOracleString(3).ToString(), dr.GetInt32(4),
                    dr.GetOracleString(5).ToString(), dr.GetOracleString(6).ToString(),
                    dr.GetOracleString(7).ToString(), dr.GetInt32(8), dr.GetOracleString(9).ToString());
                instance.Add(r);
            }

            while (dr.Read())
            {
                Record r = new Record(dr.GetOracleString(0).ToString(), dr.GetInt32(1),
                    dr.GetOracleString(2).ToString(), dr.GetOracleString(3).ToString(), dr.GetInt32(4),
                    dr.GetOracleString(5).ToString(), dr.GetOracleString(6).ToString(),
                    dr.GetOracleString(7).ToString(), dr.GetInt32(8), dr.GetOracleString(9).ToString());
                instance.Add(r);
            }

            return instance;
        }

        public static List<Record> SearchContent(String content, int pagesMin, int pagesMax, String lastModifiedMin,
            String lastModifiedMax, String creationDateMin, String creationDateMax, int fileSizeMin, int fileSizeMax,
            String extension, String title)
        {
            List<Record> instance = new List<Record>();
            OracleCommand cmd = new OracleCommand {Connection = Conn, CommandType = CommandType.Text};
            String command;
            if (content == "") //Query normal 
            {
                command =
                    "SELECT fd.last_modified, fd.date_created, fd.fileSize, fd.extension, fd.author, fd.title, fd.pages,f.id FROM file_details fd , folders f WHERE fd.folder_id = f.id ";
                command = command + CreateCommand(content, pagesMin, pagesMax, lastModifiedMin, lastModifiedMax,
                              creationDateMin, creationDateMax, fileSizeMin, fileSizeMax, extension, title);
                cmd.CommandText = command;

                OracleDataReader dr = cmd.ExecuteReader();
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Record r = new Record(dr.GetString(0), dr.GetString(1), dr.GetInt32(2),
                        dr.GetOracleString(3).ToString(), dr.GetOracleString(4).ToString(),
                        dr.GetOracleString(5).ToString(), dr.GetInt32(6));
                    int pathId = dr.GetInt32(7);

                    OracleCommand oracleCommand = new OracleCommand
                    {
                        Connection = Conn,
                        CommandType = CommandType.Text,
                        CommandText = "SELECT listagg(local_path,'\\') within group (order by level desc) as fullp" +
                                      " FROM folders START WITH id= " + pathId + " CONNECT BY prior parent_id = id "
                    };

                    OracleDataReader odr = oracleCommand.ExecuteReader();
                    odr.Read();
                    r.LocalPath = odr.GetOracleString(0).ToString();
                    instance.Add(r);
                }

                return instance;
            }
            else //Query relaxation
            {
                command =
                    "SELECT fd.last_modified,fd.date_created,fd.fileSize,fd.extension,fd.author,fd.title,fd.pages,f.id, listagg(cd.id , ',') within group (ORDER BY score(1) DESC)" +
                    " FROM content_data cd, file_details fd, folders f WHERE cd.file_details_id = fd.id AND f.id = fd.folder_id ";
                command = command + CreateCommand(content, pagesMin, pagesMax, lastModifiedMin, lastModifiedMax,
                              creationDateMin, creationDateMax, fileSizeMin, fileSizeMax, extension, title);


                string relax = "'<query><textquery grammar = \"CONTEXT\"> " +
                               content +
                               "<progression><seq><rewrite>transform((TOKENS, \"{\", \"}\", \" \"))</rewrite></seq>" +
                               "<seq><rewrite> transform((TOKENS, \"{\", \"}\", \"AND\"))</rewrite></seq>" +
                               "<seq><rewrite>transform((TOKENS, \"?{\", \"}\", \"AND\"))</rewrite></seq>" +
                               "<seq><rewrite>transform((TOKENS, \"{\", \"}\", \"OR\"))</rewrite></seq>" +
                               "<seq><rewrite>transform((TOKENS, \"?{\", \"}\", \"OR\"))</rewrite></seq>" +
                               "</progression></textquery><score datatype = \"INTEGER\" algorithm = \"COUNT\" /></query>'";

                string[] contents = content.Split('+', '-', '"');
                if (contents.Length == 1) // No operators -> standard query relaxation
                {
                    command = command + " and CONTAINS(cd.continut," + relax + " , 1) >0";
                    command = command +
                              " GROUP BY fd.last_modified,fd.date_created,fd.fileSize,fd.extension,fd.author,fd.title,fd.pages,f.id  ORDER BY SUM(SCORE(1)) DESC";
                }
                else // Manual query relaxation
                {
                    command = AddOperators(command, content, contents);
                }

                cmd.CommandText = command;
                var dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    Record record = new Record(dataReader.GetString(0), dataReader.GetString(1), dataReader.GetInt32(2),
                        dataReader.GetOracleString(3).ToString(), dataReader.GetOracleString(4).ToString(),
                        dataReader.GetOracleString(5).ToString(), dataReader.GetInt32(6));
                    int pathId = dataReader.GetInt32(7);
                    String pageIds = dataReader.GetOracleString(8).ToString();

                    OracleCommand oracleCommand = new OracleCommand
                    {
                        Connection = Conn,
                        CommandType = CommandType.Text,
                        CommandText = "SELECT listagg(local_path,'\\') within group (order by level desc) as fullp" +
                                      " FROM folders START WITH id= " + pathId + " CONNECT BY parent_id = id "
                    };

                    OracleDataReader oracleDataReader = oracleCommand.ExecuteReader();
                    oracleDataReader.Read();
                    record.LocalPath = oracleDataReader.GetOracleString(0).ToString();

                    string[] bestId = pageIds.Split(',');
                    record.MostRelevantPage = Int32.Parse(bestId[0]);

                    oracleCommand.CommandText = "SELECT continut FROM content_data WHERE id = " + bestId[0];
                    oracleDataReader = oracleCommand.ExecuteReader();
                    oracleDataReader.Read();
                    string relevantContent = oracleDataReader.GetOracleString(0).ToString();
                    string[] contentTokens = content.Split(' ', '+', '-', '"');
                    foreach (string s in contentTokens)
                    {
                        if (s != "")
                            relevantContent = relevantContent.Replace(s, "<<" + s + ">>");
                    }

                    int startIndex = 0;
                    List<int> relevantWordsLocation = new List<int>();
                    List<int> relevantWordsDensity = new List<int>();

                    while ((startIndex = relevantContent.IndexOf("<<", startIndex + 1, StringComparison.Ordinal)) > 0)
                    {
                        relevantWordsLocation.Add(startIndex);
                    }

                    if (relevantWordsLocation.Count > 0)
                    {
                        foreach (int index in relevantWordsLocation)
                        {
                            int counter = 0;
                            foreach (int index2 in relevantWordsLocation)
                            {
                                if (index2 <= index + 300)
                                {
                                    if (index2 >= index)
                                    {
                                        counter++;
                                    }
                                }
                                else break;
                            }

                            relevantWordsDensity.Add(counter);
                        }

                        int bestIndex =
                            relevantWordsLocation.ElementAt(relevantWordsDensity.IndexOf(relevantWordsDensity.Max()));

                        if (bestIndex > 30)
                        {
                            if (relevantContent.Length > bestIndex + 400)
                            {
                                record.RelevantContent = "..." + relevantContent.Substring(bestIndex - 30, 400);
                            }
                            else
                            {
                                record.RelevantContent =
                                    "..." + relevantContent.Substring(bestIndex - 30,
                                        relevantContent.Length - bestIndex);
                            }
                        }
                        else
                        {
                            if (relevantContent.Length > bestIndex + 400)
                            {
                                record.RelevantContent = relevantContent.Substring(0, 400);
                            }
                            else
                            {
                                record.RelevantContent = relevantContent.Substring(0, relevantContent.Length);
                            }
                        }
                    }
                    else
                    {
                        if (relevantContent.Length > 430)
                        {
                            record.RelevantContent = relevantContent.Substring(0, 430);
                        }
                        else
                        {
                            record.RelevantContent = relevantContent.Substring(0, relevantContent.Length);
                        }
                    }

                    instance.Add(record);
                }

                return instance;
            }
        }

        private static String AddOperators(String command, String content, string[] contents)
        {
            if (contents == null) throw new ArgumentNullException(nameof(contents));
            String relax = "'<query>    <textquery grammar = \"CONTEXT\">         <progression> ";
            List<String> seqList = new List<string>();
            List<String> seqListQuotes = new List<string>();
            List<String> seqListPlus = new List<string>();
            List<String> seqListMinus = new List<string>();

            seqList.Add(content);

            contents = content.Split('"');
            if (contents.Length > 1)
            {
                String addString = "";
                foreach (String s in contents)
                    addString = addString + s;
                seqListQuotes.Add(addString);

                for (int j = 1; j < contents.Length; j = j + 2)
                {
                    if (j % 2 == 1)
                    {
                        contents[j] = "fuzzy(" + contents[j] + ",,, weight)";
                    }
                }

                addString = "";
                foreach (String s in contents)
                {
                    addString = addString + s;
                }

                seqListQuotes.Add(addString);
                seqList.Clear();
                foreach (String s in seqListQuotes)
                {
                    seqList.Add(s);
                }
            }


            foreach (String s in seqList)
            {
                if (!s.Contains("+")) continue;
                seqListPlus.Add(s.Replace("+", " AND "));
                seqListPlus.Add(s.Replace("+", " ACCUM "));
            }

            if (seqListPlus.Count > 0)
            {
                seqList.Clear();
                foreach (String s in seqListPlus)
                {
                    seqList.Add(s);
                }
            }

            foreach (String s in seqList)
            {
                if (!s.Contains("-")) continue;
                seqListMinus.Add(s.Replace("-", " NOT "));
                seqListMinus.Add(s.Replace("-", " - "));
            }

            if (seqListMinus.Count > 0)
            {
                seqList.Clear();
                foreach (String s in seqListMinus)
                {
                    seqList.Add(s);
                }
            }

            foreach (String s in seqList)
            {
                relax = relax + " <seq> " + s + "</seq> ";
            }

            relax = relax +
                    " </progression>                             </textquery>                            <score datatype = \"INTEGER\" algorithm = \"COUNT\"/>           </query> ' ";
            command = command + " and CONTAINS(cd.continut," + relax + " , 1) >0";
            command = command +
                      " GROUP BY fd.last_modified,fd.date_created,fd.fileSize,fd.extension,fd.author,fd.title,fd.pages,f.local_path,f.id  ORDER BY SUM(SCORE(1)) DESC";
            return command;
        }

        public static void RefreshIndex()
        {
            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn, CommandType = CommandType.StoredProcedure, CommandText = "CTX_DDL.SYNC_INDEX"
            };

            cmd.Parameters.Add("idx_name", OracleDbType.Char).Value = "idx_content";
            cmd.ExecuteNonQuery();
        }

        public static void Write(FileInfo file)
        {
            IFileHandler textHandler = null;
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

            if (textHandler != null)
            {
                FileDetails fileDetails = textHandler.GetFileDetails();
                OracleCommand cmd = new OracleCommand {Connection = Conn};
                try
                {
                    textHandler.ExtractContent();

                    var folderId = GetCurrentSeqValue("folders_id.currval");

                    cmd.CommandText = "Insert into file_details VALUES (default,'" + fileDetails.LastModified + "','" +
                                      fileDetails.DateCreated + "'," + fileDetails.FileSize + ",'" +
                                      fileDetails.Extension +
                                      "','" + fileDetails.Author + "','" + fileDetails.Title + "'," +
                                      fileDetails.Pages +
                                      "," + folderId + ")";
                    int rowsUpdated = cmd.ExecuteNonQuery();

                    var fileDetailsId = GetCurrentSeqValue("file_details_id.currval");

                    int i = 1;
                    foreach (String s in fileDetails.Content)
                    {
                        cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folderId +
                                          "," + fileDetailsId + ")";
                        cmd.ExecuteNonQuery();
                        i++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static void Write(FileInfo file, String accountEmail)
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

            try
            {
                if (fileHandler == null) return;
                fileHandler.ExtractContent();
                FileDetails fileDetails = fileHandler.GetFileDetails();

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;

                var folderId = GetCurrentSeqValue("folders_id.currval");

                cmd.CommandText = "Insert into file_details VALUES (default,'" + fileDetails.LastModified + "','" +
                                  fileDetails.DateCreated + "'," + fileDetails.FileSize + ",'" +
                                  fileDetails.Extension +
                                  "','" + fileDetails.Author + "','" + fileDetails.Title + "'," +
                                  fileDetails.Pages +
                                  "," + folderId + ",'" + accountEmail + "')";
                cmd.ExecuteNonQuery();

                var fileDetailsId = GetCurrentSeqValue("file_details_id.currval");

                int i = 1;
                foreach (String s in fileDetails.Content)
                {
                    cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folderId +
                                      "," + fileDetailsId + ")";
                    cmd.ExecuteNonQuery();
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void WriteWeb(FileInfo file, String accountEmail, int folderId)
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

            try
            {
                if (fileHandler != null)
                {
                    fileHandler.ExtractContent();
                    FileDetails fileDetails = fileHandler.GetFileDetails();

                    OracleCommand cmd = new OracleCommand();
                    cmd.Connection = Conn;

                    cmd.CommandText = "Insert into file_details VALUES (default,'" + fileDetails.LastModified + "','" +
                                      fileDetails.DateCreated + "'," + fileDetails.FileSize + ",'" +
                                      fileDetails.Extension +
                                      "','" + fileDetails.Author + "','" + fileDetails.Title + "'," +
                                      fileDetails.Pages +
                                      "," + folderId + ",'" + accountEmail + "')";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "Select ID FROM file_details WHERE title ='" + file.Name + "' AND folder_id =" +
                                      folderId;
                    cmd.CommandType = CommandType.Text;
                    OracleDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    var fileDetailsId = dr.GetInt32(0);

                    int i = 1;
                    foreach (String s in fileDetails.Content)
                    {
                        cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folderId +
                                          "," + fileDetailsId + ")";
                        cmd.ExecuteNonQuery();
                        i++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public static int GetFileId()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select content_data_id.currval  FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            dr.Read();
            int fileId = dr.GetInt32(0);
            cmd.Cancel();
            return fileId;
        }

        public static int GetFileDetailsId()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select file_details_id.currval  FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            dr.Read();
            int fileDetailsId = dr.GetInt32(0);
            cmd.Cancel();
            return fileDetailsId;
        }

        public static int GetFolderId()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select folders_id.currval FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            int folderId = dr.GetInt32(0);
            cmd.Cancel();
            return folderId;
        }

        public static int Register(String email, String password, String dateCreated, String type)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Insert into ACCOUNTS values('" + email + "','" + password + "','" + dateCreated + "','" +
                              type + "',NULL,NULL,NULL)";
            int rowsUpdated = cmd.ExecuteNonQuery();
            return rowsUpdated;
        }

        public static bool VerifyAccountIsUnique(String email)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select email FROM accounts WHERE email ='" + email + "'";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            bool hasRows = dr.HasRows;
            cmd.Cancel();
            return hasRows;
        }

        public static Account GetAccountByEmail(String email)
        {
            Account account = new Account();

            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn,
                CommandText =
                    "Select email,password,date_created,type,first_name,last_name,location FROM accounts WHERE email='" +
                    email + "'",
                CommandType = CommandType.Text
            };
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            account.Email = email;
            account.DateCreated = dr.GetOracleString(2).ToString();
            account.Type = dr.GetOracleString(3).ToString();
            account.FirstName = dr.GetOracleString(4).ToString();
            account.LastName = dr.GetOracleString(5).ToString();
            account.Location = dr.GetOracleString(6).ToString();
            cmd.Cancel();

            return account;
        }

        public static int EditAccount(String currentEmail, String newEmail, String password, String firstName,
            String lastName, String location)
        {
            if (password != null)
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                int rowsUpdated;
                int totalRowsUpdated = 0;
                if (newEmail != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET email='" + newEmail + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (password != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET password='" + password + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (firstName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET first_name='" + firstName + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (lastName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET last_name='" + lastName + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (location != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET location='" + location + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                return totalRowsUpdated;
            }
            else
            {
                OracleCommand cmd = new OracleCommand {Connection = Conn};
                int rowsUpdated;
                int totalRowsUpdated = 0;
                if (newEmail != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET email='" + newEmail + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (firstName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET first_name='" + firstName + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (lastName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET last_name='" + lastName + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                if (location != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET location='" + location + "'" + "WHERE email = '" +
                                      currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }

                return totalRowsUpdated;
            }
        }

        public static FileDetailsVector ReadFileDetailsByEmail(String email)
        {
            FileDetailsVector fileDetailsVector = new FileDetailsVector();

            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn,
                CommandText =
                    "SELECT fd.id,fd.last_modified,fd.date_created,fd.fileSize,fd.extension,fd.author,fd.title,fd.pages,fd.folder_id,f.local_path FROM file_details fd, folders f WHERE fd.folder_id = f.id AND f.parent_id is null AND fd.accountemail ='" +
                    email + "'",
                CommandType = CommandType.Text
            };
            OracleDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Models.FileDetails fileDetails = new Models.FileDetails
                {
                    Id = dr.GetInt32(0),
                    LastModified = dr.GetOracleString(1).ToString(),
                    DateCreated = dr.GetOracleString(2).ToString(),
                    FileSize = dr.GetInt32(3),
                    Extension = dr.GetOracleString(4).ToString(),
                    Author = dr.GetOracleString(5).ToString(),
                    Title = dr.GetOracleString(6).ToString(),
                    Pages = dr.GetInt32(7),
                    FolderId = dr.GetInt32(8),
                    Path = dr.GetOracleString(9).ToString()
                };
                fileDetailsVector.FileDetailsList.Add(fileDetails);
            }

            return fileDetailsVector;
        }

        public static FileDetailsVector ReadFileDetailsByFolderId(int folderId)
        {
            FileDetailsVector fileDetailsVector = new FileDetailsVector();

            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText =
                "SELECT fd.id,fd.last_modified,fd.date_created,fd.fileSize,fd.extension,fd.author,fd.title,fd.pages,fd.folder_id,f.local_path,fd.accountemail FROM file_details fd, folders f WHERE fd.folder_id = f.id AND f.id = " +
                folderId;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Models.FileDetails fileDetails = new Models.FileDetails();
                fileDetails.Id = dr.GetInt32(0);
                fileDetails.LastModified = dr.GetOracleString(1).ToString();
                fileDetails.DateCreated = dr.GetOracleString(2).ToString();
                fileDetails.FileSize = dr.GetInt32(3);
                fileDetails.Extension = dr.GetOracleString(4).ToString();
                fileDetails.Author = dr.GetOracleString(5).ToString();
                fileDetails.Title = dr.GetOracleString(6).ToString();
                fileDetails.Pages = dr.GetInt32(7);
                fileDetails.FolderId = dr.GetInt32(8);
                fileDetails.Path = dr.GetOracleString(9).ToString();
                fileDetails.AccountEmail = dr.GetOracleString(10).ToString();
                fileDetailsVector.FileDetailsList.Add(fileDetails);
            }

            return fileDetailsVector;
        }

        public static int GetFolderId(String localPath)
        {
            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn,
                CommandText = "SELECT id FROM folders WHERE local_path = '" + localPath + "'",
                CommandType = CommandType.Text
            };
            OracleDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                int folderId = dr.GetInt32(0);
                return folderId;
            }

            return 0;
        }

        public static List<Folder> GetSubfolders(int folderId)
        {
            List<Folder> folderList = new List<Folder>();

            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn,
                CommandText = "SELECT id,local_path,parent_id,fullScan FROM folders WHERE parent_id =" + folderId,
                CommandType = CommandType.Text
            };
            OracleDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Folder folder = new Folder
                {
                    Id = dr.GetInt32(0),
                    LocalPath = dr.GetOracleString(1).ToString(),
                    ParentId = dr.GetInt32(2),
                    FullScan = dr.GetInt32(3)
                };
                folderList.Add(folder);
            }

            return folderList;
        }

        public static int VerifyAccountPassword(String accountEmail, String password)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT password FROM accounts WHERE email ='" + accountEmail + "'";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            String accountPassword = dr.GetOracleString(0).ToString();
            if (accountPassword == password)
            {
                return 1;
            }

            return 0;
        }

        public static int DeleteFile(String filePath)
        {
            try
            {
                string[] splitter = filePath.Split('\\');

                String folderPath = "";
                for (int i = 0; i <= splitter.Length - 3; i++)
                {
                    folderPath = folderPath + splitter[i] + "\\";
                }

                folderPath = folderPath + splitter[splitter.Length - 2];

                //get File_Details id of file
                OracleCommand cmd = new OracleCommand
                {
                    Connection = Conn,
                    CommandText =
                        "SELECT fd.id,fd.title, f.local_path FROM file_details fd, folders f WHERE f.id = fd.folder_id AND fd.title='" +
                        splitter[splitter.Length - 1] + "' AND f.local_path='" + folderPath + "'",
                    CommandType = CommandType.Text
                };
                OracleDataReader dr = cmd.ExecuteReader();
                dr.Read();
                int id = dr.GetInt32(0);

                //Delete file knowing id
                cmd.CommandText = "DELETE FROM file_details WHERE id = '" + id + "'";
                int rowsDeleted = cmd.ExecuteNonQuery();
                return rowsDeleted;
            }
            catch
            {
                return 0;
            }
        }

        public static int DeleteFolder(String folderPath)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "DELETE FROM folders WHERE local_path = '" + folderPath + "'";
            int rowsDeleted = cmd.ExecuteNonQuery();
            return rowsDeleted;
        }

        public static bool VerifyFolderPathExists(String folderPath)
        {
            OracleCommand cmd = new OracleCommand
            {
                Connection = Conn,
                CommandText = "SELECT local_path FROM folders WHERE local_path ='" + folderPath + "'",
                CommandType = CommandType.Text
            };
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                return true;
            }

            return false;
        }

        public static bool CreateGroup(String groupName)
        {
            try
            {
                OracleCommand cmd;

                cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO groups VALUES(default,'" + groupName + "','" +
                                  DateTime.Now.Day.ToString() + "/" + DateTime.Now.Month.ToString() + "/" +
                                  DateTime.Now.Year.ToString() + "')";

                int rowsUpdated = cmd.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool VerifyGroupNameUnique(String groupName)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "Select group_name FROM groups WHERE group_name ='" + groupName + "'";
                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                dr.Read();
                bool hasRows = dr.HasRows;
                cmd.Cancel();
                return !hasRows;
            }
            catch
            {
                return false;
            }
        }

        public static bool AddGroupMember(int groupId, String accountEmail, String memberType)
        {
            try
            {
                var cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO groupmembers VALUES(default," + groupId + ",'" + accountEmail + "','" +
                                  memberType + "')";
                int rowsUpdated = cmd.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static int GetGroupId(String groupName)
        {
            try
            {
                OracleCommand cmd = new OracleCommand
                {
                    Connection = Conn,
                    CommandText = "Select groupid FROM groups WHERE group_name ='" + groupName + "'",
                    CommandType = CommandType.Text
                };
                OracleDataReader dr = cmd.ExecuteReader();
                dr.Read();
                int groupId = dr.GetInt32(0);
                cmd.Cancel();
                return groupId;
            }
            catch
            {
                return -1;
            }
        }

        public static Groups GetGroupData(String accountEmail)
        {
            try
            {
                Groups groups = new Groups {GroupList = new List<Group>()};

                OracleCommand cmd = new OracleCommand
                {
                    Connection = Conn,
                    CommandText =
                        "SELECT g.groupid,g.group_name, g.date_created,gm.member_type FROM groupmembers gm, groups g WHERE gm.groupid = g.groupid AND gm.accountemail = '" +
                        accountEmail + "'",
                    CommandType = CommandType.Text
                };
                OracleDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Group group = new Group();
                    group.GroupId = dr.GetInt32(0);
                    group.GroupName = dr.GetOracleString(1).ToString();
                    group.DateCreated = dr.GetOracleString(2).ToString();
                    group.MemberType = dr.GetOracleString(3).ToString();
                    groups.GroupList.Add(group);
                }

                return groups;
            }
            catch
            {
                return new Groups();
            }
        }

        public static GroupMembers GetGroupMembers(int groupId)
        {
            try
            {
                GroupMembers groupMembers = new GroupMembers {GroupMemberList = new List<GroupMember>()};

                OracleCommand cmd = new OracleCommand
                {
                    Connection = Conn,
                    CommandText = "SELECT groupid, accountemail, member_type FROM groupmembers WHERE groupid=" +
                                  groupId,
                    CommandType = CommandType.Text
                };
                OracleDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    GroupMember groupMember = new GroupMember
                    {
                        GroupId = dr.GetInt32(0),
                        AccountEmail = dr.GetOracleString(1).ToString(),
                        MemberType = dr.GetOracleString(2).ToString()
                    };
                    groupMembers.GroupMemberList.Add(groupMember);
                }

                return groupMembers;
            }
            catch
            {
                return new GroupMembers();
            }
        }

        public static bool DeleteGroupMember(String memberEmail, int groupId)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "Delete from groupmembers WHERE accountemail='" + memberEmail + "' AND groupid = " +
                                  groupId;
                int rowsUpdated = cmd.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}