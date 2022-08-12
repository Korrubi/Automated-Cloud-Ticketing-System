using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace WorkerService
{
    public class Plate
    {
        public String PlateID { get; set; }
    }

    public class ticketInformation
    {
        public String Color { get; set; }
        public String Make { get; set; }
        public String Model { get; set; }
        public String PreferredLanguage { get; set; }
        public String PlateNumber { get; set; }
    }

    public class Worker : BackgroundService
    {


        //Queue URLs
        private const string upwardQueueURL = "https://sqs.us-east-1.amazonaws.com/219420209588/UpwardQueue";
        private const string downwardQueueURL = "https://sqs.us-east-1.amazonaws.com/219420209588/Downward";

        //stored plate number in the downwardqueue 
        public static string queuePlateNumber = "";

        //Keeping track of civilian information
        public static string storePreferredLanguage = "";
        public static string storeColor = "";
        public static string storeMake = "";
        public static string storeModel = "";
        public static string dateTime = "";
        public static string infractionAddress = "";
        public static string violation = "";
        public static string ticketAmount = "";


        //DMV database plate number
        public static string storedDMVPlateNumber = "";

        //for aws credentials
        private const string aws_access_key_id = "ASIATGFTWWG2KO622N5B";
        private const string aws_secret_access_key = "mA5rtc6Tx2XQvuZTHr6x1QVAnCtEF/DqKgwgZaK1";
        private const string aws_session_token = "FwoGZXIvYXdzEPj//////////wEaDArMGErhJye+ABioCSK/AYAyz9Y+ks4k+bZam0FCVAKOFGaXQGpYG5p5FX8I8gjL2Du/vGMMueGAdKYmpPcdbTqbhdU8chjRQixmF6f9B/ep7gD0TYN6u9ej160yXZKEpSXpFKM2rPy6QCj9gDjR6hvZrQoJfD8smG2MYoJjFyaar/GPO4M5CDBnbCtDk+1bZcL4O3jCqW4apvHbOvLI59irD3mRn+l2iOA3r/LoAphWYNHJ+1ueXXmlob1sKViBeYyhvaMetdOfkELHNZF8KLOb/5QGMi0x1pYBkRlAjJtLPxxOiwJm5ivjaXxV1Czbi3AavpFsg6bqcu6PYF7H5TnCSbc=";

        //logpath to write to
        private const string logPath = @"F:\temp\DMVLog.log";


        SessionAWSCredentials awsCredentials = new Amazon.Runtime.SessionAWSCredentials(aws_access_key_id, aws_secret_access_key, aws_session_token);


        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public static void parseDMV()
        {
            //new xmlDocumnet
            XmlDocument xmlDoc = new XmlDocument();

            //loading text into xmlDoc
            using (StreamReader reader = new StreamReader("F:/DMVDatabase.xml"))
            {
                string content = reader.ReadToEnd();
                //Console.WriteLine(content);
                xmlDoc.LoadXml(content);
                XmlElement root = xmlDoc.DocumentElement;

                XmlNode plate = root.SelectSingleNode("vehicle[@plate =\"" + queuePlateNumber + "\"]");

                if (plate != null)
                {
                    XmlNode make = plate.SelectSingleNode("make");

                    if (make != null)
                    {
                        storeMake = make.InnerText;
                        //WriteToLog("make: " + make.InnerText);
                    }

                    XmlNode model = plate.SelectSingleNode("model");

                    if (model != null)
                    {
                        storeModel = model.InnerText;
                        Console.WriteLine("{0}", model.InnerText);
                    }

                    XmlNode color = plate.SelectSingleNode("color");

                    if (color != null)
                    {
                        storeColor = color.InnerText;
                        Console.WriteLine("{0}", color.InnerText);
                    }

                    XmlNode language = plate.SelectSingleNode("owner/@preferredLanguage");
                    if (language != null)
                    {
                        storePreferredLanguage = language.InnerText;
                        Console.WriteLine("{0}", language.InnerText);
                    }
                }
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (AmazonSQSClient sqsClient = new AmazonSQSClient(awsCredentials, Amazon.RegionEndpoint.USEast1))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var plates = await getMessage(sqsClient, downwardQueueURL);
                    if (plates != null)
                    {
                        foreach (var plate in plates.Messages)
                        {
                            WriteToLog("IN the fr loop");
                            Plate plateJson = new Plate();
                            WriteToLog("Here");

                            plateJson = JsonSerializer.Deserialize<Plate>(plate.Body);

                            WriteToLog("platebody:" + plate.Body);

                            WriteToLog("Here" + plateJson.PlateID);

                            queuePlateNumber = plateJson.PlateID;

                            WriteToLog("Queue Plate Number: " + queuePlateNumber);
                            WriteToLog("Read Message: " + plate);

                            //comapare ID number with database 
                            parseDMV();
                        }

                        ticketInformation ticket = new ticketInformation();

                        ticket.Color = storeColor;
                        ticket.Make = storeMake;
                        ticket.Model = storeModel;
                        ticket.PreferredLanguage = storePreferredLanguage;
                        ticket.PlateNumber = queuePlateNumber;

                        string ticketMessage = JsonSerializer.Serialize(ticket);

                        SendMessageRequest sendMessageRequest = new SendMessageRequest()
                        {
                            QueueUrl = upwardQueueURL,
                            MessageBody = ticketMessage,
                        };

                        SendMessageResponse sendMessageResponse = await sqsClient.SendMessageAsync(sendMessageRequest);
                        

                        //no survivors, purge them
                        queuePlateNumber = "";
                        storeColor = "";
                        storeMake = "";
                        storeModel = "";
                        storePreferredLanguage = "";

                    }
                }
            }
        }

        private static async Task<ReceiveMessageResponse> getMessage(IAmazonSQS sqsClient, string queueURL)
        {
            //for long polling 20 seconds 
            return await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueURL,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 1
            });

        }

        public static void WriteToLog(string message)
        {
            string text = String.Format("{0}:{1}", DateTime.Now, message);
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine(text);
            }
        }
    }
}
