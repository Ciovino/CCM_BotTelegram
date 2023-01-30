using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    internal class CommandTest
    {
        private string[] test = { "Questo è un test", "Questo non è un test", "Questo potrebbe essere un test", "Voglio licenziarmi"};

        private bool active;
        public bool Active { get { return active; } }

        public CommandTest()
        {
            this.active = false;
        }

        public MessageWrapper Activate()
        {
            this.active = true;

            // Set the test keyboard
            ReplyKeyboardMarkup keyboard = new(new[]
            {
                 new KeyboardButton[] { test[0], test[1] },
                 new KeyboardButton[] {test[2], test[3] }
            })
            { ResizeKeyboard = true };

            string text_message = "Comando TEST (Evviva). Per uscire scrivi */remove*";

            MessageWrapper to_send = new(text_message, keyboard);

            return to_send;
        }

        public MessageWrapper Deactivate()
        {
            this.active = false;

            // Remove keyboard
            string text_message = "Comando REMOVE. Abbiamo finito il test.";

            MessageWrapper to_send = new(text_message);

            return to_send;
        }
    }
}
