using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Timer = System.Timers.Timer;

namespace CCM_BotTelegram
{
    enum State{
        Idle,           // Nothing to do, waiting for commands
        WaitPoll_cah,   // Cah starting state: wait for /start command from the master
        Playing_cah,    // Playing Cah game
        Round,          // Player choose the card
        ShowAnswers,    // Answers are shown in the group chat
        Points,         // Points are assigned to the players
        EndMatch,       // Final results
    }

    struct WinningPlayerStats
    {
        public long id;
        public string name;
        public int points;
    }

    struct PollInfo
    {
        public string id;
        public int messageId;
    }

    internal class Program
    {
        private static Timer? roundTimer;
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

        private static Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"{bot} | {exception.Message} | {token}");
            return Task.CompletedTask;
        }

        private async static Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message)
            {
                string text = update.Message.Text ?? "";

                if (text[0] == '/') // Possible Command
                {
                    if (SimpleCommand(text) == "bot_state")
                    {
                        Console.WriteLine(botState);
                        await bot.SendTextMessageAsync(update.Message.Chat.Id, $"{botState}", cancellationToken: token);

                        return;
                    }
                }
            }

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
                                case "play":
                                    if (update.Message.Chat.Type == ChatType.Group || update.Message.Chat.Type == ChatType.Supergroup)
                                    {
                                        botState = State.WaitPoll_cah;

                                        Message pollMatch = await bot.SendPollAsync(chatId: chatId, 
                                            question: "Chi vuole giocare?",
                                            options: new[] {"Io sii", "Ma vatten"},
                                            isAnonymous: false,
                                            allowsMultipleAnswers: false,
                                            replyToMessageId: update.Message.MessageId,
                                            cancellationToken: token);

                                        cahMatch = new(
                                            chatId, 
                                            new PollInfo { id = pollMatch.Poll.Id, messageId = pollMatch.MessageId }, 
                                            update.Message.From.Id
                                        );
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chatId, $"Per usare il comando {text} devi essere in un gruppo",
                                        cancellationToken: token);
                                    }
                                    
                                    break;

                                case "bot_state":
                                    Console.WriteLine(botState);
                                    await bot.SendTextMessageAsync(update.Message.Chat.Id, $"{botState}", cancellationToken: token);
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
                            {
                                var user = update.PollAnswer.User;
                                cahMatch.addPlayer(user.Id, user.FirstName);
                            }
                        }
                    }
                    else if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "start":
                                    if (cahMatch.GetChatId() == chatId) 
                                    {
                                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                                        {
                                            await StartMatch();
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                                cancellationToken: token);
                                        }
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
                                    if (cahMatch.GetChatId() == chatId)
                                    {
                                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                                        {
                                            botState = State.Idle;
                                            ResetMatch();
                                            await bot.SendTextMessageAsync(chatId, "Partita annullata",
                                                cancellationToken: token);
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando", 
                                                cancellationToken: token);
                                        }
                                    }                                    
                                    break;

                                case "start_round":
                                    if (cahMatch.GetChatId() == chatId)
                                    {
                                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                                        {
                                            botState = State.Round;
                                            await bot.SendTextMessageAsync(chatId, "Inizio Round. Avete 60 secondi per scegliere la vostra carta",
                                                cancellationToken: token);

                                            Card sentence = cahMatch.StartRound();
                                            List<long> allPlayers = cahMatch.GetPlayers();
                                            foreach (long player in allPlayers)
                                            {
                                                await Client.SendTextMessageAsync(player, $"Frase: {sentence.text.Replace("_", "________")}",
                                                    cancellationToken: cts.Token);
                                            }

                                            SetRoundTimer();
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                                cancellationToken: token);
                                        }
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
                            var userId = update.Message.From.Id;
                            var userChatId = update.Message.Chat.Id;

                            if (cahMatch.GetPlayers().Contains(userId))
                            {
                                if (userId == userChatId)
                                {
                                    await bot.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId, token);
                                    await bot.SendTextMessageAsync(update.Message.Chat.Id, "La partita non è ancora iniziata", 
                                        cancellationToken: token);
                                }                                
                            }
                        }
                    }
                    break;

                case State.Round:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;
                        var userId = update.Message.From.Id;

                        if (cahMatch.GetPlayers().Contains(userId))
                        {
                            if (userId == chatId)
                            {
                                List<Card> playerCard = cahMatch.GetPlayerCard(userId);

                                int idx = -1;
                                for(int i = 0; i < playerCard.Count; i++)
                                {
                                    if (playerCard[i].text == text)
                                        idx = i;
                                }

                                if(idx == -1)
                                {
                                    await bot.DeleteMessageAsync(chatId, update.Message.MessageId, token);
                                    await bot.SendTextMessageAsync(chatId, "Scegli una tra le tue 10 carte",
                                        cancellationToken: token);
                                }
                                else
                                {
                                    cahMatch.SetPlayerChoice(userId, idx);
                                    await bot.SendTextMessageAsync(chatId, "Scelta salvata",
                                        cancellationToken: token);
                                }
                            }
                        }
                    }
                    break;

                case State.ShowAnswers:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "next":
                                    if (cahMatch.GetChatId() == chatId)
                                    {
                                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                                        {
                                            if (cahMatch.HasNextAnswer())
                                            {
                                                Card nextAnswer = cahMatch.GetNextAnswer();
                                                Card sentence = cahMatch.GetRoundSentence();

                                                string completeSentence = sentence.text;
                                                if (completeSentence.Contains('_'))
                                                    completeSentence = completeSentence.Replace("_", nextAnswer.text);
                                                else
                                                    completeSentence = $"{completeSentence} {nextAnswer.text}";

                                                Message answerPoll = await bot.SendPollAsync(chatId: chatId,
                                                    question: $"{completeSentence}.\nFa ridere?",
                                                    options: new[] { "Tantissimoo", "Ho visto di meglio" , "Per niente" },
                                                    isAnonymous: false,
                                                    allowsMultipleAnswers: false,
                                                    cancellationToken: token);

                                                cahMatch.SaveAnswerPoll(
                                                    new PollInfo { id = answerPoll.Poll.Id, messageId = answerPoll.MessageId }
                                                );
                                            }
                                            else
                                            {
                                                botState = State.Points;
                                                await bot.SendTextMessageAsync(chatId, "Round finito",
                                                    cancellationToken: token);

                                                List<long> allPlayers = cahMatch.GetPlayers();
                                                foreach (long player in allPlayers)
                                                { 
                                                    await Client.SendTextMessageAsync(player, $"Hai {cahMatch.GetPlayerPoints(player)} punti",
                                                        cancellationToken: cts.Token);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                                cancellationToken: token);
                                        }
                                    }
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
                        }
                    }
                    else if (update.Type == UpdateType.PollAnswer)
                    {
                        if (cahMatch.IsAnswerPoll(update.PollAnswer.PollId))
                        {
                            cahMatch.AddPoints(update.PollAnswer.OptionIds[0]);
                        }
                    }
                    break;

                case State.Points:
                    if (update.Type == UpdateType.Message)
                    {
                        string text = update.Message.Text ?? "";
                        long chatId = update.Message.Chat.Id;

                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "start_round":
                                    if (cahMatch.GetChatId() == chatId)
                                    {
                                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                                        {
                                            // Update Cards
                                            cahMatch.UpdatePlayersCards();

                                            // Update Player Keyboard
                                            List<long> allPlayers = cahMatch.GetPlayers();

                                            foreach (long player in allPlayers)
                                            {
                                                List<Card> playerCard = cahMatch.GetPlayerCard(player);
                                                ReplyKeyboardMarkup playerCardsKeyboard = new(
                                                    new[] {
                                                new KeyboardButton[] { playerCard[0].text, playerCard[1].text },
                                                new KeyboardButton[] { playerCard[2].text, playerCard[3].text },
                                                new KeyboardButton[] { playerCard[4].text, playerCard[5].text },
                                                new KeyboardButton[] { playerCard[6].text, playerCard[7].text },
                                                new KeyboardButton[] { playerCard[8].text, playerCard[9].text }
                                                    });

                                                await Client.SendTextMessageAsync(player, "Hai una nuova carta",
                                                    replyMarkup: playerCardsKeyboard,
                                                    cancellationToken: cts.Token);
                                            }

                                            // Start a new Round
                                            botState = State.Round;
                                            await bot.SendTextMessageAsync(chatId, "Inizio Round. Avete 60 secondi per scegliere la vostra carta",
                                               cancellationToken: token);

                                            Card sentence = cahMatch.StartRound();
                                            foreach (long player in allPlayers)
                                            {
                                                await Client.SendTextMessageAsync(player, $"Frase: {sentence.text.Replace("_", "________")}",
                                                    cancellationToken: cts.Token);
                                            }

                                            SetRoundTimer();
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                                cancellationToken: token);
                                        }
                                    }                                    
                                    break;

                                case "stop":
                                    if (cahMatch.GetChatId() == chatId)
                                    {
                                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                                        {
                                            WinningPlayerStats win = cahMatch.WinningPlayer();

                                            await bot.SendTextMessageAsync(chatId, $"Congratulazioni {win.name}, hai vinto con {win.points}!!",
                                                cancellationToken: token);

                                            ResetMatch();
                                            botState = State.Idle;
                                            await bot.SendTextMessageAsync(chatId, "Partita terminata",
                                                cancellationToken: token);
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chatId, "Solo il master puo' usare quel comando",
                                                cancellationToken: token);
                                        }
                                    }
                                    break;

                                default:
                                    await bot.SendTextMessageAsync(chatId, $"Il comando /{SimpleCommand(text)} non esiste",
                                        cancellationToken: token);
                                    break;
                            }
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

        private async static Task StartMatch()
        {
            await Client.StopPollAsync(cahMatch.GetChatId(), cahMatch.GetStartingPollMessageId(), cancellationToken: cts.Token);

            if (cahMatch.Start())
            {
                botState = State.Playing_cah;
                // Send to all player their card
                List<long> allPlayers = cahMatch.GetPlayers();

                foreach (long player in allPlayers)
                {
                    List<Card> playerCard = cahMatch.GetPlayerCard(player);
                    ReplyKeyboardMarkup playerCardsKeyboard = new(
                        new[] {
                            new KeyboardButton[] { playerCard[0].text, playerCard[1].text },
                            new KeyboardButton[] { playerCard[2].text, playerCard[3].text },
                            new KeyboardButton[] { playerCard[4].text, playerCard[5].text },
                            new KeyboardButton[] { playerCard[6].text, playerCard[7].text },
                            new KeyboardButton[] { playerCard[8].text, playerCard[9].text }
                        })
                    {
                        ResizeKeyboard = true
                    };

                    await Client.SendTextMessageAsync(player, "Sotto ci sono le tue carte",
                        replyMarkup: playerCardsKeyboard,
                        cancellationToken: cts.Token);
                }

                await Client.SendTextMessageAsync(cahMatch.GetChatId(), "Il master controlla la partita", cancellationToken: cts.Token);
                await Client.SendTextMessageAsync(
                    cahMatch.GetChatId(), 
                    "Si comincia", 
                    cancellationToken: cts.Token
                );
            }
            else
            {
                await Client.SendTextMessageAsync(
                    cahMatch.GetChatId(), 
                    "Non ci sono abbastanza giocatori", 
                    cancellationToken: cts.Token
                );
                botState = State.Idle;
            }
        }

        private static async void ResetMatch()
        {
            // Delete keyboard
            List<long> allPlayers = cahMatch.GetPlayers();
            foreach (long player in allPlayers)
            { 
                await Client.SendTextMessageAsync(player, "Partita terminata",
                    cancellationToken: cts.Token);
            }

            cahMatch.Reset();
        }

        private static void SetRoundTimer()
        {
            roundTimer = new(60000);
            roundTimer.Elapsed += RoundEnd;
            roundTimer.Enabled = true;
        }

        private static async void RoundEnd(object source, ElapsedEventArgs e)
        {
            roundTimer.Enabled = false;
            List<long> playersId = cahMatch.PlayersWhoHaventChoose();

            if(playersId.Count != 0)
            {
                foreach(long player in playersId)
                {
                    Card randomChoice = cahMatch.ChooseRandomly(player);

                    await Client.SendTextMessageAsync(player, $"Hai impegato troppo tempo nello scegliere la carta. Ho scelto io questa per te: {randomChoice.text}", 
                        cancellationToken: cts.Token);
                }
            }

            await Client.SendTextMessageAsync(cahMatch.GetChatId(), "Tempo scaduto. E' ora di vedere le vostre frasi",
                cancellationToken: cts.Token);

            botState = State.ShowAnswers;
        }
    }
}