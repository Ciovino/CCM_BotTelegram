using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    internal class IncognitoMode : Command
    {
        // When the command is active, the json log file doesn't update

        public override MessageWrapper Activate()
        {
            this.Active = true;

            // Set the exit keyboard
            ReplyKeyboardMarkup keyboard = new(new[]
            {
                 new KeyboardButton("/exit")
            })
            { ResizeKeyboard = true };

            string text = "Modalità Incognito attiva.";

            return new MessageWrapper(text, keyboard);
        }

        public override MessageWrapper Deactivate()
        {
            this.Active = false;

            string text = "Modalità Incognito disattiva.";

            return new MessageWrapper(text);
        }
    }
}
