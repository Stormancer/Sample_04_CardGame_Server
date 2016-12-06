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

namespace Server.Plugins.TurnByTurn
{
 

    class TurnByTurnController : ControllerBase
    {

        private const string TURN_STATE_CHANGED_ROUTE = "turnbyturn.turnStateChanged";
        private const string SEND_ACTION_RPC = "turnbyturn.sendaction";
        private const string SETUP_GAME_RPC = "turbByTurn.setupGame";

        private const string SEND_ENDTURN_RPC = "turnbyturn.sendendturn";
      
        private readonly ILogger _logger;

        public TurnByTurnController( ILogger logger)
        {
            _logger = logger;

          
           
        }

        public Task SendAction(RequestContext<IScenePeerClient> ctx)
        {

        }

        public Task SendEndTurn(RequestContext<IScenePeerClient> ctx)
        {

        }


    }
   

}
