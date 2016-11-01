using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Plugins.API;
using Stormancer.Server;
using Server.Users;

namespace Server.Plugins.TurnByTurn
{
    class TurnByTurnPlugin : IHostPlugin
    {
        public const string METADATA_KEY = "stormancer.turnByTurn";

        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostDependenciesRegistration += (IDependencyBuilder builder) =>
              {
           
                  builder.Register<TurnByTurnController>().InstancePerRequest();
                 

              };
            ctx.SceneDependenciesRegistration += (IDependencyBuilder builder, ISceneHost scene) =>
              {
                  if(scene.Metadata.ContainsKey(METADATA_KEY))
                  {
                      builder.Register<TurnBasedGame>().InstancePerScene();
                  }
              };
            ctx.SceneCreated += (ISceneHost scene) =>
             {
                 if (scene.Metadata.ContainsKey(METADATA_KEY))
                 {
                     scene.AddController<TurnByTurnController>();
                     
                 }
             };
        }
    }
}
