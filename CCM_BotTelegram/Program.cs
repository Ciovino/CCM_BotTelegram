﻿using System.Timers;
using Telegram.Bot;
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
        Setting_Round   // Change number of round
    }

    internal class Program
    {
        private static Timer? roundTimer;
        static readonly TelegramBotClient Client = new(PrivateConfiguration.GetToken());
        static readonly string botUsername = "@CAHMontpelos_BOT";

        static State botState = State.Idle;
        static Match cahMatch = new();
        static MatchSetting cahSetting = new(-1, -1);
        static readonly CancellationTokenSource cts = new();

        static void Main()
        {
            var receiverOptions = new ReceiverOptions()
            { 
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message,
                    UpdateType.PollAnswer,
                    UpdateType.CallbackQuery
                },
                ThrowPendingUpdates = true
            };

            Client.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cts.Token);

            Console.ReadLine();
            cts.Cancel();
        }

        private static Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private async static Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            string text = "";
            if(update.Type == UpdateType.Message)
            {
                text = update.Message.Text ?? "";
                if (text == "") return;
                
                // bot_state Command: Print the current bot state
                if (text[0] == '/') // Possible Command
                {
                    if (SimpleCommand(text) == "bot_state")
                    {
                        Console.WriteLine(botState);
                        await bot.SendTextMessageAsync(
                            update.Message.Chat.Id, 
                            $"{botState}", 
                            cancellationToken: token
                        );
                        return;
                    }
                }
            }

            switch (botState)
            {                 
                case State.Idle:
                    if (update.Type == UpdateType.Message)
                    {
                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "play":
                                    await PlayCommand(update.Message.Chat, update.Message, token);                                    
                                    break;

                                default: break;
                            }
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
                                cahMatch.AddPlayer(user.Id, user.FirstName);
                            }
                        }
                    }
                    else if (update.Type == UpdateType.Message)
                    {
                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "start_game":
                                    await StartGameCommand(update.Message.Chat, update.Message, token);                                        
                                    break;

                                default: break;
                            }
                        }
                    }
                    break;

                case State.Playing_cah:
                    if (update.Type == UpdateType.Message)
                    {
                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "stop":
                                    await EndGameCommand(update.Message.Chat, update.Message, token);                                    
                                    break;

                                case "start_round":
                                    await StartRoundCommand(update.Message.Chat, update.Message, token);
                                    break;

                                default: break;
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
                                    await bot.DeleteMessageAsync(
                                        update.Message.Chat.Id, 
                                        update.Message.MessageId, 
                                        token
                                    );
                                }                                
                            }
                        }
                    }
                    else if (update.Type == UpdateType.CallbackQuery)
                    {
                        if (update.CallbackQuery.From.Id == cahMatch.GetMasterId())
                        {
                            if (update.CallbackQuery.Data == "01")
                            {
                                botState = State.Setting_Round;

                                await Client.SendTextMessageAsync(
                                    cahMatch.GetChatId(),
                                    $"Quanti round vuoi fare? (Scrivi -1 per avere round infiniti)",
                                    cancellationToken: token
                                );
                            }
                        }
                    }
                    break;

                case State.Round:
                    if (update.Type == UpdateType.Message)
                    {
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
                                    await bot.DeleteMessageAsync(
                                        chatId, 
                                        update.Message.MessageId, 
                                        token
                                    );

                                    await bot.SendTextMessageAsync(
                                        chatId, 
                                        "Scegli una tra le tue 10 carte",
                                        cancellationToken: token
                                    );
                                }
                                else
                                {
                                    cahMatch.SetPlayerChoice(userId, idx);
                                    await bot.SendTextMessageAsync(
                                        chatId, 
                                        "Scelta salvata",
                                        cancellationToken: token
                                    );
                                }
                            }
                        }
                    }
                    break;

                case State.ShowAnswers:
                    if (update.Type == UpdateType.Message)
                    {
                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "next":
                                    await NextAnswerCommand(update.Message.Chat, update.Message, token);
                                    break;

                                default: break;
                            }
                        }
                    }
                    else if (update.Type == UpdateType.PollAnswer)
                    {
                        if (cahMatch.IsAnswerPoll(update.PollAnswer.PollId, update.PollAnswer.User.Id))
                        {
                            cahMatch.AddPoints(update.PollAnswer.OptionIds[0]);
                        }
                    }
                    break;

                case State.Points:
                    if (update.Type == UpdateType.Message)
                    {
                        if (text[0] == '/') // Possible Command
                        {
                            switch (SimpleCommand(text))
                            {
                                case "start_round":
                                    if (cahMatch.HasNewRound())
                                        await UpdatePlayersCardsCommand(update.Message.Chat, update.Message, token);
                                    else
                                    {
                                        await Client.SendTextMessageAsync(
                                            update.Message.Chat.Id,
                                            "Fine partita",
                                            cancellationToken: token
                                        );

                                        await WinningPlayerCommand(update.Message.Chat, update.Message, token);
                                    }

                                    break;

                                case "stop":
                                    await WinningPlayerCommand(update.Message.Chat, update.Message, token);
                                    break;

                                default: break;
                            }
                        }
                    }
                    break;

                case State.Setting_Round:
                    if (update.Type == UpdateType.Message)
                    {
                        if (cahMatch.GetMasterId() == update.Message.From.Id)
                        {
                            try
                            {
                                int newRound = int.Parse(text);

                                if (newRound == 0)
                                {
                                    await Client.SendTextMessageAsync(
                                        cahMatch.GetChatId(),
                                        $"Ora mi spieghi come fai a fare 0 round. Vedi se scegli un numero valido",
                                        cancellationToken: token
                                    );
                                }
                                else
                                {
                                    cahMatch.SetSettingRound(newRound);

                                    await SettingCommand(update.Message.Chat, update.Message, "Numero di round aggiornato", token);
                                }
                            }
                            catch
                            {
                                await Client.SendTextMessageAsync(
                                    cahMatch.GetChatId(),
                                    $"Inserisi un numero valido, plz",
                                    cancellationToken: token
                                );
                            }
                        }                        
                    }
                    break;

                default: break;
            }
        }

        private static async Task SettingCommand(Chat updateChat, Message updateMessage, string promptMessage, CancellationToken token)
        {
            if (updateChat.Type == ChatType.Group || updateChat.Type == ChatType.Supergroup)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    botState = State.Playing_cah;

                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                       InlineKeyboardButton.WithCallbackData(
                           text: $"Numero di round: {cahMatch.GetSettingRound()}",
                           callbackData: "01"
                        )
                    });

                    await Client.DeleteMessageAsync(
                        updateChat,
                        cahMatch.GetSettingMessageId(),
                        token
                    );

                    Message newSettingMessageId = await Client.SendTextMessageAsync(
                        updateChat,
                        "Impostazioni aggiornate",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: token
                    );

                    cahMatch.SetSettingMessageId(newSettingMessageId.MessageId);
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id,
                        "Solo il master puo' usare quel comando",
                        cancellationToken: token
                    );
                }
            }
            else
            {
                await Client.SendTextMessageAsync(
                    updateChat.Id,
                    $"Per usare il comando {SimpleCommand(updateMessage.Text)} devi essere in un gruppo",
                    cancellationToken: token
                );
            }
        }

        public static async Task PlayCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (updateChat.Type == ChatType.Group || updateChat.Type == ChatType.Supergroup)
            {
                botState = State.WaitPoll_cah;

                Message pollMatch = await Client.SendPollAsync(
                    chatId: updateChat.Id,
                    question: "Chi vuole giocare?",
                    options: new[] { "Io sii", "Ma vatten" },
                    isAnonymous: false,
                    allowsMultipleAnswers: false,
                    replyToMessageId: updateMessage.MessageId,
                    cancellationToken: token
                );

                cahMatch = new(
                    updateChat.Id,
                    new PollInfo { id = pollMatch.Poll.Id, messageId = pollMatch.MessageId },
                    updateMessage.From.Id,
                    new MatchSetting(-1, -1)
                );
            }
            else
            {
                await Client.SendTextMessageAsync(
                    updateChat.Id,
                    $"Per usare il comando {SimpleCommand(updateMessage.Text)} devi essere in un gruppo",
                    cancellationToken: token
                );
            }
        }

        public static async Task StartGameCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (cahMatch.GetChatId() == updateChat.Id)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    await StartMatch();
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id, 
                        "Solo il master puo' usare quel comando",
                        cancellationToken: token
                    );
                }
            }
        }

        public static async Task UpdatePlayersCardsCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (cahMatch.GetChatId() == updateChat.Id)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    // Update Cards
                    cahMatch.UpdatePlayersCards();

                    // Update Player Keyboard
                    List<long> allPlayers = cahMatch.GetPlayers();

                    foreach (long player in allPlayers)
                    {
                        await Client.SendTextMessageAsync(
                            player, 
                            "Hai una nuova carta",
                            replyMarkup: CreatePlayerDeck(player),
                            cancellationToken: cts.Token
                        );
                    }

                    // Start a new Round
                    await StartRoundCommand(updateChat, updateMessage, token);
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id, 
                        "Solo il master puo' usare quel comando",
                        cancellationToken: token
                    );
                }
            }
        }

        public static async Task StartRoundCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (cahMatch.GetChatId() == updateChat.Id)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    botState = State.Round;                    

                    Card sentence = cahMatch.StartRound();
                    await Client.SendTextMessageAsync(
                        updateChat.Id,
                        $"Round #{cahMatch.GetRoundNumber()}. Avete 60 secondi per scegliere la vostra carta",
                        cancellationToken: token
                    );

                    List<long> allPlayers = cahMatch.GetPlayers();
                    foreach (long player in allPlayers)
                    {
                        await Client.SendTextMessageAsync(
                            player, 
                            $"Frase: {sentence.text.Replace("_", "________")}",
                            cancellationToken: cts.Token
                        );
                    }

                    SetRoundTimer();
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id, 
                        "Solo il master puo' usare quel comando",
                        cancellationToken: token
                    );
                }
            }
        }

        public static async Task NextAnswerCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (cahMatch.GetChatId() == updateChat.Id)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    if (cahMatch.HasNextAnswer())
                    {
                        Card nextAnswer = cahMatch.GetNextAnswer();
                        Card sentence = cahMatch.GetRoundSentence();

                        Message answerPoll = await Client.SendPollAsync(
                            chatId: updateChat.Id,
                            question: $"{CompleteSentence(sentence, nextAnswer)}\n\nFa ridere?",
                            options: new[] { "Tantissimoo", "Ho visto di meglio", "Per niente" },
                            isAnonymous: false,
                            allowsMultipleAnswers: false,
                            cancellationToken: token
                        );

                        cahMatch.SaveAnswerPoll(
                            new PollInfo { 
                                id = answerPoll.Poll.Id, 
                                messageId = answerPoll.MessageId 
                        });
                    }
                    else
                    {
                        botState = State.Points;
                        await Client.SendTextMessageAsync(
                            updateChat.Id, 
                            "Round finito",
                            cancellationToken: token
                        );

                        List<long> allPlayers = cahMatch.GetPlayers();
                        foreach (long player in allPlayers)
                        {
                            await Client.SendTextMessageAsync(
                                player, 
                                $"Hai {cahMatch.GetPlayerPoints(player)} punti",
                                cancellationToken: cts.Token
                            );
                        }
                    }
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id, 
                        "Solo il master puo' usare quel comando", 
                        cancellationToken: token
                    );
                }
            }
        }

        public static async Task WinningPlayerCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (cahMatch.GetChatId() == updateChat.Id)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    List<PlayerStats> leaderboard = cahMatch.Leaderboard();

                    // Check for tie
                    int tie = 0;
                    for(int i = 1; i < leaderboard.Count; i++)
                    {
                        if (leaderboard[i].points == leaderboard[0].points)
                            tie++;
                        else
                            break;
                    }

                    if (tie > 0)
                    {
                        // There is a tie
                        await Client.SendTextMessageAsync(
                            updateChat.Id,
                            $"C'è un pareggio!!",
                            cancellationToken: token
                        );

                        string allWinners = leaderboard[0].name;
                        for (int i = 1; i < tie; i++)
                            allWinners += $", {leaderboard[i].name}";
                        allWinners += $" e {leaderboard[tie].name}";

                        await Client.SendTextMessageAsync(
                            updateChat.Id,
                            $"Congratulazioni a {allWinners} per la vittoria!!\n" +
                                $"Ora andate a festeggiare",
                            cancellationToken: token
                        );
                    }
                    else
                    {
                        await Client.SendTextMessageAsync(
                            updateChat.Id,
                            $"Congratulazioni a {leaderboard[0].name} per la vittoria!!\n" +
                                $"Vai pure a festeggiare",
                            cancellationToken: token
                        );
                    }

                    string leaderboardToString = leaderboard[0].ToString();
                    for(int i = 1; i < leaderboard.Count; i++)
                        leaderboardToString += $"\n{leaderboard[i]}";

                    await Client.SendTextMessageAsync(
                            updateChat.Id,
                            $"Ecco la classifica completa:\n{leaderboardToString}",
                            cancellationToken: token
                        );

                    await EndGameCommand(updateChat, updateMessage, token);
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id, 
                        "Solo il master puo' usare quel comando",
                        cancellationToken: token
                    );
                }
            }
        }

        public static async Task EndGameCommand(Chat updateChat, Message updateMessage, CancellationToken token)
        {
            if (cahMatch.GetChatId() == updateChat.Id)
            {
                if (cahMatch.GetMasterId() == updateMessage.From.Id)
                {
                    botState = State.Idle;
                    
                    if (cahMatch.GetRoundNumber() == 0)
                    {
                        await Client.SendTextMessageAsync(
                            updateChat.Id, 
                            "Partita annullata",
                            cancellationToken: token
                        );
                    }

                    ResetMatch();
                }
                else
                {
                    await Client.SendTextMessageAsync(
                        updateChat.Id, 
                        "Solo il master puo' usare quel comando",
                        cancellationToken: token
                    );
                }
            }
        }

        private static string SimpleCommand(string command)
        {
            command = command[1..];

            if (command.Contains(botUsername))
            {
                var length = command.IndexOf('@');
                command = command[..length];
            }

            return command;
        }

        private static string CompleteSentence(string sentence, string answer)
        {
            string completeSentence;

            if (sentence.Contains('_'))
                completeSentence = sentence.Replace("_", answer);
            else
                completeSentence = $"{sentence} {answer}";

            return completeSentence;
        }

        private static string CompleteSentence(Card sentence, Card answer)
        {
            string sent = sentence.text, ans = answer.text;

            if (sentence.modify && answer.modify) 
                ans = char.ToUpper(ans[0]) + ans[1..];

            return CompleteSentence(sent, ans);
        }

        private static ReplyKeyboardMarkup CreatePlayerDeck(long player)
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

            return playerCardsKeyboard;
        }

        private async static Task StartMatch()
        {
            await Client.StopPollAsync(
                cahMatch.GetChatId(), 
                cahMatch.GetStartingPollMessageId(), 
                cancellationToken: cts.Token
            );

            if (cahMatch.Start())
            {
                botState = State.Playing_cah;
                // Send to all player their card
                List<long> allPlayers = cahMatch.GetPlayers();
                foreach (long player in allPlayers)
                {
                    await Client.SendTextMessageAsync(
                        player, 
                        "Sotto ci sono le tue carte",
                        replyMarkup: CreatePlayerDeck(player),
                        cancellationToken: cts.Token
                    );
                }

                // Settings
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: $"Numero di round: {cahSetting.RoundToString()}",
                        callbackData: "01"
                    )
                });

                Message settings = await Client.SendTextMessageAsync(
                    cahMatch.GetChatId(), 
                    "Si comincia",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cts.Token
                );

                cahMatch.SetSettingMessageId(settings.MessageId);
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
                await Client.SendTextMessageAsync(
                    player, 
                    "Partita terminata",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cts.Token
                );
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

                    await Client.SendTextMessageAsync(
                        player, 
                        $"Hai impegato troppo tempo nello scegliere la carta. Ho scelto io questa" +
                        $" per te: {randomChoice.text}",
                        cancellationToken: cts.Token
                    );
                }
            }

            await Client.SendTextMessageAsync(
                cahMatch.GetChatId(), 
                "Tempo scaduto. E' ora di vedere le vostre frasi",
                cancellationToken: cts.Token
            );

            botState = State.ShowAnswers;
        }
    }
}