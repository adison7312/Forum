using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebApplication17.Models;

namespace WebApplication17.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
            private set { _signInManager = value; }
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess
                    ? "Your password has been changed."
                    : message == ManageMessageId.SetPasswordSuccess
                        ? "Your password has been set."
                        : message == ManageMessageId.SetTwoFactorSuccess
                            ? "Your two-factor authentication provider has been set."
                            : message == ManageMessageId.Error
                                ? "An error has occurred."
                                : message == ManageMessageId.AddPhoneSuccess
                                    ? "Your phone number was added."
                                    : message == ManageMessageId.RemovePhoneSuccess
                                        ? "Your phone number was removed."
                                        : "";

            var userId = User.Identity.GetUserId();
            if (UserManager.FindById(userId).Avatar == null)
            {
                ViewBag.Error = "Obrazek jest nullem";
                return View("Error");
            }

            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                Avatar = UserManager.FindById(userId).Avatar
            };

            return View(model);
        }


        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result =
                await
                    UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                        new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                    await SignInManager.SignInAsync(user, false, false);
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
                await SignInManager.SignInAsync(user, false, false);
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
                await SignInManager.SignInAsync(user, false, false);
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null
                ? View("Error")
                : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var result =
                await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                    await SignInManager.SignInAsync(user, false, false);
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
                await SignInManager.SignInAsync(user, false, false);
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var result =
                await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                    await SignInManager.SignInAsync(user, false, false);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                        await SignInManager.SignInAsync(user, false, false);
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess
                    ? "The external login was removed."
                    : message == ManageMessageId.Error
                        ? "An error has occurred."
                        : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
                return View("Error");
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins =
                AuthenticationManager.GetExternalAuthenticationTypes()
                    .Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider))
                    .ToList();
            ViewBag.ShowRemoveButton = (user.PasswordHash != null) || (userLogins.Count > 1);
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"),
                User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded
                ? RedirectToAction("ManageLogins")
                : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (_userManager != null))
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        [HttpPost]
        public ActionResult FileUpload(HttpPostedFileBase file)
        {
            if ((file != null) && (file.ContentLength > 0) && IsImage(file))
                try
                {
                    var img = Image.FromStream(file.InputStream, true, true);

                    var user = UserManager.FindById(User.Identity.GetUserId());
                    var filename = user.UserName + "-avatar.jpg";
                    var path = Path.Combine(Server.MapPath("~/Content/Images"), filename);

                    var resizedIMG = ResizeImage(img, 100, 100);
                    resizedIMG.Save(path);
                    // file.SaveAs(path);

                    var db = new ApplicationDbContext();
                    user.Avatar = "/Content/Images/" + filename;
                    db.Set<ApplicationUser>().AddOrUpdate(user);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = ex.StackTrace;
                    return View("Error");
                }
            return View("Error");
        }

        private bool IsImage(HttpPostedFileBase file)
        {
            if (file.ContentType.Contains("image"))
                return true;

            string[] formats = { ".jpg", ".png", ".jpeg" }; // add more if u like...

            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public ActionResult AccountDetails()
        {
            var userid = User.Identity.GetUserId();
            var user = db.Users.FirstOrDefault(x => x.Id == userid);

            ViewBag.UserName = user.UserName;
            ViewBag.Registered = user.RegistrationDate;
            ViewBag.PostCount = db.Posts.Count(x => x.UserId == user.Id);
            ViewBag.ThreadCount = db.Threads.Count(x => x.UserId == user.Id);
            return PartialView("AccDetails");
        }

        public ActionResult Inbox()
        {
            return PartialView("Inbox");
        }

        public ActionResult ChangeDetails()
        {
            var userid = User.Identity.GetUserId();
            var user = db.Users.FirstOrDefault(x => x.Id == userid);
            if (user == null)
                return HttpNotFound();
            return PartialView("EditDetails", user);
        }

        [HttpPost]
        public ActionResult ChangeDetails(ApplicationUser user)
        {
            var thatuser = db.Users.FirstOrDefault(x => x.Id == user.Id);
            thatuser.UserName = user.UserName;
            thatuser.Email = user.Email;
            try
            {
                db.Entry(thatuser).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception)
            {
                ViewBag.Error = "Nie udało się zapisać danych w bazie";
                return PartialView("EditDetails", user);
            }

            return RedirectToAction("AccountDetails");
        }

        public ActionResult ChangeAvatar()
        {
            var id = User.Identity.GetUserId();
            var user = db.Users.FirstOrDefault(x => x.Id == id);
            ViewBag.Avatar = user.Avatar;
            return PartialView("EditAvatar");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ManageUsers()
        {
            ViewBag.UserList = db.Users.ToList();
            return PartialView("UserManagement");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ManageForum()
        {
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ManageSubjectsInForum(int id)
        {
            return PartialView("SubjectsForumManagement", db.Subjects.Where(s => s.Category.Id == id).ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ManageNews()
        {
            var news = db.News.ToList();
            return PartialView("NewsManagement", news);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult CreateNews()
        {
            return PartialView("CreateNews");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult CreateNewsSubmit(News news)
        {
            news.UserId = User.Identity.GetUserId();
            news.Date = DateTime.Now;
            db.News.Add(news);
            db.SaveChanges();
            return PartialView("NewsManagement", db.News.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditNews(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var news = db.News.Find(id);
            return PartialView("EditNews", news);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult EditNewsSubmit(News news)
        {
            if (news == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            news.Date = DateTime.Now;
            db.Entry(news).State = EntityState.Modified;
            db.SaveChanges();

            return PartialView("NewsManagement", db.News.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeleteNews(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var news = db.News.Find(id);
            return PartialView("DeleteNews", news);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult DeleteNewsConfirmed(int id)
        {
            var news = db.News.Find(id);
            db.News.Remove(news);
            db.SaveChanges();
            return PartialView("NewsManagement", db.News.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditUser(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var user = db.Users.FirstOrDefault(x => x.Id == id);

            return PartialView("EditUser", user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult EditUserSubmit(ApplicationUser user)
        {
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception)
            {
                return PartialView("Error");
            }

            return PartialView("UserManagement", db.Users.ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult ChangeStateForUser(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var user = db.Users.FirstOrDefault(x => x.Id == id);
            try
            {
                user.LockoutEnabled = !user.LockoutEnabled;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.UserList = db.Users.ToList();
                return PartialView("UserManagement");
            }
            catch (Exception)
            {
                return HttpNotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult CreateCategory()
        {
            return PartialView("CreateCategory");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult CreateCategorySubmit(Category category)
        {
            if (category == null)
                return HttpNotFound("Nie ma tu nic");
            db.Categories.Add(category);
            db.SaveChanges();
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditCategory(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var category = db.Categories.Find(id);
            return PartialView("EditCategory", category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult EditCategorySubmit(Category category)
        {
            if (category == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            db.Entry(category).State = EntityState.Modified;
            db.SaveChanges();
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeleteCategory(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var category = db.Categories.Find(id);
            return PartialView("DeleteCategory", category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult DeleteCategoryConfirmed(int id)
        {
            var category = db.Categories.Find(id);
            db.Categories.Remove(category);
            db.SaveChanges();
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult CreateSubject(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ViewBag.CategoryId = id;
            return PartialView("CreateSubject");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult CreateSubjectSubmit(Subject subject)
        {
            if (subject == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!User.IsInRole("Admin")) return HttpNotFound();
            db.Subjects.Add(subject);
            db.SaveChanges();
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditSubject(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!User.IsInRole("Admin")) return HttpNotFound();
            var subject = db.Subjects.Find(id);
            ViewBag.CategoryId = subject.CategoryId;
            return PartialView("EditSubject", subject);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult EditSubjectSubmit(Subject subject)
        {
            if (subject == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!User.IsInRole("Admin")) return HttpNotFound();
            db.Entry(subject).State = EntityState.Modified;
            db.SaveChanges();
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeleteSubject(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!User.IsInRole("Admin")) return HttpNotFound();
            var subject = db.Subjects.Find(id);
            return PartialView("DeleteSubject", subject);
        }

        public ActionResult SendMessages()
        {

            var id = User.Identity.GetUserId();
            var Message_id = db.MessageUser.Where(mu => mu.SenderId == id).ToList();
            LinkedList<Message> list = new LinkedList<Message>();
            foreach (var item in Message_id)
            {
                list.AddFirst(db.Messeges.FirstOrDefault(m => m.Id == item.MessageId));
            }
            var newlist = list.OrderByDescending(m => m.Date);
            return PartialView("~/Views/Messages/Index.cshtml", newlist);
        }

        public ActionResult FriendMessages(string id)
        {

            var idd = User.Identity.GetUserId();
            var Message_id = db.MessageUser.Where(mu => mu.ReceiverId == idd || mu.SenderId == id || mu.ReceiverId == id || mu.SenderId == idd).ToList();
            LinkedList<Message> list = new LinkedList<Message>();
            foreach (var item in Message_id)
            {
                list.AddFirst(db.Messeges.FirstOrDefault(m => m.Id == item.MessageId));
            }
            var newlist = list.OrderByDescending(m => m.Date);

            return PartialView("~/Views/Messages/Index.cshtml", newlist);
        }


        public ActionResult PMessage()
        {

            var id = User.Identity.GetUserId();
            var Message_id = db.MessageUser.Where(mu => mu.ReceiverId == id).ToList();
            LinkedList<Message> list = new LinkedList<Message>();
            foreach (var item in Message_id)
            {
                list.AddFirst(db.Messeges.FirstOrDefault(m => m.Id == item.MessageId));
            }
            var newlist = list.OrderByDescending(m => m.Date);
            return PartialView("~/Views/Messages/Index.cshtml", newlist);
        }

        public ActionResult PFriends()
        {
            var id = User.Identity.GetUserId();
            return PartialView("~/Views/Messages/FriendsPartial.cshtml", db.Friends.Where(f => f.User.Id == id).ToList());
        }

        public ActionResult FriendsAdd()
        {
            return PartialView("~/Views/Friends/Add.cshtml");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult DeleteSubjectConfirmed(int id)
        {
            var subject = db.Subjects.Find(id);
            db.Subjects.Remove(subject);
            db.SaveChanges();
            return PartialView("ForumManagement", db.Categories.ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult ChangeAdminRole(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            try
            {
                var im = new IdentityManager();
                if (im.isUserInRole(id, "Admin"))
                {
                    var result = im.ClearUserFromRole(id, "Admin");
                    if (result == false)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    var user = db.Users.Find(id);
                    user.Privileges = "Użytkownik";
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    im.AddUserToRole(id, "Admin");
                    var user = db.Users.Find(id);
                    user.Privileges = "Administrator";
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                }
                ViewBag.UserList = db.Users.ToList();
                return PartialView("UserManagement");
            }
            catch (Exception)
            {
                return HttpNotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ModeratorManage(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var subjectModerator = db.Moderators.Where(m => m.UserId == id).ToList();
            ViewBag.UserName = db.Users.FirstOrDefault(x => x.Id == id).UserName;
            ViewBag.UserId = db.Users.FirstOrDefault(x => x.Id == id).Id;
            var subjects = db.Subjects.ToList();
            ViewBag.SelectList = new SelectList(subjects, "Id", "Title");
            return PartialView("ManageModerator", subjectModerator);
        }
        [HttpPost]
        public ActionResult ModeratorAdd(string selectedId, string userId)
        {

            var subId = int.Parse(selectedId);
            var modcheck = db.Moderators.Any(x => x.SubjectId == subId && x.UserId == userId);
            if (modcheck)
            {
                return PartialView("ManageModerator", db.Moderators.Where(m => m.UserId == userId).ToList());
            }

            var moderator = new Moderator
            {
                UserId = userId,
                SubjectId = subId
            };
            var im = new IdentityManager();
            var user = db.Users.Find(userId);
            im.AddUserToRole(userId, "Moderator");
            user.Privileges = "Moderator";
            db.Entry(user).State = EntityState.Modified;
            db.Moderators.Add(moderator);
            db.SaveChanges();
            return PartialView("ManageModerator", db.Moderators.Where(m => m.UserId == userId).ToList());
        }

        [HttpPost]
        public ActionResult ModeratorRemove(string subjectId, string userId)
        {
            var im = new IdentityManager();
            var subId = int.Parse(subjectId);
            var moderator = db.Moderators.First(x => x.SubjectId == subId && x.UserId == userId);
            var user = db.Users.Find(userId);

            user.Privileges = "Użytkownik";
            db.Entry(user).State = EntityState.Modified;
            db.Moderators.Remove(moderator);
            db.SaveChanges();
            var mod = db.Moderators.Any(x => x.UserId == userId);
            if (!mod)
            {
                im.ClearUserFromRole(userId, "Moderator");
            }
            return PartialView("ManageModerator", db.Moderators.Where(m => m.UserId == userId).ToList());
        }

        #region Helpers

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
                return user.PasswordHash != null;
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
                return user.PhoneNumber != null;
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        #endregion


    }
}