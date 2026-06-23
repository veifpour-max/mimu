using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LocalMimu.Models;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using System.Collections.Generic;

namespace MimuGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        _ = ConnectToServerAsync();

        ActiveChats.Add(new User("Тунг тунг кеджон", "kejdo"));
        ActiveChats.Add(new User("Лакки", "lakki_sabakin"));
    }

    private async Task ConnectToServerAsync()
    {
        try
        {
            await _net.ConnectAsync("127.0.0.1", 5000);
            _net.OnMessageReceived += HandleIncomingMessage;
            Debug.WriteLine("Подключено к серверу");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка подключения: {ex.Message}");
        }
    }

    private readonly NetworkService _net = new NetworkService();
    public string Greeting { get; } = "LocalMimu v0.1";
    public string InputText { get; set; } = "Пиши сюда...";

    private string _username = "";
    private string _password = "";
    private Guid _myId;
    private User? _selectedUser;
    public string _StatusMessage = "";
    private bool _isLoginVisible = true;
    public ObservableCollection<User> ActiveChats {get; set;} = new ObservableCollection<User>();
    public ObservableCollection<Message> ChatMessages { get; } = new ObservableCollection<Message>();
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }
    public string StatusMessage
    {
        get => _StatusMessage;
        set => SetProperty(ref _StatusMessage, value);
    }
    public bool IsLoginVisible
    {
        get => _isLoginVisible;
        set => SetProperty(ref _isLoginVisible, value);
    }
    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if(SetProperty(ref _selectedUser, value) && value != null)
            {
                _ = LoadChatHistory(value.Id);
            }
        }
    }
    private void HandleIncomingMessage(Message msg)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = $"Сообщение от {msg.SenderUsername} : {msg.Text}";
        });
    }
public async Task LoadChatHistory(Guid targetId)
{
    try
    {
        if (_myId != Guid.Empty)
        {
            StatusMessage = $"Загрузка чата с @{SelectedUser?.Username}...";

            var joinedStr = string.Join("|", _myId, targetId);
            var packet = new NetworkPacket(PacketType.GetChatsHistory, joinedStr);
            
            ServerState.rawText = null;
            await _net.SendPacket(packet);
            await ServerState.ResponseSignal.WaitAsync();
            
            var history = Deser.DeserJson<List<Message>>(ServerState.rawText);
            
            if (history != null)
            {
                // Физика потоков: перебрасываем изменение коллекции в UI-поток!
                Dispatcher.UIThread.Post(() =>
                {
                    ChatMessages.Clear(); // Чистим старый чат
                    foreach (var msg in history)
                    {
                        ChatMessages.Add(msg); // Заливаем сообщения нового чата
                    }
                });

                StatusMessage = $"Чат с @{SelectedUser?.Username}"; // Заголовок чата
            }
        }
    }
    catch (Exception ex)
    {
        StatusMessage = $"Ошибка загрузки истории: {ex.Message}";
    }
}

    public async void OnLogClicked()
    {
        if (shTools.check(Username) && shTools.check(Password))
        {
            StatusMessage = "Вход..";
            var hash = Crypto.SHA256Encode(Password);
            var loginPayload = new LoginPayload(Username, hash);
            var user = await _net.AuthenticateAsync(loginPayload);

            if (user != null)
            {
                _myId = user.Id;
                StatusMessage = $"Добро пожаловать, {user.Username}";
                _net.StartListening();
                IsLoginVisible = false;
                await _net.RequestChatsAsync(_myId);
            }
            if (user == null)
            {
                StatusMessage = "Неверный логин или пароль";
            }
        }
        else if (!shTools.check(Username) || !shTools.check(Password) || !shTools.check(Password) && !shTools.check(Password))
        {
            StatusMessage = "Заполните все поля!";
        }

    }

}
