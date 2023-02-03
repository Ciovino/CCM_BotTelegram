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
        WaitPoll_cah,   // Cah starting state: wait for a 60s timer, then check if the match can start
        Playing_cah,    // Playing Cah game
        Retry,          // Cannot play Cah
        _ShowCards      // Not playing, only show cards (testing)
    }

    internal class Program
    {
        private static Timer? timer; // 60 seconds timer
        static readonly TelegramBotClient Client = new(PrivateConfiguration.GetToken());
        static readonly string botUsername = "@CAHMontpelos_BOT";

        static State botState = State.Idle;
        static Match cahMatch = new();
        static readonly CancellationTokenSource cts = new();

        static void Main()
        {
            var receiverOptions = new ReceiverOptions()
            { 
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message,
                    UpdateType.PollAnswer
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
                await bot.SendTextMessageAsync(PrivateConfiguration.GetLogChatId(),
                    apiRequestException.ToString(),
                    cancellationToken: token);
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
                                    break;

                                case "play":
                                    if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Supergroup)
                                    {
                                        botState = State.WaitPoll_cah;
                                    
                                        SetTimer();

                                        Message pollMatch = await bot.SendPollAsync(chatId: chatId, 
                                            question: "Chi vuole giocare?",
                                            options: new[] {"Io sii", "Ma vatten"},
                                            isAnonymous: false,
                                            allowsMultipleAnswers: false,
                                            openPeriod: 60,
                                            replyToMessageId: update.Message.MessageId,
                                            cancellationToken: token);

                                        cahMatch = new(chatId, pollMatch.Poll.Id, update.Message.From.Id);
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, $"Per usare il comando {text} devi essere in un gruppo",
                                        cancellationToken: token);
                                    }
                                    
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            
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
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            
                        }
                    }
                    break;

                case State.WaitPoll_cah:
                    if(update.Type == UpdateType.PollAnswer)
                    {
                        // Answer from the game poll
                        if (cahMatch.IsMatchPoll(update.PollAnswer.PollId))
                        {
                            if (update.PollAnswer.OptionIds[0] == 0)
                                cahMatch.addPlayer(update.PollAnswer.User.Id);
                        }
                    }
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
                                    if (cahMatch.GetMasterId() == update.Message.From.Id)
                                    {
                                        botState = State.Idle;
                                        cahMatch.Reset();
                                        await bot.SendTextMessageAsync(chatId, "Partita annullata",
                                            cancellationToken: token);
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando", 
                                            cancellationToken: token);
                                    }
                                    break;

                                case "show":
                                    if (cahMatch.GetMasterId() == update.Message.From.Id)
                                    {
                                        botState = State._ShowCards;
                                        await bot.SendTextMessageAsync(chatId, "Test (andrà levato sto comando)",
                                            cancellationToken: token);
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                            cancellationToken: token);
                                    }
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            
                        }
                    }
                    break;

                case State._ShowCards:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "stop":
                                    if (cahMatch.GetMasterId() == update.Message.From.Id)
                                    {
                                        botState = State.Idle;
                                        cahMatch.Reset();
                                        await bot.SendTextMessageAsync(chatId, "Partita annullata",
                                            cancellationToken: token);
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                            cancellationToken: token);
                                    }
                                    break;

                                case "back":
                                    if (cahMatch.GetMasterId() == update.Message.From.Id)
                                    {
                                        botState = State.Playing_cah;
                                        await bot.SendTextMessageAsync(chatId, "Pre-partita",
                                            cancellationToken: token);
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                            cancellationToken: token);
                                    }                                    
                                    break;

                                case "sentence":
                                    Card sentence = cahMatch.GetRandomSentence();
                                    await bot.SendTextMessageAsync(chatId, $"Frase scelta: {sentence.text}",
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

                        }
                    }
                    break;

                case State.Retry:
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
                                    await bot.SendTextMessageAsync(chatId, "Partita annullata",
                                        cancellationToken: token);
                                    break;

                                case "retry":
                                    if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Supergroup)
                                    {
                                        botState = State.WaitPoll_cah;

                                        SetTimer();

                                        Message pollMatch = await bot.SendPollAsync(chatId: chatId,
                                            question: "Chi vuole giocare?",
                                            options: new[] { "Io sii", "Ma vatten" },
                                            isAnonymous: false,
                                            allowsMultipleAnswers: false,
                                            openPeriod: 60,
                                            replyToMessageId: update.Message.MessageId,
                                            cancellationToken: token);

                                        cahMatch = new(chatId, pollMatch.Poll.Id, update.Message.From.Id);
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, $"Per usare il comando {text} devi essere in un gruppo",
                                        cancellationToken: token);
                                    }
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                        else // Normal message
                        {
                            
                        }
                    }
                    break;

                default: break;
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
            timer = new(10000); // 60 seconds timer
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;
        }

        private static async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // 60 seconds have passed
            await Client.SendTextMessageAsync(cahMatch.GetChatId(), "So passati 60 secondi", cancellationToken: cts.Token);
            timer.Enabled = false;

            if (cahMatch.Start())
            {
                botState = State.Playing_cah;
                // Send to all player their card
                List<long> allPlayers = cahMatch.GetPlayers();

                foreach (long player in allPlayers)
                {
                    await Client.SendTextMessageAsync(player, "Queste sono le tue carte", cancellationToken: cts.Token);
                    foreach(Card c in cahMatch.GetPlayerCard(player))
                    {
                        await Client.SendTextMessageAsync(player, c.text, cancellationToken: cts.Token);
                    }
                }

                await Client.SendTextMessageAsync(cahMatch.GetChatId(), "Il master controlla la partita", cancellationToken: cts.Token);
                botState = State.Retry;
            }
            else
            {
                await Client.SendTextMessageAsync(cahMatch.GetChatId(), "Non ci sono abbastanza giocatori", cancellationToken: cts.Token);
                botState = State.Retry;
                cahMatch.Reset();
            }
        }
    }
}