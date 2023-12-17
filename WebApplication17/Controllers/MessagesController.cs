using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication17.Models;

namespace WebApplication17.Controllers
{
    public class MessagesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Messages
        public ActionResult Index()
        {
            return View(db.Messeges.ToList());
        }

        // GET: Messages/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Message message = db.Messeges.Find(id);
            if (message == null)
            {
                return HttpNotFound();
            }
            return PartialView("Details", message);
        }

        // GET: Messages/Create
        public ActionResult Create()
        {
            return PartialView("Create");
        }

        // POST: Messages/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Text,Source")] Message message)
        {
            string sending = Request["toName"].ToString();
            string[] tosend = null;
            if (sending == "")
            {
                ModelState.AddModelError("", "pole Do: jest wymagane");

            }
            else
            {

                sending = sending.ToCharArray()
               .Where(c => !Char.IsWhiteSpace(c))
               .Select(c => c.ToString())
               .Aggregate((a, b) => a + b);
                tosend = sending.Split(',');


                foreach (var item in tosend)
                {
                    try
                    {
                        var cos = db.Users.First(u => u.UserName == item).UserName;
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", "pole Do: błędne dane");
                        break;
                    }

                }
            }

            if (ModelState.IsValid)
            {
                message.Date = DateTime.Now;
                db.Messeges.Add(message);
                db.SaveChanges();
                foreach (var item in tosend)
                {
                    MessageUser MU = new MessageUser();
                    MU.MessageId = message.Id;
                    MU.SenderId = User.Identity.GetUserId();
                    MU.ReceiverId = db.Users.FirstOrDefault(u => u.UserName == item).Id;
                    db.MessageUser.Add(MU);
                    db.SaveChanges();
                }


                return RedirectToAction("Inbox", "Manage");
            }

            return PartialView("Create", message);
        }

        // GET: Messages/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Message message = db.Messeges.Find(id);
            if (message == null)
            {
                return HttpNotFound();
            }
            return View(message);
        }

        // POST: Messages/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Text,Date,Source")] Message message)
        {
            if (ModelState.IsValid)
            {
                db.Entry(message).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(message);
        }

        // GET: Messages/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Message message = db.Messeges.Find(id);
            if (message == null)
            {
                return HttpNotFound();
            }
            return View(message);
        }

        // POST: Messages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Message message = db.Messeges.Find(id);
            db.Messeges.Remove(message);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult NewMessageFriends()
        {
            var id = User.Identity.GetUserId();
            var q = db.Friends.Where(f => f.User.Id == id).ToList();
            return PartialView("NewMessageFriends", q);
        }
        public void IfViewd(int? id)
        {
            var idUser = User.Identity.GetUserId();
            Message message = db.Messeges.Find(id);

            if (db.MessageUser.FirstOrDefault(mu => mu.MessageId == id).ReceiverId == idUser)
            {
                message.IsRead = true;
                db.Entry(message).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public string dateMessage(int id)
        {
            var date = db.Messeges.FirstOrDefault(m => m.Id == id).Date;
            DateTime localDate = DateTime.Now;
            if (date.ToString("MM dd yyyy") == localDate.ToString("MM dd yyyy"))
            {
                return date.ToString("HH:mm");
            }
            else
            {
                return date.ToString("dd MMM");
            }

        }


        public int Unread()
        {
            var id = User.Identity.GetUserId();
            var messageId = db.MessageUser.Where(mu => mu.ReceiverId == id).ToList();
            var list = new LinkedList<Message>();

            foreach (var item in messageId)
            {
                var ii = db.Messeges.FirstOrDefault(m => m.Id == item.MessageId && m.IsRead == false);
                if (ii != null)
                {
                    list.AddFirst(ii);
                }


            }
            int alejaa = list.Count();
            return list.Count();
        }

        public static int Unread2(string idx)
        {
            var db = new ApplicationDbContext();
            var messageId = db.MessageUser.Where(mu => mu.ReceiverId == idx).ToList();
            var list = new LinkedList<Message>();
            foreach (var item in messageId)
            {

                var ii = db.Messeges.FirstOrDefault(m => m.Id == item.MessageId && m.IsRead == false);
                if (ii != null)
                {
                    list.AddFirst(ii);
                }
            }

            return list.Count();
        }

        public void MarkAsRead(int id)
        {
            var m = db.Messeges.FirstOrDefault(x => x.Id == id);
            if (m.IsRead == false)
            {
                m.IsRead = true;
                db.Entry(m).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

    }
}
