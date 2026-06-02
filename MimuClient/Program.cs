using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LocalMimu.Models;
using LocalMimu.Repositories;

NetworkService net = new NetworkService();

Guid myFakeId = Guid.NewGuid();

IStorage mainstorage = new FileStorage();

await net.ConnectAsync("127.0.0.1", 5000);
Console.WriteLine("[CLIENT] Вы подключены к серверу Mimu!");

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
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(name))
            {
                var registerUser = new User(name, username);
                var success = await net.RegisterAsync(registerUser);
                if (success)
                {
                    myFakeId = registerUser.Id;
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
        if (username != null)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                var serverAnswer = await net.AuthenticateAsync(username);
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

            ServerState.IsFlaged = false;
            ServerState.rawText = null;

            await net.SendPacket(searchingUser);
            Console.WriteLine("Запрос отправлен");
            while (!ServerState.IsFlaged)
            {
                await Task.Delay(100);
            }

            var deser = Deser.DeserJson<User>(ServerState.rawText);
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
                await net.SendPacket(npMsg);

                ServerState.IsFlaged = false;
                ServerState.rawText = null;
            

                while (!ServerState.IsFlaged)
                {
                    await Task.Delay(100);
                }

                var targetUser = Deser.DeserJson<User>(ServerState.rawText);

                if (targetUser != null)
                {
                    Console.WriteLine($"=== Чат с @{targetUser.Username} ===");
                    Console.WriteLine("Введи 'exit' для выхода");
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
                            Console.WriteLine("[Client] Сообщение отправлено");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Пользователь не найден локально");
                }


            }
        }

        if (choice == "3")
        {
           var newPacket = new NetworkPacket(PacketType.GetChats, myFakeId.ToString());
           await net.SendPacket(newPacket);

           ServerState.IsFlaged = false;

            while (!ServerState.IsFlaged)
            {
                await Task.Delay(100);
            }

            var contactId = Deser.DeserJson<List<User>>(ServerState.rawText);

            if(contactId != null && contactId.Count > 0)
            {
                Console.WriteLine("Ваши чаты");

                foreach(var id in contactId)
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
