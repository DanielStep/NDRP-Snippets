//Copyright Arpan Jain, Daniel Stepanenko

using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Net.Mime;

namespace NDRP_solution.Storage
{
    /*
     * Working Example - 
     
            AzureStorage storage = new AzureStorage();

            //Upload a new file
            //variable uploadStream will come from the UI
            Stream uploadStream = new FileStream(@"C:\example.txt", FileMode.Open, FileAccess.Read);
            string path = storage.UploadBlob(AzureStorage.Containers.projects, "test", uploadStream);
            
            //Download a file
            Stream downloadStream = storage.DownloadBlob(AzureStorage.Containers.projects, "test");
            
            //Delete a file
            storage.DeleteBlob("projects/test");

     * To test if it is working: download the Storage Explorer from http://storageexplorer.com/
     * It will also ask you to download the storge emulator, if not already present
     * 
     * A blob can also be accessed via a URL.  In the dev environment, it will look like this:
     *  [REDACTED]
     * However, I have left the container and the blobs private and therefore will be inaccessible via the URL
     * 
     * Trying to download or delete an invalid blob will throw a Storage Exception which basically results in a 404 exception.
     * This is normally the default behavior, however can be changed if needed.
     */
    public class AzureStorage
    {
        /*
         * A container name must be a valid DNS name, conforming to the following naming rules:
         * 1. Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.
         * 2. Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.
         * 3. All letters in a container name must be lowercase.
         * 4. Container names must be from 3 through 63 characters long.
         * 
         * PS: Dash (-) is not supported by enums.
         */
        public enum Containers
        {
            projects,
            sites,
            audits,
            auditdetails,
            cardimages,
            avatars
        }

        private CloudBlobClient blobClient;

        public AzureStorage()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        public string UploadBlob(Containers containerName, string blobName, Stream stream, string contentType = null)
        {
            // Retrieve reference to the container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName.ToString());

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            // Retrieve reference to the blob.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Change the content type (if supplied and valid) from default application/octet-stream
            if (contentType != null)
            {
                try
                {
                    ContentType validContentType = new ContentType(contentType);
                    blockBlob.Properties.ContentType = contentType;
                }
                catch { }
            }

            // By default, the new container is private.
            // In order to access the blob via the URL anonymously, uncomment the below code
            BlobContainerPermissions permissions =
                new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob };
            container.SetPermissions(permissions);

            // Create or overwrite the blob with contents from the file stream.
            blockBlob.UploadFromStream(stream);

            // Return blob's access path.
            return (containerName.ToString() + "/" + blobName);
        }

        public Stream DownloadBlob(string blobPath)
        {
            // Retrieve reference to the blob.
            CloudBlockBlob blockBlob = getBlockBlobReference(blobPath);

            // If invalid container or blob doesn't exist, then return.
            if (blockBlob == null || !blockBlob.Exists())
            {
                return null;
            }

            // Initialise a new MemoryStream object to store the contents of the blob.
            MemoryStream memoryStream = new MemoryStream();

            // Save blob contents to the memory stream.
            blockBlob.DownloadToStream(memoryStream);

            // Return the memory stream.
            return memoryStream;
        }

        public void DeleteBlob(string blobPath)
        {
            // Retrieve reference to the blob.
            CloudBlockBlob blockBlob = getBlockBlobReference(blobPath);

            // If invalid container, then return.
            if (blockBlob == null)
            {
                return;
            }

            // Delete the blob.
            blockBlob.DeleteIfExists();
        }

        public bool BlobExists(Containers containerName, string blobName)
        {
            // Retrieve reference to the container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName.ToString());

            // Retrieve reference to the blob.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            return blockBlob.Exists();
        }

        private CloudBlockBlob getBlockBlobReference(string blobPath)
        {
            // Retrieve the container name from the path.
            string containerName = blobPath.Substring(0, blobPath.IndexOf('/'));

            // If invalid container, then return.
            if (!System.Enum.IsDefined(typeof(Containers), containerName))
            {
                return null;
            }

            // Retrieve the blob name from the path.
            string blobName = blobPath.Substring(blobPath.IndexOf('/') + 1);

            // Retrieve reference to the container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to the blob.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Return the reference.
            return blockBlob;
        }
    }
}