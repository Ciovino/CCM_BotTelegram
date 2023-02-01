using Telegram.Bot.Types.ReplyMarkups;

namespace CCM_BotTelegram
{
    internal class CommandTest : Command
    {
        private readonly string[] test = { "Questo è un test", "Questo non è un test", "Questo potrebbe essere un test", "Voglio licenziarmi"};

        public override MessageWrapper Activate()
        {
            this.Active = true;

            // Set the test keyboard
            ReplyKeyboardMarkup keyboard = new(new[]
            {
                 new KeyboardButton[] { test[0], test[1] },
                 new KeyboardButton[] { test[2], test[3] }
            })
            { ResizeKeyboard = true };

            string text_message = "Comando TEST (Evviva). Per uscire scrivi */remove*";

            MessageWrapper to_send = new(text_message, keyboard);

            return to_send;
        }

        public override MessageWrapper Deactivate()
        {
            this.Active = false;

            // Remove keyboard
            string text_message = "Comando REMOVE. Abbiamo finito il test.";

            MessageWrapper to_send = new(text_message);

            return to_send;
        }

        public override async Task<State> ExecuteCommand(string messageText, long chatId, CancellationToken token)
        {
            State returnValue = State.CommandTest;
            ReplyKeyboardMarkup keyboard = new(new[]
            {
                 new KeyboardButton[] { test[0], test[1] },
                 new KeyboardButton[] { test[2], test[3] }
            })
            { ResizeKeyboard = true };

            // Check if is a command
            if (messageText[0] == '/')
            {
                string command = messageText[1..];
                switch (Program.SimpleCommand(command))
                {
                    case "remove": // Back to NoCommand
                        returnValue = State.NoCommand;

                        MessageWrapper commandTestMessage = Deactivate();
                        await Program.SendWrapperMessageAsync(chatId, commandTestMessage, token);
                        break;

                    default: // Send Error message
                        MessageWrapper message = new("Non esiste stu comand asscemo", keyboard);
                        await Program.SendWrapperMessageAsync(chatId, message, token);
                        break;
                }
            }
            else
            {
                if (!ValidMessage(messageText))
                {
                    MessageWrapper message = new("Devi scegliere una delle opzioni possibili", keyboard);
                    await Program.SendWrapperMessageAsync(chatId, message, token);
                }
            }

            return returnValue;
        }

        private bool ValidMessage(string message)
        {
            foreach(string possible in test)
                if (message == possible)
                    return true;

            return false;
        }
    }
}
