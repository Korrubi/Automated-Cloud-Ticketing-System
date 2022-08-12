using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Text.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TicketProcessingFunction;



public class Function
{
    public static string storePlateNumber = "";
    public static string storePreferredLanguage = "";
    public static string storeColor = "";
    public static string storeMake = "";
    public static string storeModel = "";
    public static string dateTime = "";
    public static string infractionAddress = "";
    public static string violation = "";
    public static string violationAmount = "";
    public static string ticketAmount = "";
    
    // Replace sender@example.com with your "From" address.
    // This address must be verified with Amazon SES.
    static readonly string senderAddress = "conner.bennett@bellevuecollege.edu";

    // Replace recipient@example.com with a "To" address. If your account
    // is still in the sandbox, this address must be verified.
    static readonly string receiverAddress = "conner.bennett@bellevuecollege.edu";

    // The subject line for the email.
    /*static readonly string subject = "Your vehicle was involved in a traffic violation. " +
        "Please pay the specified ticket amount by 30 days:";*/

    // The email body for recipients with non-HTML email clients.
    static readonly string textBody = "Your vehicle was involved in a traffic violation. " +
        "Please pay the specified ticket amount by 30 days:";

    /*    // The HTML body of the email.
        static string htmlBody = @"<html>
                                                <head>
                                                </head>
                                                <body>
                                                    <p> Vehicle:" + storeColor + storeMake + storeModel  + "</p>"
                                                   + "<p> License plate: " + storePlateNumber + "</p>"
                                                   + "<p> Date: [The date/time the violation took place]</p>"
                                                   + "<p> Violation address: [Address where the violation took place]</p>"
                                                   + "<p> Vilation type: [Type of violation]</p>"
                                                   + "<p> Ticket amount: [The ticket amount]</p>"
                                                + "</body>"
                                                + "</html>";*/

    public class ticketInformation
    {
        public String Color { get; set; }
        public String Make { get; set; }
        public String Model { get; set; }
        public String PreferredLanguage { get; set; }
        public String PlateNumber { get; set; }
        public String Violation { get; set; }
        public String ViolationAddress { get; set; }
        public String datetime { get; set; }
    }

    public static String languageCheck(String langugage)
    {
        String subjectbody = "test";
        if (langugage == "english")
        {

            subjectbody = "Your vehicle was involved in a traffic violation. Please pay the specified ticket amount by 30 days:";

        }
        if (langugage == "spanish")
        {
            subjectbody = "Su vehículo estuvo involucrado en una infracción de tráfico. Pague el monto del boleto especificado antes de los 30 días.";

        }
        if (langugage == "russian")
        {
            subjectbody = "Ваш автомобиль был причастен к нарушению правил дорожного движения. Пожалуйста, оплатите указанную сумму билета до 30 дней";

        }
        if (langugage == "french")
        {
            subjectbody = "Votre véhicule a été impliqué dans une infraction au code de la route. Veuillez payer le montant du billet spécifié dans les 30 jours";

        }


        return subjectbody;

    }

    public static void violationCheck()
    {
        if(violation == "no stop")
        {

            violationAmount = "$300.00";
            Console.WriteLine(violationAmount);
        }
        if (violation == "no full stop on right")
        {
            violationAmount = "$75.00";
            Console.WriteLine(violationAmount);
        }
        if (violation == "no right on red")
        {
            violationAmount = "$125.00";
            Console.WriteLine(violationAmount);
        }
    }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {

    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach(var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed message {message.Body}");

        ticketInformation ticket = new ticketInformation();
        ticket = JsonSerializer.Deserialize<ticketInformation>(message.Body);
        storePlateNumber = ticket.PlateNumber;
        Console.WriteLine(storePlateNumber);
        storePreferredLanguage = ticket.PreferredLanguage;
        storeColor = ticket.Color;
        storeMake = ticket.Make;
        storeModel = ticket.Model;
        violation = ticket.Violation;
        dateTime = ticket.datetime;
        infractionAddress = ticket.ViolationAddress;

        violationCheck();
        string subject = languageCheck(storePreferredLanguage);


        Console.WriteLine(violationAmount);
        // The HTML body of the email.
        string htmlBody = @"<html>
                                            <head>
                                            </head>
                                            <body>
                                                <p> Vehicle: " + storeColor + ", " + storeMake + ", " + storeModel + "</p>"
                                                   + "<p> License plate: " + storePlateNumber + "</p>"
                                                   + "<p> Date: " + dateTime + "</p>"
                                                   + "<p> Violation address: " + infractionAddress + "</p>"
                                                   + "<p> Vilation type: " + violation + "</p>"
                                                   + "<p> Ticket amount: " + violationAmount + "</p>"
                                                + "</body>"
                                                + "</html>";


        using (var client = new AmazonSimpleEmailServiceClient(RegionEndpoint.USEast1))
        {
            var sendRequest = new SendEmailRequest
            {
                Source = senderAddress,
                Destination = new Destination
                {
                    ToAddresses =
                    new List<string> { receiverAddress }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        },
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        }
                    }
                },
                // If you are not using a configuration set, comment
                // or remove the following line 
            };
            try
            {
                Console.WriteLine("Sending email using Amazon SES...");
                var response = await client.SendEmailAsync(sendRequest);
                Console.WriteLine("The email was sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("The email was not sent.");
                Console.WriteLine("Error message: " + ex.Message);

            }
        }

        Console.Write("Press any key to continue...");

        // TODO: Do interesting work based on the new message
        await Task.CompletedTask;
    }
}