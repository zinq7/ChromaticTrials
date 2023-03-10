using System;
using System.Collections.Generic;

namespace ChromaticTrials
{
    [Serializable]
    public class Lobby
    {
        [UnityEngine.SerializeField]
        public List<RunData> leaderboard;
        public string seed;
        public List<string> artifacts, survivors, freeItems, mods;
        public string time;
        public string lobbyName, hostUsername;
        public int stages;
        public bool crystalsDropItems, vengeancifyBossTwo;
        public int lobbyID;
        public int stageCount, crystalCount, crystalsRequired;
    }
}