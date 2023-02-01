namespace CCM_BotTelegram
{
    internal abstract class Command
    {
        bool active;
        public bool Active { 
            get { return active; }
            set { active = value; } 
        }

        protected Command()
        {
            Active = false;
        }

        public abstract MessageWrapper Activate();

        public abstract MessageWrapper Deactivate();

        public abstract Task<State> ExecuteCommand(string messageText, long chatId, CancellationToken token);
    }
}