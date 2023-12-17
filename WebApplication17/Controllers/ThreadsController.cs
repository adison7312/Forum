using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication17.Models;

namespace WebApplication17.Controllers
{
    public class ThreadsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public ActionResult ThreadsSubject(int id)
        {
            var threads = db.Threads.Where(t => t.SubjectId == id).OrderByDescending(t => t.Date).ToList();
            var threadsNotPinned = threads.Where(t => t.IsPinned == false);
            var threadsPinned = threads.Where(t => t.IsPinned == true);

            var model = new ThreadsWithPinnedViewModel
            {
                UnpinnedThreads = threadsNotPinned,
                PinnedThreads = threadsPinned
            };

            var firstOrDefault = db.Subjects.FirstOrDefault(s => s.Id == id);
            if (firstOrDefault != null)
            {
                ViewBag.Title = firstOrDefault.Title;
                ViewBag.CategoryTitle = firstOrDefault.Category.Title;
            }



            return View(model);
        }


        // GET: Threads
        public ActionResult Index()
        {
            return View(db.Threads.ToList());
        }

        // GET: Threads/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Thread thread = db.Threads.Find(id);
            if (thread == null)
            {
                return HttpNotFound();
            }
            return View(thread);
        }

        // GET: Threads/Create
        [Authorize]
        public ActionResult Create(int id)
        {
            ViewBag.SubjectId = id;
            return View();
        }

        // POST: Threads/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Thread thread)
        {
            thread.Date = DateTime.Now;
            thread.UserId = User.Identity.GetUserId();

            try
            {
                db.Threads.Add(thread);
                db.SaveChanges();
                return RedirectToAction("ThreadsSubject", new { id = thread.SubjectId });
            }
            catch (DbEntityValidationException ex)
            {
                return View(thread);
            }
        }

        // GET: Threads/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Thread thread = db.Threads.Find(id);
            ViewBag.SubjectId = thread.SubjectId;
            ViewBag.UserId = thread.UserId;
            ViewBag.Date = thread.Date;
            if (thread == null)
            {
                return HttpNotFound();
            }
            return View(thread);
        }

        // POST: Threads/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Thread thread)
        {
            db.Entry(thread).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("ThreadsSubject", new { id = thread.SubjectId });
        }

        // GET: Threads/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Thread thread = db.Threads.Find(id);
            if (thread == null)
            {
                return HttpNotFound();
            }
            return View(thread);
        }

        // POST: Threads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Thread thread = db.Threads.Find(id);
            db.Threads.Remove(thread);
            db.SaveChanges();
            return RedirectToAction("ThreadsSubject", new { id = thread.SubjectId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public string PostCount(int id)
        {
            return db.Posts.Count(s => s.Thread.Id == id).ToString();
        }

        public string ViewCount(int id)
        {
            var thread = db.Threads.FirstOrDefault(x => x.Id == id);
            return thread.ViewCount.ToString();
        }

        public ActionResult LastPost(int id)
        {
            var post = db.Posts.Where(t => t.ThreadId == id).OrderByDescending(t => t.Date).FirstOrDefault();
            if (post != null)
                return PartialView("LastPost", post);
            else
            {
                ViewBag.NoPost = "Brak postów";
                return HttpNotFound("");

            }
        }

    }
}
