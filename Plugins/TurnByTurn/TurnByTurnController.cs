using Server.Plugins.API;
using Server.Plugins.Database;
using Server.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Plugins;
using Stormancer.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Server.Plugins.TurnByTurn
{
 

    public class TurnByTurnController : ControllerBase
    {

        private const string TURN_STATE_CHANGED_ROUTE = "turnbyturn.turnStateChanged";
        private const string SEND_ACTION_RPC = "turnbyturn.sendaction";
        private const string SETUP_GAME_RPC = "turbByTurn.setupGame";

        private readonly ILogger _logger;
        private readonly TurnBasedGame _game;
        private readonly IUserSessions _sessions;

        public TurnByTurnController( ILogger logger, TurnBasedGame game, IUserSessions sessions)
        {
            _logger = logger;
            _game = game;
            _sessions = sessions;
          
           
        }

      
        public async Task SubmitTransaction(RequestContext<IScenePeerClient> ctx)
        {
            var userId = (await _sessions.GetUser(ctx.RemotePeer))?.Id;

            if(userId == null)
            {
                throw new ClientException("Authentication required");
            }
            var dto = ctx.ReadObject<TransactionDto>();
            await _game.SubmitTransaction(userId, dto.PlayerId, dto.Command, JObject.Parse(dto.Args));
        }


    }

    public class TransactionDto
    {
        public string PlayerId { get; set; }
        public string Command { get; set; }
        public string Args { get; set; }

    }
   

}
