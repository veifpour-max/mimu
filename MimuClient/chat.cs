namespace LocalMimu.Models;

public class Chat
{
    public Guid Id {get; private set;}
    public string Name {get; private set;} // можно менять в менюшке какой-то

    private List<string> MemberIds = new List<string> ();
    private List <Message> messages = new List<Message> ();

    public Chat(string name)
    // пришлось разбираться с гуглом
    {
        Guid GenID = Guid.NewGuid();
        Id = GenID;
        Name = name;

    }
    public void AddMember(string userId)
    {
        if(!MemberIds.Contains(userId)){
        MemberIds.Add(userId);
        }
    }
    public string GetLastMessage()
    {
        var result = messages.LastOrDefault();
        return result?.Text ?? "Сообщений нет"; // мой любимый синтаксис.... ОБОЖАЮ НАХУЙ
     
    }
}