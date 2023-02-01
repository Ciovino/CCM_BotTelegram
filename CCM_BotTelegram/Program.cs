using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    enum State{
        NoCommand = -1,
        Incognito,
        CardTest
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
        const string botUsername = "@CAHMontpelos_BOT";
        static TelegramBotClient Client = new TelegramBotClient(PrivateConfiguration.getToken());
        static State botState = State.NoCommand;
        static List<BotUpdate> botUpdates = new List<BotUpdate>();

        static List<Command> allCommands = new();

        static void Main()
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
                },
                ThrowPendingUpdates = true
            };

            // Create and Add command
            allCommands.Add(new IncognitoMode());
            allCommands.Add(new CardTest());

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

                    // Check bot state
                    switch (botState)
                    {
                        case State.NoCommand: // There's no active command
                            // Check if a new command needs to be activate
                            if (message_update.text[0] == '/')
                            {
                                string command = message_update.text.Substring(1);
                                switch (SimpleCommand(command))
                                {
                                    case "incognito": // Activate IncognitoMode
                                        botState = State.Incognito;

                                        MessageWrapper commandTestMessage = allCommands[(int) botState].Activate();
                                        await SendWrapperMessageAsync(message_update.chat_id, commandTestMessage, token);
                                        break;

                                    case "cardtest": // Activate CardTest
                                        botState = State.CardTest;

                                        MessageWrapper cardTestMessage = allCommands[(int)botState].Activate();
                                        await SendWrapperMessageAsync(message_update.chat_id, cardTestMessage, token);

                                        // Send a poll
                                        // Wait 1 minute
                                        // Close poll
                                        // If someone vote "Yes", send them a message

                                        break;

                                    default: // Send Error message
                                        MessageWrapper message = new("Non puoi ancora usare questo comando.");
                                        await SendWrapperMessageAsync(message_update.chat_id, message, token);

                                        AddMessageToJson(message_update);
                                        break;
                                }
                            }
                            else
                                AddMessageToJson(message_update);

                            break;

                        case State.Incognito:
                            if (message_update.text[0] == '/')
                            {
                                string command = message_update.text.Substring(1);
                                switch (SimpleCommand(command))
                                {
                                    case "exit": // Back to NoCommand
                                        MessageWrapper commandTestMessage = allCommands[(int) botState].Deactivate();
                                        await SendWrapperMessageAsync(message_update.chat_id, commandTestMessage, token);

                                        botState = State.NoCommand;
                                        break;

                                    default: // Send Error message
                                        MessageWrapper message = new("Non puoi usare questo comando ora");
                                        await SendWrapperMessageAsync(message_update.chat_id, message, token);

                                        AddMessageToJson(message_update);
                                        break;
                                }
                            }

                            break;

                        case State.CardTest:
                            if (message_update.text[0] == '/')
                            {
                                string command = message_update.text.Substring(1);
                                switch (SimpleCommand(command))
                                {
                                    case "exit": // Back to NoCommand
                                        MessageWrapper commandTestMessage = allCommands[(int)botState].Deactivate();
                                        await SendWrapperMessageAsync(message_update.chat_id, commandTestMessage, token);

                                        botState = State.NoCommand;
                                        break;

                                    case "card": // Send a new card in chat
                                        MessageWrapper newCard_message = ((CardTest)allCommands[(int)botState]).NewCard();

                                        await SendWrapperMessageAsync(message_update.chat_id, newCard_message, token);
                                        break;

                                    default: // Send Error message
                                        MessageWrapper message = new("Non puoi usare questo comando ora");
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

        private static void AddMessageToJson(BotUpdate new_message)
        {
            // Write an update
            botUpdates.Add(new_message);
            var message_update_string = JsonConvert.SerializeObject(botUpdates);
            System.IO.File.WriteAllText(PrivateConfiguration.getLogFileName(), message_update_string);
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


        private static string SimpleCommand(string command)
        {
            if (command.Contains(botUsername))
            {
                var length = command.IndexOf('@');

                command = command.Substring(0, length);
            }

            return command;
        }
    }

    public class MessageWrapper
    {
        private readonly string text;
        public string Text { get { return text; } }

        private ReplyKeyboardMarkup? keyboard;
        public ReplyKeyboardMarkup? Keyboard { get { return keyboard; } }

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