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

    public string Username
    {
        // а че для всего нужно такое свойство? типа гет это откуда мы получаем инфу и сет это то что мы вставляем. ну или свойство где гет это обозначсение переменной а сет это что с ней делать
        get => _username;
        set => SetProperty(ref _username, value); // у меня тут не вставляется реактив так что нашел в гугле замену
    }
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public void OnLogClicked()
    {
        Debug.WriteLine($"Нажата кнопка войти: Логин: {Username} | Пароль: {Password}");
        Console.WriteLine($"Нажата кнопка войти: Логин: {Username} | Пароль: {Password}");
    }

}
