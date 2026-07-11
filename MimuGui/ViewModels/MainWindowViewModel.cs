using System;
using System.Threading.Tasks;
using LocalMimu.Models;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using System.Reflection;
using System.Dynamic;


namespace MimuGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SessionManager _sessionManager = new SessionManager();
    public MainWindowViewModel()
    {
        _net.OnMessageReceived += HandleIncomingMessage;
        
        _ = InitilizeAppAsync();
    }
    private async Task InitilizeAppAsync()
    {
        var savedSession = _sessionManager.LoadSession();

        if (savedSession != null)
        {
            await AutoLoginAsync(savedSession);
        }
        else
        {
            try
            {
                StatusMessage = "Подключение к серверу..";
                await _net.ConnectAsync("146.158.101.114", 8000);
                StatusMessage = "Готов ко входу";
                IsLoginVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка подключения к серверу: {ex.Message}";
                IsLoginVisible = true;

            }
        }
    }

    private async Task AutoLoginAsync(SessionModel session)
    {
        try
        {
            await _net.ConnectAsync("146.158.101.114", 8000);
            var loginPayload = new LoginPayload(session.Username, session.PasswordHash);
            var user = await _net.AuthenticateAsync(loginPayload);

            if (user != null)
            {
                _myId = user.Id;
                StatusMessage = $"Добро пожаловать обратно, {user.Username}";
                _net.StartListening();
                IsLoginVisible = false;

                var result = await _net.SendAndWaitAsync(new NetworkPacket(PacketType.GetChats, _myId.ToString()));
                var chats = Deser.DeserJson<List<User>>(result);
                if (chats != null)
                {
                    ActiveChats.Clear();
                    foreach (var c in chats)
                    {
                        ActiveChats.Add(c);
                    }
                }
            }
            else
            {
                _sessionManager.DeleteSession();
                StatusMessage = "Войдите заново. Сессия устарела или потеряна";
                IsLoginVisible = true;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка авто-входа: {ex.Message}";
            IsLoginVisible = true;
        }
    }

    private readonly NetworkService _net = new NetworkService();
    public string Greeting { get; } = "LocalMimu v0.1";
    public string InputText { get; set; } = "Пиши сюда...";

    public Brush MessagesBrush;

    private string _username = "";
    private string _password = "";
    private Guid _myId;
    private User? _selectedUser;
    public string _StatusMessage = "";
    private bool _isLoginVisible = true;
    public bool isSearchVisible = false;
    private string _search;
    private string _newMessageText;

    public ObservableCollection<User> SearchResult { get; set; } = new();
    public ObservableCollection<User> ActiveChats { get; set; } = new ObservableCollection<User>();
    public ObservableCollection<Message> ChatMessages { get; } = new ObservableCollection<Message>();
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public Guid MyId => _myId;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool IsSearchVisible
    {
        get => isSearchVisible;
        set => SetProperty(ref isSearchVisible, value);
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

    public string SearchingText
    {
        get => _search;
        set => SetProperty(ref _search, value);
    }

    public Brush MessageColor
    {
        get => MessagesBrush;
        set => SetProperty(ref MessagesBrush, value);
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value) && value != null)
            {
                _ = LoadChatHistory(value.Id);
                CancelSearch();
            }
        }
    }
    public string NewMessageText
    {
        get => _newMessageText;
        set => SetProperty(ref _newMessageText, value);
    }
    private void HandleIncomingMessage(Message msg)
    {
        Dispatcher.UIThread.Post(() =>
      {
          try
          {
              var findingSame = ActiveChats.FirstOrDefault(i => i.Id == msg.SenderID);
              if (findingSame == null)
              {
                  var user = new User("Unknown", msg.SenderUsername) { Id = msg.SenderID };
                  ActiveChats.Add(user);
              }
              if (SelectedUser != null && msg.SenderID == SelectedUser.Id)
              {
                  ChatMessages.Add(msg);
              }
              else
              {
                  StatusMessage = $"Сообщение от {msg.SenderUsername}";
              }
          }
          catch (Exception ex)
          {
              StatusMessage = $"Ошибка отриcовки: {ex.Message}";
          }

      });
    }

    public void FlaggingSearch()
    {
        SearchResult.Clear();
        IsSearchVisible = true;
    }
    public void CancelSearch()
    {
        SearchingText = "";
        IsSearchVisible = false;
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

                var result = await _net.SendAndWaitAsync(packet);
                var history = Deser.DeserJson<List<Message>>(result);

                if (history != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ChatMessages.Clear();
                        foreach (var msg in history)
                        {
                            ChatMessages.Add(msg);
                        }
                        
                    });

                    StatusMessage = $"Чат с @{SelectedUser?.Username}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки истории: {ex.Message}";
        }
    }

    public async Task SearchingUserAsync()
    {
        if (shTools.check(SearchingText))
        {
            var packet = new NetworkPacket(PacketType.SearchUser, SearchingText);

            FlaggingSearch();
            var result = await _net.SendAndWaitAsync(packet);

            var desering = Deser.DeserJson<User>(result);
            StatusMessage = $"{IsSearchVisible}";

            if (desering != null)
            {
                SearchResult.Add(desering);
            }
            else if (desering == null)
            {
                var dummy = new User("Такого польователя не существует", "Никого нет");
                SearchResult.Add(dummy);
                StatusMessage = "Пользователь не найден";
            }

        }
    }

    public async void OnLogClicked()
    {
        if (shTools.check(Username) && shTools.check(Password))
        {
            StatusMessage = "Вход..";
            var loginPayload = new LoginPayload(Username, Password);
            var user = await _net.AuthenticateAsync(loginPayload);

            if (user != null)
            {
                _myId = user.Id;
                StatusMessage = $"Добро пожаловать, {user.Username}";
                _sessionManager.SaveSession(user.Username, Password, "127.0.0.1");
                _net.StartListening();
                IsLoginVisible = false;

                var result = await _net.SendAndWaitAsync(new NetworkPacket(PacketType.GetChats, _myId.ToString()));
                var chats = Deser.DeserJson<List<User>>(result);
                if (chats != null)
                {
                    ActiveChats.Clear();
                    foreach (var c in chats)
                    {
                        ActiveChats.Add(c);
                    }
                }
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
    public async Task OnSendClicked()
    {
        try
        {
            if (SelectedUser == null || !shTools.check(NewMessageText))
            {
                return;
            }
            var checking = ActiveChats.FirstOrDefault(i => i.Id == SelectedUser.Id);
            if (checking == null)
            {
                ActiveChats.Add(SelectedUser);
            }
            var msg = new Message(NewMessageText, _myId, SelectedUser.Id, MessageType.Text);
            string sering = Deser.SerJson(msg);
            var newPacket = new NetworkPacket(PacketType.ChatMessage, sering);
            await _net.SendPacket(newPacket);
            Dispatcher.UIThread.Post(() =>
            {
                ChatMessages.Add(msg);
                NewMessageText = "";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка отправки: {ex.Message}";
        }
    }

}
