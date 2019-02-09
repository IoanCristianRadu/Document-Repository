using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnidecodeSharpFork;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers;
using DocumentRepositoryOnline.Models;

namespace DocumentRepositoryOnline.DocumentRepository
{
    public class DBSingleton
    {
        private static OracleConnection conn = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522))(CONNECT_DATA=(SERVICE_NAME=orcl))); Password=DocRep;User ID = C##DocRep");
        //"Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = orclu))); Password=DocRep;User ID = C##DocRep");
        //DATA SOURCE=DocRep;USER ID=C##DOCREP

        private static int lastNullFolder;

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

        public static List<String> getDBFolderPaths()
        {
            try
            {

                OracleCommand cmd = new OracleCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "Select local_path, id from folders where parent_id IS NULL and fullscan = 1";
                cmd.Connection = Conn;

                OracleDataReader dr = cmd.ExecuteReader();
                List<string> pathList = new List<string>();
                while (dr.Read())
                {
                    pathList.Add(dr.GetOracleString(0).ToString());
                }
                dr.Close();

                cmd.CommandText = "Select f.local_path ||'\\'|| fd.title from folders f,file_details fd where f.id = fd.folder_id AND f.fullscan = 0";
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

        public static int writeFolder(String path, int? parentId, int fullscan)
        {
            try
            {

                OracleCommand cmd;
                if (parentId.HasValue)
                {
                    cmd = new OracleCommand();
                    parentId = getCurrentSeqValue("folders_id.currval");
                    /*
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "Select folders_id.currval FROM dual ";
                    OracleDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    dr.Close();         
                    */
                }

                cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "Insert into folders (LOCAL_PATH, PARENT_ID,FULLSCAN) VALUES ('" + path + "', " + (parentId.GetValueOrDefault() == 0 ? "null" : parentId.ToString()) + "," + fullscan + ")";

                int rowsUpdated = cmd.ExecuteNonQuery();



                int primaryKey = getCurrentSeqValue("folders_id.currval");

                if (parentId.GetValueOrDefault() == 0)
                {
                    lastNullFolder = primaryKey;
                }
                //Conn.Close();
                return primaryKey;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
            return -1;
        }

        public static int writeFolderWeb(String path, int? parentId, int fullscan)
        {
            try
            {
                OracleCommand cmd;

                cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "Insert into folders (LOCAL_PATH, PARENT_ID,FULLSCAN) VALUES ('" + path + "', " + (parentId.GetValueOrDefault() == 0 ? "null" : parentId.ToString()) + "," + fullscan + ")";

                int rowsUpdated = cmd.ExecuteNonQuery();

                return rowsUpdated;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
            return -1;
        }

        public static String getFiletypes()
        {
            try
            {

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "Select filetype FROM app_settings";
                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                dr.Read();
                String filetypes = dr.GetString(0);
                //Conn.Close();
                return filetypes;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return "";
        }

        public static int getCurrentSeqValue(string seqName)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select " + seqName + " FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            dr.Read();
            int file_id = dr.GetInt32(0);
            cmd.Cancel();
            return file_id;
        }

        public static void DeleteLastFolderAdded()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Delete from FOLDERS WHERE id=" + lastNullFolder;
            int rowsUpdated = cmd.ExecuteNonQuery();
        }

        public static String createCommand(String content, int pagesMin, int pagesMax, String lastModifiedMin, String lastModifiedMax, String creationDateMin, String creationDateMax, int fileSizeMin, int fileSizeMax, String extension, String title)
        {
            String command = "";
            if (lastModifiedMin != null || lastModifiedMax != null)
            {
                if (lastModifiedMin != "" || lastModifiedMax != "")
                {
                    command = command + " and fd.last_modified between TO_DATE('" + lastModifiedMin + "', 'dd/mm/yyyy') AND TO_DATE('" + lastModifiedMax + "', 'dd/mm/yyyy')";
                }
            }

            if (creationDateMin != null || creationDateMax != null)
            {
                if (creationDateMin != "" || creationDateMax != "")
                {
                    command = command + " and fd.date_created between TO_DATE('" + creationDateMin + "', 'dd/mm/yyyy') AND TO_DATE('" + creationDateMax + "', 'dd/mm/yyyy')";
                }
            }

            if (fileSizeMin != 0 || fileSizeMax != 0)
            {
                command = command + " and fd.filesize BETWEEN " + fileSizeMin + " AND " + fileSizeMax;
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
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT cd.continut,cd.part_number,fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,f.local_path" +
                             " FROM content_data cd, file_details fd, folders f WHERE cd.file_details_id = fd.id AND f.id = fd.folder_id " +
                             " and cd.continut LIKE('%" + content + "%')";
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Record r = new Record(dr.GetOracleString(0).ToString(), dr.GetInt32(1), dr.GetOracleString(2).ToString(), dr.GetOracleString(3).ToString(), dr.GetInt32(4), dr.GetOracleString(5).ToString(), dr.GetOracleString(6).ToString(), dr.GetOracleString(7).ToString(), dr.GetInt32(8), dr.GetOracleString(9).ToString());
                instance.Add(r);
            }
            while (dr.Read())
            {
                Record r = new Record(dr.GetOracleString(0).ToString(), dr.GetInt32(1), dr.GetOracleString(2).ToString(), dr.GetOracleString(3).ToString(), dr.GetInt32(4), dr.GetOracleString(5).ToString(), dr.GetOracleString(6).ToString(), dr.GetOracleString(7).ToString(), dr.GetInt32(8), dr.GetOracleString(9).ToString());
                instance.Add(r);
            }
            return instance;
        }

        public static List<Record> SearchContent(String content, int pagesMin, int pagesMax, String lastModifiedMin, String lastModifiedMax, String creationDateMin, String creationDateMax, int fileSizeMin, int fileSizeMax, String extension, String title)
        {/*
            if (pagesMin == 0)
            {
                pagesMin = null;
            }
            if (pagesMax == 0)
            {
                pagesMax = null;
            }
            if (fileSizeMax == 0)
            {
                fileSizeMax = null;
            }
            if (fileSizeMin == 0)
            {
                fileSizeMin = null;
            }
            */
            List<Record> instance = new List<Record>();
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandType = CommandType.Text;
            String command;
            if (content == "") //Query normal 
            {
                command = "SELECT fd.last_modified, fd.date_created, fd.filesize, fd.extension, fd.author, fd.title, fd.pages,f.id FROM file_details fd , folders f WHERE fd.folder_id = f.id ";

                command = command + createCommand(content, pagesMin, pagesMax, lastModifiedMin, lastModifiedMax, creationDateMin, creationDateMax, fileSizeMin, fileSizeMax, extension, title);

                cmd.CommandText = command;
                /*"SELECT last_modified,date_created,filesize,extension,author,title,pages FROM file_details fd WHERE " +
                     //" TO_DATE(fd.last_modified , 'mm/dd/yyyy HH:MI:SS AM') between TO_DATE('" + lastModifiedMin + "', 'dd/mm/yyyy') AND TO_DATE('" + lastModifiedMax + "', 'dd/mm/yyyy')" +
                     " fd.last_modified between TO_DATE('" + lastModifiedMin + "', 'dd/mm/yyyy') AND TO_DATE('" + lastModifiedMax + "', 'dd/mm/yyyy')" +
                     " and fd.date_created between TO_DATE('" + creationDateMin + "', 'dd/mm/yyyy') AND TO_DATE('" + creationDateMax + "', 'dd/mm/yyyy')" +
                     " and fd.filesize BETWEEN " + fileSizeMin + " AND " + fileSizeMax +
                     " and fd.extension LIKE('%." + extension + "%')" +
                     " and fd.title LIKE('%" + title + "%')" +
                     " and fd.pages BETWEEN " + pagesMin + " AND " + pagesMax;*/
                OracleDataReader dr = cmd.ExecuteReader();
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Record r = new Record(dr.GetString(0), dr.GetString(1), dr.GetInt32(2), dr.GetOracleString(3).ToString(), dr.GetOracleString(4).ToString(), dr.GetOracleString(5).ToString(), dr.GetInt32(6));
                    int PathId = dr.GetInt32(7);


                    OracleCommand orclcmd = new OracleCommand();
                    orclcmd.Connection = Conn;
                    orclcmd.CommandType = CommandType.Text;
                    orclcmd.CommandText = "SELECT listagg(local_path,'\\') within group (order by level desc) as fullp" +
                                          " FROM folders START WITH id= " + PathId + " CONNECT BY prior parent_id = id ";

                    OracleDataReader odr = orclcmd.ExecuteReader();
                    odr.Read();
                    r.Local_path = odr.GetOracleString(0).ToString();
                    instance.Add(r);
                }
                return instance;
            }
            else //Query relaxation pe content
            {
                command = "SELECT fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,f.id, listagg(cd.id , ',') within group (ORDER BY score(1) DESC)" +
                         " FROM content_data cd, file_details fd, folders f WHERE cd.file_details_id = fd.id AND f.id = fd.folder_id ";
                command = command + createCommand(content, pagesMin, pagesMax, lastModifiedMin, lastModifiedMax, creationDateMin, creationDateMax, fileSizeMin, fileSizeMax, extension, title);


                string relax = "'<query><textquery grammar = \"CONTEXT\"> " +
                                    content + "<progression><seq><rewrite>transform((TOKENS, \"{\", \"}\", \" \"))</rewrite></seq>" +
                                    "<seq><rewrite> transform((TOKENS, \"{\", \"}\", \"AND\"))</rewrite></seq>" +
                                    "<seq><rewrite>transform((TOKENS, \"?{\", \"}\", \"AND\"))</rewrite></seq>" +
                                    "<seq><rewrite>transform((TOKENS, \"{\", \"}\", \"OR\"))</rewrite></seq>" +
                                    "<seq><rewrite>transform((TOKENS, \"?{\", \"}\", \"OR\"))</rewrite></seq>" +
                                    "</progression></textquery><score datatype = \"INTEGER\" algorithm = \"COUNT\" /></query>'";

                //command = command + " and cd.continut LIKE('%" + content + "%')";


                string[] contents = content.Split('+', '-', '"');
                if (contents.Length == 1) // Daca nu avem operatori, folosim query relaxation standard
                {
                    command = command + " and CONTAINS(cd.continut," + relax + " , 1) >0";
                    command = command + " GROUP BY fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,f.id  ORDER BY SUM(SCORE(1)) DESC";
                }
                else // Daca avem mai mult de 1 operator, folosim query relaxation manual 
                {
                    command = addOperators(command, content, contents);
                }


                cmd.CommandText = command;
                /*"SELECT cd.continut,cd.part_number,fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,f.local_path" +
                     " FROM content_data cd, file_details fd, folders f WHERE cd.file_details_id = fd.id AND f.id = fd.folder_id " +
                     " and cd.continut LIKE('%" + content + "%')" +
                     " and TO_DATE(fd.last_modified , 'mm/dd/yyyy HH:MI:SS AM') between TO_DATE('" + lastModifiedMin + "', 'dd/mm/yyyy') AND TO_DATE('" + lastModifiedMax + "', 'dd/mm/yyyy')" +
                     " and TO_DATE(fd.date_created , 'mm/dd/yyyy HH:MI:SS AM') between TO_DATE('" + creationDateMin + "', 'dd/mm/yyyy') AND TO_DATE('" + creationDateMax + "', 'dd/mm/yyyy')" +
                     " and fd.filesize BETWEEN " + fileSizeMin + " AND " + fileSizeMax +
                     " and fd.extension LIKE('%." + extension + "%')" +
                     " and fd.title LIKE('%" + title + "%')" +
                     " and fd.pages BETWEEN " + pagesMin + " AND " + pagesMax;*/
                OracleDataReader dataReader = cmd.ExecuteReader();

                dataReader = cmd.ExecuteReader();
                /*
                if (dr.Read())
                {
                    Record r = new Record(dr.GetOracleDate(0).ToString(), dr.GetOracleDate(1).ToString(), dr.GetInt32(2), dr.GetOracleString(3).ToString(), dr.GetOracleString(4).ToString(), dr.GetOracleString(5).ToString(), dr.GetInt32(6));
                    int PathId = dr.GetInt32(7);

                    OracleCommand orclcmd = new OracleCommand();
                    orclcmd.Connection = Conn;
                    orclcmd.CommandType = CommandType.Text;
                    orclcmd.CommandText = "SELECT listagg(local_path,'\') within group (order by level)" +
                                          " FROM folders START WITH id " + PathId + " CONNECT BY prior id = parent_id; ";

                    OracleDataReader odr = cmd.ExecuteReader();
                    r.Local_path = odr.GetOracleString(0).ToString();

                    String PageIds = dr.GetOracleString(8).ToString();
                    string[] bestId = PageIds.Split(',');
                    r.MostRelevantPage = Int32.Parse(bestId[0]);
                    instance.Add(r);
                }*/
                while (dataReader.Read())
                {
                    Record r = new Record(dataReader.GetString(0), dataReader.GetString(1), dataReader.GetInt32(2), dataReader.GetOracleString(3).ToString(), dataReader.GetOracleString(4).ToString(), dataReader.GetOracleString(5).ToString(), dataReader.GetInt32(6));
                    int PathId = dataReader.GetInt32(7);
                    String PageIds = dataReader.GetOracleString(8).ToString();


                    OracleCommand orclcmd = new OracleCommand();
                    orclcmd.Connection = Conn;
                    orclcmd.CommandType = CommandType.Text;
                    orclcmd.CommandText = "SELECT listagg(local_path,'\\') within group (order by level desc) as fullp" +
                                          " FROM folders START WITH id= " + PathId + " CONNECT BY parent_id = id ";

                    OracleDataReader odr = orclcmd.ExecuteReader();
                    odr.Read();
                    r.Local_path = odr.GetOracleString(0).ToString();

                    string[] bestId = PageIds.Split(',');
                    r.MostRelevantPage = Int32.Parse(bestId[0]);


                    orclcmd.CommandText = "SELECT continut FROM content_data WHERE id = " + bestId[0];
                    odr = orclcmd.ExecuteReader();
                    odr.Read();
                    string relevantContent = odr.GetOracleString(0).ToString();
                    string[] continutTokens = content.Split(' ', '+', '-', '"');
                    foreach (string s in continutTokens)
                    {
                        if (s != "")
                            relevantContent = relevantContent.Replace(s, "<<" + s + ">>");
                    }

                    int startIndex = 0;
                    List<int> relevantWordsLocation = new List<int>();
                    List<int> relevantWordsDensity = new List<int>();

                    //Tine minte pozitia cuvintelor din cautare
                    while ((startIndex = relevantContent.IndexOf("<<", startIndex + 1)) > 0)
                    {
                        relevantWordsLocation.Add(startIndex);
                    }

                    //Gaseste bucata din text cu cele mai multe cuvinte din cautare intr-un range de ~400 caractere
                    if (relevantWordsLocation.Count > 0)
                    {
                        foreach (int index in relevantWordsLocation)
                        {
                            int contor = 0;
                            foreach (int index2 in relevantWordsLocation)
                            {
                                if (index2 <= index + 300)
                                {
                                    if (index2 >= index)
                                    {
                                        contor++;
                                    }
                                }
                                else break;
                            }
                            relevantWordsDensity.Add(contor);
                        }

                        int bestIndex = relevantWordsLocation.ElementAt(relevantWordsDensity.IndexOf(relevantWordsDensity.Max()));

                        if (bestIndex > 30)
                        {
                            if (relevantContent.Length > bestIndex + 400)
                            {
                                r.RelevantContent = "..." + relevantContent.Substring(bestIndex - 30, 400);
                            }
                            else
                            {
                                r.RelevantContent = "..." + relevantContent.Substring(bestIndex - 30, relevantContent.Length - bestIndex);
                            }
                        }
                        else
                        {
                            if (relevantContent.Length > bestIndex + 400)
                            {
                                r.RelevantContent = relevantContent.Substring(0, 400);
                            }
                            else
                            {
                                r.RelevantContent = relevantContent.Substring(0, relevantContent.Length);
                            }
                        }
                    }
                    else
                    {
                        if (relevantContent.Length > 430)
                        {
                            r.RelevantContent = relevantContent.Substring(0, 430);
                        }
                        else
                        {
                            r.RelevantContent = relevantContent.Substring(0, relevantContent.Length);
                        }

                    }
                    instance.Add(r);
                }
                return instance;
            }
        }

        //Functie auxiliara folosita pentru search
        //Adauga in "command" query relaxation manual pentru 2 sau mai multi operatori
        private static String addOperators(String command, String content, string[] contents)
        {
            String relax = "'<query>    <textquery grammar = \"CONTEXT\">         <progression> ";
            List<String> seqList = new List<string>();
            List<String> seqListGhilimele = new List<string>();
            List<String> seqListPlus = new List<string>();
            List<String> seqListMinus = new List<string>();

            seqList.Add(content);


            contents = content.Split('"');
            if (contents.Length > 1)
            {
                String addString = "";
                foreach (String s in contents)
                    addString = addString + s;
                seqListGhilimele.Add(addString);

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

                seqListGhilimele.Add(addString);
                seqList.Clear();
                foreach (String s in seqListGhilimele)
                {
                    seqList.Add(s);
                }
            }


            foreach (String s in seqList)
            {
                if (s.Contains("+"))
                {
                    seqListPlus.Add(s.Replace("+", " AND "));
                    seqListPlus.Add(s.Replace("+", " ACCUM "));
                }
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
                if (s.Contains("-"))
                {
                    seqListMinus.Add(s.Replace("-", " NOT "));
                    seqListMinus.Add(s.Replace("-", " - "));
                }
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
            relax = relax + " </progression>                             </textquery>                            <score datatype = \"INTEGER\" algorithm = \"COUNT\"/>           </query> ' ";
            command = command + " and CONTAINS(cd.continut," + relax + " , 1) >0";
            command = command + " GROUP BY fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,f.local_path,f.id  ORDER BY SUM(SCORE(1)) DESC";
            return command;
        }

        public static void refreshIndex()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.CommandText = "CTX_DDL.SYNC_INDEX";
            cmd.Parameters.Add("idx_name", OracleDbType.Char).Value = "idx_content";
            cmd.ExecuteNonQuery();
        }

        public static void Write(FileInfo file) //move me to FilesTraverse
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
            try
            {
                textHandler.extractContent();

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;

                int folder_id;
                int file_details_id;

                //Get current folder_id
                folder_id = getCurrentSeqValue("folders_id.currval");


                //Insert file details for file
                cmd.CommandText = "Insert into file_details VALUES (default,'" + textHandler.last_modified + "','" + textHandler.date_created + "'," + textHandler.fileSize + ",'" + textHandler.extension + "','" + textHandler.author + "','" + textHandler.title + "'," + textHandler.pages + "," + folder_id + ")";
                int rowsUpdated = cmd.ExecuteNonQuery();

                //get current file_details_id
                file_details_id = getCurrentSeqValue("file_details_id.currval");

                int i = 1;
                foreach (String s in textHandler.content)
                {

                    cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folder_id + "," + file_details_id + ")";
                    cmd.ExecuteNonQuery();
                    i++;
                }
                //Conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void Write(FileInfo file, String accountEmail)
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
            try
            {
                textHandler.extractContent();

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;

                int folder_id;
                int file_details_id;

                //Get current folder_id
                folder_id = getCurrentSeqValue("folders_id.currval");


                //Insert file details for file
                cmd.CommandText = "Insert into file_details VALUES (default,'" + textHandler.last_modified + "','" + textHandler.date_created + "'," + textHandler.fileSize + ",'" + textHandler.extension + "','" + textHandler.author + "','" + textHandler.title + "'," + textHandler.pages + "," + folder_id + ",'" + accountEmail + "')";
                int rowsUpdated = cmd.ExecuteNonQuery();

                //get current file_details_id
                file_details_id = getCurrentSeqValue("file_details_id.currval");

                int i = 1;
                foreach (String s in textHandler.content)
                {

                    cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folder_id + "," + file_details_id + ")";
                    cmd.ExecuteNonQuery();
                    i++;
                }
                //Conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void WriteWeb(FileInfo file, String accountEmail, int folderId)
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
            try
            {
                textHandler.extractContent();

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;

                int fileDetailsId;

                //Insert file details for file
                cmd.CommandText = "Insert into file_details VALUES (default,'" + textHandler.last_modified + "','" + textHandler.date_created + "'," + textHandler.fileSize + ",'" + textHandler.extension + "','" + textHandler.author + "','" + textHandler.title + "'," + textHandler.pages + "," + folderId + ",'" + accountEmail + "')";
                int rowsUpdated = cmd.ExecuteNonQuery();

                //get current file_details_id
                cmd.CommandText = "Select ID FROM file_details WHERE title ='" + file.Name + "' AND folder_id =" + folderId;
                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                dr.Read();
                fileDetailsId = dr.GetInt32(0);

                int i = 1;
                foreach (String s in textHandler.content)
                {

                    cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folderId + "," + fileDetailsId + ")";
                    cmd.ExecuteNonQuery();
                    i++;
                }
                //Conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public static int getFileId()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select content_data_id.currval  FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            dr.Read();
            int file_id = dr.GetInt32(0);
            cmd.Cancel();
            return file_id;
        }

        public static int getFileDetailsId()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select file_details_id.currval  FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr = cmd.ExecuteReader();
            dr.Read();
            int file_details_id = dr.GetInt32(0);
            cmd.Cancel();
            return file_details_id;
        }

        public static int getFolderId()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select folders_id.currval FROM dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            int folder_id = dr.GetInt32(0);
            cmd.Cancel();
            return folder_id;
        }

        //Web begins

        public static int register(String email, String password, String dateCreated, String type)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Insert into ACCOUNTS values('" + email + "','" + password + "','" + dateCreated + "','" + type + "',NULL,NULL,NULL)";
            int rowsUpdated = cmd.ExecuteNonQuery();
            return rowsUpdated;
        }

        public static bool verifyAccountIsUnique(String email)
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

        public static Account getAccountByEmail(String email)
        {
            Account account = new Account();

            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "Select email,password,date_created,type,first_name,last_name,location FROM accounts WHERE email='" + email + "'";
            cmd.CommandType = CommandType.Text;
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

        public static int editAccount(String currentEmail, String newEmail, String password, String firstName, String lastName, String location)
        {
            if (password != null) // Update cu parola
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                int rowsUpdated;
                int totalRowsUpdated = 0;
                if (newEmail != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET email='" + newEmail + "'" + "WHERE email = '" + currentEmail + "'";
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
                    cmd.CommandText = "UPDATE accounts SET first_name='" + firstName + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                if (lastName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET last_name='" + lastName + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                if (location != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET location='" + location + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                return totalRowsUpdated;
            }
            else // Update fara parola
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                int rowsUpdated;
                int totalRowsUpdated = 0;
                if (newEmail != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET email='" + newEmail + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                if (firstName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET first_name='" + firstName + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                if (lastName != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET last_name='" + lastName + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                if (location != "null")
                {
                    cmd.CommandText = "UPDATE accounts SET location='" + location + "'" + "WHERE email = '" + currentEmail + "'";
                    rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated != 0)
                    {
                        totalRowsUpdated++;
                    }
                }
                return totalRowsUpdated;
            }
            return 0;
        }

        public static FileDetailsVector readFileDetailsByEmail(String email)
        {
            FileDetailsVector fileDetailsVector = new FileDetailsVector();

            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT fd.id,fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,fd.folder_id,f.local_path FROM file_details fd, folders f WHERE fd.folder_id = f.id AND f.parent_id is null AND fd.accountemail ='" + email + "'";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                FileDetails fileDetails = new FileDetails();
                fileDetails.ID = dr.GetInt32(0);
                fileDetails.LastModified = dr.GetOracleString(1).ToString();
                fileDetails.DateCreated = dr.GetOracleString(2).ToString();
                fileDetails.FileSize = dr.GetInt32(3);
                fileDetails.Extension = dr.GetOracleString(4).ToString();
                fileDetails.Author = dr.GetOracleString(5).ToString();
                fileDetails.Title = dr.GetOracleString(6).ToString();
                fileDetails.Pages = dr.GetInt32(7);
                fileDetails.FolderId = dr.GetInt32(8);
                fileDetails.Path = dr.GetOracleString(9).ToString();
                fileDetailsVector.FileDetailsList.Add(fileDetails);
            }
            return fileDetailsVector;
        }

        public static FileDetailsVector readFileDetailsByFolderId(int folderId)
        {
            FileDetailsVector fileDetailsVector = new FileDetailsVector();

            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT fd.id,fd.last_modified,fd.date_created,fd.filesize,fd.extension,fd.author,fd.title,fd.pages,fd.folder_id,f.local_path,fd.accountemail FROM file_details fd, folders f WHERE fd.folder_id = f.id AND f.id = " + folderId;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                FileDetails fileDetails = new FileDetails();
                fileDetails.ID = dr.GetInt32(0);
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

        public static int getFolderId(String localPath)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT id FROM folders WHERE local_path = '" + localPath + "'";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                int folderId = dr.GetInt32(0);
                return folderId;
            }
            return 0;
        }

        public static List<Folder> getSubfolders(int folderId)
        {
            List<Folder> folderList = new List<Folder>();

            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT id,local_path,parent_id,fullscan FROM folders WHERE parent_id =" + folderId;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Folder folder = new Folder();
                folder.ID = dr.GetInt32(0);
                folder.LocalPath = dr.GetOracleString(1).ToString();
                folder.ParentId = dr.GetInt32(2);
                folder.Fullscan = dr.GetInt32(3);
                folderList.Add(folder);
            }

            return folderList;
        }

        public static int verifyAccountPassword(String accountEmail, String password)
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

        public static int deleteFile(String filePath)
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
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "SELECT fd.id,fd.title, f.local_path FROM file_details fd, folders f WHERE f.id = fd.folder_id AND fd.title='" + splitter[splitter.Length - 1] + "' AND f.local_path='" + folderPath + "'";
                cmd.CommandType = CommandType.Text;
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

        public static int deleteFolder(String folderPath)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "DELETE FROM folders WHERE local_path = '" + folderPath + "'";
            int rowsDeleted = cmd.ExecuteNonQuery();
            return rowsDeleted;
        }

        public static bool verifyFolderPathExists(String folderPath)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT local_path FROM folders WHERE local_path ='" + folderPath + "'";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                return true;
            }
            return false;
        }

        public static bool createGroup(String groupName)
        {
            try
            {
                OracleCommand cmd;

                cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO groups VALUES(default,'" + groupName + "','" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString() + "')";

                int rowsUpdated = cmd.ExecuteNonQuery();
                if(rowsUpdated > 0)
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

        public static bool verifyGroupNameUnique(String groupName)
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

        public static bool addGroupMember(int groupId, String accountEmail, String memberType)
        {
            try
            {
                OracleCommand cmd;

                cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO groupmembers VALUES(default," + groupId + ",'" + accountEmail + "','"+ memberType +"')";
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

        public static int getGroupId(String groupName)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "Select groupid FROM groups WHERE group_name ='" + groupName + "'";
                cmd.CommandType = CommandType.Text;
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

        public static Groups getGroupData(String accountEmail)
        {
            try
            {
                Groups groups = new Groups();
                groups.groupList = new List<Group>();

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "SELECT g.groupid,g.group_name, g.date_created,gm.member_type FROM groupmembers gm, groups g WHERE gm.groupid = g.groupid AND gm.accountemail = '" + accountEmail + "'";
                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Group group = new Group();
                    group.GroupId = dr.GetInt32(0);
                    group.GroupName = dr.GetOracleString(1).ToString();
                    group.DateCreated = dr.GetOracleString(2).ToString();
                    group.MemberType = dr.GetOracleString(3).ToString();
                    groups.groupList.Add(group);
                }
                return groups;
            }
            catch
            {
                return new Groups();
            }
        }

        public static GroupMembers getGroupMembers(int groupId)
        {
            try
            {
                GroupMembers groupMembers = new GroupMembers();
                groupMembers.groupMemberList = new List<GroupMember>();

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "SELECT groupid, accountemail, member_type FROM groupmembers WHERE groupid=" + groupId;
                cmd.CommandType = CommandType.Text;
                OracleDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    GroupMember groupMember = new GroupMember();
                    groupMember.groupId = dr.GetInt32(0);
                    groupMember.accountEmail = dr.GetOracleString(1).ToString();
                    groupMember.memberType = dr.GetOracleString(2).ToString();
                    groupMembers.groupMemberList.Add(groupMember);
                }
                return groupMembers;
            }
            catch
            {
                return new GroupMembers();
            }
        }

        public static bool deleteGroupMember(String memberEmail,int groupId)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "Delete from groupmembers WHERE accountemail='" + memberEmail + "' AND groupid = " + groupId;
                int rowsUpdated = cmd.ExecuteNonQuery();
                if(rowsUpdated > 0){
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }

        }

        /*
        public void WriteOfficeFile(OfficeHandler oh)
        {

            OracleCommand cmd = new OracleCommand();
            cmd.Connection = conn;

            int folder_id;
            int file_details_id;

            //Get current folder_id
            folder_id = getFolderId();

            //Get text content
            oh.extractContent();

            //Insert file details for file
            cmd.CommandText = "Insert into file_details VALUES (default,'" + oh.last_modified + "','" + oh.date_created + "'," + oh.fileSize + ",'" + oh.extension + "','" + oh.author + "','" + oh.title + "'," + oh.pages + "," + folder_id + ")";
            int rowsUpdated = cmd.ExecuteNonQuery();

            //get current file_details_id
            file_details_id = getFileDetailsId();

            int i = 1;
            foreach (String s in oh.content)
            {

                cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folder_id + "," + file_details_id + ")";
                cmd.ExecuteNonQuery();
                i++;
            }
            conn.Close();
        }

        public void writePdfFile(PdfHandler ph)
        {
            ph.extractContent();

            try
            {

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = conn;

                int folder_id;
                int file_details_id;

                //Get current folder_id
                folder_id = getFolderId();


                //Insert file details for file
                cmd.CommandText = "Insert into file_details VALUES (default,'" + ph.last_modified + "','" + ph.date_created + "'," + ph.fileSize + ",'" + ph.extension + "','" + ph.author + "','" + ph.title + "'," + ph.pages + "," + folder_id + ")";
                int rowsUpdated = cmd.ExecuteNonQuery();

                //get current file_details_id
                file_details_id = getFileDetailsId();

                int i = 1;
                foreach (String s in ph.content)
                {

                    cmd.CommandText = "Insert into content_data values(default,'" + s + "'," + i + "," + folder_id + "," + file_details_id + ")";
                    cmd.ExecuteNonQuery();
                    i++;
                }
                conn.Close();
            }
            catch(Exception e)
            {

            }

        }

        public void writeTextFile(TextHandler th)
        {
            try
            {

                OracleCommand cmd = new OracleCommand();
                cmd.Connection = conn;

                //Get current folder_id
                int folder_id = getFolderId();

                //Insert file details for file
                cmd.CommandText = "Insert into file_details VALUES (default,'" + th.last_modified + "','" + th.date_created + "'," + th.fileSize + ",'" + th.extension + "','" + th.author + "','" + th.title + "'," + th.pages + "," + folder_id + ")";
                int rowsUpdated = cmd.ExecuteNonQuery();

                //Get text content
                th.extractContent();


                //get current file_details_id
                int file_details_id = getFileDetailsId();


                //Insert file
                foreach(string s in th.content)
                {
                    cmd.CommandText = "Insert into content_data values(default,'" + s + "',0," + folder_id + "," + file_details_id + ")";
                    cmd.ExecuteNonQuery();
                }





                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        */
    }
}