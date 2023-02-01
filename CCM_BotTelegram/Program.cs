using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    enum State{
        NoCommand = -1,
        CommandTest,
        CAHGame
    }

    internal class Program
    {
        static readonly TelegramBotClient Client = new(PrivateConfiguration.getToken());
        static readonly string botUsername = "@CAHMontpelos_BOT";

        static State botState = State.NoCommand;

        static readonly List<Command> allCommands = new();

        static void Main()
        {
            var receiverOptions = new ReceiverOptions()
            { 
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message
                },
                ThrowPendingUpdates = true
            };

            // Add commands
            allCommands.Add(new CommandTest());
            allCommands.Add(new CAHGame());

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
                if (update.Message != null && update.Message.Type == MessageType.Text)
                {
                    string text = update.Message.Text ?? "";
                    var chatId = update.Message.Chat.Id;

                    // Check bot state
                    switch (botState)
                    {
                        case State.NoCommand: // There's no active command
                            botState = await CheckExecutionAsync(text, chatId, token);
                            break;

                        case State.CommandTest:
                        case State.CAHGame:
                            botState = await allCommands[(int)botState].ExecuteCommand(text, chatId, token);
                            break;

                        default:
                            break;
                    }

                    Console.WriteLine(botState.ToString());
                }                
            }
        }

        private static async Task<State> CheckExecutionAsync(string text, long chatId, CancellationToken token)
        {
            State returnValue = State.NoCommand;

            // Check if is a command
            if (text[0] == '/')
            {
                string command = text[1..];
                switch (SimpleCommand(command))
                {
                    case "test": // Activate CommandTest
                        returnValue = State.CommandTest;

                        MessageWrapper test_message = allCommands[(int) State.CommandTest].Activate();
                        await SendWrapperMessageAsync(chatId, test_message, token);
                        break;

                    case "play": // Start a new game
                        returnValue = State.CAHGame;

                        MessageWrapper game_message = allCommands[(int) State.CAHGame].Activate();
                        await SendWrapperMessageAsync(chatId, game_message, token);
                        break;

                    default: // Send Error message
                        MessageWrapper error_message = new("Non esiste stu comand asscemo");
                        await SendWrapperMessageAsync(chatId, error_message, token);

                        break;
                }
            }
            else // Simple message
            {
                MessageWrapper message = new("Scrivi qualcosa di utile, idiota");
                await SendWrapperMessageAsync(chatId, message, token);
            }

            return returnValue;
        }

        public static async Task<Message> SendWrapperMessageAsync(ChatId id, MessageWrapper to_send, CancellationToken token)
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

        public static string SimpleCommand(string command)
        {
            if (command.Contains(botUsername))
            {
                var length = command.IndexOf('@');

                command = command[..length];
            }

            return command;
        }
    }

    public class MessageWrapper
    {
        private readonly string text;
        public string Text { get { return text; } }

        private readonly ReplyKeyboardMarkup? keyboard;
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