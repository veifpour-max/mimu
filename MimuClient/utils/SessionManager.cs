using System.Security.Cryptography;

namespace LocalMimu.Models;

//public class SessionManager
//{
  //  public void SaveSession(string username, string hash, string address)
  //  {
      //  var newSession = new SessionModel(username, hash, address);
     //   var json = Deser.SerJson(newSession);
    //    var rawBytes = System.Text.Encoding.UTF8.GetBytes(json);
//        var encrypted = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
    //   // а как же пользователи линукс?
     //   File.WriteAllBytes("%appdata%/LocalMimusession.bin", encrypted);
   // }
  //  public SessionModel? LoadSession()
 //   {
     //   if (!File.Exists("%appdata%/LocalMimu/session.bin"))
     //   {
           // return null;
    //    }
    //    else
    //    {
      //      try{
      //      var read = File.ReadAllBytes("%appdata%/LocalMimu/session.bin");
     //       if(read != null)
     //       {
//                byte[] decryptedBytes = ProtectedData.Unprotect(read, null, DataProtectionScope.CurrentUser);
 //               string finalAfterDecryption = System.Text.Encoding.UTF8.GetString(decryptedBytes);
   //             var parseJson = Deser.DeserJson<SessionModel>(finalAfterDecryption);
   //             return null;
    //        }
     //       }
     //       catch(Exception ex)
    //        {
    //            Console.WriteLine($"Ошибка обработки сессии: {ex.Message}");
     //       }
    //        return null;

     //   }
  //  }
// }