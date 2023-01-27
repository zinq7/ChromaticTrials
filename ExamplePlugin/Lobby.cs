using System;
using System.Collections.Generic;

namespace SpeedyBirb
{
    [Serializable]
    public class Lobby
    {
        [UnityEngine.SerializeField]
        public List<RunData> leaderboard;
        public string seed;
        public List<string> artifacts, survivors;
        public string time;
        public string lobbyName;
        public string hostUsername;
        public int lobbyID;
    }
}