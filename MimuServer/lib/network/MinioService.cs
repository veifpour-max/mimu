using Minio;
using Minio.DataModel.Args;

namespace LocalMimu.Models;
public class MinioService
{
    IMinioClient minio;
    public MinioService()
    {
        minio = new MinioClient().WithEndpoint("146.158.101.114:9000").WithCredentials("mimich67", "rnimu_DeV_%%67").Build();
    }

    public async Task<string?> GenerateUploadUrl(string fileName) // не знаю правильно или нет но я чуть гуглил
    {
        var args = new PresignedPutObjectArgs().WithBucket("mimu-bucket").WithObject(fileName).WithExpiry(300);
        var url = await minio.PresignedPutObjectAsync(args);     
        return url;
    }
}