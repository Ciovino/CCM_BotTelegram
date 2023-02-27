using System.Runtime.CompilerServices;
using System.Timers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Timer = System.Timers.Timer;

namespace CCM_BotTelegram
{
    internal class ChatState
    {
        readonly Chat chatInfo;
        private Action[,] actions;
        private static Timer? timer;

        public enum Events { Play, Stop, Retry };
        public enum States { Idle, WaitPoll, Playing, Retry };
        public States State { get; set; }

        public ChatState(Chat chatInfo)
        {
            this.chatInfo = chatInfo;

            // events * states
            actions = new Action[4, 3] {
                 // Play          // Stop         // Retry
                 { WaitForPoll,     DoNothing,      DoNothing }, // Idle
                 { DoNothing,       DoNothing,      DoNothing }, // WaitPoll
                 { DoNothing,       StopPlaying,    DoNothing }, // Playing
                 { DoNothing,       StopPlaying,    WaitForPoll }  // Retry
            };
        }

        private void DoNothing() { return; }
        private void WaitForPoll() 
        {
            SetPollTimer();
            State = States.WaitPoll; 
        }
        private void StopPlaying() { State = States.Idle; }

        private void SetPollTimer()
        {
            timer = new(60000); // 60 seconds timer
            timer.Elapsed += EndTimer;
            timer.Enabled = true;
        }

        private void EndTimer(object source, ElapsedEventArgs e)
        {
            // 60 seconds have passed
            timer.Enabled = false;

            this.State = States.Playing;
        }

        public bool UseThisChat(Chat chatToCheck)
        {
            return chatToCheck.Id == chatInfo.Id;
        }
    }
}
