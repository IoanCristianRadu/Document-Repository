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
                String path = Path.Combine(Server.MapPath("~/DocumentRepository/UploadedFiles"),
                    Session["Email"].ToString());
                Session["RootPath"] = path;
                FoldersAndFiles foldersAndFiles = new FoldersAndFiles();
                List<Folder> subfolderList;
                if (Session["FolderPath"] == null || Session["FolderPath"].ToString() == path)
                {
                    Session["FolderPath"] = path;

                    foldersAndFiles.FileList =
                        DocumentRepository.DBSingleton.ReadFileDetailsByEmail(Session["Email"].ToString());
                    foldersAndFiles.FileList.FileDetailsList.Sort();

                    int folderId = DocumentRepository.DBSingleton.GetFolderId(path);
                    subfolderList = DocumentRepository.DBSingleton.GetSubfolders(folderId);
                    Folders folders = new Folders {FolderList = subfolderList};

                    foldersAndFiles.FolderList = folders;
                    return View(foldersAndFiles);
                }
                else
                {
                    int folderId = DocumentRepository.DBSingleton.GetFolderId(Session["FolderPath"].ToString());
                    subfolderList = DocumentRepository.DBSingleton.GetSubfolders(folderId);
                    Folders folders = new Folders {FolderList = subfolderList};

                    foldersAndFiles.FileList = DocumentRepository.DBSingleton.ReadFileDetailsByFolderId(folderId);
                    foldersAndFiles.FileList.FileDetailsList.Sort();

                    foldersAndFiles.FolderList = folders;
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
            String sessionPath = Path.Combine(Server.MapPath("~/DocumentRepository/UploadedFiles"),
                Session["Email"].ToString());
            try
            {
                if (Session["FolderPath"] == null || Session["FolderPath"].ToString() == sessionPath)
                {
                    if (!Directory.Exists(sessionPath))
                    {
                        Directory.CreateDirectory(sessionPath);
                        DocumentRepository.DBSingleton.WriteFolderWeb(sessionPath, null, 1);
                    }

                    foreach (HttpPostedFileBase file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string filePath = sessionPath + "\\" + fileName;
                            file.SaveAs(filePath);
                            FileInfo fileInfo = new FileInfo(filePath);

                            int folderId = DocumentRepository.DBSingleton.GetFolderId(sessionPath);

                            var fileDetailsVector = DocumentRepository.DBSingleton.ReadFileDetailsByFolderId(folderId);

                            bool fileAlreadyExists = false;
                            foreach (var fileDetails in fileDetailsVector.FileDetailsList)
                            {
                                if (fileDetails.Title == fileName)
                                {
                                    fileAlreadyExists = true;
                                    break;
                                }
                            }

                            if (!fileAlreadyExists)
                            {
                                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(),
                                    folderId);
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
                            sessionPath = Session["FolderPath"].ToString();

                            string fileName = Path.GetFileName(file.FileName);
                            string filePath = sessionPath + "\\" + fileName;
                            file.SaveAs(filePath);
                            FileInfo fileInfo = new FileInfo(filePath);
                            int folderId = DocumentRepository.DBSingleton.GetFolderId(sessionPath);
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
            String path = Path.Combine(Server.MapPath("~/DocumentRepository/UploadedFiles"),
                Session["Email"].ToString());
            if (!string.IsNullOrEmpty(foldersAndFiles.CreateFolderPath))
            {
                if (Session["FolderPath"] == null || Session["FolderPath"].ToString() == path)
                {
                    if (!DocumentRepository.DBSingleton.VerifyFolderPathExists(path))
                    {
                        DocumentRepository.DBSingleton.WriteFolderWeb(path, null, 1);
                    }
                }
                else
                {
                    path = Session["FolderPath"].ToString();
                }

                String pathAndFolderName = path + "\\" + foldersAndFiles.CreateFolderPath;

                if (!DocumentRepository.DBSingleton.VerifyFolderPathExists(pathAndFolderName))
                {
                    DocumentRepository.DBSingleton.WriteFolderWeb(pathAndFolderName,
                        DocumentRepository.DBSingleton.GetFolderId(path),
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
            DocumentRepository.DBSingleton.DeleteFile(filePath);
            file.Delete();

            return RedirectToAction("Personal");
        }

        public ActionResult DeleteFolder(String folderPath)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(folderPath);
                DocumentRepository.DBSingleton.DeleteFolder(folderPath);
                directory.Delete(true);
            }
            catch
            {
                // ignored
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

        public ActionResult MoveFile(String path)
        {
            Session["Moving"] = "true";
            Session["PathToMove"] = path;
            Session["IsFile"] = "true";
            string[] splitter = path.Split('\\');
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
                    int folderId = DocumentRepository.DBSingleton.GetFolderId(Session["FolderPath"].ToString());
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

                    DocumentRepository.DBSingleton.DeleteFolder(folderPath);

                    WriteWholeFolderToDb(Session["FolderPath"].ToString() + "\\" + folderName);

                    DeleteFolder(folderPath);
                }

                return RedirectToAction("Personal");
            }
            catch
            {
                return RedirectToAction("Personal");
            }
        }

        public void
            WriteWholeFolderToDb(
                String folderPath) // 1 - files in folder, files in subfolders, 0 - only files in current folder
        {
            DirectoryInfo directory = new DirectoryInfo(folderPath);

            int parentFolderId = DocumentRepository.DBSingleton.GetFolderId(Session["FolderPath"].ToString());
            DocumentRepository.DBSingleton.WriteFolderWeb(folderPath, parentFolderId, 1);

            int currentFolderId = DocumentRepository.DBSingleton.GetFolderId(folderPath);
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), currentFolderId);
            }

            foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
            {
                String parentFolderPath = dir.Parent.FullName;
                int parentId = DocumentRepository.DBSingleton.GetFolderId(parentFolderPath);
                DocumentRepository.DBSingleton.WriteFolderWeb(parentFolderPath + "\\" + dir.Name, parentId, 1);
                int thisFolderId = DocumentRepository.DBSingleton.GetFolderId(parentFolderPath + "\\" + dir.Name);
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
                FoldersAndFiles foldersAndFiles = new FoldersAndFiles();
                String pathGroup = Path.Combine(Server.MapPath("~/DocumentRepository/Groups"),
                    Session["GroupName"].ToString());
                Session["RootPath"] = pathGroup;

                List<Folder> subfolderList;
                if (Session["GroupPath"] == null || Session["GroupPath"].ToString() == pathGroup)
                {
                    Session["GroupPath"] = pathGroup;

                    int folderId = DocumentRepository.DBSingleton.GetFolderId(pathGroup);
                    foldersAndFiles.FileList = DocumentRepository.DBSingleton.ReadFileDetailsByFolderId(folderId);
                    foldersAndFiles.FileList.FileDetailsList.Sort();

                    subfolderList = DocumentRepository.DBSingleton.GetSubfolders(folderId);
                    Folders folders = new Folders();
                    folders.FolderList = subfolderList;

                    foldersAndFiles.FolderList = folders;
                    return View(foldersAndFiles);
                }
                else
                {
                    int folderId = DocumentRepository.DBSingleton.GetFolderId(Session["GroupPath"].ToString());
                    subfolderList = DocumentRepository.DBSingleton.GetSubfolders(folderId);
                    Folders folders = new Folders {FolderList = subfolderList};

                    foldersAndFiles.FileList = DocumentRepository.DBSingleton.ReadFileDetailsByFolderId(folderId);
                    foldersAndFiles.FileList.FileDetailsList.Sort();

                    foldersAndFiles.FolderList = folders;
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
                if (Session["GroupPath"] == null || Session["GroupPath"].ToString() == path)
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        DocumentRepository.DBSingleton.WriteFolderWeb(path, null, 1);
                    }

                    foreach (HttpPostedFileBase file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string filePath = path + "\\" + fileName;
                            file.SaveAs(filePath);
                            FileInfo fileInfo = new FileInfo(filePath);

                            int folderId = DocumentRepository.DBSingleton.GetFolderId(path);
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

                            string fileName = Path.GetFileName(file.FileName);
                            string filePath = path + "\\" + fileName;
                            file.SaveAs(filePath);
                            FileInfo fileInfo = new FileInfo(filePath);
                            int folderId = DocumentRepository.DBSingleton.GetFolderId(path);
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
            if (!string.IsNullOrEmpty(foldersAndFiles.CreateFolderPath))
            {
                if (Session["GroupPath"] == null || Session["GroupPath"].ToString() == path)
                {
                    if (!DocumentRepository.DBSingleton.VerifyFolderPathExists(path))
                    {
                        DocumentRepository.DBSingleton.WriteFolderWeb(path, null, 1);
                    }
                }
                else
                {
                    path = Session["GroupPath"].ToString();
                }

                String pathAndFolderName = path + "\\" + foldersAndFiles.CreateFolderPath;
                DocumentRepository.DBSingleton.WriteFolderWeb(pathAndFolderName,
                    DocumentRepository.DBSingleton.GetFolderId(path),
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
            DocumentRepository.DBSingleton.DeleteFile(filePath);
            file.Delete();

            return RedirectToAction("GroupContent");
        }

        public ActionResult GroupDeleteFolder(String folderPath)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(folderPath);
                DocumentRepository.DBSingleton.DeleteFolder(folderPath);
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
                int folderId = DocumentRepository.DBSingleton.GetFolderId(Session["GroupPath"].ToString());
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

                DocumentRepository.DBSingleton.DeleteFolder(folderPath);

                GroupWriteWholeFolderToDb(Session["GroupPath"].ToString() + "\\" + folderName);

                DeleteFolder(folderPath);
            }

            return RedirectToAction("GroupContent");
        }

        public void
            GroupWriteWholeFolderToDb(
                String folderPath) // 1 - files in folder, files in subfolders, 0 - only files in current folder
        {
            DirectoryInfo directory = new DirectoryInfo(folderPath);

            int parentFolderId = DocumentRepository.DBSingleton.GetFolderId(Session["GroupPath"].ToString());
            DocumentRepository.DBSingleton.WriteFolderWeb(folderPath, parentFolderId, 1);

            int currentFolderId = DocumentRepository.DBSingleton.GetFolderId(folderPath);
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), currentFolderId);
            }

            foreach (var dir in directory.GetDirectories("*", SearchOption.AllDirectories))
            {
                if (dir.Parent != null)
                {
                    String parentFolderPath = dir.Parent.FullName;
                    int parentId = DocumentRepository.DBSingleton.GetFolderId(parentFolderPath);
                    DocumentRepository.DBSingleton.WriteFolderWeb(parentFolderPath + "\\" + dir.Name, parentId, 1);
                    int thisFolderId = DocumentRepository.DBSingleton.GetFolderId(parentFolderPath + "\\" + dir.Name);
                    foreach (FileInfo fileInfo in dir.GetFiles())
                    {
                        DocumentRepository.DBSingleton.WriteWeb(fileInfo, Session["Email"].ToString(), thisFolderId);
                    }
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
            int groupId = DocumentRepository.DBSingleton.GetGroupId(groupName);

            if (foldersAndFiles.AddEmail != null)
            {
                if (foldersAndFiles.AddEmail != "")
                {
                    DocumentRepository.DBSingleton.AddGroupMember(groupId, foldersAndFiles.AddEmail, "member");
                    return RedirectToAction("GroupContent");
                }
            }

            return RedirectToAction("GroupContent");
        }

        public ActionResult ViewMemberList()
        {
            try
            {
                String rootPath = Session["RootPath"].ToString();
                string[] splitter = rootPath.Split('\\');
                String groupName = splitter[splitter.Length - 1];
                int groupId = DocumentRepository.DBSingleton.GetGroupId(groupName);

                var groupMembers = DocumentRepository.DBSingleton.GetGroupMembers(groupId);
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
            search.RecordList = DocumentRepository
                .DBSingleton
                .SearchContent(
                    search.Content,
                    search.PagesMin,
                    search.PagesMax,
                    search.LastModifiedMin,
                    search.LastModifiedMax,
                    search.CreationDateMin,
                    search.CreationDateMax,
                    search.FileSizeMin,
                    search.FileSizeMax,
                    search.Extension,
                    search.Title);
            String rootPath = Session["RootPath"].ToString();
            string[] rootPathSplitter = rootPath.Split('\\');

            foreach (DocumentRepositoryOnline.DocumentRepository.Record record in search.RecordList)
            {
                string[] recordPathSplitter = record.LocalPath.Split('\\');
                int ok = 1;
                if (rootPathSplitter.Length <= recordPathSplitter.Length)
                {
                    for (int i = 0; i < rootPathSplitter.Length; i++)
                    {
                        if (recordPathSplitter[i] != rootPathSplitter[i])
                        {
                            ok = 0;
                            break;
                        }
                    }

                    if (ok == 0)
                    {
                        record.IsCorrectPath = false;
                    }
                }
            }

            return View(search);
        }

        public ActionResult DeleteGroupMember(String memberEmail)
        {
            String rootPath = Session["RootPath"].ToString();

            string[] splitter = rootPath.Split('\\');
            String groupName = splitter[splitter.Length - 1];
            int groupId = DocumentRepository.DBSingleton.GetGroupId(groupName);

            DocumentRepository.DBSingleton.DeleteGroupMember(memberEmail, groupId);
            return RedirectToAction("ViewMemberList");
        }
    }
}