using LocalMimu.Models;

namespace LocalMimu.Models;

public enum MessageType{ Auth, Text, System }

class Message
{
   public string? Text {get; set;} 
   public Guid SenderID {get; set;}
   public DateTime SentAt {get; set;}
   public MessageStatus Status {get; set;}
   public Guid ReceiverID {get; set;}
   public MessageType Type {get; set;}

   public Message(){}

   public Message(string text, Guid senderId, Guid receiverId, MessageType type) 
    {
        Text = text;
        SenderID = senderId;
        SentAt = DateTime.Now;
        Status = MessageStatus.Sent;
        ReceiverID = receiverId;
        Type = type;
    }
}