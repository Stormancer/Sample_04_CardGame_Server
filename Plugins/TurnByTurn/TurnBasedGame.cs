﻿using Newtonsoft.Json.Linq;
using Server.Users;
using Stormancer.Core;
using Stormancer.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using MsgPack.Serialization;

namespace Server.Plugins.TurnByTurn
{
    public interface ITurnBaseGameEventHandler
    {
        Task OnDesynchronization(string reason);
    }

    public class TurnBasedGame
    {
        public const string ENDTURN_CMD = "#endturn";

        public const string VALIDATE_TRANSACTION_RPC = "transaction.execute";
        public const string DESYNCHRONIZATION_ROUTE = "tbt.desync";
        public const string REPLAY_TRANSACTION_LOG_RPC = "tbt.replayTLog";

        private readonly AsyncLock _transactionLock = new AsyncLock();
        private readonly List<TransactionLogItem> _commands = new List<TransactionLogItem>();
        private readonly ISceneHost _scene;
        private readonly ILogger _logger;
        private readonly IUserSessions _sessions;
        private readonly IEnumerable<ITurnBaseGameEventHandler> _handlers;


        public TurnBasedGame(ISceneHost scene, ILogger logger, IUserSessions sessions, IEnumerable<ITurnBaseGameEventHandler> handlers)
        {
            _scene = scene;
            _logger = logger;
            _sessions = sessions;
            _handlers = handlers;
            scene.Connected.Add(OnConnected);
            scene.Disconnected.Add(OnDisconnected);
        }

        private async Task OnConnected(IScenePeerClient client)
        {
            _logger.Log(LogLevel.Debug, "gameSession", "client connected",new { });
            var user = await _sessions.GetUser(client);
            if (user == null)
            {
                throw new InvalidOperationException("The client must be authenticated");
            }

            _players[user.Id] = client;

            await ReplayTransactionLog(client);
        }

        public async Task ReplayTransactionLog(IScenePeerClient peer)
        {
            using (await _transactionLock.LockAsync())
            {
                await peer.RpcVoid(REPLAY_TRANSACTION_LOG_RPC, TransactionLog);
            }
        }

        private async Task OnDisconnected(DisconnectedArgs args)
        {
            var user = await _sessions.GetUser(args.Peer);
            _players[user.Id] = null;
        }
        //
        public Dictionary<string, string> PlayerMap = new Dictionary<string, string>();

        private Dictionary<string, IScenePeerClient> _players = new Dictionary<string, IScenePeerClient>();
        public int CurrrentStepId { get; private set; }

        public IEnumerable<TransactionLogItem> TransactionLog { get { return _commands; } }
        /// <summary>
        /// Adds a command to the game state
        /// </summary>
        /// <param name="userId">User that issued the command</param>
        /// <param name="playerId">Player that issued the command. (Different from userId to allow IA players or several players on a single client)</param>
        /// <param name="command">The command name</param>
        /// <param name="args"></param>
        /// <param name="updatedGameHash"></param>
        /// <returns></returns>
        public Task SubmitTransaction(string userId, string playerId, string command, JObject args)
        {
            if (userId != null)
            {
                if (playerId == null)
                {
                    throw new ClientException("Missing playerId");
                }
                string u;

                if (!PlayerMap.TryGetValue(playerId, out u))
                {
                    throw new InvalidOperationException("The user is not in the PlayerMap");
                }
                if (u != userId)
                {
                    throw new InvalidOperationException("The user cannot handler the specified playerId");
                }
                if (!_players.ContainsKey(userId))
                {
                    throw new InvalidOperationException("The user is not connected");
                }
            }
            if (command == null)
            {
                throw new ClientException("Missing command");
            }

            var cmd = new TransactionCommand
            {
                Cmd = command,
                Arguments = args.ToString(),
                CreatedOn = DateTime.UtcNow,
                IssuerPlayerId = playerId,
                IssuerUserId = userId,
                FinalStepId = CurrrentStepId++
            };

            return SubmitTransaction(cmd);

        }

