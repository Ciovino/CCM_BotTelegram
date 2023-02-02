using System.Threading;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Timer = System.Timers.Timer;

namespace CCM_BotTelegram
{
    enum State{
        Idle,           // Nothing to do, waiting for commands
        Test,           // Test state, will be deleted
        WaitPoll_cah,   // Cah starting state: wait for a 60s timer, than change to Playing
        Playing_cah,    // Playing Cah game
    }

    internal class Program
    {
        private static Timer timer; // 60 seconds timer
        static readonly TelegramBotClient Client = new(PrivateConfiguration.GetToken());
        static readonly string botUsername = "@CAHMontpelos_BOT";

        static State botState = State.Idle;
        static long chatIdMatch;
        static CancellationTokenSource cts = new();

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

            Client.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cts.Token);

            Console.ReadLine();
            cts.Cancel();
        }

        private async static Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken token)
        {
            if (exception is ApiRequestException apiRequestException)
            {
                await bot.SendTextMessageAsync(PrivateConfiguration.GetLogChatId(), apiRequestException.ToString());
            }
        }

        private async static Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            switch (botState)
            {
                case State.Idle:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "test":
                                    botState = State.Test;
                                    await bot.SendTextMessageAsync(chatId, "StateMachine Update: Cambio in Test",
                                        cancellationToken: token);
                                    break;

                                case "play":
                                    botState = State.WaitPoll_cah;

                                    chatIdMatch = update.Message.Chat.Id;
                                    SetTimer();

                                    await bot.SendTextMessageAsync(chatId, "StateMachine Update: Cambio in WaitPoll_cah",
                                        cancellationToken: token);
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            await bot.SendTextMessageAsync(chatId, "StateMachine Update: Rimango in Idle",
                                        cancellationToken: token);
                        }
                    }
                    break;

                case State.Test:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "exit":
                                    botState = State.Idle;
                                    await bot.SendTextMessageAsync(chatId, "StateMachine Update: Cambio in Idle",
                                        cancellationToken: token);
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            await bot.SendTextMessageAsync(chatId, "StateMachine Update: Rimango in Test",
                                        cancellationToken: token);
                        }
                    }
                    break;

                case State.WaitPoll_cah:
                    break;

                case State.Playing_cah:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "stop":
                                    botState = State.Idle;
                                    await bot.SendTextMessageAsync(chatId, "StateMachine Update: Cambio in Idle",
                                        cancellationToken: token);
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            await bot.SendTextMessageAsync(chatId, "StateMachine Update: Rimango in Gioco",
                                        cancellationToken: token);
                        }
                    }
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(botState));
            }
        }        

        public static string SimpleCommand(string command)
        {
            command = command[1..];

            if (command.Contains(botUsername))
            {
                var length = command.IndexOf('@');
                command = command[..length];
            }

            return command;
        }

        private static void SetTimer()
        {
            timer = new(60000); // 60 seconds timer
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // 60 seconds have passed
            botState = State.Playing_cah;
            Client.SendTextMessageAsync(chatIdMatch, "1 minuto è passsato", cancellationToken: cts.Token);
        }
    }
}