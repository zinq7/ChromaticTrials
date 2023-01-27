using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using RoR2.ExpansionManagement;
using RoR2.UI; // LOBBYCONFIG API is for adding bars such as "expansions"
using System.Text;

namespace SpeedyBirb
{
    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    //[BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class SpeedyBirb : BaseUnityPlugin
    {

        public static Lobby lobby;
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "zinq7";
        public const string PluginName = "ChromaticTrials";
        public const string PluginVersion = "1.0.1";

        public static string username = "zoinq";
        public static AssetBundle bundle;
        public LobbyList lobbyList;


        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            bundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("SpeedyBirb.dll", "scrollaboardbundle"));

            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            
            On.RoR2.DisableIfGameModded.OnEnable += No;
            On.RoR2.UI.WeeklyRunScreenController.InitializeLeaderboardInfo += No;
            On.RoR2.UI.WeeklyRunScreenController.UpdateLeaderboard += No;
            On.RoR2.UI.WeeklyRunScreenController.SetCurrentLeaderboard += No;
            On.RoR2.UI.WeeklyRunScreenController.UpdateLeaderboard += No;
            On.RoR2.UI.WeeklyRunScreenController.Update += No;

            On.RoR2.WeeklyRun.GenerateSeedForNewRun += SelectLobbySeed;
            On.RoR2.WeeklyRun.OverrideRuleChoices += MyRules;
            On.RoR2.WeeklyRun.ClientSubmitLeaderboardScore += SubmitToMe; ;

            On.RoR2.UI.WeeklyRunScreenController.OnEnable += SpawnHOOF;

