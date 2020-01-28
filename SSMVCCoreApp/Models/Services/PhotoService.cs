using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SSMVCCoreApp.Models.Abstract;

namespace SSMVCCoreApp.Models.Services
{
    public class PhotoService : IPhotoService
    {
        private CloudStorageAccount _storageAccount;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(IOptions<StorageUtility> storageUtility, ILogger<PhotoService> logger)
        {
            _storageAccount = storageUtility.Value.StorageAccount;
            _logger = logger;
        }

        public async Task<string> UploadPhotoAsync(string category, IFormFile photoToUpload)
        {
            if (photoToUpload == null || photoToUpload.Length == 0) return null;
            var categoryLowerCase = category.ToLower().Trim();
            string fullPath = null;
            try
            {
                //Creating a contatiner and then blob
                CloudBlobClient blobClient = _storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(categoryLowerCase);
                if (await blobContainer.CreateIfNotExistsAsync())
                {
                    await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                    _logger.LogInformation($"Successfully created a blob storage '{blobContainer.Name}' container and made it public");
                }

                string imageName = $"pp{Guid.NewGuid().ToString()}{Path.GetExtension(photoToUpload.FileName.Substring(photoToUpload.FileName.LastIndexOf("/") + 1))}";

                //upload image to the blob
                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(imageName);
                blockBlob.Properties.ContentType = photoToUpload.ContentType;
                await blockBlob.UploadFromStreamAsync(photoToUpload.OpenReadStream());

                fullPath = blockBlob.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading photo to blob storage");
            }
            return fullPath;
        }

        public async Task<bool> DeletePhotoAsync(string category, string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
            {
                return true;
            }
            string categoryLowerCase = category.ToLower().Trim();
            bool deleteFlag = false; ;
            try
            {
                var blobClient = _storageAccount.CreateCloudBlobClient();
                var blobContainer = blobClient.GetContainerReference(categoryLowerCase);
                if (blobContainer.Name == categoryLowerCase)
                {
                    string blobName = photoUrl.Substring(photoUrl.LastIndexOf("/") + 1);
                    var blockBlob = blobContainer.GetBlockBlobReference(blobName);
                    deleteFlag = await blockBlob.DeleteIfExistsAsync();
                    _logger.LogInformation($"Blob Service, PhotoService.DeletePhoto, deletedimagepath: '{photoUrl}'");

                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error while deleting photo from blob storage");
            }
            return deleteFlag;
        }
    }
}
