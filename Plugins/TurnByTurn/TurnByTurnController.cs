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
using MsgPack.Serialization;

namespace Server.Plugins.TurnByTurn
{
 

    public class TransactionController : ControllerBase
    {


        private readonly ILogger _logger;
        private readonly TurnBasedGame _game;
        private readonly IUserSessions _sessions;

        public TransactionController( ILogger logger, TurnBasedGame game, IUserSessions sessions)
        {
            _logger = logger;
            _game = game;
            _sessions = sessions;
          
           
        }

      
        public async Task Submit(RequestContext<IScenePeerClient> ctx)
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
        [MessagePackMember(0)]
        public string PlayerId { get; set; }
        [MessagePackMember(1)]
        public string Command { get; set; }
        [MessagePackMember(2)]
        public string Args { get; set; }

    }
   

}
