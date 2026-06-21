using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LocalMimu.Models;

namespace MimuGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        _ = ConnectToServerAsync();
    }

    private async Task ConnectToServerAsync()
    {
        try
        {
            await _net.ConnectAsync("127.0.0.1", 5000);
            Debug.WriteLine("Подключено к серверу");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Ошибка подключения: {ex.Message}");
        }
    }

    private readonly NetworkService _net = new NetworkService();
    public string Greeting { get; } = "LocalMimu v0.1";
    public string InputText { get; set; } = "Пиши сюда...";

    private string _username = "";
    private string _password = "";
    public string _StatusMessage = "";
    private bool _isLoginVisible = true;

    


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

    public async void OnLogClicked()
    {
        if(shTools.check(Username) && shTools.check(Password))
        {
            StatusMessage = "Вход..";
            var hash = Crypto.SHA256Encode(Password);
            var loginPayload = new LoginPayload(Username, hash);
            var user = await _net.AuthenticateAsync(loginPayload);

            if(user != null)
            {
                StatusMessage = $"Добро пожаловать, {user.Username}";
                IsLoginVisible = false;
            }
            if(user == null)
            {
                StatusMessage = "Неверный логин или пароль";
            }
        }
        else if(!shTools.check(Username) || !shTools.check(Password) || !shTools.check(Password) && !shTools.check(Password))
        {
            StatusMessage = "Заполните все поля!";
        }

    }

}
