using DHConsulting.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Net.Mime;

namespace DHConsulting.Controllers
{
    public class HomeController : Controller
    {
        private ModelDb db = new ModelDb();

        [AllowAnonymous]
        //View principale con la lista dei prodotti
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";
			return View(db.Prodotto.ToList());
        }

        [AllowAnonymous]
        //View per la sezione about me
        public ActionResult About()
        {
            return View();
        }

		[AllowAnonymous]
		//View di dettaglio di ogni prodotto
		public ActionResult Details(int id)
        {
            ViewBag.Prodotti = db.Prodotto.Where(x => x.IdProdotto != id).ToList();
            ViewBag.Cliente = db.Cliente.FirstOrDefault(x => x.Username == User.Identity.Name);
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
                        if (carrello.Count > 0)
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

		[Authorize(Roles = "User")]
		//View per mostrare i dettagli del profilo utente con relativo storico acquisti
		public ActionResult Profilo()
        {
            return View(db.Cliente.Where(x => x.Username == User.Identity.Name).FirstOrDefault());
        }

		[Authorize(Roles = "User")]
		//View classica per la modifica del profilo
		public ActionResult EditProfilo(int id)
        {
            return View(db.Cliente.Find(id));
        }

		[Authorize(Roles = "User")]
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
                    ViewBag.Errore = "P.IVA già presente nel database";
                    return View();
                }
                var user = db2.Cliente.Where(x => x.Email == c.Email && c.Email != cliente.Email).FirstOrDefault();
                if (user != null)
                {
                    ViewBag.Errore = "Email già presente nel database";
                    return View();
                }
                var user2 = db2.Cliente.Where(x => x.Username == c.Username && c.Username != cliente.Username).FirstOrDefault();
                if (user2 != null)
                {
                    ViewBag.Errore = "Username già presente nel database";
                    return View();
                }
                //in caso di nuova password viene cryptata prima di essere salvata
                if (c.Password != cliente.Password)
                {
                    c.Password = PasswordHasher.HashPassword(c.Password);
                }
                var user3 = db2.Utente.FirstOrDefault(x => x.Username == cliente.Username);
                user3.Username = c.Username;
                db2.Entry(c).State = EntityState.Modified;
                db2.SaveChanges();
                TempData["Successo"] = "Profilo aggiornato";
                return RedirectToAction("Profilo");
            }
            ViewBag.Errore = "Errore durante la procedura";
            return View(cliente);
        }

        //Metodo per creare la mail di richiesta per il pacchetto professional
        private void MailProPack(string description)
        {
            var utente = db.Cliente.FirstOrDefault(x => x.Username == User.Identity.Name);
            string senderEmail = ConfigurationManager.AppSettings["SmtpSenderEmail"];
            string senderPassword = ConfigurationManager.AppSettings["SmtpSenderPassword"];

            var smtpClient = new SmtpClient("smtps.aruba.it")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage()
            {
                From = new MailAddress(senderEmail, "Paolo Manca Consulting"),
                Subject = "Richiesta Pacchetto Pro",
                Body = @"
                        <section class=""max-w-2xl px-6 py-8 mx-auto bg-white dark:bg-gray-900"">
                            <main class=""mt-8"">
                                <h2 class=""text-gray-700 dark:text-gray-200"">Pacchetto pro</h2>

                                <p class=""mt-2 leading-loose text-gray-600 dark:text-gray-300"">
                                    Hai appena ricevuto una richiesta per il pacchetto pro con il seguente testo:
                                </p>

                                <p class=""mt-2 leading-loose text-gray-600 dark:text-gray-300 italic"">
                                    " + description + @"
                                </p>
                            </main>
                            <footer class=""mt-8"">
                                <p class=""text-gray-500 dark:text-gray-400"">
                                    Questa email è stata inviata da " + utente.Nome + " " + utente.Cognome + @"
                                </p>
                                <p class=""mt-1 leading-loose text-gray-600 dark:text-gray-300"">
                                    Email: " + utente.Email + @"
                                </p>
                                <p class=""mt-1 leading-loose text-gray-600 dark:text-gray-300"">
                                    Telefono: " + utente.Phone + @"
                                </p>
                                <p class=""mt-3 text-gray-500 dark:text-gray-400"">" + DateTime.Now + @"</p>
                            </footer>
                        </section>",
                IsBodyHtml = true, 
            };
            mailMessage.To.Add(senderEmail);

            smtpClient.Send(mailMessage);
        }

        //Metodo per inviare istruzioni al cliente via mail
        private void CustomerMail(string recipientEmail)
        {
            string senderEmail = ConfigurationManager.AppSettings["SmtpSenderEmail"];
            string senderPassword = ConfigurationManager.AppSettings["SmtpSenderPassword"];

            var smtpClient = new SmtpClient("smtps.aruba.it")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage()
            {
                From = new MailAddress(senderEmail, "Paolo Manca Consulting"),
                Subject = "Richiesta ricevuta",
                IsBodyHtml = true,
            };

            mailMessage.Body = $@"
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;'>
            <header style='text-align: center;'>
                <img style='width: 100px; height: auto; max-width: 100%;' src='https://www.paolomancaconsulting.com/Content/Img/Logo-2.png' alt='logo'>
            </header>

            <main style='margin-top: 20px;'>
                <p style='color: #333333; font-size: 16px;'>La tua richiesta è stata inviata.</p>

                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Nelle prossime 48h analizzerò i dettagli del tuo progetto.
                </p>
                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Una volta terminata l'analisi verrai ricontattato per fissare un appuntamento e approfondire ulteriori dettagli
                </p>
                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Grazie e a presto.
                </p>
            </main>
            
            <footer style='margin-top: 20px; text-align: center;'>
                <p style='color: #888888; font-size: 12px;'>&copy; {DateTime.Now.Year} PM Consulting. Tutti i diritti riservati.</p>
            </footer>
        </div>";

            mailMessage.To.Add(recipientEmail);

            smtpClient.Send(mailMessage);
        }

        //Metodo per inviare la mail di richiesta del pro pack
        [HttpPost]
        public ActionResult ProfessionalPack(string description)
        {
            var utente = db.Cliente.FirstOrDefault(x => x.Username == User.Identity.Name);
            int id = Convert.ToInt16(TempData["IdProdotto"]);
            if(description != "")
            {
                MailProPack(description);
                CustomerMail(utente.Email);
                TempData["Successo"] = "Richiesta inviata con successo. Controlla la tua mail";
                return RedirectToAction("Details", "Home", new { id = id });
            }
            TempData["Errore"] = "Il campo descrizione non può essere vuoto";
            return RedirectToAction("Details", "Home", new { id = id });
        }
    }
}
