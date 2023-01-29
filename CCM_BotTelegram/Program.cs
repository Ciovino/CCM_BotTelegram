using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace CCM_BotTelegram
{
    struct BotUpdate
    {
        public string type;
        public string text;
        public long id;
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
                var message_update = new BotUpdate
                {
                    type = UpdateType.Message.ToString(),
                    text = update.Message.Text,
                    id = update.Message.Chat.Id,
                    username = update.Message.Chat.Username,
                };

                // Write an update
                botUpdates.Add(message_update);

                var message_update_string = JsonConvert.SerializeObject(botUpdates);

                System.IO.File.WriteAllText(PrivateConfiguration.getLogFileName(), message_update_string);
            }
        }
    }
}