using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace ForceCodeFPS
{
    public static class r_RoomProperties
    {
        #region Public static variables
        public static string RoomMapProperty = "GameMap";
        public static string RoomGameModeProperty = "GameMode";
        public static string RoomMapImageIDProperty = "GameMapImageID";
        public static string RoomStateProperty = "RoomState";
        #endregion

        #region Actions
        private static Hashtable Put<T>(string _key, T _value)
        {
            Hashtable _table = new Hashtable();
            _table.Add(_key, (T)_value);

            return _table;
        }

        private static T Get<T>(Room _pl, string _key)
        {
            if (_pl.CustomProperties.ContainsKey(_key))
                return (T)_pl.CustomProperties[_key];

            return default;
        }
        #endregion

        #region Set
        public static void WriteInt(Room _room, string _key, int _value) => Push<int>(_room, Put(_key, _value));
        public static void WriteString(Room _room, string _key, string _value) => Push<string>(_room, Put(_key, _value));
        public static void WriteDouble(Room _room, string _key, double _value) => Push<double>(_room, Put(_key, _value));
        private static void Push<T>(Room _room, Hashtable _table) => _room.SetCustomProperties(_table);
        #endregion

        #region Get
        public static int ReadInt(Room _room, string _key) => Get<int>(_room, _key);
        public static string ReadString(Room _room, string _key) => Get<string>(_room, _key);
        public static double ReadDouble(Room _room, string _key) => Get<double>(_room, _key);
        #endregion
    }
}