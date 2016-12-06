using Newtonsoft.Json.Linq;
using Server.Plugins.GameSession;
using Server.Plugins.TurnByTurn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class GameSessionEventHandler : IGameSessionEventHandler
    {
        private readonly TurnBasedGame _game;
        public GameSessionEventHandler(TurnBasedGame game)
        {
            _game = game;
        }
        public Task GameSessionCompleted(GameSessionCompleteCtx ctx)
        {
            return Task.FromResult(true);
        }

        public async Task GameSessionStarted(GameSessionStartedCtx ctx)
        {
            foreach(var player in ctx.Peers)
            {
                _game.AddPlayer(player.UserId, player.UserId);//Map each user to a single player in the game.
            }

            await _game.SubmitTransaction(null, null, "start", JObject.FromObject(new { seed = 0 }));
        }
    }
}
