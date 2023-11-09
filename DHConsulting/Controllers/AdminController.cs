using DHConsulting.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DHConsulting.Controllers
{
    public class AdminController : Controller
    {
        private ModelDb db = new ModelDb();

        // GET: Admin
        public ActionResult Index()
        {
            return View(db.Prodotto.ToList());
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Prodotto p, HttpPostedFileBase Image)
        {
            if (ModelState.IsValid)
            {
                if (Image != null && Image.ContentLength > 0)
                {
                    string fileName = Image.FileName;
                    string filePath = Path.Combine(Server.MapPath("~/Content/Img"), fileName);
                    Image.SaveAs(filePath);
                    p.Image = fileName;
                }
                else
                {
                    p.Image = "placeholder.jpg";
                }
                db.Prodotto.Add(p);
                db.SaveChanges();
                TempData["Successo"] = "Prodotto aggiunto";
                return RedirectToAction("Index");
            }
            ViewBag.Errore = "Errore durante la procedura";
            return View();
        }

        public ActionResult Edit(int id)
        {
            var prodotto = db.Prodotto.Find(id);
            return View(prodotto);
        }

        [HttpPost]
        public ActionResult Edit(Prodotto p, HttpPostedFileBase Image)
        {
            ModelDb db1 = new ModelDb();
            Prodotto prodotto = db.Prodotto.Find(p.IdProdotto);
            if (ModelState.IsValid)
            {
                if (Image != null && Image.ContentLength > 0)
                {
                    string fileName = Image.FileName;
                    string filePath = Path.Combine(Server.MapPath("~/Content/Img"), fileName);
                    Image.SaveAs(filePath);
                    p.Image = fileName;
                }
                else
                {
                    p.Image = prodotto.Image;
                }
                db1.Entry(p).State = EntityState.Modified;
                db1.SaveChanges();
                TempData["Successo"] = "Prodotto modificato";
                return RedirectToAction("Index");
            }
            ViewBag.Errore = "Errore durante la procedura";
            return View();
        }

        public ActionResult Delete(int id)
        {
            var prodotto = db.Prodotto.Find(id);
            db.Prodotto.Remove(prodotto);
            db.SaveChanges();
            TempData["Successo"] = "Prodotto rimosso";
            return RedirectToAction("Index");
        }

        public ActionResult AddAdmin()
        {
            ViewBag.Utenti = db.Utente.Where(x => x.Role == "Admin").ToList();
            return View();
        }

        [HttpPost]
        public ActionResult AddAdmin(Utente u)
        {
            if (ModelState.IsValid)
            {
                var user = db.Utente.FirstOrDefault(x => x.Username == u.Username);
                if (user == null)
                {
                    u.Role = "Admin";
                    u.Password = PasswordHasher.HashPassword(u.Password);
                    u.Confirmed = true;
                    db.Utente.Add(u);
                    db.SaveChanges();
                    ModelState.Clear();
                    ViewBag.Successo = "Nuovo Admin aggiunto";
                    ViewBag.Utenti = db.Utente.Where(x => x.Role == "Admin").ToList();
                    return View();
                }
                ViewBag.Errore = "Admin già presente";
                ViewBag.Utenti = db.Utente.Where(x => x.Role == "Admin").ToList();
                return View();
            }
            ViewBag.Errore = "Errore durante la procedura";
            ViewBag.Utenti = db.Utente.Where(x => x.Role == "Admin").ToList();
            return View();
        }

        public ActionResult EditAdmin(int id)
        {
            var utente = db.Utente.Find(id);
            return View(utente);
        }

        [HttpPost]
        public ActionResult EditAdmin(Utente u)
        {
            ModelDb db1 = new ModelDb();
            Utente utente = db.Utente.Find(u.IdUtente);
            if (ModelState.IsValid)
            {
                if (!PasswordHasher.VerifyPassword(u.Password, utente.Password))
                {
                    u.Password = PasswordHasher.HashPassword(u.Password);
                }
                db1.Entry(u).State = EntityState.Modified;
                db1.SaveChanges();
                TempData["Successo"] = "Profilo modificato";
                return RedirectToAction("AddAdmin");
            }
            ViewBag.Errore = "Errore durante la procedura";
            return View();
        }

        public ActionResult DeleteAdmin(int id)
        {
            var utente = db.Utente.Find(id);
            db.Utente.Remove(utente);
            db.SaveChanges();
            TempData["Successo"] = "Admin rimosso";
            return RedirectToAction("AddAdmin");
        }
    }
}