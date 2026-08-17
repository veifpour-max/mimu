using System.ComponentModel;
using System.Diagnostics.Tracing;
using LocalMimu.Models;

namespace LocalMimu.Models;

public enum MessageType { Text, Photo, Video, Audio, RoundVideo, VoiceMessages, File }

public class Message : INotifyPropertyChanged
{
    public string? Text
    {
        get => _text;
        set
        {
            _text = value;

            if (_text != null && _text.Contains(".enc|"))
            {
                Type = MessageType.File;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }
    public Guid SenderID { get; set; }
    public DateTime SentAt { get; set; }
    public MessageStatus _Status { get; set; }
    public Guid ReceiverID { get; set; }
    public MessageType Type { get; set; }
    private string? _text;
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