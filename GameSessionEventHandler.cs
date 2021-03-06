﻿using Newtonsoft.Json.Linq;
using Server.Plugins.GameSession;
using Server.Plugins.TurnByTurn;
using Stormancer.Diagnostics;
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
        private readonly ILogger _logger;
        public GameSessionEventHandler(TurnBasedGame game, ILogger logger)
        {
            _game = game;
            _logger = logger;
        }
        public Task GameSessionCompleted(GameSessionCompleteCtx ctx)
        {
            return Task.FromResult(true);
        }

        public async Task GameSessionStarted(GameSessionStartedCtx ctx)
        {
            _logger.Info("gamesession", "Invoking game started event handler.");
            foreach(var player in ctx.Peers)
            {
                _game.AddPlayer(player.UserId, player.UserId);//Map each user to a single player in the game.
            }

            await _game.SubmitTransaction(null, null, "start", JObject.FromObject(new { seed = 0 }));
        }
    }
}
