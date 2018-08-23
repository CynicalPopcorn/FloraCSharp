﻿using Discord;
using Discord.Commands;
using FloraCSharp.Modules.Games.Common;
using FloraCSharp.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FloraCSharp.Extensions;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;
using FloraCSharp.Services.Database.Models;

namespace FloraCSharp.Modules.Games
{
    public class Games : ModuleBase
    {
        private readonly FloraRandom _random;
        private FloraDebugLogger _logger;
        private readonly BotGameHandler _botGames;
        private Services.RNGService _rngservice = new Services.RNGService();
        private WoodcuttingLocker _woodcuttingLocker;

        public Games(FloraRandom random, FloraDebugLogger logger, BotGameHandler botGames, WoodcuttingLocker woodcuttingLocker)
        {
            _random = random;
            _logger = logger;
            _botGames = botGames;

            _woodcuttingLocker = woodcuttingLocker;
        }

        private readonly Dictionary<string, int> TreeID = new Dictionary<string, int>()
        {
            { "normal", 0 },
            { "achey", 1 },
            { "oak", 2 },
            { "willow", 3 },
            { "teak", 4 },
            { "maple", 5 },
            { "hollow", 6 },
            { "mahogany", 7 },
            { "arctic_pine", 8},
            { "yew", 9},
            { "sulliuscep", 10},
            { "magic", 11 },
            { "redwood", 12 }
        };

        private enum Axes { iron, steel, black, mithril, adamant, rune, dragon };

        private readonly Dictionary<int, int> TreeLevel = new Dictionary<int, int>
        {
            { 0, 1 },
            { 1, 1 },
            { 2, 15 },
            { 3, 30 },
            { 4, 35 },
            { 5, 45 },
            { 6, 45 },
            { 7, 50 },
            { 8, 54 },
            { 9, 60 },
            { 10, 65 },
            { 11, 75 },
            { 12, 90 }
        };

        private readonly Dictionary<int, double> TreeXP = new Dictionary<int, double>
        {
            { 0, 25 },
            { 1, 25 },
            { 2, 37.5 },
            { 3, 67.5 },
            { 4, 85 },
            { 5, 100 },
            { 6, 82.5 },
            { 7, 125 },
            { 8, 40 },
            { 9, 175 },
            { 10, 127 },
            { 11, 250 },
            { 12, 380 }
        };

        private readonly Dictionary<int, string> AxeLevel = new Dictionary<int, string>
        {
            { 61, "dragon" },
            { 41, "rune" },
            { 31, "adamant" },
            { 21, "mithril" },
            { 11, "black" },
            { 6, "steel" },
            { 1, "iron" }
        };

        private readonly Dictionary<int, double> AxeTiming = new Dictionary<int, double>
        {
            { 0, 16 },
            { 1, 14 },
            { 2, 12 },
            { 3, 10 },
            { 4, 8 },
            { 5, 6 },
            { 6, 4 }
        };

        private readonly Dictionary<int, int> LogValues = new Dictionary<int, int>
        {
            { 0, 2 },
            { 1, 2 },
            { 2, 5 },
            { 3, 10 },
            { 4, 15 },
            { 5, 25 },
            { 6, 25 },
            { 7, 35 },
            { 8, 40 },
            { 9, 50 },
            { 10, 65 },
            { 11, 75 },
            { 12, 100 },
        };

        [Command("RNGGame"), Summary("Starts an RNG Game between given bounds. Optionally specifying a timeout in seconds")]
        [RequireContext(ContextType.Guild)]
        public async Task RNGGame([Summary("The minimum, inclusive bound")] int min, [Summary("The maximum, exclusive bound")] int max, int timeout = 30)
        {
            if (timeout > 300)
                timeout = 0;

            if (timeout > 0)
            {
                timeout = timeout * 1000;
            }
            else
            {
                timeout = 30000;
            }

            RNGGame Game = new RNGGame
            {
                Channel = Context.Channel.Id,
                MinGuess = min,
                MaxGuess = max,
                Guesses = new HashSet<Guess>()
            };

            if (_rngservice.StartRNGG(Game, (DiscordSocketClient)Context.Client))
            {
                await Context.Channel.SendSuccessAsync($"RNG Game (Min: {min}, Max: {max})", $"Game started! Type your guesses now! You have {timeout/1000} seconds.");
                await Task.Delay(timeout);
                await _rngservice.EndGameInChannel(Context.Guild, Context.Channel, _random);
            }
        }

