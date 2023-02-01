using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM_BotTelegram
{
    struct Card
    {
        public int id;
        public string text;
        public bool used;
    }

    internal class CardTest : Command
    {
        private const string cards_file = "cah.json";
        private List<Card> allCards = new();
        readonly Random random = new();

        public CardTest()
        {
            // Load cards in memory
            string allCards_str = System.IO.File.ReadAllText(cards_file);
            allCards = JsonConvert.DeserializeObject<List<Card>>(allCards_str);
        }

        public override MessageWrapper Activate()
        {
            this.Active = true;

            // Load cards in memory
            allCards.Clear();
            string allCards_str = System.IO.File.ReadAllText(cards_file);
            allCards = JsonConvert.DeserializeObject<List<Card>>(allCards_str);

            string text = "Carte contro l'Umanità. Yee";

            return new MessageWrapper(text);
        }

        public override MessageWrapper Deactivate()
        {
            this.Active = false;

            string text = "Niente più carte contro l'umanità.";

            return new MessageWrapper(text);
        }

        public MessageWrapper NewCard()
        {
            bool gotNewCard = false;
            int idx = -1;

            while (!gotNewCard)
            {
                idx = random.Next(0, allCards.Count);

                gotNewCard = !allCards[idx].used;
            }

            string text = allCards[idx].text;

            return new MessageWrapper(text);
        }
    }
}
