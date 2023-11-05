﻿using DHConsulting.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DHConsulting.Controllers
{
    public class HomeController : Controller
    {
        private ModelDb db = new ModelDb();

        //View principale con la lista dei prodotti
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View(db.Prodotto.ToList());
        }

        //View di dettaglio di ogni prodotto
        public ActionResult Details(int id)
        {
            var prodotto = db.Prodotto.Find(id);
            return View(prodotto);
        }

        //Metodo per salvare il carrello nel cookie
        private void SaveCart(List<Dettaglio> carrello)
        {
            //serializzo la risposta in modo da poterla salvare come stringa
            string carrelloJson = JsonConvert.SerializeObject(carrello);
            HttpCookie cookie = new HttpCookie("carrello");
            cookie.Value = carrelloJson;
            //imposto la scadenza a 3 giorni
            cookie.Expires = DateTime.Now.AddDays(3);
            Response.Cookies.Add(cookie);
        }

        //Metodo per recuperare il carrello dal cookie
        private List<Dettaglio> CartFromCookie()
        {
            //se il carrello esiste ritorno la sua conversione in lista
            HttpCookie cookie = Request.Cookies["carrello"];
            if (cookie != null && !string.IsNullOrEmpty(cookie.Value))
            {
                string carrelloJson = cookie.Value;
                List<Dettaglio> carrello = JsonConvert.DeserializeObject<List<Dettaglio>>(carrelloJson);
                return carrello;
            }
            //altrimenti creo una nuova lista
            return new List<Dettaglio>();
        }

        //Metodo per aggiungere un nuovo prodotto nel carrello
        [HttpPost]
        public ActionResult AddProdotto(int Quantita)
        {
            //creo un nuovo elemento Dettaglio
            int id = Convert.ToInt16(TempData["IdProdotto"]);
            Dettaglio d = new Dettaglio
            {
                IdProdotto = id,
                Quantita = Quantita,
            };
            //recupero il carrello dal cookie
            List<Dettaglio> carrello = CartFromCookie();
            // cerco se l'elemento esiste già nel carrello
            Dettaglio elementoEsistente = carrello.FirstOrDefault(item => item.IdProdotto == d.IdProdotto);
            if (elementoEsistente != null)
            {
                //se l'elemento esiste già, aggiorno la quantità
                elementoEsistente.Quantita += d.Quantita;
            }
            else
            {
                //altrimenti, aggiungi il nuovo elemento al carrello
                carrello.Add(d);
            }
            SaveCart(carrello);
            TempData["Successo"] = "Prodotto aggiunto al carrello";
            return RedirectToAction("Details", new { id = id });
        }

        //Metodo per ridurre la quantità di un determinato prodotto nel carrello
        public ActionResult Less(int id)
        {
            List<Dettaglio> carrello = CartFromCookie();
            foreach (Dettaglio d in carrello)
            {
                //al prodotto corrispondente nel cookie viene decrementata 1 unità
                //e salvato nuovamente il cookie aggiornata
                if (d.IdProdotto == id)
                {
                    if (d.Quantita > 1)
                    {
                        //decremento la quantità solo se è maggiore di 1
                        d.Quantita -= 1;
                        SaveCart(carrello);
                    }
                    else
                    {
                        //rimuovo completamente il prodotto dal carrello se la quantità è 1
                        carrello.Remove(d);
                        if(carrello.Count > 0)
                        {
                            SaveCart(carrello);
                        }
                        //elimino la sessione se non ci sono altri prodotti nel carrello
                        else
                        {
                            DeleteCart();
                        }
                    }
                    return RedirectToAction("Cart", "Payment");
                }
            }
            return View();
        }

        //Metodo per aumentare la quantità di un determinato prodotto nel carrello
        public ActionResult More(int id)
        {
            List<Dettaglio> carrello = CartFromCookie();
            foreach (Dettaglio d in carrello)
            {
                //al prodotto corrispondente nel cookie viene incrementata 1 unità
                //e salvato nuovamento il cookie aggiornato
                if (d.IdProdotto == id)
                {
                    d.Quantita += 1;
                    SaveCart(carrello);
                    return RedirectToAction("Cart", "Payment");
                }
            }
            return View();
        }

        //Metodo per eliminare un prodotto dal carrello
        public ActionResult Delete(int id)
        {
            List<Dettaglio> carrello = CartFromCookie();
            foreach (Dettaglio d in carrello)
            {
                //viene cercato il prodotto con id uguale a quello presente nel carrello e rimosso
                if (d.IdProdotto == id)
                {
                    carrello.Remove(d);
                    //se il carrello ha ancora elementi risalvo il cookie
                    if (carrello.Count > 0)
                    {
                        SaveCart(carrello);
                        return RedirectToAction("Cart", "Payment");
                    }
                    //altrimenti elimino il cookie
                    DeleteCart();
                    return RedirectToAction("Cart", "Payment");
                }
            }
            return View();
        }

        //Metodo per eliminare tutto il carrello
        public ActionResult DeleteCart()
        {
            HttpCookie carrelloCookie = HttpContext.Request.Cookies["carrello"];
            //segno il cookie come scaduto
            carrelloCookie.Expires = DateTime.Now.AddYears(-1);
            HttpContext.Response.Cookies.Add(carrelloCookie);
            return RedirectToAction("Cart", "Payment");
        }

        //View per mostrare i dettagli del profilo utente con relativo storico acquisti
        public ActionResult Profilo()
        {
            return View(db.Cliente.Where(x => x.Username == User.Identity.Name).FirstOrDefault());
        }

        //View classica per la modifica del profilo
        public ActionResult EditProfilo(int id)
        {
            return View(db.Cliente.Find(id));
        }

        //Metodo per la modifica del profilo
        [HttpPost]
        public ActionResult EditProfilo(Cliente c)
        {
            //utilizzo di un nuovo ModelDb per evitare i problemi relativi alle ultime versioni di EF
            ModelDb db2 = new ModelDb();
            Cliente cliente = db.Cliente.Find(c.IdCliente);
            if (ModelState.IsValid)
            {
                //controllo per eventuali dati uguali nel db
                var utente = db2.Cliente.Where(x => x.Piva == c.Piva && c.Piva != cliente.Piva).FirstOrDefault();
                if (utente != null)
                {
                    ViewBag.Errore = "P.Iva già presente nel database";
                    return View();
                }
                var user = db2.Cliente.Where(x => x.Email == c.Email && c.Email != cliente.Email).FirstOrDefault();
                if (user != null)
                {
                    ViewBag.Errore = "Email già presente nel database";
                    return View();
                }
                //in caso di nuova password viene cryptata prima di essere salvata
                if (c.Password != cliente.Password)
                {
                    c.Password = PasswordHasher.HashPassword(c.Password);
                }
                db2.Entry(c).State = EntityState.Modified;
                db2.SaveChanges();
                TempData["Successo"] = "Profilo aggiornato";
                return RedirectToAction("Profilo");
            }
            ViewBag.Errore = "Errore durante la procedura";
            return View();
        }
    }
}