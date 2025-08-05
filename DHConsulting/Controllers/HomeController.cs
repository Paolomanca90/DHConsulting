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
		private void MailProPack(string description, string nome = null, string cognome = null, string email = null, string telefono = null)
		{
			// Se l'utente è autenticato, usa i suoi dati
			var utente = User.Identity.IsAuthenticated ? db.Cliente.FirstOrDefault(x => x.Username == User.Identity.Name) : null;

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
				From = new MailAddress(senderEmail, "Five Innovation Hub"),
				Subject = "🚀 Nuova Richiesta Pacchetto Professional",
				IsBodyHtml = true,
			};

			// Determina i dati del richiedente
			string richiedenteNome = utente?.Nome ?? nome ?? "Non specificato";
			string richiedenteCognome = utente?.Cognome ?? cognome ?? "Non specificato";
			string richiedenteEmail = utente?.Email ?? email ?? "Non specificata";
			string richiedenteTelefono = utente?.Phone ?? telefono ?? "Non specificato";
			string statusCliente = utente != null ? "Cliente Registrato" : "Nuovo Contatto";
			string badgeColor = utente != null ? "#10b981" : "#f59e0b";

			mailMessage.Body = $@"
        <div style='max-width: 700px; margin: 0 auto; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; background-color: #f8fafc;'>
            <!-- Header -->
            <div style='background: linear-gradient(135deg, #1f2937 0%, #374151 100%); padding: 40px 20px; text-align: center; border-radius: 16px 16px 0 0;'>
                <div style='background: white; width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; box-shadow: 0 10px 25px rgba(0,0,0,0.1);'>
                    <span style='font-size: 36px;'>👑</span>
                </div>
                <h1 style='color: white; font-size: 28px; font-weight: 700; margin: 0 0 10px 0;'>Richiesta Pacchetto Professional</h1>
                <p style='color: #d1d5db; font-size: 16px; margin: 0;'>Nuova richiesta di preventivo ricevuta</p>
            </div>

            <!-- Main Content -->
            <div style='background: white; padding: 40px 30px; border-radius: 0 0 16px 16px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                
                <!-- Status Badge -->
                <div style='text-align: center; margin-bottom: 30px;'>
                    <span style='background-color: {badgeColor}; color: white; padding: 8px 20px; border-radius: 25px; font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;'>
                        {statusCliente}
                    </span>
                </div>

                <!-- Client Info Card -->
                <div style='background: #f8fafc; border: 2px solid #e2e8f0; border-radius: 12px; padding: 25px; margin-bottom: 30px;'>
                    <h2 style='color: #1f2937; font-size: 20px; font-weight: 600; margin: 0 0 20px 0; display: flex; align-items: center;'>
                        <span style='background: #3b82f6; color: white; width: 30px; height: 30px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; margin-right: 12px; font-size: 14px;'>👤</span>
                        Dati del Richiedente
                    </h2>
                    
                    <div style='display: grid; gap: 15px;'>
                        <div style='display: flex; align-items: center; padding: 12px 0; border-bottom: 1px solid #e5e7eb;'>
                            <span style='background: #dbeafe; color: #3b82f6; width: 35px; height: 35px; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; margin-right: 15px; font-size: 16px;'>👨‍💼</span>
                            <div>
                                <strong style='color: #374151; font-size: 14px; display: block;'>Nome e Cognome</strong>
                                <span style='color: #6b7280; font-size: 16px;'>{richiedenteNome} {richiedenteCognome}</span>
                            </div>
                        </div>
                        
                        <div style='display: flex; align-items: center; padding: 12px 0; border-bottom: 1px solid #e5e7eb;'>
                            <span style='background: #fef3c7; color: #d97706; width: 35px; height: 35px; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; margin-right: 15px; font-size: 16px;'>📧</span>
                            <div>
                                <strong style='color: #374151; font-size: 14px; display: block;'>Email</strong>
                                <span style='color: #6b7280; font-size: 16px;'>{richiedenteEmail}</span>
                            </div>
                        </div>
                        
                        <div style='display: flex; align-items: center; padding: 12px 0;'>
                            <span style='background: #d1fae5; color: #059669; width: 35px; height: 35px; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; margin-right: 15px; font-size: 16px;'>📱</span>
                            <div>
                                <strong style='color: #374151; font-size: 14px; display: block;'>Telefono</strong>
                                <span style='color: #6b7280; font-size: 16px;'>{richiedenteTelefono}</span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Project Description -->
                <div style='background: linear-gradient(145deg, #fef7cd 0%, #fef3c7 100%); border: 2px solid #fbbf24; border-radius: 12px; padding: 25px; margin-bottom: 30px;'>
                    <h3 style='color: #92400e; font-size: 18px; font-weight: 600; margin: 0 0 15px 0; display: flex; align-items: center;'>
                        <span style='background: #fbbf24; color: white; width: 30px; height: 30px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; margin-right: 12px; font-size: 14px;'>💡</span>
                        Descrizione del Progetto
                    </h3>
                    <div style='background: white; border-radius: 8px; padding: 20px; border: 1px solid #f59e0b;'>
                        <p style='color: #374151; font-size: 16px; line-height: 1.6; margin: 0; white-space: pre-wrap;'>{description}</p>
                    </div>
                </div>

                <!-- Action Buttons -->
                <div style='text-align: center; margin-bottom: 20px;'>
                    <a href='mailto:{richiedenteEmail}' style='background: #3b82f6; color: white; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-weight: 600; margin-right: 15px; display: inline-block; box-shadow: 0 4px 6px rgba(59, 130, 246, 0.3);'>
                        📧 Rispondi via Email
                    </a>
                    <a href='tel:{richiedenteTelefono.Replace(" ", "")}' style='background: #10b981; color: white; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3);'>
                        📞 Chiama Ora
                    </a>
                </div>

                <!-- Footer Info -->
                <div style='text-align: center; padding-top: 20px; border-top: 2px solid #e5e7eb;'>
                    <p style='color: #6b7280; font-size: 14px; margin: 0 0 5px 0;'>
                        <strong>Data richiesta:</strong> {DateTime.Now:dd MMMM yyyy - HH:mm}
                    </p>
                    <p style='color: #6b7280; font-size: 12px; margin: 0;'>
                        Questa email è stata generata automaticamente dal sistema di Five Innovation Hub.
                    </p>
                </div>
            </div>
        </div>";

			mailMessage.To.Add(senderEmail);
			smtpClient.Send(mailMessage);
		}

		//Metodo per inviare istruzioni al cliente via mail
		private void CustomerMail(string recipientEmail, string nome = "Cliente")
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
				From = new MailAddress(senderEmail, "Five Innovation Hub"),
				Subject = "✅ Richiesta Preventivo Ricevuta - FIH",
				IsBodyHtml = true,
			};

			mailMessage.Body = $@"
        <div style='max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; background-color: #f8fafc;'>
            <!-- Header -->
            <table width='100%' cellpadding='0' cellspacing='0' style='background: linear-gradient(135deg, #1f2937 0%, #374151 100%); border-radius: 16px 16px 0 0;'>
                <tr>
                    <td style='padding: 40px 20px; text-align: center;'>
                        <img style='width: 80px; height: auto; margin-bottom: 20px; display: block; margin-left: auto; margin-right: auto;' src='https://www.fiveinnovationhub.com/Content/Img/logo_orizz_sf.png' alt='FIH Logo'>
                        <h1 style='color: white; font-size: 28px; font-weight: 700; margin: 0 0 10px 0;'>Richiesta Ricevuta!</h1>
                        <p style='color: #d1d5db; font-size: 16px; margin: 0;'>Grazie per aver scelto Five Innovation Hub.</p>
                    </td>
                </tr>
            </table>

            <!-- Main Content -->
            <table width='100%' cellpadding='0' cellspacing='0' style='background: white; border-radius: 0 0 16px 16px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                <tr>
                    <td style='padding: 40px 30px;'>
                        
                        <!-- Success Icon Section -->
                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom: 30px;'>
                            <tr>
                                <td style='text-align: center;'>
                                    <!-- Success Icon with Table-based Centering -->
                                    <table cellpadding='0' cellspacing='0' style='width: 80px; height: 80px; background: #dbeafe; border-radius: 50%; margin: 0 auto 20px;'>
                                        <tr>
                                            <td style='text-align: center; vertical-align: middle; font-size: 36px; line-height: 80px;'>✅</td>
                                        </tr>
                                    </table>
                                    <h2 style='color: #1f2937; font-size: 24px; font-weight: 600; margin: 0 0 10px 0;'>Ciao {nome}!</h2>
                                    <p style='color: #6b7280; font-size: 16px; margin: 0;'>La tua richiesta è stata inviata con successo</p>
                                </td>
                            </tr>
                        </table>

                        <!-- Timeline -->
                        <table width='100%' cellpadding='0' cellspacing='0' style='background: #f8fafc; border-radius: 12px; margin-bottom: 30px; border: 2px solid #e2e8f0;'>
                            <tr>
                                <td style='padding: 25px;'>
                                    <h3 style='color: #1f2937; font-size: 18px; font-weight: 600; margin: 0 0 20px 0; text-align: center;'>📋 Prossimi Passi</h3>
                                    
                                    <!-- Step 1 -->
                                    <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom: 20px;'>
                                        <tr>
                                            <td style='width: 35px; vertical-align: top; padding-right: 15px;'>
                                                <table cellpadding='0' cellspacing='0' style='width: 35px; height: 35px; background: #3b82f6; border-radius: 50%;'>
                                                    <tr>
                                                        <td style='color: white; text-align: center; vertical-align: middle; font-weight: 600; font-size: 14px; line-height: 35px;'>1</td>
                                                    </tr>
                                                </table>
                                            </td>
                                            <td style='vertical-align: top;'>
                                                <strong style='color: #374151; display: block; margin-bottom: 4px;'>Analisi della Richiesta</strong>
                                                <span style='color: #6b7280; font-size: 14px;'>Nelle prossime 48h analizzeremo i dettagli del tuo progetto</span>
                                            </td>
                                        </tr>
                                    </table>
                                    
                                    <!-- Step 2 -->
                                    <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom: 20px;'>
                                        <tr>
                                            <td style='width: 35px; vertical-align: top; padding-right: 15px;'>
                                                <table cellpadding='0' cellspacing='0' style='width: 35px; height: 35px; background: #10b981; border-radius: 50%;'>
                                                    <tr>
                                                        <td style='color: white; text-align: center; vertical-align: middle; font-weight: 600; font-size: 14px; line-height: 35px;'>2</td>
                                                    </tr>
                                                </table>
                                            </td>
                                            <td style='vertical-align: top;'>
                                                <strong style='color: #374151; display: block; margin-bottom: 4px;'>Preventivo Personalizzato</strong>
                                                <span style='color: #6b7280; font-size: 14px;'>Riceverai un preventivo dettagliato via email</span>
                                            </td>
                                        </tr>
                                    </table>
                                    
                                    <!-- Step 3 -->
                                    <table width='100%' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td style='width: 35px; vertical-align: top; padding-right: 15px;'>
                                                <table cellpadding='0' cellspacing='0' style='width: 35px; height: 35px; background: #f59e0b; border-radius: 50%;'>
                                                    <tr>
                                                        <td style='color: white; text-align: center; vertical-align: middle; font-weight: 600; font-size: 14px; line-height: 35px;'>3</td>
                                                    </tr>
                                                </table>
                                            </td>
                                            <td style='vertical-align: top;'>
                                                <strong style='color: #374151; display: block; margin-bottom: 4px;'>Appuntamento di Approfondimento</strong>
                                                <span style='color: #6b7280; font-size: 14px;'>Fisseremo un meeting per approfondire ulteriori dettagli</span>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>

                        <!-- Contact Info -->
                        <table width='100%' cellpadding='0' cellspacing='0' style='background: linear-gradient(145deg, #fef7cd 0%, #fef3c7 100%); border-radius: 12px; margin-bottom: 30px;'>
                            <tr>
                                <td style='padding: 25px; text-align: center;'>
                                    <h3 style='color: #92400e; font-size: 18px; font-weight: 600; margin: 0 0 15px 0;'>💬 Hai Domande?</h3>
                                    <p style='color: #374151; margin: 0 0 20px 0;'>Non esitare a contattarci per qualsiasi chiarimento</p>
                                    
                                    <!-- Buttons -->
                                    <table cellpadding='0' cellspacing='0' style='margin: 0 auto;'>
                                        <tr>
                                            <td style='padding-right: 15px;'>
                                                <a href='mailto:info@fiveinnovationhub.com' style='background: #3b82f6; color: white; padding: 10px 20px; border-radius: 8px; text-decoration: none; font-weight: 500; display: inline-block;'>
                                                    📧 Email
                                                </a>
                                            </td>
                                            <td>
                                                <a href='https://wa.me/393880416518' style='background: #10b981; color: white; padding: 10px 20px; border-radius: 8px; text-decoration: none; font-weight: 500; display: inline-block;'>
                                                    📱 WhatsApp
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>

                        <!-- Footer -->
                        <table width='100%' cellpadding='0' cellspacing='0' style='border-top: 2px solid #e5e7eb; padding-top: 20px;'>
                            <tr>
                                <td style='text-align: center;'>
                                    <p style='color: #374151; font-size: 16px; font-weight: 600; margin: 0 0 10px 0;'>Grazie e a presto! 🚀</p>
                                    <p style='color: #6b7280; font-size: 12px; margin: 0;'>
                                        &copy; {DateTime.Now.Year} Five Innovation Hub. Tutti i diritti riservati.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </div>";

			mailMessage.To.Add(recipientEmail);
			smtpClient.Send(mailMessage);
		}

		//Metodo per inviare la mail di richiesta del pro pack
		[HttpPost]
		public ActionResult ProfessionalPack(string description, string nome, string cognome, string email, string telefono)
		{
			int id = Convert.ToInt16(TempData["IdProdotto"]);

			// Validazione dei campi obbligatori per utenti non autenticati
			if (!User.Identity.IsAuthenticated)
			{
				if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(cognome) ||
					string.IsNullOrEmpty(email) || string.IsNullOrEmpty(telefono))
				{
					TempData["Errore"] = "Tutti i campi sono obbligatori per inviare la richiesta";
					return RedirectToAction("Details", "Home", new { id = id });
				}

				// Validazione email
				if (!IsValidEmail(email))
				{
					TempData["Errore"] = "Inserisci un indirizzo email valido";
					return RedirectToAction("Details", "Home", new { id = id });
				}
			}

			if (!string.IsNullOrEmpty(description))
			{
				// Invia email a te con i dati del richiedente
				MailProPack(description, nome, cognome, email, telefono);

				// Invia email di conferma al richiedente
				string nomeRichiedente = User.Identity.IsAuthenticated ?
					db.Cliente.FirstOrDefault(x => x.Username == User.Identity.Name)?.Nome : nome;
				string emailRichiedente = User.Identity.IsAuthenticated ?
					db.Cliente.FirstOrDefault(x => x.Username == User.Identity.Name)?.Email : email;

				CustomerMail(emailRichiedente, nomeRichiedente);

				TempData["Successo"] = "Richiesta inviata con successo. Controlla la tua email";
				return RedirectToAction("Details", "Home", new { id = id });
			}

			TempData["Errore"] = "Il campo descrizione non può essere vuoto";
			return RedirectToAction("Details", "Home", new { id = id });
		}

		// Metodo helper per validare l'email
		private bool IsValidEmail(string email)
		{
			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			}
			catch
			{
				return false;
			}
		}
	}
}
