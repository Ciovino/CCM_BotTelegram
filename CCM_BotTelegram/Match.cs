using Newtonsoft.Json;
using File = System.IO.File;

namespace CCM_BotTelegram
{
    struct Card
    {
        public int id;
        public string text;
        public bool used;
    }

    internal class Match
    {
        const int MAX_CARDS = 10;
        static Random random = new();

        long? master, chatId;
        string? pollId, answerPollId;
        int? messageAnswerPollId;
        List<PlayerCah> players = new();
        List<Card> cards = new(), sentences = new();

        Round roundManager = new();

        public Match()
        {
            master = null;
            chatId = null;
            pollId = null;
            answerPollId = null;
            messageAnswerPollId = null;
        }

        public Match(long chatId, string pollId, long master)
        {
            this.master = master;
            this.chatId = chatId;
            this.pollId = pollId;

            // Load cards
            cards.Clear();
            string cards_str = File.ReadAllText(PrivateConfiguration.GetCardsFile());
            cards = JsonConvert.DeserializeObject<List<Card>>(cards_str) ?? new();

            // Load sentence
            sentences.Clear();
            string sentences_str = File.ReadAllText(PrivateConfiguration.GetSencencesFile());
            sentences = JsonConvert.DeserializeObject<List<Card>>(sentences_str) ?? new();

            // Reset Round
            roundManager.ResetRound();
        }

        public void addPlayer(long playerId)
        {
            // Assign cards to player
            List<Card> playerCards = new();

            for(int i = 0; i < MAX_CARDS; i++)
            {
                int cardIdx = -1;
                while(cardIdx < 0)
                {
                    int idx = random.Next(0, cards.Count);

                    if (!cards[idx].used)
                        cardIdx = idx;
                }
                cards[cardIdx] = new Card{ id = cards[cardIdx].id, text = cards[cardIdx].text, used = true};
                playerCards.Add(cards[cardIdx]);
            }

            players.Add(new(playerId, playerCards));
        }

        public bool IsMatchPoll(string pollId) 
        { 
            return pollId == this.pollId;
        }

        public long GetChatId() 
        { 
            return chatId.Value; 
        }

        public long GetMasterId () { return master.Value; }

        public List<long> GetPlayers()
        {
            List<long> playersId = new();

            foreach (PlayerCah p in players)
                playersId.Add(p.GetId());

            return playersId;
        }

        public List<Card> GetPlayerCard(long playerId)
        {
            foreach(PlayerCah player in players)
                if (player.GetId() == playerId)
                    return player.GetPlayerCards();

            return new();
        }

        public Card GetRandomSentence()
        {
            int sentenceIdx = -1;
            while (sentenceIdx < 0)
            {
                int idx = random.Next(0, sentences.Count);

                if (!sentences[idx].used)
                    sentenceIdx = idx;
            }

            sentences[sentenceIdx] = new Card { id = sentences[sentenceIdx].id, text = sentences[sentenceIdx].text, used = true };

            return sentences[sentenceIdx];
        }

        public Card StartRound()
        {
            Card sentence = GetRandomSentence();

            roundManager.NewRound(sentence);

            return sentence;
        }

        public void SetPlayerChoice(long playerId, int choise)
        {            
            for(int i = 0; i < players.Count; i++)
            {
                if (players[i].GetId() == playerId)
                {
                    players[i].ChosenCard = choise;
                    players[i].ShownAnswer = false;
                    roundManager.playerChoose(playerId);
                    return;
                }
            }
        }

        public List<long> PlayersWhoHaventChoose()
        {
            // Check if someone didnt chose
            if(players.Count != roundManager.HowManyHaveChose())
            {
                List<long> whoDidntChoose = new();

                foreach(PlayerCah player in players)
                {
                    if (!roundManager.playerHasChoose(player.GetId()))
                    {
                        whoDidntChoose.Add(player.GetId());
                    }
                }

                return whoDidntChoose;
            }
            else
            {
                return new();
            }
        }

        public Card ChooseRandomly(long playerId)
        {
            int cardChoosen = random.Next(0, 10);

            SetPlayerChoice(playerId, cardChoosen);

            List<Card> playerCard = GetPlayerCard(playerId);
            return playerCard[cardChoosen];
        }

