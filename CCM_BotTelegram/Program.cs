using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    struct BotUpdate
    {
        public string type;
        public string text;
        public long chat_id;
        public long message_id;
        public string? username;
    }

    internal class Program
    {
        static TelegramBotClient Client = new TelegramBotClient(PrivateConfiguration.getToken());
        static List<BotUpdate> botUpdates = new List<BotUpdate>();

        static void Main(string[] args)
        {
            // Read all saved updates
            try
            {
                var botUpdatesString = System.IO.File.ReadAllText(PrivateConfiguration.getLogFileName());

                botUpdates = JsonConvert.DeserializeObject<List<BotUpdate>>(botUpdatesString) ?? botUpdates;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            var receiverOptions = new ReceiverOptions()
            { 
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message
                }
            };

            Client.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions);

            Console.ReadLine();
        }

        private static Task ErrorHandler(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }

        private static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message)
            {
                if (update.Message.Type == MessageType.Text)
                {
                    var message_update = new BotUpdate
                    {
                        type = UpdateType.Message.ToString(),
                        text = update.Message.Text,
                        chat_id = update.Message.Chat.Id,
                        message_id = update.Message.MessageId,
                        username = update.Message.Chat.Username,
                    };

                    // Write an update
                    botUpdates.Add(message_update);

                    var message_update_string = JsonConvert.SerializeObject(botUpdates);

                    System.IO.File.WriteAllText(PrivateConfiguration.getLogFileName(), message_update_string);

                    // If is a command
                    if (message_update.text[0] == '/')
                    {
                        string command = message_update.text.Substring(1);

                        switch (command)
                        {
                            case "test":
                                ReplyKeyboardMarkup possible_choices = new ReplyKeyboardMarkup(new[]
                                {
                                    new KeyboardButton[] {"Questo è un test"},
                                    new KeyboardButton[] {"Questo non è un test" }
                                })
                                {
                                    ResizeKeyboard = true
                                };

                                Message add_keyboard = await Client.SendTextMessageAsync(
                                    chatId: message_update.chat_id,
                                    text: "Facciam stu test waglio",
                                    replyMarkup: possible_choices,
                                    cancellationToken: token);

                                break;
                            case "remove":
                                Message remove_keyboard = await Client.SendTextMessageAsync(
                                    chatId: message_update.chat_id,
                                    text: "Finiamo stu test wagioo",
                                    replyMarkup: new ReplyKeyboardRemove(),
                                    cancellationToken: token);

                                break;
                            default:
                                Message no_command = await Client.SendTextMessageAsync(
                                    chatId: message_update.chat_id,
                                    text: "Non esiste stu comand asscemo",
                                    cancellationToken: token);
                                break;
                        }
                    }
                }                
            }
        }
    }
}