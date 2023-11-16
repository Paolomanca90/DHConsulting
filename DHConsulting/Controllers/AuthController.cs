using DHConsulting.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using GoogleAuthentication.Services;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PayPal.Api;
using System.Net.Mime;

namespace DHConsulting.Controllers
{
    public class AuthController : Controller
    {
        private ModelDb db = new ModelDb();

        //secret key per la generazione del token
        private static string SecretKey = Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()));
        private static readonly byte[] Key = Encoding.ASCII.GetBytes(SecretKey);

        // GET: Auth
        public ActionResult Index()
        {
            return View();
        }

        //View classica per la registrazione
        public ActionResult Register()
        {
            //Gestisco le credenziali per l'accesso tramite google
            string clientId = ConfigurationManager.AppSettings["IDClient"];
            var url = "https://localhost:44339/Auth/GoogleLogin";
            var response = GoogleAuth.GetAuthUrl(clientId, url);
            ViewBag.Response = response;
            return View();
        }

        //Metodo per registrare un nuovo utente
        [HttpPost]
        public ActionResult Register(Cliente c)
        {
            //verifiche su eventuali elementi uguali nel db
            if (ModelState.IsValid)
            {
                var user = db.Cliente.Where(x => x.Username == c.Username).FirstOrDefault();
                if (user != null)
                {
                    ViewBag.User = "Username già presente nel database";
                    return View();
                }
                var user1 = db.Cliente.Where(x => x.CF == c.CF).FirstOrDefault();
                if (user1 != null)
                {
                    ViewBag.User = "CF già presente nel database";
                    return View();
                }
                var user2 = db.Cliente.Where(x => x.Email == c.Email).FirstOrDefault();
                if (user2 != null)
                {
                    ViewBag.User = "Email già presente nel database";
                    return View();
                }
                //viene cryptata la password usando BCrypt
                c.Password = PasswordHasher.HashPassword(c.Password);
                c.CF = c.CF.ToUpper();
                //salvataggio nel db di utente e cliente contemporaneamente
                db.Cliente.Add(c);
                db.SaveChanges();
                Cliente cliente = db.Cliente.FirstOrDefault(x => x.Username == c.Username);
                //generazione del token da usare nel link di verifica inviato tramite mail
                //e impostato valido solo per 24h
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("userId", cliente.IdCliente.ToString()),
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenBytes = Encoding.UTF8.GetBytes(tokenHandler.WriteToken(token));
                var tokenString = tokenHandler.WriteToken(token);
                string confirmationLink = Url.Action("Confirm", "Auth", new { token = tokenString }, Request.Url.Scheme);
                Utente u = new Utente
                {
                    Username = c.Username,
                    Password = c.Password,
                    Role = "User",
                    FailedLoginAttempts = 0,
                    Confirmed = false,
                    Token = tokenBytes
                };
                db.Utente.Add(u);
                db.SaveChanges();
                FormsAuthentication.SetAuthCookie(c.Username, false);
                //invio email di conferma dell'account
                SendConfirmationEmail(c.Email, confirmationLink);
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Errore = "Errore durante la registrazione";
            return View();
        }

        //View classica per il login
        public ActionResult Login()
        {
            //Gestisco le credenziali per l'accesso tramite google
            string clientId = ConfigurationManager.AppSettings["IDClient"];
            var url = "https://localhost:44339/Auth/GoogleLogin";
            var response = GoogleAuth.GetAuthUrl(clientId, url);
            ViewBag.Response = response;
            return View();
        }

        //Metodo per il login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Username, string Password)
        {
            //viene controllato il numero di tentativi effettuato dall'utente
            var cliente = db.Utente.FirstOrDefault(x => x.Username == Username);
            if (cliente != null)
            {
                if (cliente.LockoutEndTime.HasValue && cliente.LockoutEndTime > DateTime.UtcNow)
                {
                    ViewBag.Errore = "Hai superato il limite di tentativi. Riprova dopo qualche minuto";
                    return View();
                }
                //viene controllata la password e se l'account risulta attivo
                if (PasswordHasher.VerifyPassword(Password, cliente.Password))
                {
                    if (cliente.Confirmed)
                    {
                        cliente.FailedLoginAttempts = 0;
                        db.SaveChanges();
                        FormsAuthentication.SetAuthCookie(Username, false);
                        if (cliente.Role == "Admin")
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ViewBag.Errore = "Non hai ancora attivato il tuo account. Controlla la mail";
                        return View();
                    }
                }
                //in caso di errori viene incrementato il numero di tentativi effettati e bloccato
                //l'utente in caso di superamento del terzo tentativo per un tempo di 15 minuti
                else
                {
                    cliente.FailedLoginAttempts++;
                    if (cliente.FailedLoginAttempts >= 3)
                    {
                        cliente.FailedLoginAttempts = 0;
                        cliente.LockoutEndTime = DateTime.UtcNow.AddSeconds(900);
                        db.SaveChanges();
                        ViewBag.Errore = "Numero massimo di tentativi raggiunti. Potrai riprovare tra 15 minuti";
                        return View();
                    }
                    db.SaveChanges();
                    ViewBag.Errore = "Password non corretta";
                    return View();
                }
            }
            ViewBag.Errore = "Nessun utente trovato";
            return View();
        }

        //Metodo per l'accesso tramite Google Login
        public async Task<ActionResult> GoogleLogin(string code)
        {
            try
            {
                string clientId = ConfigurationManager.AppSettings["IDClient"];
                string secretClient = ConfigurationManager.AppSettings["ClientSecret"];
                var url = "https://localhost:44339/Auth/GoogleLogin";
                var token = await GoogleAuth.GetAuthAccessToken(code, clientId, secretClient, url);
                var userProfile = await GoogleAuth.GetProfileResponseAsync(token.AccessToken.ToString());
                var googleUser = JsonConvert.DeserializeObject<GoogleProfile>(userProfile);
                if (googleUser != null)
                {
                    //se esiste un utente con quelle credenziali lo porto alla home
                    var utente = db.Cliente.FirstOrDefault(x => x.Email == googleUser.Email && x.CF != null);
                    if(utente != null)
                    {
                        FormsAuthentication.SetAuthCookie(utente.Username, false);
                        return RedirectToAction("Index", "Home");
                    }
                    //altrimenti lo registro con info placeholder
                    Utente u = new Utente
                    {
                        Username = googleUser.Given_Name + "google",
                        Password = PasswordHasher.HashPassword("Password.2023"),
                        Role = "User",
                        Confirmed = true
                    };
                    Cliente c = new Cliente
                    {
                        Nome = googleUser.Given_Name,
                        Cognome = googleUser.Family_Name,
                        DataNascita = new DateTime(1900, 01, 01),
                        Indirizzo = "via xxx 1",
                        Citta = "xxx",
                        Username = googleUser.Given_Name + "google",
                        Password = PasswordHasher.HashPassword("Password.2023"),
                        Email = googleUser.Email,
                    };
                    if (googleUser.MobilePhone != null)
                    {
                        c.Phone = googleUser.MobilePhone;
                    }
                    else
                    {
                        c.Phone = "1111111111";
                    }
                    db.Cliente.Add(c);
                    db.Utente.Add(u);
                    db.SaveChanges();
                    //e lo rimando alla pagina di modifica del profilo per completare i campi mancanti
                    Cliente cliente = db.Cliente.FirstOrDefault(x => x.Username == c.Username);
                    FormsAuthentication.SetAuthCookie(googleUser.Given_Name + "google", false);
                    TempData["Utente"] = "Completa la registrazione compilando i campi del form";
                    return RedirectToAction("EditProfilo", "Home", new { id = cliente.IdCliente});
                }
            }
            catch(Exception ex)
            {
                TempData["Errore"] = "Errore durante la procedura";
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        //Metodo per il logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Auth");
        }

        // Metodo per inviare l'email di conferma
        private void SendConfirmationEmail(string recipientEmail, string token)
        {
            var utente = db.Cliente.FirstOrDefault(x => x.Email == recipientEmail);
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
                From = new MailAddress(senderEmail, "PM Consulting"),
                Subject = "Conferma la tua registrazione",
                IsBodyHtml = true,
            };
            string logoPath = Server.MapPath("~/Content/Img/Logo-2.png");
            Attachment inlineLogo = new Attachment(logoPath);
            inlineLogo.ContentDisposition.Inline = true;
            inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
            inlineLogo.ContentId = "logo";
            mailMessage.Attachments.Add(inlineLogo);

            mailMessage.Body = $@"
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;'>
            <header style='text-align: center;>
                <img style='width: 100px; height: auto; max-width: 100%;' src='cid:logo' alt='logo'>
            </header>

            <main style='margin-top: 20px;'>
                <p style='color: #333333; font-size: 16px;'>Ciao {utente.Nome},</p>

                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Grazie per la tua registrazione. Per attivare il profilo clicca sul link sottostante.
                </p>

                <a href='{token}' style='display: inline-block; margin-top: 20px; padding: 10px 20px; 
                   background-color: #3498db; color: #ffffff; text-decoration: none; font-size: 14px; 
                   border-radius: 5px;'>
                   Completa registrazione
                </a>

                <p style='margin-top: 20px; color: #666666; font-size: 14px;'>Grazie</p>
            </main>
            
            <footer style='margin-top: 20px; text-align: center;'>
                <p style='color: #888888; font-size: 12px;'>&copy; {DateTime.Now.Year} PM Consulting. Tutti i diritti riservati.</p>
            </footer>
        </div>";

            mailMessage.To.Add(recipientEmail);
            smtpClient.Send(mailMessage);
        }

        //Metodo per inviare la mail di attivazione account
        private void ActivatedAccount(string recipientEmail)
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
                From = new MailAddress(senderEmail, "PM Consulting"),
                Subject = "Account attivato",
                IsBodyHtml = true,
            };
            string logoPath = Server.MapPath("~/Content/Img/Logo-2.png");
            Attachment inlineLogo = new Attachment(logoPath);
            inlineLogo.ContentDisposition.Inline = true;
            inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
            inlineLogo.ContentId = "logo";
            mailMessage.Attachments.Add(inlineLogo);

            mailMessage.Body = $@"
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;'>
            <header style='text-align: center;>
                <img style='width: 100px; height: auto; max-width: 100%;' src='cid:logo' alt='logo'>
            </header>

            <main style='margin-top: 20px;'>
                <p style='color: #333333; font-size: 16px;'>Grazie per aver attivato il tuo account.</p>

                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Da questo momento puoi effettuate i tuoi acquisti.
                </p>
            </main>
            
            <footer style='margin-top: 20px; text-align: center;'>
                <p style='color: #888888; font-size: 12px;'>&copy; {DateTime.Now.Year} PM Consulting. Tutti i diritti riservati.</p>
            </footer>
        </div>";

            mailMessage.To.Add(recipientEmail);

            smtpClient.Send(mailMessage);
        }

        //Conferma link registrazione
        public ActionResult Confirm(string token)
        {
            //viene inizializzato il gestore dei token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                //vengono assegnati i paramentri del token
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out validatedToken);
                //una volta validato il token viene estrapolato l'utente corrispondente
                var userId = int.Parse(principal.FindFirst("userId").Value);
                var cliente = db.Cliente.Find(userId);
                var utente = db.Utente.Where(x => x.Username == cliente.Username).FirstOrDefault();
                //viene controllata la corrispondenza tra il token dell'utente e il token presente nel link
                if (Convert.ToBase64String(Encoding.UTF8.GetBytes(token)) == Convert.ToBase64String(utente.Token))
                {
                    utente.Confirmed = true;
                    db.SaveChanges();
                    ActivatedAccount(cliente.Email);
                    FormsAuthentication.SetAuthCookie(cliente.Username, false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Token"] = "Token di conferma non valido.";
                    return View("Register");
                }
            }
            catch (Exception ex)
            {
                TempData["Token"] = "Token di conferma non valido.";
                return View("Register");
            }
        }

        //View classica per inserire l'email collegata all'account
        public ActionResult ChangePassword()
        {
            return View();
        }

        //Metodo per inviare il link di cambio password
        [HttpPost]
        public ActionResult ChangePassword(string email)
        {
            //se esiste un cliente corrispondente e il campo mail non è vuoto
            var cliente = db.Cliente.FirstOrDefault(x => x.Email == email);
            if (email != null && cliente != null)
            {
                var utente = db.Utente.FirstOrDefault(x => x.Username == cliente.Username);
                //imposto l'utente come non confermato
                utente.Confirmed = false;
                //genero un nuovo token
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("userId", utente.IdUtente.ToString()),
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenBytes = Encoding.UTF8.GetBytes(tokenHandler.WriteToken(token));
                //lo aggiorno all'interno dell'utente
                utente.Token = tokenBytes;
                db.SaveChanges();
                var tokenString = tokenHandler.WriteToken(token);
                //creo il link e lo invio tramite mail
                string confirmationLink = Url.Action("NewPassword", "Auth", new { token = tokenString }, Request.Url.Scheme);
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
                    From = new MailAddress(senderEmail, "PM Consulting"),
                    Subject = "Modifica password",
                    IsBodyHtml = true,
                };
                string logoPath = Server.MapPath("~/Content/Img/Logo-2.png");
                Attachment inlineLogo = new Attachment(logoPath);
                inlineLogo.ContentDisposition.Inline = true;
                inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                inlineLogo.ContentId = "logo";
                mailMessage.Attachments.Add(inlineLogo);

                mailMessage.Body = $@"
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;'>
            <header style='text-align: center;>
                <img style='width: 100px; height: auto; max-width: 100%;' src='cid:logo' alt='logo'>
            </header>

            <main style='margin-top: 20px;'>

                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Clicca sul seguente link per ripristinare la password.
                </p>

                <a href='{confirmationLink}' style='display: inline-block; margin-top: 20px; padding: 10px 20px; 
                   background-color: #3498db; color: #ffffff; text-decoration: none; font-size: 14px; 
                   border-radius: 5px;'>
                   Nuova password
                </a>

                <p style='margin-top: 20px; color: #666666; font-size: 14px;'>Grazie</p>
            </main>
            
            <footer style='margin-top: 20px; text-align: center;'>
                <p style='color: #888888; font-size: 12px;'>&copy; {DateTime.Now.Year} PM Consulting. Tutti i diritti riservati.</p>
            </footer>
        </div>";

                mailMessage.To.Add(cliente.Email);

                smtpClient.Send(mailMessage);
                return RedirectToAction("Login");
            }
            ViewBag.Email = "Inserisci un indirizzo email valido";
            return View();
        }

        //Metodo che reindirizza alla pagina di modifica mail
        public ActionResult NewPassword(string token)
        {
            //viene inizializzato il gestore dei token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                //vengono assegnati i paramentri del token
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out validatedToken);
                //una volta validato il token viene estrapolato l'utente corrispondente
                var userId = int.Parse(principal.FindFirst("userId").Value);
                var utente = db.Utente.Where(x => x.IdUtente == userId).FirstOrDefault();
                //viene controllata la corrispondenza tra il token dell'utente e il token presente nel link
                if (Convert.ToBase64String(Encoding.UTF8.GetBytes(token)) == Convert.ToBase64String(utente.Token))
                {
                    utente.Confirmed = true;
                    db.SaveChanges();
                    //viene salvato l'utente in una session e si viene reindirizzati
                    Session["Utente"] = utente;
                    return RedirectToAction("ModifyPassword");
                }
                //se il token non corrisponde viene generata un'eccezione
                else
                {
                    TempData["Token"] = "Token di conferma non valido.";
                    return RedirectToAction("Login");
                }
            }
            //se il token non corrisponde viene generata un'eccezione
            catch (Exception ex)
            {
                TempData["Token"] = "Token di conferma non valido.";
                return RedirectToAction("Login");
            }
        }

        //View classica per la modifica della password
        public ActionResult ModifyPassword()
        {
            return View();
        }

        //Metodo per inserire la nuova password
        [HttpPost]
        public ActionResult ModifyPassword(string NewPassword)
        {
            ModelDb db2 = new ModelDb();
            Utente utente = Session["Utente"] as Utente;
            //una volta recuperato l'utente nella sessione e verificato che non sia nullo
            if (utente != null)
            {
                Cliente c = db.Cliente.FirstOrDefault(x => x.Username == utente.Username);
                //viene validata la nuova password e controllato se non sia uguale alla precedente
                if (IsValidPassword(NewPassword))
                {
                    if (utente.Password != NewPassword)
                    {
                        //viene criptata e risalvata nel db impostando l'utente a confermato
                        utente.Password = PasswordHasher.HashPassword(utente.Password);
                        c.Password = PasswordHasher.HashPassword(utente.Password);
                        utente.Confirmed = true;
                        db2.SaveChanges();
                        //infine viene creato il cookie ed effettato il login
                        FormsAuthentication.SetAuthCookie(utente.Username, false);
                        return RedirectToAction("Index", "Home");
                    }
                    ViewBag.Password = "La password deve essere diversa dalla precedente";
                    return View();
                }
                ViewBag.Password = "La password deve contenere almeno 8 caratteri, una lettera minuscola, una lettera maiuscola, un numero e un carattere speciale tra .!?@&$%";
                return View();
            }
            ViewBag.Password = "Non è stato selezionato nessun account. Prova a rifare la procedura di recupero";
            return View();
        }

        //Metodo per valutare che la password rispetti i parametri richiesti
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < 8)
            {
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                return false;
            }

            if (!password.Any(c => ".!?@&$%".Contains(c)))
            {
                return false;
            }

            return true;
        }
    }
}