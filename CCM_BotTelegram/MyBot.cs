using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace CCM_BotTelegram
{
    internal class MyBot
    {
        static TelegramBotClient myBotClient = new(PrivateConfiguration.GetToken());
        static List<Chat> knownChats = new();

        static void Main()
        {
            // Loads chat
            try
            {
                string knownChats_str = File.ReadAllText(PrivateConfiguration.GetKnownUsersFile());
                knownChats = JsonConvert.DeserializeObject<List<Chat>>(knownChats_str) ?? new();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = new UpdateType[] 
                { 
                    UpdateType.Message 
                },
                ThrowPendingUpdates = true
            };

            myBotClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions);

            Console.ReadLine();
        }

        private static Task ErrorHandler(ITelegramBotClient bot, Exception exc, CancellationToken token)
        {
            Console.WriteLine($"{bot} | {exc.Message} | {token}");
            return Task.CompletedTask;
        }

        private static Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if(update.Type == UpdateType.Message)
            {
                var chat = update.Message.Chat;

                if (IsNewChat(chat))
                {
                    AddChat(chat);
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsNewChat(Chat newChat)
        {
            foreach(Chat chat in knownChats)
                if (chat.Id == newChat.Id)
                    return false;

            return true;
        }

        private static void AddChat(Chat newChatId)
        {
            knownChats.Add(newChatId);

            // Save to file
            string knownChats_str = JsonConvert.SerializeObject(knownChats);
            File.WriteAllText(PrivateConfiguration.GetKnownUsersFile(), knownChats_str);
        }
    }
}