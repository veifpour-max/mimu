using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using LocalMimu.Models;

NetworkService net = new NetworkService();
Guid myFakeId = Guid.NewGuid();
try
{
    try
    {
        Ping ping = new();
        var pinging = await ping.SendPingAsync("146.158.101.114");
        if (pinging.Status == IPStatus.Success)
        {
            Console.WriteLine("Пинг дошел");
            Console.WriteLine("Пробую подключиться....");
            string ip = "146.158.101.114";
            int port = 8000;
            Console.WriteLine($"Подключаюсь к {ip}:{port}");
            await net.ConnectAsync(ip, port);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не дошел... {ex.Message}");
    }


}
catch (Exception ex)
{
    Console.WriteLine($"Не удалось подключиться к серверу: {ex.Message}");
}


bool IsRegistred = false;

while (!IsRegistred)
{
    Console.WriteLine(" \n Добро пожаловать в LocalMimu!");
    Console.WriteLine(" \n 1. Войти | 2. Регистрация | 0. Выход");
    Console.Write("\n Ваш выбор: ");
    var choice = Console.ReadLine();
    if (choice == null) continue;
    if (choice == "0")
    {
        break;
    }
    if (!string.IsNullOrWhiteSpace(choice))
    {
        if (choice == "2")
        {
            Console.Write("Введите свой username: ");
            var username = Console.ReadLine();
            Console.Write("Введите свое имя: ");
            var name = Console.ReadLine();
            Console.Write("Введите свой пароль(ЗАПОМНИТЕ ЕГО)");
            var password = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(name) && shTools.check(password))
            {
                var registerUser = new RegisterPayload(name, username, password);
                var success = await net.RegisterAsync(registerUser);
                if (success)
                {
                    myFakeId = registerUser.id;
                    IsRegistred = true;
                }
            }
        }
    }
    else
    {
        Console.WriteLine("Такой пользователь уже существует!");
    }

    if (choice == "1")
    {
        Console.Write("Введи свой username: ");
        var username = Console.ReadLine();
        Console.Write("Введи свой пароль: ");
        var password = Console.ReadLine();
        if (username != null)
        {
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var loginToServer = new LoginPayload(username, password);
                var serverAnswer = await net.AuthenticateAsync(loginToServer);
                if (serverAnswer != null)
                {
                    myFakeId = serverAnswer.Id;
                    IsRegistred = true;
                }
                else
                {
                    Console.WriteLine("Ошибка входа. Убедитесь что все введено верно");
                }
            }

        }
    }
}



net.StartListening();

while (true)
{
    try
    {

        Console.WriteLine("=================================");
        Console.WriteLine("=== LocalMimu Protocol v0.1 ===");
        Console.WriteLine("=================================");
        Console.WriteLine("Выберите действие:");
        Console.WriteLine("1. Поиск пользователей по username | 2. Написать сообщение по username | 3. Чаты | 0. Выход");
        Console.Write("Выбор: ");

        var choice = Console.ReadLine();
        if (choice == "0") break;

        if (choice == "1")
        {
            Console.Write("Введите username контакта: ");
            var usernameOfContact = Console.ReadLine();
            if (usernameOfContact == null)
            {
                Console.WriteLine("Не найдено.");
                await Task.Delay(1500);
                continue;
            }

            var searchingUser = new NetworkPacket(PacketType.SearchUser, usernameOfContact);
            var answer = await net.SendAndWaitAsync(searchingUser);
            Console.WriteLine("Запрос отправлен");
            var deser = Deser.DeserJson<User>(answer);
            if (deser != null)
            {
                Console.WriteLine($"[ПОИСК] Найден: {deser.Name} | @{deser.Username}");
            }
            else
            {
                Console.WriteLine("Никого не найдено");
            }


        }
        if (choice == "2")
        {
            Console.Write("Введи Username получателя: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                var npMsg = new NetworkPacket(PacketType.SearchUser, input);
                var answer = await net.SendAndWaitAsync(npMsg);
                var targetUser = Deser.DeserJson<User>(answer);
                if (targetUser != null)
                {
                    string splitedIds = string.Join("|", myFakeId, targetUser.Id);
                    NetworkPacket packetToSend = new NetworkPacket(PacketType.GetChatsHistory, splitedIds);

                    var serverAnswer = await net.SendAndWaitAsync(packetToSend);
                    var ResponseAbtUser = Deser.DeserJson<List<Message>>(serverAnswer);

                    Console.Clear();

                    Console.WriteLine($"=== Чат с @{targetUser.Username} ===");
                    Console.WriteLine("Введи 'exit' для выхода");

                    if (ResponseAbtUser != null)
                    {
                        foreach (var c in ResponseAbtUser)
                        {
                            string senderName = c.SenderID == myFakeId ? "Вы" : targetUser.Username;
                            Console.WriteLine($"{shTools.FormatTime(c.SentAt)} | @{senderName} : {c.Text}");
                        }
                    }
                    while (true)
                    {
                        Console.Write("Вы: ");
                        var message = Console.ReadLine();
                        if (message != null)
                        {
                            var mesg = new Message(message, myFakeId, targetUser.Id, MessageType.Text);
                            if (string.IsNullOrWhiteSpace(mesg.Text))
                            {
                                continue;
                            }
                            string json = Deser.SerJson(mesg);
                            var msgPacket = new NetworkPacket(PacketType.ChatMessage, json);
                            if (message == "exit")
                            {
                                break;
                            }
                            await net.SendPacket(msgPacket);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Пользователь не существует");
                }


            }
        }

        if (choice == "3")
        {
            var newPacket = new NetworkPacket(PacketType.GetChats, myFakeId.ToString());

            var answerFromServer = await net.SendAndWaitAsync(newPacket);
            var contactId = Deser.DeserJson<List<User>>(answerFromServer);

            if (contactId != null && contactId.Count > 0)
            {
                Console.WriteLine("Ваши чаты");

                foreach (var id in contactId)
                {
                    Console.WriteLine($"Чат с пользователем @{id.Username} | {id.Name}");
                }
            }
            else
            {
                Console.WriteLine("Активных переписок нет");
            }

        }

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
        await Task.Delay(250);
    }
}
