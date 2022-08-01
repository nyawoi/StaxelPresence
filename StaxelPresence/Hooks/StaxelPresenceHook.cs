using AetharNet.Moonbow.Experimental.Templates;
using AetharNet.Moonbow.Experimental.Utilities;
using DiscordRPC;
using Plukit.Base;
using Staxel;
using Staxel.Logic;
using Staxel.Modding;

namespace AetharNet.StaxelPresence.Hooks
{
    internal class StaxelPresenceHook : ModHookV4Template, IModHookV4
    {
        private const string RpcClientId = "857763271959904328";
        private static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };

        private static DiscordRpcClient _rpcClient;
        private static RichPresence _presence;
        
        private int _previousDay;
        private string _previousPhase;
        private bool _wasCreativeEnabled;

        public StaxelPresenceHook()
        {
            if (!GameUtilities.IsClient) return;

            _rpcClient ??= new DiscordRpcClient(RpcClientId);
            _presence ??= new RichPresence();

            _presence.WithAssets(new Assets
            {
                LargeImageKey = "staxel_logo",
                LargeImageText = Properties.ContentRootVersion
            });
        }
        
        public override void Dispose()
        {
            _rpcClient?.Dispose();
            _rpcClient = null;
            _presence = null;
        }

        public override void CleanupOldSession()
        {
            _previousDay = 0;
            _previousPhase = "";
            _wasCreativeEnabled = false;

            if (_presence == null) return;
            
            _presence.State = "In Main Menu";
            _presence.Details = null;
            _presence.Timestamps = null;
                
            _rpcClient.SetPresence(_presence);
        }

        public override void GameContextInitializeAfter()
        {
            if (_presence == null) return;

            _presence.State = "In Main Menu";
            _presence.Details = null;
            _presence.Timestamps = null;
            
            _rpcClient.SetPresence(_presence);
            
            if (!_rpcClient.IsInitialized) _rpcClient.Initialize();
        }

        public override void UniverseUpdateBefore(Universe universe, Timestep step)
        {
            if (!GameUtilities.IsClient || !GameUtilities.ClientMainLoop.AvatarAvailable()) return;

            _presence.Timestamps ??= Timestamps.Now;
            
            var serverName = universe.ServerName();
            
            if (serverName.IsNullOrEmpty()) return;
            
            var serverDay = universe.DayNightCycle().Day + 1;
            var phaseName = universe.DayNightCycle().IsNight ? "Night" : "Day";
            var creativeModeEnabled = GameUtilities.ClientMainLoop.Avatar().PlayerEntityLogic.CreativeModeEnabled();
            
            if (_previousDay == serverDay
                && _previousPhase == phaseName
                && _wasCreativeEnabled == creativeModeEnabled) return;
            
            _previousDay = serverDay;
            _previousPhase = phaseName;
            _wasCreativeEnabled = creativeModeEnabled;
            
            var gameMode = creativeModeEnabled ? "Creative" : "Story";
            var actionVerb = creativeModeEnabled ? "Building" : "Playing";
            var seasonName = Seasons[universe.DayNightCycle().GetSeason()];
            var seasonDay = universe.DayNightCycle().GetCalendarDayOfSeason() + 1;
            
            _presence.Details = $"{actionVerb} in {gameMode} Mode ({serverName})";
            _presence.State = $"{seasonName} {seasonDay}{GetOrdinal(seasonDay)} ({phaseName} {serverDay})";
            
            _rpcClient.SetPresence(_presence);
        }
        
        private static string GetOrdinal(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }
    }
}