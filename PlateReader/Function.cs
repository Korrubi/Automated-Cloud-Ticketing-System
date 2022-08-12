using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PlateReader;

public class Plate
{
    public String plateID { get; set; }
    public String voltype { get; set; }
    public String voladd { get; set; }
    public String datetime { get; set; }
}

public class Function
{
    IAmazonS3 S3Client { get; set; }

    private const string sourceBucket = "cs455-project3-conner";
    private const string destinationBucket = "outofstate-bucket-conner";
    // Specify your bucket region (an example region is shown).
    private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast1;
    private static readonly IAmazonS3? s3Client = new AmazonS3Client(bucketRegion);


    public static string vtype = "";
    public static string vadd = "";
    public static string datetime = "";
    public static string newPlateID = "";
    public static string plateNumber = "";
    public static string plateState = "";
    private const string downwardQueueURL = "https://sqs.us-east-1.amazonaws.com/990004435945/DownwardQueue";


    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// 

    /*    private static async Task SendMessage(IAmazonSQS sqsClient, string qUrl, string messageBody)
        {
            SendMessageResponse sendMessage = await sqsClient.SendMessageAsync(qUrl, messageBody);
            Console.WriteLine($"Message added to queue {qUrl}");
        }*/




    private static async Task CopyingObjectAsync(string key)
    {
        try
        {
            CopyObjectRequest request = new CopyObjectRequest
            {
                SourceBucket = sourceBucket,
                SourceKey = key,
                DestinationBucket = destinationBucket,
                DestinationKey = key
            };
            CopyObjectResponse response = await s3Client.CopyObjectAsync(request);
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
        }
    }
    static async Task ReadObjectDataAsync(string key)
    {
        string responseBody = "";
        try
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = sourceBucket,
                Key = key
            };
            using (GetObjectResponse response = await s3Client.GetObjectAsync(request))
            using (Stream responseStream = response.ResponseStream)
            using (StreamReader reader = new StreamReader(responseStream))
            {
                datetime = response.Metadata["x-amz-meta-datetime"]; // Assume you have "title" as medata added to the object.
                vtype = response.Metadata["x-amz-meta-violation"]; // Assume you have "title" as medata added to the object.
                vadd = response.Metadata["x-amz-meta-location"]; // Assume you have "title" as medata added to the object.
                //string contentType = response.Headers["Content-Type"];


                Console.WriteLine("Object metadata, Title: {0}", datetime);
                Console.WriteLine("Object metadata, Title: {0}", vtype);
                Console.WriteLine("Object metadata, Title: {0}", vadd);
                //Console.WriteLine("Content type: {0}", contentType);

                responseBody = reader.ReadToEnd(); // Now you process the response body.
            }
        }
        catch (AmazonS3Exception e)
        {
            // If bucket or object does not exist
            Console.WriteLine("Error encountered ***. Message:'{0}' when reading object", e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
        }
    }


    public static async Task readPlateAsync(string bucketName, string photoName)
    {
        var rekognitionClient = new AmazonRekognitionClient();

        var detectTextRequest = new DetectTextRequest()
        {
            Image = new Image()
            {
                S3Object = new Amazon.Rekognition.Model.S3Object()
                {
                    Name = photoName,
                    Bucket = bucketName,
                },
            },
        };

        try
        {
            DetectTextResponse detectTextResponse = await rekognitionClient.DetectTextAsync(detectTextRequest);
            Console.WriteLine($"Detected lines and words for {photoName}");
            detectTextResponse.TextDetections.ForEach(text =>
            {
                if (text.DetectedText == "California" && text.Type == "WORD")
                {
                    plateState = text.DetectedText;
                    Console.WriteLine(plateState);
                }

                if (text.DetectedText.Length == 7 && text.Type == "WORD")
                {
                    plateNumber = text.DetectedText;
                    Console.WriteLine(plateNumber);
                  
                    Console.WriteLine("New Plate ID", newPlateID);
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task<string?> FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Event = evnt.Records?[0].S3;
        if (s3Event == null)
        {
            return null;
        }

        try
        {

            var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
            Console.WriteLine("Bucket: {0}", s3Event.Bucket.Name);
            Console.WriteLine("File: {0}", s3Event.Object.Key);

            string bucketName = s3Event.Bucket.Name;
            string objectKey = s3Event.Object.Key;

            await readPlateAsync(bucketName, objectKey);

            

            var sqsClient = new AmazonSQSClient();

            if (plateState == "California" || plateState == "california" || plateState == "\"California\"" || plateState == "\"california\"")
            {
                await ReadObjectDataAsync(objectKey);

                Plate plateJson = new Plate
                {
                    plateID = plateNumber,
                    voltype = vtype,
                    voladd = vadd,
                    datetime = datetime
                };

                newPlateID = JsonSerializer.Serialize(plateJson);

                SendMessageRequest sendMessageRequest = new SendMessageRequest()
                {
                    QueueUrl = downwardQueueURL,
                    MessageBody = newPlateID,
                };

                SendMessageResponse sendMessageResponse = await sqsClient.SendMessageAsync(sendMessageRequest);
            }

            else
            {
                Console.WriteLine("Copying an object");
                await CopyingObjectAsync(objectKey);

            }

            plateNumber = "";
            plateState = "";

            return response.Headers.ContentType;
        }
        catch (Exception e)
        {
            context.Logger.LogInformation($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogInformation(e.Message);
            context.Logger.LogInformation(e.StackTrace);
            throw;
        }
    }

    private object CopyingObjectAsync()
    {
        throw new NotImplementedException();
    }
}