            SteamworksClientManager.onLoaded += () =>
            {
                username = SteamworksClientManager.instance.steamworksClient.Username; // save the STEAM username of the player
            };

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
            Logger.LogInfo(MsgToSeed("Acrid the prismatic trial") + " -> Acrid the prismatic trial");
        }

        private void SubmitToMe(On.RoR2.WeeklyRun.orig_ClientSubmitLeaderboardScore orig, WeeklyRun self, RunReport runReport)
        {
            RunData finishedRun = new();
            if (!runReport.gameEnding.isWin) return; // don't submit on loss!!
            float time = runReport.runStopwatchValue;
            finishedRun.timeM = (int)time / 60;
            time %= 60;
            finishedRun.timeS = (int)time;
            finishedRun.timeL = (int)(time - (int)time);
            finishedRun.chara = runReport.FindFirstPlayerInfo().bodyName;
            finishedRun.user = username; // get username

            RunData betterRun = lobby.leaderboard.Find((x) => x.chara == finishedRun.chara && x.timeS * 10000 + x.timeM * 100 + x.timeL <
               finishedRun.timeS * 10000 + finishedRun.timeM * 100 + finishedRun.timeL);

            if (betterRun == null)
            {
                lobby.leaderboard.Add(finishedRun);
            }

            RefreshLBFile();
        }

        private void MyRules(On.RoR2.WeeklyRun.orig_OverrideRuleChoices orig, WeeklyRun self, RuleChoiceMask mustInclude, RuleChoiceMask mustExclude, ulong runSeed)
        {
            orig(self, mustInclude, mustExclude, runSeed);


            for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                self.ForceChoice(mustInclude, mustExclude, FindRuleForArtifact((ArtifactIndex)i).FindChoice("Off"));
            }

            foreach (string artifactName in lobby.artifacts)
            {
                self.ForceChoice(mustInclude, mustExclude, "Artifacts." + artifactName + ".On");
            }
        }
        private static RuleDef FindRuleForArtifact(ArtifactIndex artifactIndex)
        {
            ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
            return RuleCatalog.FindRuleDef("Artifacts." + artifactDef.cachedName);
        }

        private ulong SelectLobbySeed(On.RoR2.WeeklyRun.orig_GenerateSeedForNewRun orig, WeeklyRun self)
        {
            if (lobby != null)
            {
                return MsgToSeed(lobby.seed);
            } 
            else
            {
                return orig(self);
            }
        }
        private async void SpawnHOOF(On.RoR2.UI.WeeklyRunScreenController.orig_OnEnable orig, WeeklyRunScreenController self)
        {
            GameObject newLb = self.leaderboard.gameObject.transform.GetParent().GetParent().gameObject; // ignore me

            // update the "global leaderboards" tab into "lobbyList" tab
            newLb.transform.Find("HeaderContainer").Find("GenericHeaderButton (Global)").gameObject.name = "ListButton";
            newLb.transform.Find("HeaderContainer").Find("ListButton").GetComponentInChildren<HGTextMeshProUGUI>().text = "Lobby List";
            newLb.transform.Find("HeaderContainer").Find("ListButton").gameObject.GetComponent<LanguageTextMeshController>().enabled = false;
            newLb.transform.Find("HeaderContainer").Find("ListButton").gameObject.GetComponent<HGButton>().updateTextOnHover = false;

            // update the "leaderboards" tab 
            newLb.transform.Find("HeaderContainer").Find("GenericHeaderButton (Friends)").gameObject.name = "LeaderboardButton";
            newLb.transform.Find("HeaderContainer").Find("LeaderboardButton").GetComponentInChildren<HGTextMeshProUGUI>().text = "Leaderboard";
            newLb.transform.Find("HeaderContainer").Find("LeaderboardButton").gameObject.GetComponent<LanguageTextMeshController>().enabled = false;
            newLb.transform.Find("HeaderContainer").Find("LeaderboardButton").gameObject.GetComponent<HGButton>().updateTextOnHover = false;

            // fix the description and shiss
            Transform mainPanel = self.transform.Find("Main Panel");
            mainPanel.Find("Title").gameObject.GetComponent<HGTextMeshProUGUI>().text = RainbowString("Chromatic Trials");
            mainPanel.Find("GenericMenuButtonPanel").Find("Description").gameObject.GetComponent<HGTextMeshProUGUI>().text =
                (
                "Complete in Chromatic Trials with friends and foes! Join a lobby and try to reach the top!\nThere are a multitude" +
                " of different challenges to race other against, ranging from the standard \"Clear X stages\" to \"Kill a Scavenger\", with a " +
                "Leaderboard keeping track of either time or points!\n\n" +
                "Chromatic Trials offer a fun new way to play with your friends asynchronously, and once I bother to make it, you can " +
                "even host your own lobby! \n\nBegin by selecting a lobby to play in!");
            mainPanel.Find("GenericMenuButtonPanel").Find("Description").gameObject.GetComponent<LanguageTextMeshController>().enabled = false;

            // check whether they have selected a lobby yet
            if (lobby == null)
            {
                // go to the lobby list, cancel non lobby list features
                newLb.transform.Find("HeaderContainer").Find("ListButton").gameObject.GetComponent<HGButton>().InvokeClick(); // simulate click
                newLb.transform.Find("HeaderContainer").Find("LeaderboardButton").gameObject.GetComponent<HGButton>().enabled = false;
                mainPanel.Find("GenericMenuButtonPanel").Find("JuicePanel").Find("GenericMenuButton (Start)")
                    .gameObject.GetComponent<HGButton>().enabled = false; // disable "start game" button until lobby selected
            }

            // create a rainbo background to make it look cooler
            GameObject rainbo = Instantiate(bundle.LoadAsset<GameObject>("SlideRainbow"));
            rainbo.transform.SetParent(self.transform);
            rainbo.transform.SetAsFirstSibling(); // go to back
            FixRectTransform(rainbo, new Vector2(-1f, -1f), new Vector2(2f, 2f));
            Image rainboIMG = rainbo.GetComponent<Image>();
            rainboIMG.color = new Color(0.8f, 0.8f, 0.8f, 0.05f);
            rainboIMG.raycastTarget = false; // add the rainbow backdrop
            rainbo.AddComponent<Speen>().rt = rainbo.GetComponent<RectTransform>(); // make it speeeeen

            // override the size of the leaderboard
            RectTransform rectTransform = newLb.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.60f, 0.1f); // bottom left corner
            rectTransform.anchorMax = new Vector2(0.95f, 0.9f); // top right corner (MUST BE BIGGER THAN THE OTHER ONE)
            rectTransform.sizeDelta = Vector2.zero; // autoscale?
            rectTransform.anchoredPosition = Vector2.zero;

            // shift newLb down a layer
            newLb = newLb.transform.Find("Pages").gameObject;
            Destroy(newLb.transform.Find("NoEntry").gameObject, 0); // clear the "no submission" message

            if (lobby != null)
            {
                UpdateLobby(self); // u get it
            }

            // READ JSON FILE
            StreamReader pog = new(Assembly.GetExecutingAssembly().Location.Replace("SpeedyBirb.dll", "lobbies.json"));
            string json = await pog.ReadToEndAsync(); // temp way to get the JSON
            lobbyList = JsonUtility.FromJson<LobbyList>(json);

            foreach (Lobby lob in lobbyList.lobbies)
            {
                GameObject runList = self.leaderboard.transform.GetParent().GetParent()
                    .Find("Pages").Find("GlobalPage").Find("LeaderboardArea").Find("Content").gameObject;

                GameObject strip = Instantiate(bundle.LoadAsset<GameObject>("LobbyStrip")); // get the strip prefab

                // unhighlight if different lobby or no lobby is selected
                if (lobby != null && lobby.lobbyName == lob.lobbyName)
                {
                    strip.transform.Find("AmHostHighlight").gameObject.SetActive(true); // only highlight selected lobby
                } else
                {
                    strip.transform.Find("AmHostHighlight").gameObject.SetActive(false); // unhighlight otherwise
                }

                // set name and properties
                strip.name = "-" + lob.lobbyID;
                strip.transform.SetParent(runList.transform);
                strip.transform.Find("LobbyLabel").gameObject.AddComponent<HGTextMeshProUGUI>().text = lob.lobbyName;
                strip.transform.Find("PlayerNumLabel").gameObject.AddComponent<HGTextMeshProUGUI>().text = lob.leaderboard.Count + "";
                strip.AddComponent<HGButton>().onClick.AddListener(() =>
                {
                    ProcessClick(self, lob);
                }); // make it BOUTON

                //strip.GetComponent<HGButton>().imageOnHover.sprite = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("Hoof")).pickupIconSprite;
            }

            // TODO: update the leaderboards.json file format to be a lobbies.json and contain stuff
        }

        public void UpdateLobby(WeeklyRunScreenController self)
        {
            GameObject stripPrefab = self.leaderboard.stripPrefab; // get the strip prefab
            GameObject runList = self.leaderboard.transform.GetParent().GetParent()
                .Find("Pages").Find("FriendsPage").Find("LeaderboardArea").Find("Content").gameObject;

            // clear the current leaderboard
            foreach (Transform t in runList.GetComponentInChildren<Transform>())
            {
                t.gameObject.SetActive(false);
                Destroy(t.gameObject, 0);
            }

            // uh oh 
            for (int i = 0; i < lobby.leaderboard.Count && i < 16; i++)
            {
                RunData data = lobby.leaderboard[i];

                GameObject strip = Instantiate(stripPrefab);
                strip.transform.SetParent(runList.transform);
                strip.transform.Find("RankLabel").gameObject.GetComponent<HGTextMeshProUGUI>().text = (i + 1) + "";
                strip.transform.Find("UsernameLabel").gameObject.GetComponent<HGTextMeshProUGUI>().text = data.user;
                strip.transform.Find("TimeLabel").gameObject.GetComponent<HGTextMeshProUGUI>().text = data.timeM + ":" + To2(data.timeS) + "." + To2(data.timeL);
                strip.transform.Find("SurvivorImagePanel").Find("SurvivorImage").gameObject.GetComponent<RawImage>().texture = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("Hoof")).pickupIconSprite.texture;

                // HIDE OUT NON YOU USERS
                if (data.user != username)
                {
                    strip.transform.Find("IsMeHighlight").gameObject.SetActive(false);
                }
                strip.transform.position = new Vector2(-50f, -50f);
            }
        }

        private void ProcessClick(WeeklyRunScreenController screen, Lobby rules)
        {
            lobby = rules; // set the lobby!

            GameObject runList = screen.leaderboard.transform.GetParent().GetParent()
                .Find("Pages").Find("GlobalPage").Find("LeaderboardArea").Find("Content").gameObject;

            for (int i = 0; i < runList.transform.childCount; i++)
            {
                GameObject lobbyStrip = runList.transform.GetChild(i).gameObject;
                if (lobbyStrip.name != "-" + lobby.lobbyID)
                {
                    lobbyStrip.transform.Find("AmHostHighlight").gameObject.SetActive(false);
                } 
                else
                {
                    lobbyStrip.transform.Find("AmHostHighlight").gameObject.SetActive(true);
                }
            }

            GameObject newLb = screen.leaderboard.gameObject.transform.GetParent().GetParent().gameObject;
            Transform mainPanel = screen.transform.Find("Main Panel");

            // go to the lobby list, add lobby features
            newLb.transform.Find("HeaderContainer").Find("LeaderboardButton").gameObject.GetComponent<HGButton>().InvokeClick(); // simulate click
            newLb.transform.Find("HeaderContainer").Find("LeaderboardButton").gameObject.GetComponent<HGButton>().enabled = true;
            mainPanel.Find("GenericMenuButtonPanel").Find("JuicePanel").Find("GenericMenuButton (Start)")
                .gameObject.GetComponent<HGButton>().enabled = true; // enable "start game" button until lobby selected

            UpdateLobby(screen); // update to match the current lobby
        }

        private void RefreshLBFile() {
            string jsonFile = JsonUtility.ToJson(lobbyList);
            FileStream fs = new(Assembly.GetExecutingAssembly().Location.Replace("SpeedyBirb.dll", "lobbies.json"), FileMode.Open);
            byte[] info = new UTF8Encoding(true).GetBytes(jsonFile);
            fs.Write(info, 0, info.Length); 
            //TODO: fix this code and make it function
        }

        private ulong MsgToSeed(string msg)
        {
            ulong seed = 0; // seed
            List<string> msgs = new(); // create list for the message subsegments
            string temp = "";

            // splice the message into 4 - length segments and add those to the list
            for (int i = 0; i < msg.Length; i++)
            {
                temp += msg[i];
                if (temp.Length == 4)
                {
                    msgs.Add(temp);
                    temp = "";
                }
            }

            // apply a shift to for every character in the string
            foreach (string fourPart in msgs)
            {
                for (int i = 0; i < fourPart.Length; i++)
                {
                    seed += ((ulong)fourPart[i] - '0') * (ulong)Mathf.Pow(10, i * 2); // bitshift
                }
            }

            return seed;
        }

        private string RainbowString(string og)
        {
            // make a string full of color tags
            string ret = "";
            string[] rainbow = new string[] { "<color=#FF99FF>", "<color=#FFFF99>", "<color=#999999>",
                "<color=#99FFFF>", "<color=#99FF99>", "<color=#9999FF>", "<color=#FF9999>"};

            for (int i = 0; i < og.Length; i++)
            {
                ret += rainbow[i % rainbow.Length] + og[i];
            }

            return ret;
        }
        private void FixRectTransform(GameObject obj, Vector2 min, Vector2 max)
        {
            RectTransform rct = obj.GetComponent<RectTransform>();
            rct.anchorMin = min;
            rct.anchorMax = max;
            rct.sizeDelta = Vector2.zero;
            rct.anchoredPosition = Vector2.zero; // huh
        }

        /// <summary>
        /// For times, makes it 2 characters so that 8:5.9 become 8:05.09
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private string To2(int num)
        {
            if ((num + "").Length < 2)
                return "0" + num;
            else
                return num + "";
        }

        public void ListAllComponents(GameObject obj, string path = "")
        {
            if (obj is null) { Logger.LogInfo("that ain't an obect"); return; }

            //Logger.LogInfo("PRINTING INFO ABOUT " + obj);
            foreach (Component comp in obj.GetComponents<Component>())
                Logger.LogInfo(path + comp);

            for (int i = 0; i < obj.transform.childCount; i++)
                ListAllComponents(obj.transform.GetChild(i).gameObject, " -> " + path + obj.name);

        }

        private void Annihilate(GameObject target)
        {
            while (target.transform.childCount > 0)
                Annihilate(target.transform.GetChild(0).gameObject);

            Destroy(target);
        }

        private void No<T, R>(T no, R nah) { }

        private void No<T, R, C>(T no, R nah, C nope) { }

       // private void No<T, R, C, V>(T no, R nah, C nope, V nill) { }

       // private void No<T, R, C, V, P>(T no, R nah, C nope, V nill, P nuh) { }


    }
}