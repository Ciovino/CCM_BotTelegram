using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
                    UpdateType.Message,
                    UpdateType.EditedMessage
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
                }                
            }
            else if (update.Type == UpdateType.EditedMessage)
            {
                if (update.EditedMessage.Type == MessageType.Text)
                {
                    var message_update = new BotUpdate
                    {
                        type = UpdateType.EditedMessage.ToString(),
                        text = update.EditedMessage.Text,
                        chat_id = update.EditedMessage.Chat.Id,
                        message_id=update.EditedMessage.MessageId,
                        username = update.EditedMessage.Chat.Username,
                    };

                    // Remove the message that was edited
                    RemoveEditedMessage(message_update);
                    botUpdates.Add(message_update);

                    var message_update_string = JsonConvert.SerializeObject(botUpdates);

                    System.IO.File.WriteAllText(PrivateConfiguration.getLogFileName(), message_update_string);
                }
            }
        }

        static void RemoveEditedMessage(BotUpdate new_message)
        {
            int need_remove = -1;
            for(int i = 0; i < botUpdates.Count; i++)
            {
                if(SameMessage(new_message, botUpdates[i]))
                {
                    need_remove = i;
                    break;
                }
            }

            if(need_remove > 0)
            {
                botUpdates.RemoveAt(need_remove);
            }
        }

        private static bool SameMessage(BotUpdate a, BotUpdate b)
        {
            return a.message_id == b.message_id;
        }
    }
}