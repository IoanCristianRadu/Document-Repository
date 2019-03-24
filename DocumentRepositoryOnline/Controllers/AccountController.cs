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
            if (account.Password.Equals(account.RetypePassword))
            {
                bool accountExists = DocumentRepository.DbSingleton.VerifyAccountIsUnique(account.Email);
                if (!accountExists)
                {
                    CreateAccount(account);
                    return RedirectToAction("/Details");
                }
            }
            return View();
        }

        private void CreateAccount(Account account)
        {
            DocumentRepository.DbSingleton.Register(account.Email, account.Password,
                DateTime.Now.Day.ToString() + "/" + DateTime.Now.Month.ToString() + "/" +
                DateTime.Now.Year.ToString(), "standard");
            Session["Email"] = account.Email;
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
            bool accountExists = DocumentRepository.DbSingleton.VerifyAccountIsUnique(account.Email);
            if (accountExists)
            {
                bool correctPassword = DocumentRepository.DbSingleton.VerifyAccountPassword(account.Email, account.Password) == 1;
                if (correctPassword)
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
                    var account = FillAccountFieldsWithMinus();
                    return View(account);
                }
                else
                {
                    return RedirectToAction("Login");
                }
            }
            catch
            {
                return RedirectToAction("Login");
            }
        }

        private Account FillAccountFieldsWithMinus()
        {
            var account = DocumentRepository.DbSingleton.GetAccountByEmail(Session["Email"].ToString());
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
            return account;
        }

        public ActionResult Edit()
        {
            try
            {
                var account = FillAccountFieldsWithEmptyString();

                return View(account);
            }
            catch
            {
                return Redirect("/Home/Index");
            }
        }

        private Account FillAccountFieldsWithEmptyString()
        {
            Account account = DocumentRepository.DbSingleton.GetAccountByEmail(Session["Email"].ToString());
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

            return account;
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

                int rowsUpdated = DocumentRepository.DbSingleton.EditAccount(Session["Email"].ToString(), account.Email,
                    account.Password, account.FirstName, account.LastName, account.Location);
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
                var groups = DocumentRepository.DbSingleton.GetGroupData(Session["Email"].ToString());
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
            if (DocumentRepository.DbSingleton.VerifyGroupNameUnique(group.GroupName))
            {
                DocumentRepository.DbSingleton.CreateGroup(group.GroupName);
                int groupId = DocumentRepository.DbSingleton.GetGroupId(group.GroupName);
                DocumentRepository.DbSingleton.AddGroupMember(groupId, Session["Email"].ToString(), "admin");

                String path = Path.Combine(Server.MapPath("~/DocumentRepository"), "Groups");
                if (DocumentRepository.DbSingleton.GetFolderId(path) == 0)
                {
                    DocumentRepository.DbSingleton.WriteWebFolder(path, null, 1);
                }

                String pathAndFolderName = path + "\\" + group.GroupName;
                DocumentRepository.DbSingleton.WriteWebFolder(pathAndFolderName,
                    DocumentRepository.DbSingleton.GetFolderId(path),
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