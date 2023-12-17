using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication17.Models;

namespace WebApplication17.Controllers
{
    public class PostsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Posts
        public ActionResult PostThread(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var thread = db.Threads.FirstOrDefault(t => t.Id == id);

            thread.ViewCount++;
            db.Entry(thread).State = EntityState.Modified;
            db.SaveChanges();


            var posts = db.Posts.Where(t => t.Thread.Id == id).ToList();
            ViewBag.ThreadTitle = thread.Title;
            ViewBag.Title = thread.Title;
            ViewBag.CategoryTitle = thread.Subject.Category.Title;
            ViewBag.SubjectTitle = thread.Subject.Title;
            return View(posts);
        }

        public ActionResult Index()
        {
            var posts = db.Posts.Include(p => p.Thread).Include(p => p.User);
            return View(posts.ToList());
        }

        // GET: Posts/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = db.Posts.Find(id);
            if (post == null)
            {
                return HttpNotFound();
            }
            return View(post);
        }
        [Authorize]
        // GET: Posts/Create
        public ActionResult Create(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var firstOrDefault = db.Threads.FirstOrDefault(t => t.Id == id);
            if (firstOrDefault != null)
                ViewBag.ThreadTitle = firstOrDefault.Title;
            ViewBag.ThreadId = id;
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Title,Text,ThreadId")] Post post)
        {
            post.UserId = User.Identity.GetUserId();
            post.Date = DateTime.Now;
            if (ModelState.IsValid)
            {
                db.Posts.Add(post);
                db.SaveChanges();
                return RedirectToAction("PostThread", new { id = post.ThreadId });
            }

            return View(post);
        }
        [Authorize]
        // GET: Posts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var post = db.Posts.Find(id);
            ViewBag.ThreadId = post.ThreadId;
            ViewBag.Date = post.Date;
            ViewBag.UserId = post.UserId;
            if (post == null)
            {
                return HttpNotFound();
            }
            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Text,Date,UserId,ThreadId")] Post post)
        {
            if (ModelState.IsValid)
            {
                db.Entry(post).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("PostThread", new { id = post.ThreadId });
            }

            return View(post);
        }
        [Authorize]
        // GET: Posts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = db.Posts.Find(id);
            if (post == null)
            {
                return HttpNotFound();
            }
            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Post post = db.Posts.Find(id);
            db.Posts.Remove(post);
            db.SaveChanges();
            return RedirectToAction("PostThread", "Posts", new { id = post.ThreadId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public string Privillege(int id, string userid)
        {
            var user = db.Users.Find(userid);
            var im = new IdentityManager();
            if (im.isUserInRole(user.Id, "Admin"))
            {
                return "<div style=\"color: red; text-align: center;\">Administrator</div>";
            }
            else if (user.Privileges == "Moderator")
            {
                return "<div style=\"color: green; text-align: center;\">Moderator</div>";

            }

            if (id >= 0 && id <= 20)
            {
                return "<span style: \"color=white;\">Nowy użytkownik</span>";

            }
            else if (id > 20 && id <= 50)
            {
                return "Bywalec";
            }
            else if (id > 50 && id <= 100)
            {
                return "Forumowicz";
            }
            return "";
        }


        public ActionResult reportPost(int id)
        {
            LinkedList<ApplicationUser> list = new LinkedList<ApplicationUser>();

            try
            {
                var Admins = db.Users.ToList();


                //var   Moderators = db.Users.Where(u => u.Roles == db.Roles.Where(r => r.Name == "Moderator")).DefaultIfEmpty(null);


                //if(Moderators !=null)
                //{
                //    var allmoderators = db.Moderators.Where(m => m.SubjectId == id);
                //    foreach (var mod in Moderators)
                //    {
                //        foreach (var allM in allmoderators)
                //        {
                //            if (mod.Moderators == allM)
                //            {
                //                list.AddFirst(mod);
                //            }
                //        }
                //    }
                //}
                IdentityManager IM = new IdentityManager();
                foreach (var item in Admins)
                {
                    if (item != null)
                    {
                        if (IM.isAdmin(item))
                        {
                            var wtf = item.Privileges;
                            list.AddFirst(item);
                        }

                    }
                }

            }
            catch (Exception)
            {
            }

            Message message = new Message();
            message.Date = DateTime.Now;
            message.Title = "Zgłoszenie postu";
            message.Text = " Zgłoszono post : http://localhost:51438/Posts/PostThread/" + id;
            db.Messeges.Add(message);
            db.SaveChanges();
            foreach (var item in list)
            {
                MessageUser MU = new MessageUser();
                MU.MessageId = message.Id;
                MU.SenderId = User.Identity.GetUserId();
                MU.ReceiverId = item.Id;
                db.MessageUser.Add(MU);
                db.SaveChanges();
            }
            return RedirectToAction("PostThread", new { id = id });

        }
    }
}
