using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.Threading.Tasks;

namespace UploadData
{
    internal class Program
    {
        private const string bucketName = "cs455-project3-conner";
        private static string filePath = "";

        static async Task Main(string[] args)
        {
            filePath = args[0];
            // Get credentials to use the authenticate ourselvs to AWS
            AWSCredentials credentials = GetAWSCredentialsByName("default");

            //Get an object that allows us to interact with some AWS service. In this case we want to interact with S3
            using (AmazonS3Client s3Client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
            {
                var fileTransferUtility = new TransferUtility(s3Client);
                // Upload a file. The file name is used as the object key name.
                await fileTransferUtility.UploadAsync(filePath, bucketName);
                Console.WriteLine("Upload 1 completed");
            }
        }

        // Get AWS credentials by profile name
        private static AWSCredentials GetAWSCredentialsByName(string profileName)
        {
            if (String.IsNullOrEmpty(profileName))
            {
                throw new ArgumentNullException("profileName cannot be null or empty");
            }

            SharedCredentialsFile credFile = new SharedCredentialsFile();
            CredentialProfile profile = credFile.ListProfiles().Find(profile => profile.Name.Equals(profileName));
            if (profile == null)
            {
                throw new Exception(String.Format("Profile name {0} not found", profileName));
            }
            return AWSCredentialsFactory.GetAWSCredentials(profile, new SharedCredentialsFile());
        }
    }
}
