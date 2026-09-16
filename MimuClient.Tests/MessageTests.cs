using LocalMimu.Models;
using Xunit;

namespace MimuClient.Tests;

public class MessageTests
{
    [Fact]
    public void TextMessage_DefaultsToTextType()
    {
        var sender = Guid.NewGuid();
        var receiver = Guid.NewGuid();

        var msg = new Message("Привет!", sender, receiver, MessageType.Text);

        Assert.Equal(MessageType.Text, msg.Type);
        Assert.Equal("Привет!", msg.Text);
        Assert.Equal(sender, msg.SenderID);
        Assert.Equal(receiver, msg.ReceiverID);
        Assert.Equal(MessageStatus.Sent, msg.Status);
        Assert.NotEqual(Guid.Empty, msg.Id);
    }

    [Fact]
    public void SettingEncFileSuffix_SwitchesTypeToFile()
    {
        var msg = new Message("notenc", Guid.NewGuid(), Guid.NewGuid(), MessageType.Text);

        msg.Text = "somekey.enc|photo.jpg";

        Assert.Equal(MessageType.File, msg.Type);
    }

    [Fact]
    public void FileMessage_DisplayText_ReturnsFileNameAfterPipe()
    {
        var msg = new Message("encryption123|report.pdf", Guid.NewGuid(), Guid.NewGuid(), MessageType.File);

        Assert.Equal("report.pdf", msg.DisplayText);
        Assert.Equal("encryption123", msg.FileKey);
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("text with spaces", "text with spaces")]
    [InlineData("", "")]
    public void DisplayText_ForNonFile_ReturnsTextAsIs(string input, string expect)
    {
        var msg = new Message(input, Guid.NewGuid(), Guid.NewGuid(), MessageType.Text);

        Assert.Equal(expect, msg.DisplayText);
    }

    [Fact]
    public void StatusChange_PropertyChanged()
    {
        var msg = new Message("test", Guid.NewGuid(), Guid.NewGuid(), MessageType.Text);
        string? changedProperty = null;
        msg.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        msg.Status = MessageStatus.Delivered;

        Assert.Equal(nameof(Message.Status), changedProperty);
    }
}