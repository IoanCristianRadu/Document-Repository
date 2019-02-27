using DocumentRepositoryOnline.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace DocumentRepositoryOnline.Controllers
{
    public class StorageController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Personal()
        {
            try
            {
                String path = Path.Combine(Server.MapPath("~/DocumentRepository/UploadedFiles"), Session["Email"].ToString());
                Session["RootPath"] = path;
                List<Folder> subfolderList = new List<Folder>();
                FoldersAndFiles foldersAndFiles = new FoldersAndFiles();
                if (Session["FolderPath"] == null || Session["FolderPath"].ToString() == path)
                {
                    Session["FolderPath"] = path;

                    foldersAndFiles.fileList = DocumentRepository.DBSingleton.readFileDetailsByEmail(Session["Email"].ToString());
                    foldersAndFiles.fileList.FileDetailsList.Sort();

                    int folderId = DocumentRepository.DBSingleton.getFolderId(path);
                    subfolderList = DocumentRepository.DBSingleton.getSubfolders(folderId);
                    Folders folders = new Folders();
                    folders.folderList = subfolderList;

                    foldersAndFiles.folderList = folders;
                    return View(foldersAndFiles);
                }
                else
                {
                    int folderId = DocumentRepository.DBSingleton.getFolderId(Session["FolderPath"].ToString());
                    subfolderList = DocumentRepository.DBSingleton.getSubfolders(folderId);
                    Folders folders = new Folders();
                    folders.folderList = subfolderList;

                    foldersAndFiles.fileList = DocumentRepository.DBSingleton.readFileDetailsByFolderId(folderId);
                    foldersAndFiles.fileList.FileDetailsList.Sort();

                    foldersAndFiles.folderList = folders;
                    return View(foldersAndFiles);
                }

            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }

        public ActionResult Groups()
        {
            try
            {
                return View();
            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }

        [HttpPost]
        public ActionResult Personal(List<HttpPostedFileBase> files)
        {
            String path = Path.Combine(Server.MapPath("~/DocumentRepository/UploadedFiles"), Session["Email"].ToString());
            try
            {
                if (Session["FolderPath"] == null || Session["FolderPath"].ToString() == path) //Pentru folder root
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        DocumentRepository.DBSingleton.writeFolderWeb(path, null, 1);
                    }
                    foreach (HttpPostedFileBase file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            string _FileName = Path.GetFileName(file.FileName);
                            string _path = path + "\\" + _FileName;
                            file.SaveAs(_path);
                            FileInfo fileInfo = new FileInfo(_path);

                            int folderId = DocumentRepository.DBSingleton.getFolderId(path);

                            FileDetailsVector fileDetailsVector = new FileDetailsVector();
                            fileDetailsVector = DocumentRepository.DBSingleton.readFileDetailsByFolderId(folderId);

                            bool fileAlreadyExists = false;
                            for (int i = 0; i < fileDetailsVector.FileDetailsList.Count; i++)
                            {
                                if (fileDetailsVector.FileDetailsList[i].Title == _FileName)
                                {
                                    fileAlreadyExists = true;
                                    break;
                                }
                            }
                            if(!fileAlreadyExists)
                            {
                                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), folderId);
                                ViewBag.Message = "File Uploaded Successfully!!";
                            }
                        }
                        else
                        {
                            ViewBag.Message = "No file selected!";
                            return RedirectToAction("Personal");
                        }
                    }
                    return RedirectToAction("Personal");
                }
                else
                {
                    foreach (HttpPostedFileBase file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            path = Session["FolderPath"].ToString();

                            string _FileName = Path.GetFileName(file.FileName);
                            string _path = path + "\\" + _FileName;
                            file.SaveAs(_path);
                            FileInfo fileInfo = new FileInfo(_path);
                            int folderId = DocumentRepository.DBSingleton.getFolderId(path);
                            DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), folderId);
                        }
                    }
                }

                if (files.Count == 1)
                {
                    ViewBag.Message = "File Uploaded Successfully!!";
                }
                else if (files.Count > 1)
                {
                    ViewBag.Message = "Files Uploaded Successfully!!";
                }
                return RedirectToAction("Personal");
            }
            catch
            {
                ViewBag.Message = "File upload failed!!";
                return RedirectToAction("Personal");
            }
        }

        public ActionResult Download(String path)
        {
            string[] split = path.Split('\\');

            return File(path, MediaTypeNames.Application.Octet, split[split.Length - 1]);
        }

        public ActionResult CreateFolder(FoldersAndFiles foldersAndFiles)
        {
            String path = Path.Combine(Server.MapPath("~/DocumentRepository/UploadedFiles"), Session["Email"].ToString());
            if (foldersAndFiles.createFolderPath != "" && foldersAndFiles.createFolderPath != null)
            {
                if (Session["FolderPath"] == null || Session["FolderPath"].ToString() == path)
                {
                    if (!DocumentRepository.DBSingleton.verifyFolderPathExists(path))
                    {
                        DocumentRepository.DBSingleton.writeFolderWeb(path, null, 1);
                    }
                }
                else
                {
                    path = Session["FolderPath"].ToString();
                }

                String pathAndFolderName = path + "\\" + foldersAndFiles.createFolderPath;

                if (!DocumentRepository.DBSingleton.verifyFolderPathExists(pathAndFolderName))
                {
                    DocumentRepository.DBSingleton.writeFolderWeb(pathAndFolderName,
                                            DocumentRepository.DBSingleton.getFolderId(path),
                                             1);
                    Directory.CreateDirectory(pathAndFolderName);
                }
            }
            return RedirectToAction("Personal");
        }

        public ActionResult OpenFolder(String folderPath)
        {
            Session["FolderPath"] = folderPath;
            return RedirectToAction("Personal");
        }

        public ActionResult BtnBack()
        {
            String folderPath = Session["FolderPath"].ToString();
            string[] splitter = folderPath.Split('\\');

            folderPath = splitter[splitter.Length - 1];
            string[] _splitter = folderPath.Split('@');
            if (_splitter.Length == 1)
            {
                folderPath = "";
                for (int i = 0; i <= splitter.Length - 3; i++)
                {
                    folderPath = folderPath + splitter[i] + "\\";
                }
                folderPath = folderPath + splitter[splitter.Length - 2];
                Session["FolderPath"] = folderPath;
            }
            return RedirectToAction("Personal");
        }

        public ActionResult DeleteFile(String filePath)
        {
            FileInfo file = new FileInfo(filePath);
            DocumentRepository.DBSingleton.deleteFile(filePath);
            file.Delete();

            return RedirectToAction("Personal");
        }

        public ActionResult DeleteFolder(String folderPath)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(folderPath);
                DocumentRepository.DBSingleton.deleteFolder(folderPath);
                directory.Delete(true);
            }
            catch
            {

            }

            return RedirectToAction("Personal");
        }

        public ActionResult MoveFolder(String folderPath)
        {
            Session["Moving"] = "true";
            Session["PathToMove"] = folderPath;
            Session["IsFile"] = "false";
            string[] splitter = folderPath.Split('\\');
            Session["MovingThingName"] = splitter[splitter.Length - 1];
            return RedirectToAction("Personal");
        }
        public ActionResult MoveFile(String Path)
        {
            Session["Moving"] = "true";
            Session["PathToMove"] = Path;
            Session["IsFile"] = "true";
            string[] splitter = Path.Split('\\');
            Session["MovingThingName"] = splitter[splitter.Length - 1];
            return RedirectToAction("Personal");
        }

        public ActionResult MoveHere()
        {
            Session["Moving"] = "false";
            try
            {
                if (Session["IsFile"].ToString() == "true")
                {
                    String filePath = Session["PathToMove"].ToString();
                    FileInfo fileInfo = new FileInfo(filePath);
                    string[] splitter = filePath.Split('\\');
                    String fileName = splitter[splitter.Length - 1];
                    fileInfo.MoveTo(Session["FolderPath"].ToString() + "\\" + fileName);
                    int folderId = DocumentRepository.DBSingleton.getFolderId(Session["FolderPath"].ToString());
                    DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), folderId);

                    DeleteFile(filePath);
                }
                else
                {
                    String folderPath = Session["PathToMove"].ToString();

                    DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

                    string[] splitter = folderPath.Split('\\');
                    String folderName = splitter[splitter.Length - 1];
                    directoryInfo.MoveTo(Session["FolderPath"].ToString() + "\\" + folderName);

                    DocumentRepository.DBSingleton.deleteFolder(folderPath);

                    WriteWholeFolderToDB(Session["FolderPath"].ToString() + "\\" + folderName);

                    DeleteFolder(folderPath);
                }
                return RedirectToAction("Personal");
            }
            catch
            {
                return RedirectToAction("Personal");
            }
        }

        public void WriteWholeFolderToDB(String folderPath) // 1 - files in folder, files in subfolders, 0 - only files in current folder
        {
            DirectoryInfo directory = new DirectoryInfo(folderPath);

            int parentFolderId = DocumentRepository.DBSingleton.getFolderId(Session["FolderPath"].ToString());
            DocumentRepository.DBSingleton.writeFolderWeb(folderPath, parentFolderId, 1);

            int currentFolderId = DocumentRepository.DBSingleton.getFolderId(folderPath);
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), currentFolderId);
            }

            foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
            {
                String parentFolderPath = dir.Parent.FullName;
                int parentId = DocumentRepository.DBSingleton.getFolderId(parentFolderPath);
                DocumentRepository.DBSingleton.writeFolderWeb(parentFolderPath + "\\" + dir.Name, parentId, 1);
                int thisFolderId = DocumentRepository.DBSingleton.getFolderId(parentFolderPath + "\\" + dir.Name);
                foreach (FileInfo fileInfo in dir.GetFiles())
                {
                    DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), thisFolderId);
                }
            }
        }

        public ActionResult CancelMoving()
        {
            Session["Moving"] = "false";
            return RedirectToAction("Personal");
        }


        // -------------------------------------------> Groups <-------------------------------------------

        public ActionResult GoToGroup(String groupName)
        {
            Session["GroupName"] = groupName;
            Session["GroupPath"] = null;
            return RedirectToAction("GroupContent");
        }
        [HttpGet]
        public ActionResult GroupContent()
        {
            try
            {
                List<Folder> subfolderList = new List<Folder>();
                FoldersAndFiles foldersAndFiles = new FoldersAndFiles();
                String pathGroup = Path.Combine(Server.MapPath("~/DocumentRepository/Groups"), Session["GroupName"].ToString());
                Session["RootPath"] = pathGroup;

                if (Session["GroupPath"] == null || Session["GroupPath"].ToString() == pathGroup)
                {
                    Session["GroupPath"] = pathGroup;

                    int folderId = DocumentRepository.DBSingleton.getFolderId(pathGroup);
                    foldersAndFiles.fileList = DocumentRepository.DBSingleton.readFileDetailsByFolderId(folderId);
                    foldersAndFiles.fileList.FileDetailsList.Sort();

                    subfolderList = DocumentRepository.DBSingleton.getSubfolders(folderId);
                    Folders folders = new Folders();
                    folders.folderList = subfolderList;

                    foldersAndFiles.folderList = folders;
                    return View(foldersAndFiles);
                }
                else
                {
                    int folderId = DocumentRepository.DBSingleton.getFolderId(Session["GroupPath"].ToString());
                    subfolderList = DocumentRepository.DBSingleton.getSubfolders(folderId);
                    Folders folders = new Folders();
                    folders.folderList = subfolderList;

                    foldersAndFiles.fileList = DocumentRepository.DBSingleton.readFileDetailsByFolderId(folderId);
                    foldersAndFiles.fileList.FileDetailsList.Sort();

                    foldersAndFiles.folderList = folders;
                    return View(foldersAndFiles);
                }
            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }
        [HttpPost]
        public ActionResult GroupContent(List<HttpPostedFileBase> files)
        {
            String path = Session["RootPath"].ToString();
            try
            {
                if (Session["GroupPath"] == null || Session["GroupPath"].ToString() == path) //Pentru folder root
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        DocumentRepository.DBSingleton.writeFolderWeb(path, null, 1);
                    }
                    foreach (HttpPostedFileBase file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            string _FileName = Path.GetFileName(file.FileName);
                            string _path = path + "\\" + _FileName;
                            file.SaveAs(_path);
                            FileInfo fileInfo = new FileInfo(_path);

                            int folderId = DocumentRepository.DBSingleton.getFolderId(path);
                            DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), folderId);

                            ViewBag.Message = "File Uploaded Successfully!!";
                        }
                        else
                        {
                            ViewBag.Message = "No file selected!";
                            return RedirectToAction("GroupContent");
                        }
                    }
                    return RedirectToAction("GroupContent");
                }
                else 
                {
                    foreach (HttpPostedFileBase file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            path = Session["GroupPath"].ToString();

                            string _FileName = Path.GetFileName(file.FileName);
                            string _path = path + "\\" + _FileName;
                            file.SaveAs(_path);
                            FileInfo fileInfo = new FileInfo(_path);
                            int folderId = DocumentRepository.DBSingleton.getFolderId(path);
                            DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), folderId);
                        }
                    }
                }

                if (files.Count == 1)
                {
                    ViewBag.Message = "File Uploaded Successfully!!";
                }
                else if (files.Count > 1)
                {
                    ViewBag.Message = "Files Uploaded Successfully!!";
                }
                return RedirectToAction("GroupContent");
            }
            catch
            {
                ViewBag.Message = "File upload failed!!";
                return RedirectToAction("GroupContent");
            }
        }

        public ActionResult GroupDownload(String path)
        {
            string[] split = path.Split('\\');

            return File(path, MediaTypeNames.Application.Octet, split[split.Length - 1]);
        }

        public ActionResult GroupCreateFolder(FoldersAndFiles foldersAndFiles)
        {
            String path = Session["GroupPath"].ToString();
            if (foldersAndFiles.createFolderPath != "" && foldersAndFiles.createFolderPath != null)
            {
                if (Session["GroupPath"] == null || Session["GroupPath"].ToString() == path)
                {
                    if (!DocumentRepository.DBSingleton.verifyFolderPathExists(path))
                    {
                        DocumentRepository.DBSingleton.writeFolderWeb(path, null, 1);
                    }
                }
                else
                {
                    path = Session["GroupPath"].ToString();
                }

                String pathAndFolderName = path + "\\" + foldersAndFiles.createFolderPath;
                DocumentRepository.DBSingleton.writeFolderWeb(pathAndFolderName,
                                                            DocumentRepository.DBSingleton.getFolderId(path),
                                                             1);
                Directory.CreateDirectory(pathAndFolderName);
            }
            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupOpenFolder(String folderPath)
        {
            Session["GroupPath"] = folderPath;
            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupBtnBack()
        {
            String folderPath = Session["GroupPath"].ToString();
            string[] splitter = folderPath.Split('\\');

            folderPath = splitter[splitter.Length - 1];
            string[] _splitter = folderPath.Split('@');
            if (_splitter.Length == 1)
            {
                folderPath = "";
                for (int i = 0; i <= splitter.Length - 3; i++)
                {
                    folderPath = folderPath + splitter[i] + "\\";
                }
                folderPath = folderPath + splitter[splitter.Length - 2];
                Session["GroupPath"] = folderPath;
            }
            return RedirectToAction("GroupContent");
        }

        public ActionResult backToGroupRoot()
        {
            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupDeleteFile(String filePath)
        {
            FileInfo file = new FileInfo(filePath);
            DocumentRepository.DBSingleton.deleteFile(filePath);
            file.Delete();

            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupDeleteFolder(String folderPath)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(folderPath);
                DocumentRepository.DBSingleton.deleteFolder(folderPath);
                directory.Delete(true);
            }
            catch
            {

            }

            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupMoveFolder(String folderPath)
        {
            Session["GroupMoving"] = "true";
            Session["GroupPathToMove"] = folderPath;
            Session["GroupIsFile"] = "false";
            string[] splitter = folderPath.Split('\\');
            Session["GroupMovingThingName"] = splitter[splitter.Length - 1];
            return RedirectToAction("GroupContent");
        }
        public ActionResult GroupMoveFile(String Path)
        {
            Session["GroupMoving"] = "true";
            Session["GroupPathToMove"] = Path;
            Session["GroupIsFile"] = "true";
            string[] splitter = Path.Split('\\');
            Session["GroupMovingThingName"] = splitter[splitter.Length - 1];
            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupMoveHere()
        {
            Session["GroupMoving"] = "false";
            if (Session["GroupIsFile"].ToString() == "true")
            {
                String filePath = Session["GroupPathToMove"].ToString();
                FileInfo fileInfo = new FileInfo(filePath);
                string[] splitter = filePath.Split('\\');
                String fileName = splitter[splitter.Length - 1];
                fileInfo.MoveTo(Session["GroupPath"].ToString() + "\\" + fileName);
                int folderId = DocumentRepository.DBSingleton.getFolderId(Session["GroupPath"].ToString());
                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), folderId);

                DeleteFile(filePath);
            }
            else
            {
                String folderPath = Session["GroupPathToMove"].ToString();

                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

                string[] splitter = folderPath.Split('\\');
                String folderName = splitter[splitter.Length - 1];
                directoryInfo.MoveTo(Session["GroupPath"].ToString() + "\\" + folderName);

                DocumentRepository.DBSingleton.deleteFolder(folderPath);

                GroupWriteWholeFolderToDB(Session["GroupPath"].ToString() + "\\" + folderName);

                DeleteFolder(folderPath);
            }
            return RedirectToAction("GroupContent");
        }

        public void GroupWriteWholeFolderToDB(String folderPath) // 1 - files in folder, files in subfolders, 0 - only files in current folder
        {
            DirectoryInfo directory = new DirectoryInfo(folderPath);

            int parentFolderId = DocumentRepository.DBSingleton.getFolderId(Session["GroupPath"].ToString());
            DocumentRepository.DBSingleton.writeFolderWeb(folderPath, parentFolderId, 1);

            int currentFolderId = DocumentRepository.DBSingleton.getFolderId(folderPath);
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), currentFolderId);
            }
            foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
            {
                String parentFolderPath = dir.Parent.FullName;
                int parentId = DocumentRepository.DBSingleton.getFolderId(parentFolderPath);
                DocumentRepository.DBSingleton.writeFolderWeb(parentFolderPath + "\\" + dir.Name, parentId, 1);
                int thisFolderId = DocumentRepository.DBSingleton.getFolderId(parentFolderPath + "\\" + dir.Name);
                foreach (FileInfo fileInfo in dir.GetFiles())
                {
                    DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), thisFolderId);
                }
            }
        }

        public ActionResult GroupCancelMoving()
        {
            Session["GroupMoving"] = "false";
            return RedirectToAction("GroupContent");
        }

        [HttpPost]
        public ActionResult GroupAddMember(FoldersAndFiles foldersAndFiles)
        {
            String rootPath = Session["RootPath"].ToString();

            string[] splitter = rootPath.Split('\\');
            String groupName = splitter[splitter.Length - 1];
            int groupId = DocumentRepository.DBSingleton.getGroupId(groupName);

            if (foldersAndFiles.addEmail != null)
            {
                if (foldersAndFiles.addEmail != "")
                {
                    DocumentRepository.DBSingleton.addGroupMember(groupId, foldersAndFiles.addEmail, "member");
                    return RedirectToAction("GroupContent");
                }
            }
            return RedirectToAction("GroupContent");
        }

        public ActionResult ViewMemberList()
        {
            try
            {
                GroupMembers groupMembers = new GroupMembers();

                String rootPath = Session["RootPath"].ToString();
                string[] splitter = rootPath.Split('\\');
                String groupName = splitter[splitter.Length - 1];
                int groupId = DocumentRepository.DBSingleton.getGroupId(groupName);

                groupMembers = DocumentRepository.DBSingleton.getGroupMembers(groupId);
                return View(groupMembers);
            }
            catch
            {
                return Redirect("/Home/Index");
            }

        }

        [HttpGet]
        public ActionResult SearchGroup()
        {
            try
            {
                Search search = new Search();
                return View(search);
            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }

        [HttpPost]
        public ActionResult SearchGroup(Search search)
        {
            search.recordList = DocumentRepository
                                            .DBSingleton
                                              .SearchContent(
                                                 search.content,
                                                 search.pagesMin,
                                                 search.pagesMax,
                                                 search.lastModifiedMin,
                                                 search.lastModifiedMax,
                                                 search.creationDateMin,
                                                 search.creationDateMax,
                                                 search.fileSizeMin,
                                                 search.fileSizeMax,
                                                 search.extension,
                                                 search.title);
            String rootPath = Session["RootPath"].ToString();
            string[] rootPathSplitter = rootPath.Split('\\');

            foreach (DocumentRepositoryOnline.DocumentRepository.Record record in search.recordList)
            {
                string[] recordPathSplitter = record.Local_path.Split('\\');
                int ok = 1;
                if (rootPathSplitter.Length <= recordPathSplitter.Length)
                {
                    for (int i = 0; i < rootPathSplitter.Length; i++)
                    {
                        if (!(recordPathSplitter[i] == rootPathSplitter[i]))
                        {
                            ok = 0;
                            break;
                        }
                    }
                    if (ok == 0)
                    {
                        record.isCorrectPath = false;
                    }
                }

            }
            return View(search);
        }

        public ActionResult deleteGroupMember(String memberEmail)
        {
            String rootPath = Session["RootPath"].ToString();

            string[] splitter = rootPath.Split('\\');
            String groupName = splitter[splitter.Length - 1];
            int groupId = DocumentRepository.DBSingleton.getGroupId(groupName);

            DocumentRepository.DBSingleton.deleteGroupMember(memberEmail, groupId);
            return RedirectToAction("ViewMemberList");
        }
    }
}