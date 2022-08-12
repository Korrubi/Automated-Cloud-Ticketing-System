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


namespace WorkerService1
{
    public class Plate
    {
        public String plateID { get; set; }
        public String voltype { get; set; }
        public String voladd { get; set; }
        public String datetime { get; set; }
    }

    public class ticketInformation
    {
        public String Color { get; set; }
        public String Make { get; set; }
        public String Model { get; set; }
        public String PreferredLanguage { get; set; }
        public String PlateNumber { get; set; }
        public String Violation { get; set; }
        public String ViolationAddress { get; set; }
        public String datetime {get; set; }
    }

    public class Worker : BackgroundService
    {


        //Queue URLs
        private const string upwardQueueURL = "https://sqs.us-east-1.amazonaws.com/990004435945/UpwardQueue";
        private const string downwardQueueURL = "https://sqs.us-east-1.amazonaws.com/990004435945/DownwardQueue";


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
        private const string aws_access_key_id = "AKIA6NAG43PU6XYOIYPT";
        private const string aws_secret_access_key = "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";
        //private const string aws_session_token = "FwoGZXIvYXdzEPz//////////wEaDLcZIiTgQHxyn/KmFSK/ASvIoKN/bUi/5RlX70vxdNH1eZ9IxPMGtrMI3YZVgM/eQ0HIPrGAgSXNg7QXMPQBXk8HzyefudRov/rg6AnwfdSxkI0FYSMEU/Eg1OcF0hvVKyP/39CQ/qE4ySuGPB6OoddPOQS1BBOodjUZbjGb1lebyoY/fRv2a3GEfBEMC49Tnk0TYn/HKqVjjvsJnfOgJC/nZzUdoeoze1RIEI/LR5+Q5k2jK0Zjw0WV9sswyXL/gYpe1JqSloKAnW1lGbmhKN+SgJUGMi0KCOrfz1Yje9uqPLqhoth74i5Rhl5psRuk+ZBEh36z4qDQEs1k2USq1Tqu94c=";

        //logpath to write to
        private const string logPath = @"F:\temp\DMVLog.log";


        //SessionAWSCredentials awsCredentials = new Amazon.Runtime.SessionAWSCredentials(aws_access_key_id, aws_secret_access_key, aws_session_token);


        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public static void parseDMV()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("F:\\DMVDatabase.xml");

            XmlElement root = xmlDoc.DocumentElement;

            XmlNode plate = root.SelectSingleNode("vehicle[@plate =\"" + queuePlateNumber + "\"]");

            if (plate != null)
            {
                XmlNode make = plate.SelectSingleNode("make");

                if (make != null)
                {
                    storeMake = make.InnerText;
                    WriteToLog("make: " + make.InnerText);
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

            using (AmazonSQSClient sqsClient = new AmazonSQSClient(aws_access_key_id, aws_secret_access_key, Amazon.RegionEndpoint.USEast1))
            {
                while (!stoppingToken.IsCancellationRequested)
                {

                    var plates = await getMessage(sqsClient, downwardQueueURL);
                    

                    if (plates.Messages.Any())
                    {

                        foreach (var message in plates.Messages)
                        {


                            Plate plateJson = new Plate();

                            WriteToLog("Message: " + message);
                            plateJson = JsonSerializer.Deserialize<Plate>(message.Body);

                            queuePlateNumber = plateJson.plateID;
                            violation = plateJson.voltype;
                            infractionAddress = plateJson.voladd;
                            dateTime = plateJson.datetime;


                            //WriteToLog("Plate Json: " + plateJson.plateID);

                            //WriteToLog("Message Body: " + message.Body);

                            //WriteToLog("Queue plateNumber: " + queuePlateNumber);
                            //comapare ID number with database 
                            parseDMV();
                            DeleteMessageRequest deleteMessageRequest = new DeleteMessageRequest()
                            {
                                QueueUrl = downwardQueueURL,
                                ReceiptHandle = message.ReceiptHandle,
                            };

                            DeleteMessageResponse deleteMessageResponse = await sqsClient.DeleteMessageAsync(deleteMessageRequest);
                            WriteToLog("Deleted from downward queue: " + downwardQueueURL);
                        }

                        ticketInformation ticket = new ticketInformation();

                        ticket.Color = storeColor;
                        ticket.Make = storeMake;
                        ticket.Model = storeModel;
                        ticket.PreferredLanguage = storePreferredLanguage;
                        ticket.PlateNumber = queuePlateNumber;
                        ticket.Violation = violation;
                        ticket.ViolationAddress = infractionAddress;
                        ticket.datetime = dateTime;

                        string ticketMessage = JsonSerializer.Serialize(ticket);

                        SendMessageRequest sendMessageRequest = new SendMessageRequest()
                        {
                            QueueUrl = upwardQueueURL,
                            MessageBody = ticketMessage,
                        };



                        WriteToLog("Posted Message: " + ticketMessage);
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
                WaitTimeSeconds = 10,
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