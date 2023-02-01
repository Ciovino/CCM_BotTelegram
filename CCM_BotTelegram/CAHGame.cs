using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;

namespace CCM_BotTelegram
{
    internal class CAHGame : Command
    {
        public override MessageWrapper Activate()
        {
            Active = true;

            string text = "Inizio Partita (non è vero, è ancora da implementare)";

            return new MessageWrapper(text);
        }

        public override MessageWrapper Deactivate()
        {
            Active = false;

            string text = "Fine Partita";


            return new MessageWrapper(text);
        }

        public override async Task<State> ExecuteCommand(string messageText, long chatId, CancellationToken token)
        {
            State returnValue = State.CAHGame;

            // Check if is a command
            if (messageText[0] == '/')
            {
                string command = messageText[1..];
                switch (Program.SimpleCommand(command))
                {
                    case "exit": // Back to NoCommand
                        returnValue = State.NoCommand;

                        MessageWrapper gameMessage = Deactivate();
                        await Program.SendWrapperMessageAsync(chatId, gameMessage, token);
                        break;

                    default: // Send Error message
                        MessageWrapper message = new("Non esiste stu comand asscemo");
                        await Program.SendWrapperMessageAsync(chatId, message, token);
                        break;
                }
            }
            else
            {
                MessageWrapper message = new("Tanto non succede niente");
                await Program.SendWrapperMessageAsync(chatId, message, token);
            }

            return returnValue;
        }
    }
}
