using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ForceCodeFPS
{
    public static class r_PlayerProperties
    {
        #region Public static variables
        public static string KillsPropertyKey = "Kills";
        public static string DeathsPropertyKey = "Deaths";

        public static string WeaponIndexPropertyKey = "WeaponIndex";
        #endregion

        #region Set
        public static void WriteInt(Player _pl, string _key, int _value) => Push<int>(_pl, Put(_key, _value));
        public static void WriteString(Player _pl, string _key, string _value) => Push<string>(_pl, Put(_key, _value));

        private static void Push<T>(Player _pl, Hashtable _table) => _pl.SetCustomProperties(_table);

        private static Hashtable Put<T>(string _key, T _value)
        {
            Hashtable _table = new Hashtable();
            _table.Add(_key, (T)_value);

            return _table;
        }
        #endregion

        #region Get
        public static int ReadInt(Player _pl, string _key) => Get<int>(_pl, _key);
        public static string ReadString(Player _pl, string _key) => Get<string>(_pl, _key);

        private static T Get<T>(Player _pl, string _key)
        {
            if (_pl.CustomProperties.ContainsKey(_key))
                return (T)_pl.CustomProperties[_key];

            return default;
        }

        public static int GetPlayerKills(Player _player)
        {
            if (_player.CustomProperties.TryGetValue(r_PlayerProperties.KillsPropertyKey, out object _kills))
            {
                return (int)_kills;
            }
            return 0;
        }

        public static int GetPlayerDeaths(Player _player)
        {
            if (_player.CustomProperties.TryGetValue(r_PlayerProperties.DeathsPropertyKey, out object _deaths))
            {
                return (int)_deaths;
            }
            return 0;
        }
        #endregion
    }
}