        private async Task SubmitTransaction(TransactionCommand cmd)
        {
            using (await _transactionLock.LockAsync())
            {
                _logger.Log(LogLevel.Debug, "gameSession", "Transaction submitted.", new
                {
                    map = PlayerMap,
                    players = _players.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Id),
                    Cmd = cmd
                });
                var responses = await Task.WhenAll(PlayerMap.Select(async p =>
                {
                    IScenePeerClient peer = null;
                    _players.TryGetValue(p.Value, out peer);
                    
                    TransactionResponse response = null;
                    if (peer != null)
                    {
                        try
                        {
                            var r = await peer.RpcTask<TransactionCommand, TransactionResponse>(VALIDATE_TRANSACTION_RPC, cmd);
                            r.Success = true;
                            response = r;
                        }
                        catch (Exception)//Failed to execute transaction update on client
                        {
                            response = new TransactionResponse { Success = false };
                        }
                    }
                    return Tuple.Create(p.Key, response);
                }));
                var receivedResponses = responses.Where(t => t.Item2 != null);

                var isValid = true;
                if (receivedResponses.Any())
                {
                    isValid = receivedResponses.All(t => t.Item2.Success) && receivedResponses.Select(t => t.Item2).Distinct().Count() == 1;
                }

                if (!isValid)
                {
                    await EnsureTransactionFailed($"game states hash comparaison failed: [{string.Join(", ", responses.Select(t => $"'{t.Item1}' => {t.Item2}"))}]");
                }
                var hash = responses.FirstOrDefault(r => r.Item2 != null)?.Item2.Hash;
                _commands.Add(new TransactionLogItem { Command = cmd, ResultHash = hash ?? 0, HashAvailable = hash.HasValue });


            }
        }

        private async Task EnsureTransactionFailed(string reason)
        {
            foreach (var peer in _players.Values)
            {
                peer?.Send(DESYNCHRONIZATION_ROUTE, reason);
            }

            await _handlers.RunEventHandler(h => h.OnDesynchronization(reason), ex => _logger.Log(LogLevel.Error, "turnbased", "An error occured while running turnBased.OnDesync event handlers", ex));
            throw new InvalidOperationException($"Transaction failed : '{reason}'");
        }

        public void AddPlayer(string userId, string playerId)
        {
            PlayerMap.Add(playerId, userId);
        }

    }

    public class TransactionLogItem
    {
        [MessagePackMember(0)]
        public TransactionCommand Command { get; set; }

        [MessagePackMember(1)]
        public int ResultHash { get; set; }

        [MessagePackMember(2)]
        public bool HashAvailable { get; set; }
    }
    public class TransactionCommand
    {
        [MessagePackMember(0)]
        public int FinalStepId { get; set; }

        [MessagePackMember(1)]
        public DateTime CreatedOn { get; set; }

        [MessagePackMember(2)]
        public string IssuerUserId { get; set; }
        [MessagePackMember(3)]
        public string IssuerPlayerId { get; set; }
        [MessagePackMember(4)]
        public string Cmd { get; set; }
        [MessagePackMember(5)]
        public string Arguments { get; set; }
    }

    public class TransactionResponse
    {
        [MessagePackMember(0)]
        public bool Success { get; set; }
        [MessagePackMember(1)]
        public string Reason { get; set; }
        [MessagePackMember(2)]
        public int Hash { get; set; }

        public override bool Equals(object obj)
        {
            var v = obj as TransactionResponse;
            return v != null && v.Hash == Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Success}[{Hash}], {Reason}";
        }
    }

    public abstract class TurnSequenceBase
    {
        public abstract Task StartTurn();
        public TurnSequenceBase(TurnBasedGame game)
        {
            _game = game;
        }
        protected TurnBasedGame _game;
        public bool IsActive { get; protected set; }
    }

    public class PlayerTurn : TurnSequenceBase
    {
        private TaskCompletionSource<bool> _tcs;
        public PlayerTurn(TurnBasedGame game) : base(game)
        {
        }

        public string PlayerId { get; set; }
        public void EndTurn()
        {
            lock (this)
            {
                if (IsActive)
                {
                    _tcs.TrySetResult(true);
                }
            }
        }
        public override async Task StartTurn()
        {

            lock (this)
            {
                if (IsActive)
                {
                    return;
                }
                _tcs = new TaskCompletionSource<bool>();
                IsActive = true;
            }

            await _tcs.Task;
            IsActive = false;

        }
    }
    public class SequentialTurnSequence : TurnSequenceBase
    {
        public SequentialTurnSequence(TurnBasedGame game) : base(game)
        {
        }

        public List<TurnSequenceBase> Sequence { get; private set; } = new List<TurnSequenceBase>();

        public override async Task StartTurn()
        {
            var i = 0;
            while (i < Sequence.Count)
            {
                await Sequence[i++].StartTurn();
            }
        }
    }
    public class SimultaneousTurnSequence : TurnSequenceBase
    {
        public SimultaneousTurnSequence(TurnBasedGame game) : base(game)
        {
        }

        public List<TurnSequenceBase> Sequence { get; private set; } = new List<TurnSequenceBase>();

        public override Task StartTurn()
        {
            return Task.WhenAll(Sequence.Select(s => s.StartTurn()));
        }
    }
}
