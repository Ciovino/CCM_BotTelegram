using Newtonsoft.Json;
using System.Net.NetworkInformation;
using File = System.IO.File;

namespace CCM_BotTelegram
{
    struct Card
    {
        public int id;
        public string text;
        public bool used;

        public static Card InvalidCard()
        {
            return new Card
            {
                id = -1,
                text = "",
                used = false
            };
        }

        public bool IsInvalid() { return id == -1; }
    }

    struct PlayerStats
    {
        public long id;
        public string name;
        public int points;

        public bool GreaterThan(PlayerStats other) { return points > other.points; }

        public override string ToString() { return $"{name}, {points} punti"; }
    }

    internal class Match
    {
        const int MAX_CARDS = 10;
        static readonly Random random = new();

        long? master, chatId;
        PollInfo startingPoll;
        readonly List<PlayerCah> players = new();

        private readonly CardSet allCards = new(PrivateConfiguration.GetCardsFile()), sentences = new(PrivateConfiguration.GetSencencesFile());

        private readonly Round roundManager = new();

        public Match()
        {
            master = null;
            chatId = null;
        }

        public Match(long chatId, PollInfo poll, long master)
        {
            this.master = master;
            this.chatId = chatId;
            startingPoll = poll;

            // Reset Round
            roundManager.ResetRound();
        }

        public void AddPlayer(long playerId, string name)
        {
            // Assign cards to player
            List<Card> playerCards = new();

            for(int i = 0; i < MAX_CARDS; i++)
                playerCards.Add(allCards.RandomCard());

            players.Add(new(playerId, playerCards, name));
        }

        public bool IsMatchPoll(string pollId) { return pollId == startingPoll.id; }

        public int GetStartingPollMessageId() { return startingPoll.messageId; }

        public long GetChatId() { return chatId.Value; }

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

        public Card StartRound() 
        {
            Card oldSentence = roundManager.GetChoosenCard();
            sentences.UpdateCard(oldSentence);

            Card sentence = sentences.RandomCard();
            roundManager.NewRound(sentence);

            return sentence;
        }

        public int GetRoundNumber() { return roundManager.RoundNumber(); }

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
                    if (!roundManager.PlayerHasChoose(player.GetId()))
                        whoDidntChoose.Add(player.GetId());
                }

