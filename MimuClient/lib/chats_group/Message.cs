using System.ComponentModel;
using System.Diagnostics.Tracing;
using LocalMimu.Models;

namespace LocalMimu.Models;

public enum MessageType { Text, Photo, Video, Audio, RoundVideo, VoiceMessages, File }

public class Message : INotifyPropertyChanged
{
    public string? Text { get; set; }
    public Guid SenderID { get; set; }
    public DateTime SentAt { get; set; }
    public MessageStatus _Status { get; set; }
    public Guid ReceiverID { get; set; }
    public MessageType Type { get; set; }
    public string? SenderUsername { get; set; }
    public Guid Id { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayText
    {
        get
        {
            if (Type == MessageType.File && Text.Contains("|"))
            {
                var returning = Text.Split("|");
                return returning[1];
            }
            else
            {
                return Text;
            }
        }
        set
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }
    public string FileKey
    {
        get
        {
            if (Type == MessageType.File && Text.Contains("|"))
            {
                string[]? textToReturn = Text.Split("|");
                return textToReturn[0];
            }
            else
            {
                return Text;
            }
        }
        set
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }
    public MessageStatus Status
    {
        get => _Status;
        set
        {
            if (_Status != value)
            {
                _Status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }

        }
    }
    public Message()
    {
        _Status = MessageStatus.Sent;
    }
    public Message(string text, Guid senderId, Guid receiverId, MessageType type)
    {
        Text = text;
        SenderID = senderId;
        SentAt = DateTime.Now;
        _Status = MessageStatus.Sent;
        ReceiverID = receiverId;
        Type = type;
        Id = Guid.NewGuid();
    }
}