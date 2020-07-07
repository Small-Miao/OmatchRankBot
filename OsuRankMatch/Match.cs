using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RegistrationWebsite.Helper;

namespace OsuRankMatch {
    class Match {
        internal enum stats {
            ready,
            start
        }
        internal stats Now = stats.ready;
        public string matchid;
        internal int ReadyNum = 0;
        internal int Rank;
        internal int ScoreRe = 0;
        string elochange ;
        Thread close;
        internal List<string> scorestr = new List<string> ();
        internal List<String> Playerlist = new List<string> ();
        internal List<String> ReadyList = new List<string> ();
        float elo1elo2;

        internal void InitMatch () {
            try {
                close =  new Thread(() =>
                {
                    WaitToClose();
                });
                close.Start();
                var a = Database.RunQuery ($"Select * from main where osuid = '{Playerlist[0]}'");
                a.Read ();
                
                int elo1 = a.GetInt32 ("osuelo");
                var b = Database.RunQuery ($"Select * from main where osuid = '{Playerlist[1]}'");
                b.Read ();
                int elo2 = a.GetInt32 ("osuelo");
                int arg = (elo1 + elo2) / 2;
                int qian = (arg % 10000) / 1000;
                Rank = qian;
                if (Rank > 4) {
                    Rank = 4;
                }
                elo1elo2 = elo1/elo2;
                var q = Database.RunQuery ($"Select * from map where rank = '{Rank}'");
                q.Read ();
                string mapstr = q.GetString ("maplist");
                q.Close();
                Program.ColorWriteLine ($"Loading MapList Success! MapList {mapstr}", 0);
                var m = mapstr.Split ('|');
                Random random = new Random ();
                new Thread(()=>{
                    Program. IrcMessageQueuq ( "#mp_" + matchid, $"!mp map {m[random.Next(0,m.Length-1)].Trim()}");
                    Program. IrcMessageQueuq  ( "#mp_" + matchid, $"!mp set 2 3 2");
                    Program. IrcMessageQueuq ( "#mp_" + matchid, $"!mp mods freemod");
                    Program.IrcMessageQueuq("#mp_"+matchid,"!mp lock");
                }).Start();              
                new Thread (() => {
                    Thread.Sleep (10000);
                    help ();
                    Thread.Sleep (10000);
                    help ();
                    Thread.Sleep (10000);
                    help ();
                }).Start ();
                a.Close();
                b.Close();
            } catch (Exception e) {
                Program.ColorWriteLine (e.ToString (), 2);
            }

        }
        internal void help () {
            Program. IrcMessageQueuq  ("#mp_" + matchid, $"Use !ready to Ready to fight Use !matchabort to abort match");
        }
        internal void Invite () {
            while (true) {
                if (Program.IRC.JoinedChannels.Contains ("#mp_" + matchid)) {
                    new Thread(()=>{
                        foreach (var play in Playerlist) {
                           Program. IrcMessageQueuq  ( "#mp_" + matchid, $"!mp invite {play}");
                        Thread.Sleep(3000);
                        Program.ColorWriteLine ($"Send Invite to => {play}", 0);
                    }
                    }).Start();
                    
                    break;
                }
            }
            InitMatch ();
        }
        internal void MatchScore (string Message) {
            if (ScoreRe != 2) {
                ScoreRe++;
                Message = Message.Replace (" finished playing (Score:", ",").Replace (").", " ");
                var Messageinfo = Message.Split (',');
                // CalculationElo (Convert.ToInt32 (Messageinfo[1]), Messageinfo[2] == "PASSED" ? true : false, Messageinfo[0].Replace(" ","_"), ScoreRe == 1);
                scorestr.Add (Messageinfo[1] + "|" + Messageinfo[0].Replace (" ", "_") + "|" + Messageinfo[2]);
            }
            if (ScoreRe == 2) {
                Finish ();
            }

        }
        internal void Finish()
        {
            int score0 = Convert.ToInt32(scorestr[0].Split('|')[0].Trim());
            int score1 = Convert.ToInt32(scorestr[1].Split('|')[0].Trim());

            if (score0 > score1)
            {
                CalculationElo(score0, scorestr[0].Split('|')[2].Trim() == "PASSED", scorestr[0].Split('|')[1], true);
                CalculationElo(score0, scorestr[1].Split('|')[2].Trim() == "PASSED", scorestr[1].Split('|')[1], false);
            }
            else if (score0 < score1)
            {
                CalculationElo(score0, scorestr[1].Split('|')[2].Trim() == "PASSED", scorestr[1].Split('|')[1], true);
                CalculationElo(score0, scorestr[0].Split('|')[2].Trim() == "PASSED", scorestr[0].Split('|')[1], false);
            }
            else
            {
                CalculationElo(score0, scorestr[1].Split('|')[2].Trim() == "PASSED", scorestr[1].Split('|')[1], true);
                CalculationElo(score0, scorestr[0].Split('|')[2].Trim() == "PASSED", scorestr[0].Split('|')[1], true);

            }
            Database.Exec($"Insert into historymatch (MatchTimes,Player,Score) Values ('{DateTime.Now}','{Playerlist[0] + " VS " + Playerlist[1]}','{score0 + " : " + score1}')");
            new Thread(() =>
            {
                Thread.Sleep(15000);
                Program.IrcMessageQueuq("#mp_" + matchid, $"!mp close");
            }
            ).Start();
        }
            
        internal void ClosedMatch(){
             new Thread(() =>
            {
                Program.IrcMessageQueuq("#mp_" + matchid, $"!mp close");
            }
            ).Start();
        }
        internal void Start () {
            try {
                Now = stats.start;
                //等待双方准备
                Program.IrcMessageQueuq ( "#mp_" + matchid, $"!mp start 15");
                close.Abort();
            } catch (Exception e) {
                Program.ColorWriteLine (e.ToString (), 2);
            }

        }
        internal void ReadyPlayer (string PlayerName) {
            if (!ReadyList.Contains (PlayerName)) {
                ReadyNum++;

                    Program.IrcMessageQueuq( "#mp_" + matchid, PlayerName + " ready!");


                Program.ColorWriteLine (PlayerName + " ReadySuccess!", 0);
                ReadyList.Add (PlayerName);
            } else {

                    Program.IrcMessageQueuq( "#mp_" + matchid, PlayerName + " You're ready!");
               
                Program.ColorWriteLine (PlayerName + " denied!", 0);
            }
            if (ReadyNum == 2 && Now != stats.start) {
                Start ();
            }
        }
        internal void CalculationElo (int score, bool passed, string player, bool iswin) {
            int r = score / 100000;
            if (iswin) {
                    r = (int) Math.Ceiling (r * (1.0+Math.Round(elo1elo2,1) )) + 5;   
            } else {
                    r = ((int)(Math.Ceiling((10 - r)*(1.0+Math.Round(elo1elo2,1))+ 2)*-1));
            }
            var a = Database.RunQuery ($"Select * from main where osuid = '{player}'");
            a.Read ();
            int nowelo = a.GetInt32("osuelo");
            Database.Exec ($"update main set osuelo = '{a.GetInt32("osuelo")+r}',matchtimes = '{a.GetInt32("matchtimes")+1}' where osuid = '{player}'");
            Program.ColorWriteLine ($"Update Info => {player}", 0);
            Program.IrcMessageQueuq(player, $"ELO {nowelo}{(iswin?"+":"")}{r}");
            Program.ColorWriteLine ($"Player {player} Elo {(iswin?"+":"") +r}", 0);
            a.Close();
            
        }
        internal void WaitToClose () {
                Thread.Sleep(180000);
                ClosedMatch();
        }
    }
}