        public bool HasNextAnswer() { return roundManager.HasNextAnswer(); }

        public Card GetNextAnswer()
        {
            int idx = -1;
            while(idx < 0)
            {
                idx = random.Next(0, players.Count);

                if (players[idx].ShownAnswer)
                {
                    idx = -1;
                }
            }
            var cardIdx = players[idx].ChosenCard;
            players[idx].ShownAnswer = true;
            roundManager.playerCardChoosen = idx;
            roundManager.IncrementAnswer();

            return GetPlayerCard(players[idx].GetId())[cardIdx];
        }

        public void SaveAnswerPoll(string answerPollId, int messageId)
        {
            this.answerPollId = answerPollId;
            messageAnswerPollId = messageId;
        }

        public bool OpenAnswerPoll()
        {
            return messageAnswerPollId != null;
        }

        public int GetMessageAnswerPollId()
        {
            return messageAnswerPollId.Value;
        }

        public bool IsAnswerPoll(string pollId)
        {
            return pollId == answerPollId;
        }

        public void AddPoints(int answer)
        {
            players[roundManager.playerCardChoosen].AddPoints(Math.Abs(answer - 2));
        }

        public int GetPlayerPoints(long playerId)
        {
            foreach (PlayerCah player in players)
                if (player.GetId() == playerId)
                    return player.GetPoints();
            return 0;
        }

        public Card GetRoundSentence() { return roundManager.GetChoosenCard(); }

        public void UpdatePlayersCards()
        {
            foreach(PlayerCah player in players)
            {
                int cardIdx = -1;
                while (cardIdx < 0)
                {
                    int idx = random.Next(0, cards.Count);

                    if (!cards[idx].used)
                        cardIdx = idx;
                }
                cards[cardIdx] = new Card { id = cards[cardIdx].id, text = cards[cardIdx].text, used = true };

                player.UpdateCards(cards[cardIdx]);
            }
        }

        public bool Start()
        {
            if(players.Count <= 1)
            {
                // Not enough players
                return false;
            }

            return true;
        }

        public void Reset()
        {
            // Reset Cards
            cards.Clear();
            string cards_str = File.ReadAllText(PrivateConfiguration.GetCardsFile());
            cards = JsonConvert.DeserializeObject<List<Card>>(cards_str) ?? new();

            // Reset Player
            players.Clear();

            // Reset chatId
            chatId = null;

            // Reset poll
            pollId = null;

            // Reset master
            master = null;

            // Reset answerPoll
            answerPollId = null;
            messageAnswerPollId = null;
        }
    }

    class PlayerCah
    {
        long id;
        List<Card> cards = new();
        public int ChosenCard { get; set; }
        public bool ShownAnswer { get; set; }
        int points;

        public PlayerCah(long id, List<Card> cards)
        {
            this.id = id;
            this.cards = cards;
            points = 0;
        }

        public long GetId() { return id; }

        public List<Card> GetPlayerCards() { return cards; }

        public void UpdateCards(Card newCard)
        {
            cards.RemoveAt(ChosenCard);
            cards.Add(newCard);
        }

        public void AddPoints(int points)
        {
            this.points += points;
        }

        public int GetPoints() { return points; }
    }

    class Round
    {
        int numberRound = 0;
        Card chosenSentence;
        List<long> playerWhoDecide = new();
        int answerShown = 0;
        public int playerCardChoosen = -1;

        public void NewRound(Card chosenSentence)
        {
            numberRound++;
            answerShown = 0;
            this.chosenSentence = chosenSentence;
            playerWhoDecide = new();
        }

        public void playerChoose(long player) 
        { 
            if (!playerWhoDecide.Contains(player))
                playerWhoDecide.Add(player);
        }

        public bool playerHasChoose(long player) { return playerWhoDecide.Contains(player); }

        public bool HasNextAnswer() { return playerWhoDecide.Count > answerShown; }

        public void IncrementAnswer() { answerShown++; }

        public int HowManyHaveChose() { return playerWhoDecide.Count; }

        public int RoundNumber() { return numberRound; }

        public Card GetChoosenCard() { return chosenSentence; }

        public void ResetRound() { numberRound = 0; }
    }
}
