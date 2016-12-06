using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.TurnByTurn
{
    class TurnBasedGame
    {
        private AsyncLock _transactionLock = new AsyncLock();
        private List<Command> _commands = new List<Command>();
        public int CurrrentStepId { get; private set; }

        public IEnumerable<Command> Commands { get { return _commands; } }
        /// <summary>
        /// Adds a command to the game state
        /// </summary>
        /// <param name="userId">User that issued the command</param>
        /// <param name="playerId">Player that issued the command. (Different from userId to allow IA players or several players on a single client)</param>
        /// <param name="command">The command name</param>
        /// <param name="args"></param>
        /// <param name="updatedGameHash"></param>
        /// <returns></returns>
        public async Task AddAction(string userId, string playerId, string command, JObject args, int updatedGameHash)
        {
            using (await _transactionLock.LockAsync())
            {
                var cmd = new Command
                {
                    Cmd = command,
                    Arguments = args,
                    CreatedOn = DateTime.UtcNow,
                    PlayerId = playerId,
                    UserId = userId,
                    StepId = CurrrentStepId++
                };
               
            }
        }

        public async Task EndTurn(string userId, string playerId, JObject args, int updatedGameHash)
        {
            using (await _transactionLock.LockAsync())
            {

                var cmd = new Command
                {
                    Cmd = "#endturn",
                    Arguments = args,
                    CreatedOn = DateTime.UtcNow,
                    PlayerId = playerId,
                    UserId = userId,
                    StepId = CurrrentStepId++
                };
            }
        }
    }

    public class Command
    {
        public int StepId { get; set; }

        public DateTime CreatedOn { get; set; }

        public string UserId { get; set; }
        public string PlayerId { get; set; }
        public string Cmd { get; set; }

        public JObject Arguments { get; set; }
    }
}
