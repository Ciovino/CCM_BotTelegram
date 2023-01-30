using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCM_BotTelegram
{
    internal abstract class Command
    {
        bool active;
        public bool Active { get; set; }

        protected Command()
        {
            this.active = false;
        }

        public abstract MessageWrapper Activate();

        public abstract MessageWrapper Deactivate();
    }
}
