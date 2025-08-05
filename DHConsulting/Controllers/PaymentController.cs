using DHConsulting.Models;
using Newtonsoft.Json;
using PayPal.Api;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Net.Mime;

namespace DHConsulting.Controllers
{
    [Authorize(Roles = "User")]
    public class PaymentController : Controller
    {
        private ModelDb db = new ModelDb();

        // GET: Payment
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        //View classica per mostrare gli elementi presenti nel carrello
        public ActionResult Cart()
        {
            List<Dettaglio> carrello = CartFromCookie();
            List<Prodotto> lista = new List<Prodotto>();
            foreach (Dettaglio d in carrello)
            {
                Prodotto p = db.Prodotto.Find(d.IdProdotto);
                lista.Add(p);
            }
            ViewBag.Prodotti = lista;
            return View(carrello);
        }

        //Metodo per confermare il pagamento
        public ActionResult ConfirmPayment(string Cancel = null)
        {
            //genero le credenziali del pagamento
            APIContext api = PaypalConfiguration.GetAPIContext();
            try
            {
                string payerId = Request.Params["PayerID"];
                if (string.IsNullOrEmpty(payerId))
                {
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/Payment/ConfirmPayment?";
                    var guid = Convert.ToString((new Random().Next(100000)));
                    var createdPayment = CreatePayment(api, baseURI + "guid=" + guid);
                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = lnk.href;
                        }
                    }
                    Session["payment"] = createdPayment.id;
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(api, payerId, Session["payment"] as string);
                    //se l'ordine non va a buon fine torno al carrello
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return RedirectToAction("Cart");
                    }
                    //in caso di approvazione salvo l'ordine nel db
                    List<Dettaglio> carrello = CartFromCookie();
                    Cliente c = db.Cliente.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                    Ordine o = new Ordine();
                    o.IdCliente = c.IdCliente;
                    db.Ordine.Add(o);
                    //salvo un record per ogni elemento nel carrello
                    foreach (Dettaglio dettaglio in carrello)
                    {
                        dettaglio.IdOrdine = o.IdOrdine;
                        dettaglio.Prodotto = null;
                        db.Dettaglio.Add(dettaglio);
                    }
                    db.SaveChanges();
                    //salvo il pdf con i dettagli dell'ordine appena creato
                    o.InvoicePdf = GenerateOrderPdf(o.IdOrdine);
                    RecapEmail(c.Email, o.InvoicePdf);
                    MailConsultingPack();
                    db.SaveChanges();
                    //elimino il cookie e quindi il carrello
                    DeleteCart();
                    TempData["Successo"] = "Ordine completato con successo. Controlla la mail per i dettagli.";
                    return RedirectToAction("Cart");
                }
            }
            catch
            {
                TempData["Errore"] = "Il pagamento non è stato approvato.";
                return RedirectToAction("Cart");
            }
        }

        private Payment payment;

        //Metodo per eseguire il pagamento
        public Payment ExecutePayment(APIContext api, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };
            payment = new Payment()
            {
                id = paymentId
            };
            return payment.Execute(api, paymentExecution);
        }

        //Metodo per avviare il nuovo pagamento tramite PP
        private Payment CreatePayment(APIContext api, string redirectUrl)
        {
            //recupero il carrello e creo una riga per ogni elemento
            var itemList = new ItemList { items = new List<Item>() };
            List<Dettaglio> carrello = CartFromCookie();
            decimal totale = 0;
            foreach (var p in carrello)
            {
                Prodotto prodotto = db.Prodotto.Find(p.IdProdotto);
                itemList.items.Add(new Item
                {
                    name = prodotto.DescrizioneBreve,
                    currency = "EUR",
                    price = prodotto.Costo.ToString("0.00", CultureInfo.InvariantCulture),
                    quantity = p.Quantita.ToString(),
                    sku = prodotto.IdProdotto.ToString()
                });
                totale += prodotto.Costo * p.Quantita;
            }
            //inserisco il metodo di pagamento
            var payer = new Payer()
            {
                payment_method = "paypal"
            };
            //stabilisco il totale
            var amount = new Amount
            {
                currency = "EUR",
                total = totale.ToString("0.00", CultureInfo.InvariantCulture)
            };
            //genero i link di successo e cancellazione
            var redirectUrls = new RedirectUrls
            {
                return_url = redirectUrl,
                cancel_url = redirectUrl + "&Cancel=true",
            };
            //genero la transazione
            var lastId = db.Ordine.OrderByDescending(o => o.IdOrdine).FirstOrDefault();
            var invoiceId = 1;
            if (lastId != null)
            {
                invoiceId += lastId.IdOrdine;
            }
            var transactionList = new List<Transaction>();
            transactionList.Add(new Transaction
            {
                description = "Ordine Five Innovation Hub",
                invoice_number = invoiceId.ToString(),
                amount = amount,
                item_list = itemList
            });
            payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirectUrls
            };
            return payment.Create(api);
        }

		[AllowAnonymous]
		//Metodo per eliminare il carrello e il cookie
		public ActionResult DeleteCart()
        {
            HttpCookie carrelloCookie = HttpContext.Request.Cookies["carrello"];
            //segno il cookie come scaduto
            carrelloCookie.Expires = DateTime.Now.AddYears(-1);
            HttpContext.Response.Cookies.Add(carrelloCookie);
            return RedirectToAction("Cart");
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

        //Metodo per la creazione del pdf usando la libreria PdfSharp
        public byte[] GenerateOrderPdf(int id)
        {
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            //generazione dei vari font
            XFont titleFont = new XFont("Arial", 24, XFontStyle.Bold);
            XFont sectionFont = new XFont("Arial", 16, XFontStyle.Bold);
            XFont headerFont = new XFont("Arial", 12, XFontStyle.Bold);
            XFont regularFont = new XFont("Arial", 12);
            XFont smallFont = new XFont("Arial", 11);
            XFont coursiveFont = new XFont("Arial", 11, XFontStyle.Italic);
            XFont subtitleFont = new XFont("Montserrat", 12);

            double pageWidth = page.Width;
            double yPosition = 40;
            double lineHeight = 20;
            double smallLineHeight = 15;
            double tableMargin = 40;
            double cellPadding = 5;
            //generazione del logo e della scritta sottostante
            XImage logo = XImage.FromFile(Server.MapPath("~/Content/Img/logo_orizz_sf.png"));
            double logoX = (pageWidth - 200) / 2;
            double logoY = yPosition;
            gfx.DrawImage(logo, logoX, logoY, 200, 90);
            yPosition += 110;
            string titleText = "Five Innovation Hub";
            int spacing = 2;
            string spacedText = string.Join(new string(' ', spacing), titleText.ToCharArray());
            gfx.DrawString(spacedText, subtitleFont, XBrushes.OrangeRed,
                new XRect(0, yPosition, pageWidth, 20), XStringFormats.TopCenter);
            yPosition += 90;
            //generazione del totolo con il numero d'ordine
            gfx.DrawString("Ordine Numero: " + (id+100), titleFont, XBrushes.Black,
                new XRect(0, yPosition, pageWidth, 40), XStringFormats.TopCenter);
            yPosition += 90;
            //sezione relativa al cliente con dati personali e info sulla fattura
            gfx.DrawString("Fattura:", headerFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
            yPosition += lineHeight;
            string twoDigitYear = (DateTime.Now.Year % 100).ToString("D2");
            gfx.DrawString($"nr. FPR {id}/{twoDigitYear} del {DateTime.Now.ToShortDateString()}", regularFont, XBrushes.Black,
                new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
            yPosition += lineHeight;
            gfx.DrawString($"Data invio: {DateTime.Now.ToShortDateString()}", regularFont, XBrushes.Black,
                new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
            yPosition += 30;
            var ordine = db.Ordine.Find(id);
            var cliente = ordine.Cliente;
            if (cliente != null)
            {
                gfx.DrawString($"Dettagli cliente:", headerFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
                yPosition += lineHeight;
                gfx.DrawString($"Nome: {cliente.Nome} {cliente.Cognome}", regularFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
                yPosition += lineHeight;
                gfx.DrawString($"Indirizzo: {cliente.Indirizzo}, {cliente.Citta}", regularFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
                if (cliente.Piva != null)
                {
                    yPosition += lineHeight;
                    gfx.DrawString($"P.Iva: {cliente.Piva}", regularFont, XBrushes.Black,
                        new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
                }
                else
                {
                    yPosition += lineHeight;
                    gfx.DrawString($"CF: {cliente.CF}", regularFont, XBrushes.Black,
                        new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, lineHeight), XStringFormats.TopRight);
                }
            }
            yPosition += 60;
            //sezione centrale con il dettaglio dell'ordine in formato tabellare
            gfx.DrawString("Dettagli dell'ordine", sectionFont, XBrushes.Black,
                new XRect(tableMargin, yPosition, pageWidth - 2 * tableMargin, 0), XStringFormats.TopLeft);
            yPosition += 40;
            List<Dettaglio> carrello = CartFromCookie();
            decimal totale = 0;
            double col1X = tableMargin;
            double col2X = 300;
            double col3X = 400;
            void DrawCell(XGraphics g, XRect rect, string text, XFont font, XBrush brush)
            {
                g.DrawRectangle(XPens.Black, XBrushes.Transparent, rect);
                g.DrawString(text, font, brush, new XRect(rect.Left + cellPadding, rect.Top + cellPadding, rect.Width - 2 * cellPadding, rect.Height - 2 * cellPadding), XStringFormats.TopLeft);
            }
            //intestazioni della tabella
            DrawCell(gfx, new XRect(col1X, yPosition, col2X - col1X, lineHeight), "Prodotto", headerFont, XBrushes.Black);
            DrawCell(gfx, new XRect(col2X, yPosition, col3X - col2X, lineHeight), "Quantità", headerFont, XBrushes.Black);
            DrawCell(gfx, new XRect(col3X, yPosition, pageWidth - col3X - tableMargin, lineHeight), "Prezzo", headerFont, XBrushes.Black);
            yPosition += lineHeight;
            //viene generata una riga per ogni elemento dell'ordine
            foreach (Dettaglio dettaglio in carrello)
            {
                var prodotto = db.Prodotto.Find(dettaglio.IdProdotto);
                string prodottoText = $"{prodotto.DescrizioneBreve}";
                string quantitaText = $"{dettaglio.Quantita}";
                string prezzoText = $"{prodotto.Costo * dettaglio.Quantita:C}";
                DrawCell(gfx, new XRect(col1X, yPosition, col2X - col1X, lineHeight), prodottoText, regularFont, XBrushes.Black);
                DrawCell(gfx, new XRect(col2X, yPosition, col3X - col2X, lineHeight), quantitaText, regularFont, XBrushes.Black);
                DrawCell(gfx, new XRect(col3X, yPosition, pageWidth - col3X - tableMargin, lineHeight), prezzoText, regularFont, XBrushes.Black);
                yPosition += lineHeight;
                totale += prodotto.Costo * dettaglio.Quantita;
            }
            yPosition += 50;
            double totalX = pageWidth - 200 - tableMargin;
            //sezione relativa al totale
            gfx.DrawString("Totale: " + totale.ToString("C"), sectionFont, XBrushes.Black,
                new XRect(totalX, yPosition, 200, 0), XStringFormats.TopRight);
            yPosition += 60;
            //sezione relativa ai dati del fornitore
            gfx.DrawString($"Data: {DateTime.Now.ToShortDateString()}", smallFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth, lineHeight), XStringFormats.TopLeft);
            yPosition += smallLineHeight;
            gfx.DrawString("Five Innovation Hub, LDA", coursiveFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth, lineHeight), XStringFormats.TopLeft);
            yPosition += smallLineHeight;
            gfx.DrawString("P.IVA: PT518557332", smallFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth, lineHeight), XStringFormats.TopLeft);
            yPosition += smallLineHeight;
            gfx.DrawString("Av. São Gonçalo nº 1614, 4835-105, Guimarães", smallFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth, lineHeight), XStringFormats.TopLeft);
            yPosition += smallLineHeight;
            gfx.DrawString("info@fiveinnovationhub.com", smallFont, XBrushes.Black,
                    new XRect(tableMargin, yPosition, pageWidth, lineHeight), XStringFormats.TopLeft);

            MemoryStream stream = new MemoryStream();
            document.Save(stream, false);
            return stream.ToArray();
        }

        //Metodo per il download del pdf
        public ActionResult DownloadOrderPdf(int id)
        {
            byte[] pdfBytes = db.Ordine.FirstOrDefault(x => x.IdOrdine == id).InvoicePdf;
            if (pdfBytes != null)
            {
                Response.AppendHeader("Content-Disposition", $"inline; filename=Ordine-{id}.pdf");
                return File(pdfBytes, "application/pdf");
            }
            return Content("Il PDF non è disponibile.");
        }

        //Metodo per inviare la mail di recap al cliente
        private void RecapEmail(string recipientEmail, byte[] pdf)
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
                Subject = "Dettaglio dell'aquisto",
                IsBodyHtml = true,
            };

            mailMessage.Body = $@"
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;'>
            <header style='text-align: center;'>
                <img style='width: 100px; height: auto; max-width: 100%;' src='https://www.fiveinnovationhub.com/Content/Img/logo_orizz_sf.png' alt='logo'>
            </header>

            <main style='margin-top: 20px;'>
                <p style='color: #333333; font-size: 16px;'>Grazie per aver acquistato il tuo pacchetto.</p>

                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    Entro 48h verrai contattato per fissare il tuo appuntamento di consulenza.
                </p>
                <p style='margin-top: 10px; color: #666666; font-size: 14px;'>
                    In allegato a questa mail trovi anche la fattura relativa al tuo ordine
                </p>
            </main>
            
            <footer style='margin-top: 20px; text-align: center;'>
                <p style='color: #888888; font-size: 12px;'>&copy; {DateTime.Now.Year} Five Innovation Hub. Tutti i diritti riservati.</p>
            </footer>
        </div>";

            mailMessage.To.Add(recipientEmail);
            mailMessage.Attachments.Add(new Attachment(new MemoryStream(pdf), "Ordine.pdf"));

            smtpClient.Send(mailMessage);
        }

        //Metodo per inviare a me una mail di avvenuto acquisto
        private void MailConsultingPack()
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
                From = new MailAddress(senderEmail, "Five Innovation Hub"),
                Subject = "Pacchetto consulenza acquistato",
                Body = @"
                        <section class=""max-w-2xl px-6 py-8 mx-auto bg-white dark:bg-gray-900"">
                            <main class=""mt-8"">
                                <h2 class=""text-gray-700 dark:text-gray-200"">Pacchetto consulenza</h2>

                                <p class=""text-gray-500 dark:text-gray-400"">
                                    Acquistato da " + utente.Nome + " " + utente.Cognome + @"
                                </p>
                                <p class=""mt-1 leading-loose text-gray-600 dark:text-gray-300"">
                                    Email: " + utente.Email + @"
                                </p>
                                <p class=""mt-1 leading-loose text-gray-600 dark:text-gray-300"">
                                    Telefono: " + utente.Phone + @"
                                </p>
                            </main>
                            <footer class=""mt-8"">
                                <p class=""mt-3 text-gray-500 dark:text-gray-400"">" + DateTime.Now + @"</p>
                            </footer>
                        </section>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(senderEmail);

            smtpClient.Send(mailMessage);
        }
    }
}