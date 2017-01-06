using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Matchmaking.Dto
{
    public class MatchmakingRequest
    {
        [MessagePackMember(0)]
        public string Filter { get; set; }
    }
}
