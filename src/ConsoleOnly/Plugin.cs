using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

namespace ConsoleOnly
{
    [BepInPlugin(PluginId, "ConsoleOnly", "1.0.0")]
    public class ConsoleOnlyPlugin : BaseUnityPlugin
    {
        public const string PluginId = "patricnox.valheim.ConsoleOnly";
        private Harmony _harmony;
        private static ConsoleOnlyPlugin _instance;
        public static ManualLogSource p = BepInEx.Logging.Logger.CreateLogSource(PluginId);

        [UsedImplicitly]
        private void Awake()
        {
            _instance = this;
            _harmony = Harmony.CreateAndPatchAll(typeof(ConsoleOnlyPlugin).Assembly, PluginId);
            p.LogInfo("[ConsoleOnly] Started");
        }

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddPeer))]
        internal class OnPlayerJoin
        {
            private static bool Prefix(ZNetPeer netPeer)
            {
                if (netPeer.IsReady() == false || netPeer.m_socket == null)
                {
                    p.LogInfo($"[ERROR] COULD NOT LOGIN PLAYER CUS INVALID NETPEER: {netPeer}");
                    return false;
                }

                string hostName = netPeer.m_socket.GetHostName();

                var (id, platform) = NormalizeSteamID(hostName);

                string status = Client.PlayerConnecting(netPeer.m_characterID, netPeer.m_playerName, id, platform);
                switch (status)
                {
                    case "NotAllowed":
                        netPeer.m_rpc.Invoke("Error", new object[]
                        {
                            4
                        });
                        p.LogInfo($"NotAllowed: {id} on {platform}");
                        return false;
                    case "OK":
                        return true;
                }
                return true;
            }

            internal class Client
            {
                public static string PlayerConnecting(ZDOID player, string name, string id, string platform)
                {
                    if (name == "" || id == "")
                    {
                        p.LogInfo($"Could not fire PlayerConnecting, missing params. Name: {name} | Id: {id} | Platform: {platform}");
                        return "NotAllowed";
                    }

                    if (platform != "xbox") return "NotAllowed";

                    return "OK";
                }
            }
        }

        private static (string id, string? platform) NormalizeSteamID(string steamID)
        {
            if (steamID.Contains("_"))
            {
                var parts = steamID.Split('_');
                return parts.Length > 1 ? (id: parts[1], platform: parts[0]) : (id: steamID, platform: null);
            }
            return (id: steamID, platform: null);
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
