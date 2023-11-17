using DHConsulting.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.RecaptchaEnterprise.V1;

namespace DHConsulting.Models
{
    public class CreateAssessmentSample
    {
        // Crea una valutazione per analizzare il rischio di un'azione della UI.
        // projectID: L'ID del tuo progetto Google Cloud.
        // recaptchaKey: La chiave reCAPTCHA associata al sito o all'app
        // token: Il token generato ottenuto dal client.
        // recaptchaAction: Nome dell'azione corrispondente al token.
        public void createAssessment(string projectID = "pm-consulting-1700218714780", string recaptchaKey = "6LfcuxIpAAAAAFpTCHO5H1LIUL0GpSCNLPIvzhge", string token = "action-token", string recaptchaAction = "action-name")
        {
            // Crea il client reCAPTCHA.
            // DA FARE: memorizza nella cache il codice di generazione del client (consigliato) o chiama client.close() prima di uscire dal metodo.
            RecaptchaEnterpriseServiceClient client = RecaptchaEnterpriseServiceClient.Create();

            ProjectName projectName = new ProjectName(projectID);

            // Crea la richiesta di valutazione.
            CreateAssessmentRequest createAssessmentRequest = new CreateAssessmentRequest()
            {
                Assessment = new Assessment()
                {
                    // Imposta le proprietà dell'evento da monitorare.
                    Event = new Event()
                    {
                        SiteKey = recaptchaKey,
                        Token = token,
                        ExpectedAction = recaptchaAction
                    },
                },
                ParentAsProjectName = projectName
            };

            Assessment response = client.CreateAssessment(createAssessmentRequest);

            // Verifica che il token sia valido.
            if (response.TokenProperties.Valid == false)
            {
                System.Console.WriteLine("The CreateAssessment call failed because the token was: " +
                    response.TokenProperties.InvalidReason.ToString());
                return;
            }

            // Controlla se è stata eseguita l'azione prevista.
            if (response.TokenProperties.Action != recaptchaAction)
            {
                System.Console.WriteLine("The action attribute in reCAPTCHA tag is: " +
                    response.TokenProperties.Action.ToString());
                System.Console.WriteLine("The action attribute in the reCAPTCHA tag does not " +
                    "match the action you are expecting to score");
                return;
            }

            // Ottieni il punteggio di rischio e i motivi.
            // Per ulteriori informazioni sull'interpretazione del test, consulta:
            // https://cloud.google.com/recaptcha-enterprise/docs/interpret-assessment
            System.Console.WriteLine("The reCAPTCHA score is: " + ((decimal)response.RiskAnalysis.Score));

            foreach (RiskAnalysis.Types.ClassificationReason reason in response.RiskAnalysis.Reasons)
            {
                System.Console.WriteLine(reason.ToString());
            }
        }

        public static void Main(string[] args)
        {
            new CreateAssessmentSample().createAssessment();
        }
    }
}