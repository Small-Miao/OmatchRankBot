using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using MySql.Data.MySqlClient;
using RegistrationWebsite.Helper;

namespace OsuRankMatch {
    class Program {
        public static IrcClient IRC = new IrcClient();
        //定义IRC客户端
        const string ServerIp = "irc.ppy.sh";
        const int ServerPort = 6667;
        const string botusername = "********";
        const string botpassword = "********";
        static List<string> msgqueue = new List<string>();
        const string sqlconnectstr = "server= 39.104.200.83;User Id=osurank;password=7649102;Database=osurank";
        //实例化数据库客户端
        static MySqlConnection sqlconnect = new MySqlConnection(sqlconnectstr);
        static Dictionary<String, int> matchinglist = new Dictionary<String, int>();
        static List<String> MatchQueue = new List<string>();
        static List<Match> RoomList = new List<Match>();
        static bool InChannel = false;
        static bool Inmatch(string playerid) {
            foreach (var item in RoomList) {
                foreach (var i in item.Playerlist) {
                    if (i == playerid) {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void IrcMessageQueuq(string to, string Message) {
            msgqueue.Add(to + "*" + Message);
        }
        public static bool IntheMatchQueue(string playerid) {
            foreach (var item in MatchQueue) {
                foreach (var player in item.Split('|')) {
                    if (player == playerid) {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void IrcMessageQuquqSendThead() {
            ColorWriteLine("MessageThreadStart!", 0);
            new Thread(() => {
                while (true) {
                    if (msgqueue.Count > 0) {
                        for (int i = 0; i < msgqueue.Count; i++) {
                            Thread.Sleep(1000);
                            IRC.SendMessage(SendType.Message, msgqueue[i].Split('*')[0], msgqueue[i].Split('*')[1]);
                            msgqueue.Remove(msgqueue[i]);
                        }

                    }
                }
            }).Start();
        }

        static void Main(string[] args) {
            IrcInit();
            ServerPing();
            ColorWriteLine("Server Ping Thread Start", 0);
            ColorWriteLine("Match Thread Start!", 0);
            IRC.RfcJoin("#mp_59077044");

            new Thread (() => {
                while (true) {
                    DoMatch ();
                    Console.Title = "Omatchbot Alpha Now :" + RoomList.Count + "   InTheQuque: " + matchinglist.Count + "     WaitMatch:"+MatchQueue.Count;
                    Thread.Sleep (1000);
                    IRC.SendMessage(SendType.Message, "#mp_59077044", "!mp close");
                }
            }).Start ();

            //IRC.RfcJoin("#lobby");
            //Isjoinchan("#lobby");
            try {
                sqlconnect.Open ();
                ColorWriteLine ("Sql Database Open Success!", 0);
                while (true) {
                    string command = Console.ReadLine ();
                    switch (command) {
                        case "CloseAllMatch":
                                foreach(var room in RoomList){
                                    IrcMessageQueuq("#mp_"+room.matchid,"!mp close");
                                    MatchQueue.Clear();
                                }
                            break;
                    }
                }
            } catch (Exception e) {
                ColorWriteLine (e.ToString (), 2);

            }
            Console.ReadLine ();
        }

        //初始化IRC
        static void IrcInit () {
            try {
                IRC.Connect (ServerIp, ServerPort);
                IRC.Login (botusername, botusername, 0, botusername, botpassword);
                IRC.Encoding = Encoding.BigEndianUnicode;
                ListenIrc ();
                IrcMessageQuquqSendThead ();
                //动作绑定和监听
                IRC.OnQueryMessage += IRC_OnQueryMessage;
                IRC.OnChannelMessage += IRC_OnChannelMessage;
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write (String.Format ("[Error {0}]", DateTime.Now));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine (e);
            }

        }
        static void DoMatch () {
            var myList = matchinglist.ToList ();
            myList.Sort ((pair1, pair2) => pair1.Value.CompareTo (pair2.Value));
            for (int j = 0; j < 2; j += 1) {
                int scoreDelta = 50 * (j + 1);
                int count = myList.Count;
                for (int i = 0; i < count - 1; i += 1) {
                    if (Math.Abs (myList[i].Value - myList[i + 1].Value) < scoreDelta) {
                        if (RoomList.Count == 4) {
                            MatchQueue.Add(myList[i].Key+"|"+myList[i + 1].Key);
                            IrcMessageQueuq(myList[i].Key,"Your match is ready but the maximum amount of rooms are open at this time. Please wait to receive an invitation to your match. Queue position: #"+(MatchQueue.Count-1).ToString());
                            IrcMessageQueuq(myList[i+1].Key,"Your match is ready but the maximum amount of rooms are open at this time. Please wait to receive an invitation to your match. Queue position: #"+(+MatchQueue.Count-1).ToString());
                            matchinglist.Remove (myList[i].Key);
                            matchinglist.Remove (myList[i + 1].Key);
                            myList.RemoveRange (i, 2);
                            i-=1;
                            count = myList.Count;
                        } else {
                            ColorWriteLine (String.Format ("DoneMatch: {0}({1}) <-> {2}({3})", myList[i].Key, myList[i].Value, myList[i + 1].Key, myList[i + 1].Value), 0);
                            CreateMatch (string.Format ("{0}|{1}", myList[i].Key, myList[i + 1].Key));
                            IrcMessageQueuq (myList[i].Key, String.Format ("DoneMatch: {0}({1}) <-> {2}({3})", myList[i].Key, myList[i].Value, myList[i + 1].Key, myList[i + 1].Value));
                            IrcMessageQueuq (myList[i + 1].Key, String.Format ("DoneMatch: {0}({1}) <-> {2}({3})", myList[i].Key, myList[i].Value, myList[i + 1].Key, myList[i + 1].Value));
                            matchinglist.Remove (myList[i].Key);
                            matchinglist.Remove (myList[i + 1].Key);
                            myList.RemoveRange (i, 2);
                            i -= 1;
                            count = myList.Count;
                        }
                    } else {
                        //ColorWriteLine (String.Format ("FailMatch: {0}({1}) <-> {2}({3})", myList[i].Key, myList[i].Value, myList[i + 1].Key, myList[i + 1].Value), 0);
                    }
                }
            }
        }
        static void ListenIrc () {
            Thread irc = new Thread (() => {
                IRC.Listen ();
            });
            irc.Start ();
            ColorWriteLine ("Irc ListenThread Create", 1);
        }
        private static void IRC_OnChannelMessage (object sender, IrcEventArgs e) {
            try {
                if (e.Data.Channel.Split ('_') [0] == "#mp") {
                    if (RoomList.Exists (s => s.matchid == e.Data.Channel.Split ('_') [1])) {
                        if (e.Data.Message == "!matchabort") {
                            IRC.SendMessage (SendType.Message, e.Data.Channel, "!mp close");
                            var a = Database.RunQuery ($"Select * from main where osuid = '{e.Data.Nick}'");
                            a.Read ();
                            Database.Exec ($"update main set osuelo = '{a.GetInt32("osuelo") -10}',matchtimes = '{a.GetInt32("matchtimes") + 1}' where osuid = '{e.Data.Nick}'");
                            a.Close ();
                        }
                        if (e.Data.Message == "Closed the match" && e.Data.Nick == "BanchoBot") {
                            RoomList.Remove (FindMatchByMatchID (Convert.ToInt32 (e.Data.Channel.Split ('_') [1])));
                            ColorWriteLine ($"Room {e.Data.Channel.Split('_')[1].ToString()} Thread Stop", 0);
                            if(MatchQueue.Count!= 0){
                                CreateMatch(MatchQueue[0]);
                                MatchQueue.Remove(MatchQueue[0]);
                            }
                        }
                        if (e.Data.Message == "!ready") {
                            FindMatchByMatchID (Convert.ToInt32 (e.Data.Channel.Split ('_') [1])).ReadyPlayer (e.Data.Nick);
                            ColorWriteLine (e.Data.Nick + " => " + e.Data.Message + " => AcceptCommand", 0);
                        }
                        if (Regex.IsMatch (e.Data.Message, @"[\[0-9A-Za-z_\-\]]+ finished playing \(Score: [0-9]+, [A-Z]+\)\.") && e.Data.Nick == "BanchoBot") {
                            FindMatchByMatchID (Convert.ToInt32 (e.Data.Channel.Split ('_') [1])).MatchScore (e.Data.Message);
                        }
                    }
                }
            } catch (Exception ex) {
                ColorWriteLine (ex.ToString (), 2);
            }
            ColorWriteLine (e.Data.Channel + " => " + e.Data.Nick + " => " + e.Data.Message, 0);

        }

        //Irc消息
        private static void IRC_OnQueryMessage (object sender, IrcEventArgs e) {
            try {
                ColorWriteLine (e.Data.Nick + " => " + e.Data.Message, 0);
                if (e.Data.Nick == "BanchoBot") {
                    var msg = e.Data.Message.Split (' ');
                    IRC.OnConnected += IRC_OnConnected;
                    var mplink = msg[msg.Length - 2].Split ('/');
                    ColorWriteLine ("MP link => " + msg[msg.Length - 2] + "  Matchid => " + mplink[mplink.Length - 1], 0);
                    RoomList.Add (new Match {
                        matchid = mplink[mplink.Length - 1],
                            Playerlist = new List<string> () { msg[msg.Length - 1].Split ('#') [1].Split ('|') [0], msg[msg.Length - 1].Split ('#') [1].Split ('|') [1] }
                    });
                    IRC.RfcJoin ("#mp_" + mplink[mplink.Length - 1]);
                    FindMatchByMatchID (Convert.ToInt32 (mplink[mplink.Length - 1])).Invite ();
                }
                switch (e.Data.Message) {
                    case "!stats":
                        if (Convert.ToInt32 (Database.RunQueryOne ($"Select count(*) as how from main where osuid = '{e.Data.Nick}'")) > 0) {
                            ColorWriteLine ("Accept Command [Stats] => " + e.Data.Nick, 0);
                            ColorWriteLine ("User Found! Return Userinfo", 0);
                            var a = Database.RunQuery ($"Select * from main where osuid = '{e.Data.Nick}'");
                            a.Read ();

                            IrcMessageQueuq (e.Data.Nick, $"Your User info [ELO {a.GetInt32("osuelo")}] [Matchtimes {a.GetInt32("matchtimes")}]");
                            a.Close ();
                        } else {
                            ColorWriteLine ("Command denied [RankStart] => Create Account =>  " + e.Data.Nick, 0);
                            ColorWriteLine ("User Not Found! Create User Info", 1);
                            string password = Create (10);
                            Database.Exec ($"Insert into main (osuid,osuelo,matchtimes,osupassword) Values ('{e.Data.Nick}','1500','0','{password}')");
                            IrcMessageQueuq (e.Data.Nick, "Your Account Is Create! This is your Password:\"" + password + "\"  This is Very important! Please take good care of it");
                        }
                        break;
                    case "!rankstart":
                        if (Convert.ToInt32 (Database.RunQueryOne ($"Select count(*) as how from main where osuid = '{e.Data.Nick}'")) > 0) {
                            var p = Database.RunQuery ($"Select * from main where osuid = '{e.Data.Nick}'");
                            p.Read ();
                            int elo = p.GetInt32 ("osuelo");
                            p.Close ();
                            if (!matchinglist.ContainsKey (e.Data.Nick) && !Inmatch (e.Data.Nick)&&!IntheMatchQueue(e.Data.Nick)) {
                                matchinglist.Add (e.Data.Nick, elo);
                                IrcMessageQueuq (e.Data.Nick, $"Join the Ranking queue! Now {matchinglist.Count} in the Queue");
                                ColorWriteLine ("Accept Command [RankStart] => " + e.Data.Nick, 0);
                            } else {
                                ColorWriteLine ("Command denied [RankStart] => " + e.Data.Nick, 0);
                                IrcMessageQueuq (e.Data.Nick, $"You already in the Queue");
                            }

                        } else {
                            ColorWriteLine ("Command denied [RankStart] => " + e.Data.Nick, 0);
                            IrcMessageQueuq (e.Data.Nick, "Please use \"stats\" Create Your account!");
                        }
                        break;
                    case "!rankabort":

                        if (matchinglist.ContainsKey (e.Data.Nick)) {
                            matchinglist.Remove (e.Data.Nick);
                            IrcMessageQueuq (e.Data.Nick, "Abort Ranking matching!");
                            ColorWriteLine ("Accept Command [RankAbort] => " + e.Data.Nick, 0);
                        }else{
                            if(IntheMatchQueue(e.Data.Nick)){
                                for(int i = 0; i< MatchQueue.Count;i++){
                                    var info = MatchQueue[i].Split('|');
                                    for(int j = 0; j<info.Length;j++){
                                        if(info[j] == e.Data.Nick){
                                            IrcMessageQueuq (info[0], "Abort Ranking matching!");
                                            IrcMessageQueuq (info[1], "Abort Ranking matching!");
                                            MatchQueue.Remove(MatchQueue[i]);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "!help":
                        new Thread (() => {
                            IrcMessageQueuq (e.Data.Nick, "OmatchBot Alpha v0.2 !rankstart - StartRankingMatch !rankabort - AbortRankingMatch stats - userinfo !nowqueue - Display your queue position if you are in the queue.");
                        }).Start ();

                        break;
                    case "!nowmatch":

                        break;
                    case "!nowqueue":
                            if(IntheMatchQueue(e.Data.Nick)){
                                for(int i = 0; i< MatchQueue.Count;i++){
                                    var info = MatchQueue[i].Split('|');
                                    for(int j = 0; j<info.Length;j++){
                                        if(info[j] == e.Data.Nick){
                                            IrcMessageQueuq(e.Data.Nick,"Your match is ready but the maximum amount of rooms are open at this time. Please wait to receive an invitation to your match. Queue position: #"+(i+1));
                                        }
                                    }
                                }
                            }
                        break;
                }
            } catch (Exception c) {
                ColorWriteLine (c.ToString (), 2);
            }

        }

        private static void IRC_OnConnected(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        //颜色字体
        public static void ColorWriteLine (string String, int Level) {
            switch (Level) {
                case 0:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write (String.Format ("[Info {0}]", DateTime.Now));
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine (String);
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write (String.Format ("[Warn {0}]", DateTime.Now));
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine (String);
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write (String.Format ("[Error {0}]", DateTime.Now));
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine (String);
                    break;
                default:
                    break;
            }
        }
        public static bool Isjoinchan (string name) {
            bool isconnect = false;
            Thread join = new Thread (() => {
                int how = 0;
                while (true) {
                    if (IRC.JoinedChannels.Contains (name)) {
                        ColorWriteLine ("Joined => " + name, 0);
                        isconnect = true;
                        break;
                    } else {
                        if (how == 20) {
                            isconnect = false;
                            break;
                        }
                        ColorWriteLine ("WaitJoin => " + name, 1);
                        Thread.Sleep (3000);
                        how++;
                    }
                }
            });
            join.Start ();
            return isconnect;

        }
        static void CreateMatch (string playerconfig) {
            IrcMessageQueuq ("BanchoBot", $"!mp make OmatchBot#{playerconfig}");
            ColorWriteLine ("Match Room Create Command Send!", 0);
        }
        static void ServerPing () {
            new Thread (() => {
                while (true) {
                    if (!IRC.IsConnected) {
                        ColorWriteLine ("Server Disconnect! Reconnect....", 0);
                        IRC.OnQueryMessage -= IRC_OnQueryMessage;
                        IRC.OnChannelMessage -= IRC_OnChannelMessage;
                        IrcInit ();
                    }
                }
            }).Start ();
        }
        internal static Match FindMatchByMatchID (int id) {
            foreach (var matcha in RoomList) {
                if (matcha.matchid == id.ToString ())
                    return matcha;
            }
            return null;
        }
        //生成密码
        private static int createNum () {
            Random random = new Random (Guid.NewGuid ().GetHashCode ());
            int num = random.Next (10);
            return num;
        }

        /// <summary>
        /// 生成单个大写随机字母
        /// </summary>
        private static string createBigAbc () {
            //A-Z的 ASCII值为65-90
            Random random = new Random (Guid.NewGuid ().GetHashCode ());
            int num = random.Next (65, 91);
            string abc = Convert.ToChar (num).ToString ();
            return abc;
        }

        /// <summary>
        /// 生成单个小写随机字母
        /// </summary>
        private static string createSmallAbc () {
            //a-z的 ASCII值为97-122
            Random random = new Random (Guid.NewGuid ().GetHashCode ());
            int num = random.Next (97, 123);
            string abc = Convert.ToChar (num).ToString ();
            return abc;
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">字符串的长度</param>
        /// <returns></returns>
        public static string Create (int length) {
            // 创建一个StringBuilder对象存储密码
            StringBuilder sb = new StringBuilder ();
            //使用for循环把单个字符填充进StringBuilder对象里面变成14位密码字符串
            for (int i = 0; i < length; i++) {
                Random random = new Random (Guid.NewGuid ().GetHashCode ());
                //随机选择里面其中的一种字符生成
                switch (random.Next (3)) {
                    case 0:
                        //调用生成生成随机数字的方法
                        sb.Append (createNum ());
                        break;
                    case 1:
                        //调用生成生成随机小写字母的方法
                        sb.Append (createSmallAbc ());
                        break;
                    case 2:
                        //调用生成生成随机大写字母的方法
                        sb.Append (createBigAbc ());
                        break;
                }
            }
            return sb.ToString ();
        }
    }
}