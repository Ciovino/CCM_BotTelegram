using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    enum State{
        NoCommand,
        CommandTest
    }
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
        static State botState = State.NoCommand;
        static List<BotUpdate> botUpdates = new List<BotUpdate>();

        static CommandTest test = new();

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

                    // Check bot state
                    switch (botState)
                    {
                        case State.NoCommand: // There's no active command
                            // Check if a new command needs to be activate
                            if (message_update.text[0] == '/')
                            {
                                string command = message_update.text.Substring(1);
                                switch (command)
                                {
                                    case "test": // Activate CommandTest
                                        MessageWrapper commandTestMessage = test.Activate();

                                        botState = State.CommandTest;

                                        await SendWrapperMessageAsync(message_update.chat_id, commandTestMessage, token);
                                        break;

                                    default: // Send Error message
                                        MessageWrapper message = new("Non esiste stu comand asscemo");
                                        await SendWrapperMessageAsync(message_update.chat_id, message, token);
                                        
                                        break;
                                }
                            }
                            break;

                        case State.CommandTest:
                            if (message_update.text[0] == '/')
                            {
                                string command = message_update.text.Substring(1);
                                switch (command)
                                {
                                    case "remove": // Back to NoCommand
                                        MessageWrapper commandTestMessage = test.Deactivate();

                                        botState = State.NoCommand;

                                        await SendWrapperMessageAsync(message_update.chat_id, commandTestMessage, token);
                                        break;

                                    default: // Send Error message
                                        MessageWrapper message = new("Non esiste stu comand asscemo");
                                        await SendWrapperMessageAsync(message_update.chat_id, message, token);

                                        break;
                                }
                            }

                            break;

                        default:
                            break;
                    }

                    Console.WriteLine(botState.ToString());
                }                
            }
        }

        private static async Task<Message> SendWrapperMessageAsync(ChatId id, MessageWrapper to_send, CancellationToken token)
        {
            if (to_send.Keyboard == null)
            {
                return await Client.SendTextMessageAsync(chatId: id,
                                                    text: to_send.Text,
                                                    replyMarkup: new ReplyKeyboardRemove(),
                                                    cancellationToken: token);
            }
            else
            {
                return await Client.SendTextMessageAsync(chatId: id,
                                                        text: to_send.Text,
                                                        replyMarkup: to_send.Keyboard,
                                                        cancellationToken: token);
            }
        }
    }

    public class MessageWrapper
    {
        private string text;
        public string Text { get { return text; } }

        private ReplyKeyboardMarkup? keyboard;
        public ReplyKeyboardMarkup? Keyboard { 
            get {
                return keyboard; 
            } 
        }

        public MessageWrapper(string text)
        {
            this.text = text;
        }

        public MessageWrapper(string text, ReplyKeyboardMarkup keyboard)
        {
            this.text = text;
            this.keyboard = keyboard;
        }
    }
}