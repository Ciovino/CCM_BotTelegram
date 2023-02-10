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
        private List<Card> allCards = new();
        readonly Random random = new();

        public override MessageWrapper Activate()
        {
            this.Active = true;

            ResetCard();

            string text = "Carte contro l'Umanità. Yee";

            return new MessageWrapper(text);
        }

        public override MessageWrapper Deactivate()
        {
            this.Active = false;

            string text = "Niente più carte contro l'umanità.";

            return new MessageWrapper(text);
        }

        private void ResetCard()
        {
            allCards.Clear();
            string allCards_str = System.IO.File.ReadAllText(PrivateConfiguration.GetCardsFile());
            allCards = JsonConvert.DeserializeObject<List<Card>>(allCards_str);
        }

        public MessageWrapper NewCard()
        {
            if (needReset())
            {
                ResetCard();
                return new MessageWrapper("Tutte le carte sono uscite.");
            }

            bool gotNewCard = false;
            int idx = -1;

            while (!gotNewCard)
            {
                idx = random.Next(0, allCards.Count);

                gotNewCard = !allCards[idx].used;
            }

            string text = allCards[idx].text;

            // Update used
            allCards[idx] = new Card { id = idx, text = text, used = true};

            return new MessageWrapper(text);
        }

        private bool needReset()
        {
            foreach(Card c in allCards)
            {
                if (!c.used)
                    return false;
            }

            return true;
        }
    }
}
