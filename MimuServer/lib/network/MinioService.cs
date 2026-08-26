using Minio;
using Minio.DataModel.Args;

namespace LocalMimu.Models;
public class MinioService
{
    IMinioClient minio;
    public MinioService(ServerConfig conf)
    {
        minio = new MinioClient().WithEndpoint($"{conf.MinioEndpoint}").WithCredentials($"{conf.MinioUser}", $"{conf.MinioPass}").Build();
    }

    public async Task<string?> GenerateUploadUrl(string fileName, ServerConfig config)
    {
        var args = new PresignedPutObjectArgs().WithBucket(config.BucketName).WithObject(fileName).WithExpiry(300);
        var url = await minio.PresignedPutObjectAsync(args);     
        return url;
    }

    public async Task<string?> GetDownloadUrl(string filename, ServerConfig config)
    {
        var args = new PresignedGetObjectArgs().WithBucket(config.BucketName).WithObject(filename).WithExpiry(300);
        var url = await minio.PresignedGetObjectAsync(args);
        return url;
    }
}