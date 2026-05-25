using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LocalMimu.Models;
using LocalMimu.Repositories;


TcpClient client = new TcpClient();

Guid myFakeId = Guid.NewGuid();

IStorage mainstorage = new FileStorage();

// добавил заново тк я даже проверить код не смогу если их нет.
ChatManager chatManager = new ChatManager(mainstorage);
UsersRepository repo = new UsersRepository(mainstorage);


await client.ConnectAsync("127.0.0.1", 5000);
Console.WriteLine("[CLIENT] Вы подключены к серверу Mimu!");
using var stream = client.GetStream();
var reader = new StreamReader(stream);
var writer = new StreamWriter(stream) { AutoFlush = true };

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
                var regJson = Deser.SerJson(registerUser);
                var regDeJson = new NetworkPacket(PacketType.Register, regJson);
                var finalAuth = Deser.SerJson(regDeJson);
                {
                    if (finalAuth != null)
                    {
                        regDeJson.Type = PacketType.Register;
                    }

                }
                if (regDeJson != null)
                {
                    await writer.WriteLineAsync(finalAuth);
                    var waitingForServerAnswer = await reader.ReadLineAsync();
                    if (waitingForServerAnswer != null)
                    {
                        var cacheServerAnswer = Deser.DeserJson<string>(waitingForServerAnswer);

                        if (!shTools.check(cacheServerAnswer))
                        {
                            if (cacheServerAnswer == null || cacheServerAnswer != "1")
                            {
                                Console.WriteLine("Ошибка. Не удалось войти");
                            }
                            else
                            {
                                Console.WriteLine("Регистрация успешна");
                                myFakeId = registerUser.Id ;
                                IsRegistred = true;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Такой пользователь уже существует!");
                }



            }
        }
        if (choice == "1")
        {
            Console.Write("Введи свой username: ");
            var username = Console.ReadLine();
            if (username != null)
            {
                if (!string.IsNullOrWhiteSpace(username))
                {
                    var authPacket = new NetworkPacket(PacketType.Auth, myFakeId.ToString());
                    string jsonSerialize = Deser.SerJson(authPacket);
                    await writer.WriteLineAsync(jsonSerialize);
                    var readed = await reader.ReadLineAsync();
                    if (readed != null)
                    {

                        var serverAnswerNP = Deser.DeserJson<NetworkPacket>(readed);
                        if (serverAnswerNP != null && serverAnswerNP.Type == PacketType.ServerResponse)
                        {
                            var serverAnswer = Deser.DeserJson<User>(serverAnswerNP.PayLoad);
                            if (serverAnswer != null && serverAnswer.Username == username)
                            {
                                myFakeId = serverAnswer.Id;
                                IsRegistred = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ошибка входа. Убедитесь что все введено верно");
                        }
                    }

                }
            }
        }
    }
}

_ = StartReceiving(stream);

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
            var SendingToServer = Deser.SerJson(searchingUser);

            ServerState.IsFlaged = false;

            await writer.WriteLineAsync(SendingToServer);
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
                var npSer = Deser.SerJson(npMsg);
                await writer.WriteLineAsync(npSer);

                ServerState.IsFlaged = false;

                while (!ServerState.IsFlaged)
                {
                    await Task.Delay(100);
                }
                
                    
                    var targetUser = Deser.DeserJson<User>(ServerState.rawText);
                    Console.WriteLine("Десер успешный.");
                    

                    if (targetUser != null)
                    {
                        while (true)
                        {
                            Console.WriteLine("Введи 'exit' для выхода");
                            Console.Write("Сообщение: ");
                            var message = Console.ReadLine();
                            if (message != null)
                            {
                                var mesg = new Message(message, myFakeId, targetUser.Id, MessageType.Text);
                                if (string.IsNullOrWhiteSpace(mesg.Text))
                                {
                                    continue;
                                }
                                string json = Deser.SerJson(mesg); // чудо - си шарп часто понимает что мы отправляем
                                var msgPacket = new NetworkPacket(PacketType.ChatMessage, json);
                                if (message == "exit")
                                {
                                    break;
                                }
                                var JsonMsg = Deser.SerJson(msgPacket);
                                await writer.WriteLineAsync(JsonMsg);
                                if (mesg.Status == MessageStatus.Delivered)
                                {
                                    Console.WriteLine("[Client] Сообщение получено");
                                }
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

            Console.WriteLine("Ваши чаты: ");

            await repo.LoadData();

            var chats = chatManager.GetContactIds(myFakeId);
            if (chats.Count == 0)
            {
                Console.WriteLine("У вас еще нет переписок");
            }
            else
            {
                foreach (var id in chats)
                {
                    var contact = repo.GetUserById(id);
                    if (contact != null)
                    {
                        var lastMsg = chatManager.GetMessagesFromContact(myFakeId, contact.Id).LastOrDefault();

                        if (lastMsg != null && lastMsg.Text != null)
                        {
                            string shortmsg = lastMsg.Text.Length > 7 ? lastMsg.Text.Substring(0, 7) + ".." : lastMsg.Text;

                            Console.WriteLine($"{id}. {contact.Name} | {contact.Username} | {contact.Status}: {shortmsg}..");
                        }
                        Console.Write("Введите номер переписки чтобы продолжить ее: ");
                        var chatNumber = Console.ReadLine();
                    }

                }
            }
        }
    }

    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
        await Task.Delay(250);
    }
}

async Task StartReceiving(NetworkStream stream)
{
    while (true)
    {
        try
        {
            var receivedMsg = await reader.ReadLineAsync();
            if (receivedMsg == null) throw new Exception("Соединение разорвано");
            var msg = Deser.DeserJson<NetworkPacket>(receivedMsg);

            if (msg != null && msg.Type == PacketType.ChatMessage)
            {
                if (!shTools.check(msg.PayLoad)) // чистая экономия // перевод - sh = short
                {                           
                    var finalMsg = Deser.DeserJson<Message>(msg.PayLoad);
                    if (finalMsg != null)
                    {
                        if (finalMsg.SenderID == myFakeId)
                        {
                            Console.WriteLine($"{finalMsg.SentAt:HH:mm:ss} | Вы: | {finalMsg.Text} ");

                        }
                        var user = repo.GetUserById(finalMsg.SenderID);

                        if (user != null)
                        {
                            Console.WriteLine($"{finalMsg.SentAt:HH:mm:ss} | {user.Username}: | {finalMsg.Text} ");
                        }

                        else
                        {
                            Console.WriteLine($"{finalMsg.SentAt:HH:mm:ss} | {finalMsg.SenderID.ToString().Substring(0, 4)}: | {finalMsg.Text} ");
                        }

                    }
                }
            }


            if (msg != null && msg.Type == PacketType.ServerResponse)
            {
                Console.WriteLine("[DEBUG] Пакет от сервера пришел в фоновый поток");
                ServerState.rawText = msg.PayLoad;
                ServerState.IsFlaged = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка! {ex.Message}");
            break;
        }

    }
}










