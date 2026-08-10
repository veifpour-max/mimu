using System;
using System.Threading.Tasks;
using LocalMimu.Models;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Runtime.Serialization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.IO;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SQLitePCL;

namespace MimuGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SessionManager _sessionManager = new SessionManager();
    private readonly LocalMessagesRepository _localMessages = new();
    private CryptoEngine? _crypto;
    public IStorageService? StorageService { get; set; }


    public MainWindowViewModel()
    {
        StatusMessage = "Готов ко входу";
        IsLoginVisible = true;
        IsRegisterVisible = false;
        IsMainVisible = false;
        _net.OnMessageReceived += HandleIncomingMessage;
        _net.OnStateChanged += (state) => StatingConnection(state);
        _net.OnMessageStatusChanged += HandleStatusChanged;

        _ = InitilizeAppAsync();
    }
    private async Task InitilizeAppAsync()
    {
        try
        {
            await _localMessages.InitializeLocalDatabase();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка БД: {ex.Message}";
            IsLoginVisible = true;
            return;
        }

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
                await _net.ConnectAsync(_config.ServerIp, _config.ServerPort);
                StatusMessage = "Готов ко входу";
                IsLoginVisible = true;
                IsRegisterVisible = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка подключения к серверу: {ex.Message}";
                IsLoginVisible = true;
                IsRegisterVisible = false;
                throw;
            }
        }
    }

    public async Task OnAttachClick()
    {
        StatusMessage = "Открытие файлов...";

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null)
            {
                StatusMessage = "Ошибка: Окно не найдено.";
                return;
            }
            var storage = mainWindow.StorageProvider;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите файл для отправки",
                AllowMultiple = false
            });
            if (files != null && files.Count >= 1)
            {
                var filePath = files[0].TryGetLocalPath();
                StatusMessage = $"Выбран файл: {filePath}";

                if (_crypto == null)
                {
                    StatusMessage = "Сбой крипто-движка";
                    return;
                }
                StatusMessage = "1 условие пройдено";
                if (SelectedUser == null || string.IsNullOrWhiteSpace(SelectedUser.PublicKey))
                {
                    StatusMessage = "У собеседника нет ключа!";
                }
                StatusMessage = "2 условие пройдено";

                if (_crypto != null && SelectedUser?.PublicKey != null)
                {
                    var sharedSecret = _crypto.GetSharedSecret(SelectedUser.PublicKey);

                    FileInfo fi = new FileInfo(filePath);
                    if (fi.Length > 5242880)
                    {
                        StatusMessage = "Твой файл слишком большой!";
                        return;
                    }
                    var filename = Guid.NewGuid().ToString() + ".enc";
                    var networkPacket = new NetworkPacket(PacketType.RequestUploadUrl, filename);
                    StatusMessage = "Попытка связаться с сервером";
                    StatusMessage = "Запрос ссылки у сервера...";

                    var responseJson = await _net.SendAndWaitAsync(networkPacket);

                    if (string.IsNullOrEmpty(responseJson))
                    {
                        StatusMessage = "Сервер промолчал!";
                        return;
                    }

                    var response = Deser.DeserJson<NetworkPacket>(responseJson);
                    if (response == null || string.IsNullOrEmpty(response.PayLoad))
                    {
                        StatusMessage = "Сервер не дал ссылку на загрузку";
                        return;
                    }

                    string url = response.PayLoad;
                    StatusMessage = "Ссылка получена! Шифрую файл...";

                    byte[] bytesOfFile = File.ReadAllBytes(filePath);
                    var encryptedPayload = _crypto.EncryptBytes(bytesOfFile, sharedSecret);
                    var seringIntoJson = Deser.SerJson(encryptedPayload);

                    byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(seringIntoJson);
                    using ByteArrayContent content = new ByteArrayContent(contentBytes);
                    content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");

                    StatusMessage = "Загрузка файла в MinIO...";
                    using var http = new HttpClient();
                    var responseHttp = await http.PutAsync(url, content);

                    if (responseHttp.IsSuccessStatusCode)
                    {
                        StatusMessage = "Файл успешно отправлен!";
                    }
                    else
                    {
                        StatusMessage = $"Ошибка HTTP: {responseHttp.StatusCode}";
                    }
                }


            }
            else
            {
                StatusMessage = "Файл не выбран.";
            }
        }
        else
        {
            StatusMessage = "Ошибка: Неверный тип приложения.";
        }

    }


    private async void HandleStatusChanged(Guid msgId, MessageStatus newStatus)
    {

        await _localMessages.UpdateMessageStatusAsync(msgId.ToString(), newStatus);
        Dispatcher.UIThread.Post(() =>
        {
            var msg = ChatMessages.FirstOrDefault(m => m.Id == msgId);
            if (msg != null)
            {
                int index = ChatMessages.IndexOf(msg);
                msg.Status = newStatus;
                ChatMessages[index] = msg;
            }
            else
            {
                StatusMessage = "Сообщение не найдено";
            }
        });
    }

    private async Task AutoLoginAsync(SessionModel session)
    {

        _myId = session.Id;
        StatusMessage = $"Оффлайн режим, {session.Username}";
        IsLoginVisible = false;

        var localUsers = await _localMessages.GetLocalUsersAsync();
        ActiveChats.Clear();
        foreach (var user in localUsers)
        {
            ActiveChats.Add(user);
        }


        try
        {
            await _net.ConnectAsync(_config.ServerIp, _config.ServerPort);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось подключиться: {ex.Message}";
            return;
        }
        try
        {

            var loginPayload = new LoginPayload(session.Username, session.PasswordHash);
            try
            {
                var user = await _net.AuthenticateAsync(loginPayload);

                if (user != null)
                {
                    var privateKey = _sessionManager.LoadPrivateKey();
                    if (privateKey != null)
                    {
                        _crypto = new CryptoEngine();
                        _crypto.LoadMyPrivateKey(privateKey);
                    }
                    else
                    {
                        StatusMessage = $"Критическая ошибка! Приватный ключ не найден. Чат невозможен";
                    }
                    _myId = user.Id;
                    StatusMessage = $"Добро пожаловать обратно, {user.Username}";
                    _net.StartListening();
                    IsLoginVisible = false;
                    IsRegisterVisible = false;
                    IsMainVisible = true;

                    var result = await _net.SendAndWaitAsync(new NetworkPacket(PacketType.GetChats, _myId.ToString()));
                    Console.WriteLine("Тест - отправка фейк запроса на url");
                    var test = new NetworkPacket(PacketType.RequestUploadUrl, "test_file.enc");
                    var testResponce = await _net.SendAndWaitAsync(test);
                    Console.WriteLine($"Ответ сервера: {testResponce}");
                    var chats = Deser.DeserJson<List<User>>(result);
                    if (chats != null)
                    {
                        foreach (var c in chats)
                        {
                            if (!ActiveChats.Any(u => u.Id == c.Id))
                            {
                                ActiveChats.Add(c);
                            }
                        }
                    }
                }

                else
                {
                    _sessionManager.DeleteSession();
                    StatusMessage = "Войдите заново. Сессия устарела или потеряна";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Не удалось отправить пакет авторизации: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка авто-входа: {ex.Message}";
            throw;
        }
    }

    private readonly NetworkService _net = new NetworkService();
    public string Greeting { get; } = "LocalMimu v0.1";
    private readonly AppConfig _config = ConfigLoader.Load();
    public Brush MessagesBrush;

    private string _regName;
    private string _regUsername;
    private string _regPassword;
    private string _username = "";
    private string _password = "";
    public readonly Guid InstanceId = Guid.NewGuid();

    private string _attachButtonText = "📎";
    public string AttachButtonText
    {
        get => _attachButtonText;
        set => SetProperty(ref _attachButtonText, value);
    }
    private bool _isReconnecting = false;
    private bool _isMainVisible = false;
    private Guid _myId;
    private User? _selectedUser;
    public string _StatusMessage = "";
    private bool _isLoginVisible = true;
    private bool _isRegisterVisible = false;
    public bool isSearchVisible = false;
    private string _search;
    private string _newMessageText;
    private IBrush indicator = Brushes.Gray;
    private string? indicatorText = "Ожидание...";

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
    public string RegName
    {
        get => _regName;
        set => SetProperty(ref _regName, value);
    }
    public string RegPassword
    {
        get => _regPassword;
        set => SetProperty(ref _regPassword, value);
    }
    public string RegUsername
    {
        get => _regUsername;
        set => SetProperty(ref _regUsername, value);
    }

    public bool IsRegisterVisible
    {
        get => _isRegisterVisible;
        set => SetProperty(ref _isRegisterVisible, value);
    }
    public bool IsMainVisible
    {
        get => _isMainVisible;
        set => SetProperty(ref _isMainVisible, value);
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

    public IBrush IndicatorColor
    {
        get => indicator;
        set => SetProperty(ref indicator, value);
    }
    public string? IndicatorText
    {
        get => indicatorText;
        set => SetProperty(ref indicatorText, value);
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value) && value != null)
            {
                value.UnreadCount = 0;
                _ = _localMessages.SaveUserAsync(value);
                _ = LoadChatHistory(value.Id);
                if (string.IsNullOrEmpty(value.PublicKey))
                {
                    _ = PrepareChatAsync(value);
                }
                CancelSearch();
            }
        }
    }
    public string NewMessageText
    {
        get => _newMessageText;
        set => SetProperty(ref _newMessageText, value);
    }

    private async Task ReconnectLoopAsync()
    {
        if (_isReconnecting)
        {
            return;
        }
        else if (!_isReconnecting)
        {
            _isReconnecting = true;
        }
        int delay = 1000;
        while (true)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IndicatorColor = Brushes.Yellow;
            });
            try
            {
                var session = _sessionManager.LoadSession();
                if (session == null)
                {
                    break;
                }
                await AutoLoginAsync(session);
                _isReconnecting = false;
                break;
            }
            catch (Exception ex)
            {
                delay *= 2;
                if (delay > 30000)
                {
                    delay = 30000;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    IndicatorText = $"Переподключение: повтор через {delay / 1000} сек...";
                });
                await Task.Delay(delay);
            }
        }
    }

    private void StatingConnection(ConnectionStates? states)
    {
        if (states == ConnectionStates.Connected)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IndicatorText = "Mimu: Подключено";
                IndicatorColor = Brushes.Green;
            });
        }
        if (states == ConnectionStates.Connecting)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IndicatorColor = Brushes.Yellow;
            });
        }
        if (states == ConnectionStates.Disconnected)
        {
            _ = ReconnectLoopAsync();
            Dispatcher.UIThread.Post(() =>
            {
                IndicatorColor = Brushes.Gray;
            });
        }
    }

    public void SwitchToRegister()
    {
        StatusMessage = "";
        IsRegisterVisible = true;
        IsLoginVisible = false;
        IsMainVisible = false;
    }
    public void SwitchToLogin()
    {
        StatusMessage = "";
        IsLoginVisible = true;
        IsRegisterVisible = false;
        IsMainVisible = false;
    }
    public void ResetAfterRegisterOrLogin()
    {
        IsLoginVisible = false;
        IsRegisterVisible = false;
        IsMainVisible = true;
    }
    private async Task ProcessIncomingMessageAsync(Message msg)
    {
        var user = ActiveChats.FirstOrDefault(i => i.Id == msg.SenderID);
        if (user == null)
        {
            user = new User("Unknown", msg.SenderUsername) { Id = msg.SenderID };
            ActiveChats.Add(user);
        }

        if (string.IsNullOrEmpty(user.PublicKey))
            await FetchPublicKeyAsync(user);

        if (_crypto != null && !string.IsNullOrEmpty(user.PublicKey))
        {
            var encryptedPayload = Deser.DeserJson<EncryptedPayload>(msg.Text);
            if (encryptedPayload != null)
            {
                byte[] sharedSecret = _crypto.GetSharedSecret(user.PublicKey);
                string decryptedText = _crypto.Decrypt(encryptedPayload, sharedSecret);
                msg.Text = decryptedText;
            }
        }
        Dispatcher.UIThread.Post(() =>
        {
            if (SelectedUser != null && msg.SenderID == SelectedUser.Id)
                ChatMessages.Add(msg);
            else
                StatusMessage = $"Новое сообщение от {msg.SenderUsername}";
            user.LastMessageText = msg.Text;
            if (SelectedUser == null || SelectedUser.Id != msg.SenderID)
            {
                user.UnreadCount++;
                _ = _localMessages.SaveUserAsync(user);
            }
        });
    }
    private void HandleIncomingMessage(Message msg)
    {
        _ = ProcessIncomingMessageAsync(msg);
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
        byte[] secret = null;
        try
        {
            if (_myId != Guid.Empty)
            {
                StatusMessage = $"Загрузка чата с @{SelectedUser?.Username}...";

                var resultFromLoading = await _localMessages.GetChatHistoryAsync(_myId, targetId);

                if (_crypto != null && SelectedUser?.PublicKey != null)
                {
                    secret = _crypto.GetSharedSecret(SelectedUser.PublicKey);
                }
                else
                {
                    StatusMessage = "Публичный ключ собеседника не найден!";
                }
                if (resultFromLoading != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        ChatMessages.Clear();
                        foreach (var c in resultFromLoading)
                        {
                            var desered = Deser.DeserJson<EncryptedPayload>(c.Text);
                            if (desered != null && _crypto != null)
                            {
                                secret = _crypto.GetSharedSecret(SelectedUser.PublicKey);
                                try
                                {
                                    var decrypting = _crypto.Decrypt(desered, secret);
                                    c.Text = decrypting;
                                }
                                catch (Exception ex)
                                {
                                    StatusMessage = $"Не удалось дешифровать сообщение! {ex.Message}";
                                }
                                finally
                                {
                                    ChatMessages.Add(c);
                                }


                            }
                        }
                    });
                }
            }
            var joinedStr = string.Join("|", _myId, targetId);
            var packet = new NetworkPacket(PacketType.GetChatsHistory, joinedStr);

            var result = await _net.SendAndWaitAsync(packet);
            var history = Deser.DeserJson<List<Message>>(result);

            if (history != null)
            {
                foreach (var msg in history)
                {
                    await _localMessages.SaveMessagesAsync(msg);
                }
                Dispatcher.UIThread.Post(async () =>
                {
                    ChatMessages.Clear();
                    foreach (var msg in history)
                    {
                        var desering = Deser.DeserJson<EncryptedPayload>(msg.Text);
                        if (_crypto != null && desering != null)
                        {
                            secret = _crypto.GetSharedSecret(SelectedUser.PublicKey);
                            try
                            {
                                var decrypting = _crypto?.Decrypt(desering, secret);
                                msg.Text = decrypting;
                            }
                            catch
                            {
                                StatusMessage = "Не удалось расшифровать сообщения из истории";
                            }
                        }
                        ChatMessages.Add(msg);
                    }

                });

                StatusMessage = $"Чат с @{SelectedUser?.Username}";
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
            await _net.ConnectAsync(_config.ServerIp, _config.ServerPort);
            StatusMessage = "Вход..";
            var loginPayload = new LoginPayload(Username, Password);
            var user = await _net.AuthenticateAsync(loginPayload);

            if (user != null)
            {
                _myId = user.Id;
                var privateKey = _sessionManager.LoadPrivateKey();
                if (privateKey != null)
                {
                    _crypto = new CryptoEngine();
                    _crypto.LoadMyPrivateKey(privateKey);
                }
                else
                {
                    StatusMessage = $"Критическая ошибка! Приватный ключ не найден. Чат невозможен";
                }
                StatusMessage = $"Добро пожаловать, {user.Username}";
                _sessionManager.SaveSession(user.Username, Password, user.Id, "127.0.0.1");
                _net.StartListening();
                IsLoginVisible = false;
                IsRegisterVisible = false;
                IsMainVisible = true;

                var result = await _net.SendAndWaitAsync(new NetworkPacket(PacketType.GetChats, _myId.ToString()));
                var chats = Deser.DeserJson<List<User>>(result);
                if (chats != null)
                {
                    ActiveChats.Clear();
                    foreach (var c in chats)
                    {
                        ActiveChats.Add(c);
                        await _localMessages.SaveUserAsync(c);
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
            var originalText = NewMessageText;
            if (_crypto != null && !string.IsNullOrEmpty(SelectedUser.PublicKey))
            {
                byte[] sharedSecret = _crypto.GetSharedSecret(SelectedUser.PublicKey);
                EncryptedPayload ec = _crypto.Encrypt(NewMessageText, sharedSecret);
                msg.Text = Deser.SerJson(ec);
            }
            else
            {
                if (_crypto == null) StatusMessage = "Твой приватный ключ не загружен!";
                else StatusMessage = "У собеседника нет публичного ключа!";
                return;
            }
            await _localMessages.SaveMessagesAsync(msg);
            var sering = Deser.SerJson(msg);
            var newPacket = new NetworkPacket(PacketType.ChatMessage, sering);
            await _net.SendPacket(newPacket);
            var displayMsg = new Message(originalText, _myId, SelectedUser.Id, MessageType.Text);
            Dispatcher.UIThread.Post(() =>
            {
                ChatMessages.Add(displayMsg);
                NewMessageText = "";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка отправки: {ex.Message}";
        }
    }
    private async Task PrepareChatAsync(User user)
    {
        if (string.IsNullOrEmpty(user.PublicKey))
        {
            await FetchPublicKeyAsync(user);
        }

        await LoadChatHistory(user.Id);
    }
    private async Task FetchPublicKeyAsync(User user)
    {
        try
        {
            var packet = new NetworkPacket(PacketType.GetPublicKey, user.Id.ToString());
            var result = await _net.SendAndWaitAsync(packet);
            var parts = result?.Split('|');

            if (parts != null && parts.Length == 2)
            {
                user.PublicKey = parts[1];
                await _localMessages.SaveUserAsync(user);

                StatusMessage = "Ключ собеседника получен.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки ключа: {ex.Message}";
        }
    }
    public async Task OnRegisterClick()
    {
        if (shTools.check(RegName) && shTools.check(RegUsername) && shTools.check(RegPassword))
        {
            using var crypto = new CryptoEngine();
            var publicKey = crypto.GetMyPublicKeyBase64();
            var privateKey = crypto.ExportMyPrivateKey();
            var payload = new RegisterPayload(RegName, RegUsername, RegUsername, publicKey);
            var success = await _net.RegisterAsync(payload);
            _sessionManager.SavePrivateKey(privateKey);
            if (success)
            {
                StatusMessage = "Успешная регистрация, теперь войдите";
                SwitchToLogin();
                Username = RegUsername;
            }
            else
            {
                StatusMessage = "Ошибка регистрации. Возможно, username занят.";
            }

        }
    }

}