        [Command("WoodcuttingLevel"), Alias("WCLevel", "WC")]
        public async Task WoodcuttingLevel()
        {
            //Okay let's get the User
            Woodcutting wc;

            using (var uow = DBHandler.UnitOfWork())
            {
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            //New list of stuff
            Dictionary<string, int> CutLogs = new Dictionary<string, int>()
            {
                { "Achey", wc.AcheyTrees },
                { "Arctic Pine", wc.ArcticTrees },
                { "Hollow", wc.HollowTrees },
                { "Magic", wc.MagicTrees },
                { "Mahogany", wc.MahoganyTrees },
                { "Maple", wc.MapleTrees },
                { "Normal", wc.NormalTrees },
                { "Oak", wc.OakTrees },
                { "Redwood", wc.RedwoodTrees },
                { "Sulliuscep", wc.SullTrees },
                { "Teak", wc.TeakTrees },
                { "Yew", wc.YewTrees }
            };

            //List sorted
            List<KeyValuePair<string, int>> logList = CutLogs.ToList();
            logList = logList.OrderByDescending(x => x.Value).ToList();

            //What's their axe level
            string axetype = "";
            foreach (int key in AxeLevel.Keys)
            {
                if (axetype != "")
                    break;
                else if (wc.Level >= key)
                    axetype = AxeLevel[key];
            }

            EmbedBuilder emb = new EmbedBuilder().WithTitle($"Wooductting XP | User: {Context.User.Username}").WithOkColour().WithDescription($"Level: {wc.Level} | Total XP: {wc.XP} | Axe Type: {axetype}");

            EmbedFieldBuilder embF = new EmbedFieldBuilder().WithName("Amount of Logs Chopped");

            string str = "";

            foreach(KeyValuePair<string, int> Tree in logList)
            {
                str += $"{Tree.Key} trees: {Tree.Value}\n";
            }

            str += $"\nTotal Logs: {CutLogs.Sum(x => x.Value)}";

            embF = embF.WithValue(str);

            //Add to emb 
            Embed embdone = emb.AddField(embF);

            //Send
            await Context.Channel.BlankEmbedAsync(embdone);
        }

        [Command("TreeList")]
        public async Task TreeList()
        {
            //List of trees
            Dictionary<string, int> TreeToID = new Dictionary<string, int>()
            {
                { "Normal", 1 },
                { "Achey", 1 },
                { "Oak", 15 },
                { "Willow", 30 },
                { "Teak", 35 },
                { "Maple", 45 },
                { "Hollow", 45 },
                { "Mahogany", 50 },
                { "Arctic Pine", 54 },
                { "Yew", 60 },
                { "Sulliuscep", 65 },
                { "Magic", 75 },
                { "Redwood", 90 }
            };

            //Okay let's get the User
            Woodcutting wc;

            using (var uow = DBHandler.UnitOfWork())
            {
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            //wowoowowwowo
            EmbedBuilder emb = new EmbedBuilder().WithTitle("Woodcutting | Trees");

            //New field
            EmbedFieldBuilder embF = new EmbedFieldBuilder().WithName("You can chop these");
            string str = "";

            //Lets get the ones we CAN cut
            foreach (KeyValuePair<string, int> Tree in TreeToID)
            {
                if (wc.Level >= Tree.Value)
                {
                    //Aa?
                    string logName = Tree.Key.ToLower().Replace(' ', '_');
                    double XPperLog = TreeXP[TreeID[logName]];

                    //Aaa
                    str += $"{Tree.Key} | Level: {Tree.Value} | XP Per Log: {XPperLog}\n";
                } 
            }

            //Add to field
            embF = embF.WithValue(str);

            //Neewwww
            EmbedFieldBuilder embF2 = new EmbedFieldBuilder().WithName("You cannot chop these");
            string str2 = "";

            //Stuff you can't do
            //Lets get the ones we CAN cut
            foreach (KeyValuePair<string, int> Tree in TreeToID)
            {
                if (wc.Level < Tree.Value)
                {
                    //Aa?
                    string logName = Tree.Key.ToLower().Replace(' ', '_');
                    double XPperLog = TreeXP[TreeID[logName]];

                    //Aaa
                    str2 += $"{Tree.Key} | Level: {Tree.Value} | XP Per Log: {XPperLog}\n";
                }
            }

            //Add to field
            embF2 = embF2.WithValue(str2);

            //Add fields
            Embed embDone = emb.AddField(embF).AddField(embF2).WithOkColour();

            //Respond
            await Context.Channel.BlankEmbedAsync(embDone);
        }

        [Command("ClaimWC")]
        [RequireContext(ContextType.Guild)]
        public async Task ClaimMC()
        {
            var user = await Context.Guild.GetUserAsync(Context.User.Id);

            //Okay let's get the User
            Woodcutting wc;

            using (var uow = DBHandler.UnitOfWork())
            {
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            //Lets award deserved shit!
            if (wc.Level >= 99 && !user.RoleIds.ToList().Contains(480823262260101120))
            {
                IRole role = Context.Guild.GetRole(480823262260101120);
                await user.AddRoleAsync(role);
            }

            if (wc.Level == 120 && !user.RoleIds.ToList().Contains(480823388730949632))
            {
                IRole role = Context.Guild.GetRole(480823388730949632);
                await user.AddRoleAsync(role);
            }

            if (wc.XP == 200000000 && !user.RoleIds.ToList().Contains(480823441105223700))
            {
                IRole role = Context.Guild.GetRole(480823441105223700);
                await user.AddRoleAsync(role);
            }
        }

        [Command("SellLog")]
        public async Task SellLog(int count, [Summary("The tree type"), Remainder] string tree)
        {
            //first get their log count
            //Get tree ID
            string treeString = tree.ToLower().Replace(' ', '_');
            int tID;

            if (!TreeID.TryGetValue(treeString, out tID))
            {
                await Context.Channel.SendErrorAsync("Woodcutting", "Tree does not exist");
                return;
            }
            _logger.Log("Woodcutting Selling", $"Tree ID: {tID}");

            //From treeID we can get their log count
            Woodcutting wc;

            using (var uow = DBHandler.UnitOfWork())
            {
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            int specificLogCount = 0;
            switch (tID)
            {
                case 0:
                    specificLogCount = wc.NormalTrees;
                    break;
                case 1:
                    specificLogCount = wc.AcheyTrees;
                    break;
                case 2:
                    specificLogCount = wc.OakTrees;
                    break;
                case 3:
                    specificLogCount = wc.WillowTrees;
                    break;
                case 4:
                    specificLogCount = wc.TeakTrees;
                    break;
                case 5:
                    specificLogCount = wc.MapleTrees;
                    break;
                case 6:
                    specificLogCount = wc.HollowTrees;
                    break;
                case 7:
                    specificLogCount = wc.MahoganyTrees;
                    break;
                case 8:
                    specificLogCount = wc.ArcticTrees;
                    break;
                case 9:
                    specificLogCount = wc.YewTrees;
                    break;
                case 10:
                    specificLogCount = wc.SullTrees;
                    break;
                case 11:
                    specificLogCount = wc.MagicTrees;
                    break;
                case 12:
                    specificLogCount = wc.RedwoodTrees;
                    break;
            }

            if (specificLogCount < count)
            {
                await Context.Channel.SendErrorAsync("Woodcutting", $"You do not have enough {tree} logs.");
                return;
            }

            //We know they have enough. Get value.
            int logValue = LogValues[tID];

            //How much do we need to give them
            int totalVal = logValue * count;

            //Okay so a few things we need to do
            using (var uow = DBHandler.UnitOfWork())
            {
                //First we remove their logs
                uow.Woodcutting.AddTree(Context.User.Id, tID, -count);

                //Then we give them the gold
                uow.Woodcutting.AddGold(Context.User.Id, totalVal);

                //Then we done
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            //Now we need to tell them we're done
            await Context.Channel.SendSuccessAsync("Woodcutting", $"{Context.User.Username}, you have just sold {count} {tree} logs for {totalVal} gold ({logValue} per log).\nYou now have a total of {wc.Gold} gold.");
        }

        [Command("GoldCheck")]
        public async Task GoldCheck()
        {
            //From treeID we can get their log count
            Woodcutting wc;

            using (var uow = DBHandler.UnitOfWork())
            {
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            await Context.Channel.SendSuccessAsync("RPG Thing", $"You have {wc.Gold} gold.");
        }

        [Command("Chop"), Summary("Chops 29 of a specified tree type with your best equipped axe.")]
        public async Task Chop([Summary("The tree type"), Remainder] string tree) => await Chop(28, tree);

        [Command("Chop"), Summary("Chops 29 of a specified tree type with your best equipped axe.")]
        public async Task Chop(int chopcount, [Summary("The tree type"), Remainder] string tree)
        {
            if (_woodcuttingLocker.GetOrCreateUserCooldown(Context.User.Id) == 1)
            {
                await Context.Channel.SendErrorAsync("Woodcutting", $"{Context.User.Username}, you are already chopping trees cutie, be patient...");
                return;
            }

            if (chopcount < 1) chopcount = 1;
            if (chopcount > 28) chopcount = 28;

            //Get tree ID
            string treeString = tree.ToLower().Replace(' ', '_');
            int tID;
            
            if (!TreeID.TryGetValue(treeString, out tID))
            {
                await Context.Channel.SendErrorAsync("Woodcutting", "Tree does not exist");
                return;
            }
            _logger.Log("Woodcutting", $"Tree ID: {tID}");

            //Okay let's get the User
            Woodcutting wc;

            using (var uow = DBHandler.UnitOfWork())
            {
                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            //Check if they can even chop this tree
            int treeLevel = TreeLevel[tID];

            if (wc.Level < treeLevel)
            {
                await Context.Channel.SendErrorAsync("Woodcutting", $"You need {treeLevel} woodcutting to chop down {tree} trees.");
                return;
            }

            //Cool they can now
            //What's their axe level
            string axetype = "";
            foreach (int key in AxeLevel.Keys)
            {
                if (axetype != "")
                    break;
                else if (wc.Level >= key)
                    axetype = AxeLevel[key];
            }

            _logger.Log("Woodcutting", $"Axe Type: {axetype}");

            //Soemthing gone wrong?
            if (axetype == "")
            {
                await Context.Channel.SendErrorAsync("Big error oh no.");
                return;
            }

            //Okay we know their axetype, get axeID
            int aID = -1;
            switch (axetype)
            {
                case "iron":
                    aID = (int) Axes.iron;
                    break;
                case "steel":
                    aID = (int)Axes.steel;
                    break;
                case "black":
                    aID = (int)Axes.black;
                    break;
                case "mithril":
                    aID = (int)Axes.mithril;
                    break;
                case "adamant":
                    aID = (int)Axes.adamant;
                    break;
                case "rune":
                    aID = (int)Axes.rune;
                    break;
                case "dragon":
                    aID = (int)Axes.dragon;
                    break;
            }

            //We have the tree and axeID
            //Now we have to get the needed wait, and tree XP
            double tXP = TreeXP[tID] * chopcount;
            _logger.Log("Woodcutting", $"XP: {tXP}");
            double tWait = AxeTiming[aID] * chopcount;
            _logger.Log("Woodcutting", $"Wait: {tWait}s");

            //Add them to the list
            _woodcuttingLocker.SetWoodcuttingCooldowns(Context.User.Id, 1);

            //Okay lets begin
            //First we w a i t
            await Context.Channel.SendSuccessAsync($"Woodcutting | Log Count: {chopcount}", $"You swing your {axetype} axe at the {tree} tree(s), {Context.User.Username}.\n This will take: {tWait} seconds.");
            await Task.Delay((int) (tWait * 1000));

            bool levelUpFlag = false;

            //Add xp, add tree type
            using (var uow = DBHandler.UnitOfWork())
            {
                //So lets check the next level.
                int nextLevel = wc.Level + 1;

                bool maxLevel = false;

                if (nextLevel > 200000000)
                {
                    maxLevel = true;
                    nextLevel = 200000000;
                }

                //Next xp
                long nextXP = CalculateNextLevelEXP(nextLevel);

                double newXP = wc.XP + tXP;

                //Do the mathsy shit
                uow.Woodcutting.AddXP(wc.UserID, tXP);

                bool levelUp = true;

                while (levelUp && !maxLevel)
                {
                    //Is this a l e v e l u p ? 
                    if (newXP < nextXP) levelUp = false;

                    if (levelUp)
                    {
                        levelUpFlag = true;
                        nextLevel += 1;
                        nextXP = CalculateNextLevelEXP(nextLevel);
                        uow.Woodcutting.AddLevel(wc.UserID);
                    }  
                }

                _logger.Log("Woodcutting", $"New Level: {nextLevel - 1}");

                //Work 
                uow.Woodcutting.AddTree(wc.UserID, tID, chopcount);

                wc = uow.Woodcutting.GetOrCreateWoodcutting(Context.User.Id);
            }

            int nLevel = wc.Level + 1;
            long nXP = CalculateNextLevelEXP(nLevel);

            //F iiiinally
            _woodcuttingLocker.SetWoodcuttingCooldowns(Context.User.Id, 0);
            if (levelUpFlag) await Context.Channel.SendMessageAsync($"{Context.User.Mention} has levelled up to {wc.Level} woodcutting!");
            await Context.Channel.SendSuccessAsync($"Woodcutting | Log Count: {chopcount}", $"After {tWait} seconds you chop down {chopcount} {tree} tree(s), {Context.User.Username}.\n Level: {wc.Level} | XP Gained: {tXP}\nTotal XP: {wc.XP} | Next Level: {nXP} | Remaining XP: {nXP - wc.XP}");
        }

        [Command("WoodcuttingLeaderboard"), Summary("Get the top 9 (or later with pagination)")]
        [Alias("wclb")]
        public async Task WoodcuttingLeaderboard(int page = 0)
        {
            if (page != 0)
                page -= 1;

            List<Woodcutting> TopWC;
            using (var uow = DBHandler.UnitOfWork())
            {
                TopWC = uow.Woodcutting.GetTop(page);
            }

            if (!TopWC.Any())
            {
                await Context.Channel.SendErrorAsync($"No users found for page {page + 1}");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithQuoteColour().WithTitle("Woodcutting Leaderboard").WithFooter(efb => efb.WithText($"Page: {page + 1}"));

            foreach (Woodcutting c in TopWC)
            {
                IGuildUser user = await Context.Guild.GetUserAsync(c.UserID);
                string userName = user?.Username ?? c.UserID.ToString();
                EmbedFieldBuilder efb = new EmbedFieldBuilder().WithName(userName).WithValue($"XP: {c.XP} | Level: {c.Level}").WithIsInline(true);

                embed.AddField(efb);
            }

            await Context.Channel.BlankEmbedAsync(embed);
        }

        private static long CalculateNextLevelEXP(int nextLevel)
        {
            if (nextLevel == 1) return 0;

            double sum = 0;
            for (double i = 1; i < nextLevel; i++)
            {
                double power = Math.Pow(2, (double)i / 7);
                sum += Math.Floor(i + (300 * power));
            }

            return (long)Math.Floor(sum * 0.25);
        }
    }
}