                return whoDidntChoose;
            }
            else
                return new();
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
                    idx = -1;
            }

            var cardIdx = players[idx].ChosenCard;
            players[idx].ShownAnswer = true;
            roundManager.playerCardChoosen = idx;
            roundManager.IncrementAnswer();

            return GetPlayerCard(players[idx].GetId())[cardIdx];
        }

        public void SaveAnswerPoll(PollInfo answerPoll) { players[roundManager.playerCardChoosen].AnswerPoll = answerPoll; }

        public bool OpenAnswerPoll()
        {
            if (roundManager.playerCardChoosen == -1) return false;

            return players[roundManager.playerCardChoosen].InvalidPlayer();
        }

        public int GetMessageAnswerPollId() { return players[roundManager.playerCardChoosen].AnswerPoll.messageId; }

        public bool IsAnswerPoll(string pollId) { return pollId.Equals(players[roundManager.playerCardChoosen].AnswerPoll.id); }

        public void AddPoints(int answer) { players[roundManager.playerCardChoosen].AddPoints(Math.Abs(answer - 2)); }

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
                Card newCard = allCards.RandomCard();

                Card oldCard = player.UpdateCards(newCard);
                allCards.UpdateCard(oldCard);
            }
        }

        public bool Start() { return players.Count > 1; }

        public List<PlayerStats> Leaderboard()
        {
            List<PlayerStats> leaderboard = new() { players[0].GetStats() };

            for(int i = 1; i < players.Count; i++)
            {
                PlayerStats stats = players[i].GetStats();
                bool atTheAnd = true;

                for(int j = 0; j < leaderboard.Count; j++)
                {
                    if (stats.GreaterThan(leaderboard[j]))
                    {
                        leaderboard.Insert(j, stats);
                        atTheAnd = false;
                        break;
                    }
                }

                if (atTheAnd)
                    leaderboard.Add(stats);
            }

            return leaderboard;
        }

        public void Reset()
        {
            // Reset Cards
            allCards.ResetCards();
            sentences.ResetCards();

            // Reset Player
            players.Clear();

            // Reset chatId
            chatId = null;

            // Reset master
            master = null;
        }
    } 

    class PlayerCah
    {
        readonly long id;
        readonly string name;
        readonly List<Card> cards = new();
        public int ChosenCard { get; set; }
        public bool ShownAnswer { get; set; }
        public PollInfo AnswerPoll { get; set; }

        public int points;  

        public PlayerCah(long id, List<Card> cards, string name)
        {
            this.id = id;
            this.cards = cards;
            points = 0;
            this.name = name;
        }

        public long GetId() { return id; }
        public string GetName() { return name; }

        public List<Card> GetPlayerCards() { return cards; }

        public Card UpdateCards(Card newCard)
        {
            Card removedCard = cards[ChosenCard];

            cards.RemoveAt(ChosenCard);
            cards.Add(newCard);

            return removedCard;
        }

        public void AddPoints(int points)
        {
            this.points += points;
        }

        public int GetPoints() { return points; }

        public bool InvalidPlayer() { return id < 0; }

        public PlayerStats GetStats()
        {
            return new PlayerStats
            {
                id = id,
                name = name,
                points = points
            };
        }
    }

    class Round
    {
        int numberRound = 0;
        Card chosenSentence = Card.InvalidCard();
        List<long> playerWhoDecide = new();
        int answerShown = 0;
        public int playerCardChoosen = -1;

        public void NewRound(Card chosenSentence)
        {
            numberRound++;
            answerShown = 0;
            this.chosenSentence = chosenSentence;
            playerWhoDecide = new();
            playerCardChoosen = -1;
        }

        public void playerChoose(long player) 
        { 
            if (!playerWhoDecide.Contains(player))
                playerWhoDecide.Add(player);
        }

        public bool PlayerHasChoose(long player) { return playerWhoDecide.Contains(player); }

        public bool HasNextAnswer() { return playerWhoDecide.Count > answerShown; }

        public void IncrementAnswer() { answerShown++; }

        public int HowManyHaveChose() { return playerWhoDecide.Count; }

        public int RoundNumber() { return numberRound; }

        public Card GetChoosenCard() { return chosenSentence; }

        public void ResetRound() 
        {
            chosenSentence = Card.InvalidCard();
            numberRound = 0;
        }
    }

    class CardSet
    {
        static readonly Random random = new();
        private readonly string cardsFile;

        List<Card> available = new();
        readonly List<Card> inUse = new(), readyToReset = new();

        public CardSet(string availableCardFile)
        {
            cardsFile = availableCardFile;

            inUse.Clear();
            readyToReset.Clear();

            string availableCardString = File.ReadAllText(availableCardFile);
            available = JsonConvert.DeserializeObject<List<Card>>(availableCardString) ?? new();
        }

        public void ResetCards()
        {
            available.Clear();
            inUse.Clear();
            readyToReset.Clear();

            string availableCardString = File.ReadAllText(cardsFile);
            available = JsonConvert.DeserializeObject<List<Card>>(availableCardString) ?? new();
        }

        public Card RandomCard()
        {
            // Check if there isn't an aviable card
            if (available.Count == 0)
            {
                // Move from readyToReset to aviable
                foreach(Card card in readyToReset)
                {
                    available.Add(new Card
                    {
                        id = card.id,
                        text = card.text,
                        used = false
                    });
                }

                readyToReset.Clear();
            }

            int cardIdx = random.Next(0, available.Count);

            Card cardChoosen = available[cardIdx];

            available.RemoveAt(cardIdx);
            inUse.Add(cardChoosen);

            return cardChoosen;
        }

        public void UpdateCard(Card card)
        {
            if (card.IsInvalid()) return;

            int idx = inUse.IndexOf(card);

            if (idx != -1)
            {
                inUse.RemoveAt(idx);
                readyToReset.Add(new Card
                {
                    id = card.id,
                    text = card.text,
                    used = true
                });
            }
        }
    }
}
