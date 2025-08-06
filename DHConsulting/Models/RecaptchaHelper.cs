using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace DHConsulting.Models
{
	public class RecaptchaHelper
	{
		private static readonly string SecretKey = ConfigurationManager.AppSettings["RecaptchaSecretKey"];
		private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

		/// <summary>
		/// Verifica la risposta del reCAPTCHA con i server di Google
		/// </summary>
		/// <param name="recaptchaResponse">Il token di risposta del reCAPTCHA</param>
		/// <param name="userIpAddress">L'indirizzo IP dell'utente (opzionale)</param>
		/// <returns>True se la verifica è riuscita, False altrimenti</returns>
		public static bool VerifyRecaptcha(string recaptchaResponse, string userIpAddress = null)
		{
			if (string.IsNullOrEmpty(recaptchaResponse))
			{
				return false;
			}

			if (string.IsNullOrEmpty(SecretKey))
			{
				throw new InvalidOperationException("RecaptchaSecretKey non configurata nel web.config");
			}

			try
			{
				// Prepara i parametri per la richiesta
				string postData = $"secret={SecretKey}&response={recaptchaResponse}";

				if (!string.IsNullOrEmpty(userIpAddress))
				{
					postData += $"&remoteip={userIpAddress}";
				}

				// Crea la richiesta HTTP
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(VerifyUrl);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = postData.Length;

				// Invia i dati
				using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
				{
					writer.Write(postData);
				}

				// Legge la risposta
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				using (StreamReader reader = new StreamReader(response.GetResponseStream()))
				{
					string jsonResponse = reader.ReadToEnd();
					RecaptchaVerificationResult result = JsonConvert.DeserializeObject<RecaptchaVerificationResult>(jsonResponse);

					return result.Success;
				}
			}
			catch (WebException ex)
			{
				// Log l'errore (puoi usare il tuo sistema di logging preferito)
				System.Diagnostics.Debug.WriteLine($"Errore nella verifica reCAPTCHA: {ex.Message}");
				return false;
			}
			catch (Exception ex)
			{
				// Log l'errore generico
				System.Diagnostics.Debug.WriteLine($"Errore generico nella verifica reCAPTCHA: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Ottiene l'indirizzo IP reale del client considerando proxy e load balancer
		/// </summary>
		/// <param name="request">L'HttpRequest corrente</param>
		/// <returns>L'indirizzo IP del client</returns>
		public static string GetClientIpAddress(HttpRequestBase request)
		{
			// Controlla gli header comuni per l'IP reale quando si usa un proxy/load balancer
			string[] ipHeaders = {
				"HTTP_CF_CONNECTING_IP",     // Cloudflare
                "HTTP_X_FORWARDED_FOR",      // Proxy standard
                "HTTP_X_FORWARDED",          // Proxy alternativo
                "HTTP_X_CLUSTER_CLIENT_IP",  // Cluster
                "HTTP_FORWARDED_FOR",        // Proxy alternativo
                "HTTP_FORWARDED",            // RFC 7239
                "REMOTE_ADDR"                // IP diretto
            };

			foreach (string header in ipHeaders)
			{
				string ip = request.ServerVariables[header];
				if (!string.IsNullOrEmpty(ip))
				{
					// Se ci sono più IP separati da virgola, prendi il primo
					if (ip.Contains(","))
					{
						ip = ip.Split(',')[0].Trim();
					}

					// Verifica che sia un IP valido
					if (IsValidIpAddress(ip))
					{
						return ip;
					}
				}
			}

			// Fallback all'IP del request
			return request.UserHostAddress ?? "127.0.0.1";
		}

		/// <summary>
		/// Verifica se una stringa è un indirizzo IP valido
		/// </summary>
		/// <param name="ipAddress">La stringa da verificare</param>
		/// <returns>True se è un IP valido, False altrimenti</returns>
		private static bool IsValidIpAddress(string ipAddress)
		{
			if (string.IsNullOrWhiteSpace(ipAddress))
				return false;

			return IPAddress.TryParse(ipAddress, out _);
		}
	}

	/// <summary>
	/// Classe per deserializzare la risposta JSON di Google reCAPTCHA
	/// </summary>
	public class RecaptchaVerificationResult
	{
		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("challenge_ts")]
		public DateTime? ChallengeTimestamp { get; set; }

		[JsonProperty("hostname")]
		public string Hostname { get; set; }

		[JsonProperty("error-codes")]
		public string[] ErrorCodes { get; set; }

		[JsonProperty("score")]
		public float? Score { get; set; } // Per reCAPTCHA v3

		[JsonProperty("action")]
		public string Action { get; set; } // Per reCAPTCHA v3
	}
}