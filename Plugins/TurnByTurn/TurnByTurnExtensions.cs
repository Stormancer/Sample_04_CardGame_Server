using Server.Plugins.TurnByTurn;
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public static class TurnByTurnExtensions
    {
        public static void AddTurnByTurn(this ISceneHost scene)
        {
            scene.Metadata[TurnByTurnPlugin.METADATA_KEY] = "enabled";
        }
    }
}
