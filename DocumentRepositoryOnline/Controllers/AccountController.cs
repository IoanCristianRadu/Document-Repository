using DocumentRepositoryOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.IO;

namespace DocumentRepositoryOnline.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Register()
        {
            try
            {
                var account = new Account();
                return View(account);
            }
            catch
            {
                return Redirect("/Home/Index");
            }

        }

        [HttpPost]
        public ActionResult Register(Account account)
        {
            if (account.Password == account.RetypePassword)
            {
                bool accountExists = DocumentRepository.DBSingleton.verifyAccountIsUnique(account.Email);
                if (!accountExists)
                {
                    DocumentRepository.DBSingleton.register(account.Email, account.Password, DateTime.Now.Day.ToString() + "/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString(), "standard");
                    Session["Email"] = account.Email;
                    return RedirectToAction("/Details");
                }
            }
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            try
            {
                var account = new Account();
                return View(account);
            }
            catch
            {
                return Redirect("/Home/Index");
            }

        }

        [HttpPost]
        public ActionResult Login(Account account)
        {
            bool accountExists = DocumentRepository.DBSingleton.verifyAccountIsUnique(account.Email);
            if (accountExists)
            {
                if (DocumentRepository.DBSingleton.verifyAccountPassword(account.Email, account.Password) == 1)
                {
                    Session["Email"] = account.Email;
                    return Redirect("/Storage/Personal");
                }
            }
            return RedirectToAction("Login");
        }

        public ActionResult Details()
        {
            try
            {
                if (Session["Email"] != null)
                {
                    Account account = new Account();
                    account = DocumentRepository.DBSingleton.getAccountByEmail(Session["Email"].ToString());
                    if (account.FirstName == "null")
                    {
                        account.FirstName = "-";
                    }
                    if (account.LastName == "null")
                    {
                        account.LastName = "-";
                    }
                    if (account.Location == "null")
                    {
                        account.Location = "-";
                    }
                    return View(account);
                }
                else
                {
                    return RedirectToAction("Login");
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("Login");
            }

        }

        public ActionResult Edit()
        {
            try
            {
                Account account = DocumentRepository.DBSingleton.getAccountByEmail(Session["Email"].ToString());
                if (account.FirstName == "null")
                {
                    account.FirstName = "";
                }
                if (account.LastName == "null")
                {
                    account.LastName = "";
                }
                if (account.Location == "null")
                {
                    account.Location = "";
                }
                return View(account);
            }
            catch
            {
                return Redirect("/Home/Index");
            }

        }

        [HttpPost]
        public ActionResult Edit(Account account)
        {
            if (account.Password == account.RetypePassword)
            {
                if (account.Password != account.RetypePassword)
                {
                    account.Password = "null";
                }
                int rowsUpdated = DocumentRepository.DBSingleton.editAccount(Session["Email"].ToString(), account.Email, account.Password, account.FirstName, account.LastName, account.Location);
                if (rowsUpdated >= 1)
                {
                    Session["Email"] = account.Email;
                    return RedirectToAction("Details");
                }
                else
                {
                    return View();
                }
            }
            return View();
        }

        public ActionResult Groups()
        {
            try
            {
                Groups groups = new Groups();
                groups = DocumentRepository.DBSingleton.getGroupData(Session["Email"].ToString());

                return View(groups);
            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }

        [HttpGet]
        public ActionResult CreateGroup()
        {
            try
            {
                Group group = new Group();
                return View(group);
            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }

        [HttpPost]
        public ActionResult CreateGroup(Groups group)
        {
            if (DocumentRepository.DBSingleton.verifyGroupNameUnique(group.groupName))
            {
                DocumentRepository.DBSingleton.createGroup(group.groupName);
                int groupId = DocumentRepository.DBSingleton.getGroupId(group.groupName);
                DocumentRepository.DBSingleton.addGroupMember(groupId, Session["Email"].ToString(), "admin");

                String path = Path.Combine(Server.MapPath("~/DocumentRepository"), "Groups");
                if (DocumentRepository.DBSingleton.getFolderId(path) == 0)
                {
                    DocumentRepository.DBSingleton.writeFolderWeb(path, null, 1);
                }
                String pathAndFolderName = path + "\\" + group.groupName;
                DocumentRepository.DBSingleton.writeFolderWeb(pathAndFolderName,
                                                            DocumentRepository.DBSingleton.getFolderId(path),
                                                             1);
                Directory.CreateDirectory(pathAndFolderName);
            }
            return RedirectToAction("Groups");
        }

        public ActionResult CreateGroupPage()
        {
            Group group = new Group();
            return View(group);
        }

    }
}