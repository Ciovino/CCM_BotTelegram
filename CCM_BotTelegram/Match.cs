using Newtonsoft.Json;
using Telegram.Bot.Types;
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

        long? chatId;
        string? pollId;
        List<PlayerCah> players = new();
        List<Card> cards = new(), sentences = new();

        public Match()
        {
            chatId = null;
            pollId = null;
        }

        public Match(long chatId, string pollId)
        {
            this.chatId = chatId;
            this.pollId = pollId;

            // Load cards
            cards.Clear();
            string cards_str = File.ReadAllText(PrivateConfiguration.GetCardsFile());
            cards = JsonConvert.DeserializeObject<List<Card>>(cards_str) ?? new();

            // Load sentence
            sentences.Clear();
            string sentences_str = File.ReadAllText(PrivateConfiguration.getSencencesFile());
            sentences = JsonConvert.DeserializeObject<List<Card>>(sentences_str) ?? new();
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
        }
    }

    class PlayerCah
    {
        long id;
        List<Card> cards = new();

        public PlayerCah(long id, List<Card> cards)
        {
            this.id = id;
            this.cards = cards;
        }

        public long GetId() { return id; }

        public List<Card> GetPlayerCards() { return cards; }
    }
}
