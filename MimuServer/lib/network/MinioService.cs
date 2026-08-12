using Minio;
using Minio.DataModel.Args;

namespace LocalMimu.Models;
public class MinioService
{
    IMinioClient minio;
    public MinioService()
    {
        minio = new MinioClient().WithEndpoint("146.158.101.114:9000").WithCredentials("your_public_key", "your_secret_key").Build();
    }

    public async Task<string?> GenerateUploadUrl(string fileName)
    {
        var args = new PresignedPutObjectArgs().WithBucket("mimu-bucket").WithObject(fileName).WithExpiry(300);
        var url = await minio.PresignedPutObjectAsync(args);     
        return url;
    }

    public async Task<string?> GetDownloadUrl(string filename)
    {
        var args = new PresignedGetObjectArgs().WithBucket("mimu-bucket").WithObject(filename).WithExpiry(300);
        var url = await minio.PresignedGetObjectAsync(args);
        return url;
    }
}