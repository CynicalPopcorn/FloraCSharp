﻿using Discord.Commands;
using Discord;
using FloraCSharp.Services;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using FloraCSharp.Extensions;
using Newtonsoft.Json;
using Discord.WebSocket;
using System.Globalization;
using FloraCSharp.Services.Database.Models;
using System.Net;
using FloraCSharp.Services.APIModels;

namespace FloraCSharp.Modules
{
    public class Administration : ModuleBase
    {
        private readonly FloraRandom _random;
        private FloraDebugLogger _logger;
        private readonly DiscordSocketClient _client;
        private readonly BotGameHandler _botGames;
        private readonly Configuration _config;

        public Administration(FloraRandom random, FloraDebugLogger logger, DiscordSocketClient client, Configuration config, BotGameHandler botGames)
        {
            _random = random;
            _logger = logger;
            _client = client;
            _botGames = botGames;
            _config = config;
        }

        [Command("save"), Summary("Saves a given user's role")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Save([Summary("Discord User to Save")] IGuildUser user, bool verbose = true)
        {
            //User and Server ID
            ulong uID = user.Id;
            ulong sID = Context.Guild.Id;

            //Server directory path
            string directory = @"data/roles/" + sID;

            //Create the directory for the server if it does not exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            //Path to storage
            string filePath = directory + @"/" + uID + ".json";

            //User's roles
            List<ulong> roles = new List<ulong>(user.RoleIds);

            //String roles
            List<string> rolesString = new List<string>(roles.Count);

            //If they've only got @everyone and newbies
            if (roles.Count == 2 && roles.Contains((ulong)229064523053531137))
            {
                await Context.Channel.SendErrorAsync("I don't think you need to do that.");
                return;
            }

            //So it can be ported over easily
            foreach (ulong rID in roles)
            {
                rolesString.Add($"{rID}");
            }

            //Serialize JSON
            string json = JsonConvert.SerializeObject(rolesString);

            //Write json to file (overwriting)
            File.WriteAllText(filePath, json);

            //Now tell the user we did it! Yay
            if (verbose)
                await Context.Channel.SendSuccessAsync("Saved roles for " + user.Mention);
        }

        [Command("Restore"), Summary("Restores a user's roles")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Restore([Summary("Discord User to Restore")] IGuildUser user)
        {
            var sID = Context.Guild.Id;
            var uID = user.Id;

            _logger.Log(uID.ToString(), "Restore");

            //Server directory path
            string directory = @"data/roles/" + sID;

            //Path to storage
            string filePath = directory + @"/" + uID + ".json";

            //Woah hold up there, they've not had roles saved
            if (!File.Exists(filePath)) return;

            _logger.Log(filePath, "Restore");

            //Get the roles
            List<string> SavedRoles = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath));

            //Collection of roles
            List<IRole> roles = new List<IRole>();

            //Sort out the roles now
            foreach (string id in SavedRoles)
            {
                IRole role = Context.Guild.GetRole(ulong.Parse(id));
                if (!(role == Context.Guild.EveryoneRole))
                    roles.Add(role);
            }

            //Add the roles they deserve
            await user.AddRolesAsync(roles);

            //Now tell the user we did it! Yay
            await Context.Channel.SendSuccessAsync("Restored roles for " + user.Mention);
        }

        [Command("SaveAll"), Summary("Saves all user's roles")]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveAll()
        {
            List<IGuildUser> users = new List<IGuildUser>(await Context.Guild.GetUsersAsync());

            foreach (IGuildUser user in users)
            {
                await Save(user, false);
            }

            //Now tell the user we did it! Yay
            await Context.Channel.SendSuccessAsync("Saved all users");
        }

        [Command("Shutdown"), Summary("Kills the bot")]
        [Alias("die")]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Shutdown()
        {
            //Now tell the user we did it! Yay
            await Context.Channel.SendSuccessAsync("Bye-bye!");

            //Set config shutdown to 1
            _config.Shutdown = true;

            //Safely stop the bot
            await Context.Client.StopAsync();

            //Close the client
            Environment.Exit(1);
        }

        [Command("RoleID"), Summary("Gets the ID of a role")]
        [Alias("rlid")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RoleID([Remainder] string RoleName)
        {
            IRole RoleFromName = null;
            foreach (IRole role in Context.Guild.Roles)
            {
                if (role.Name.ToLower() == RoleName.ToLower())
                {
                    RoleFromName = role;
                    break;
                }
            }

            if (RoleFromName == null)
                await Context.Channel.SendErrorAsync("That is not a valid role name.");
            else
                await Context.Channel.SendSuccessAsync($"RoleID ({RoleFromName.Name})", $"{RoleFromName.Id}");
        }

        [Command("SetGame"), Summary("Sets the game the bot is currently playing")]
        [Alias("sgm")]
        [OwnerOnly]
        public async Task SetGame([Remainder] string gameName)
        {
            await _client.SetGameAsync(gameName);
        }

        [Command("SetStream"), Summary("Sets the stream the bot is currently streaming..?")]
        [Alias("sst")]
        [OwnerOnly]
        public async Task SetStream(string stream, [Remainder] string gameName)
        {
            await _client.SetGameAsync(gameName, stream, StreamType.Twitch);
        }

        [Command("AddRotatingGame"), Summary("Adds a game to the list of the rotating games")]
        [Alias("argm")]
        [OwnerOnly]
        public async Task AddRotatingGame([Remainder] string gameName)
        {
            int botGameID = await _botGames.AddGame(gameName);
            await Context.Channel.SendSuccessAsync($"Added Rotating Game #{botGameID}", gameName);
        }

        [Command("DeleteRotatingGame"), Summary("Removes a game from the list of the rotating games")]
        [Alias("drgm")]
        [OwnerOnly]
        public async Task DeleteRotatingGame([Remainder] string gameName)
        {
            await _botGames.RemoveBotGame(gameName);
            await Context.Channel.SendSuccessAsync($"Removed {gameName}.");
        }

        [Command("DeleteRotatingGame"), Summary("Removes a game from the list of the rotating games")]
        [Alias("drgm")]
        [OwnerOnly]
        public async Task DeleteRotatingGame(int id)
        {
            await _botGames.RemoveBotGameByID(id);
            await Context.Channel.SendSuccessAsync($"Removed rotating game #{id}.");
        }

        [Command("AddUserBirthday"), Summary("Adds a user's birthday.")]
        [Alias("AddBday", "Birthday", "AddDOB")]
        [OwnerOnly]
        public async Task AddUserBirthday(IGuildUser user, string birthday, int age)
        {
            DateTime dt;
            if (!DateTime.TryParseExact(birthday, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                await Context.Channel.SendErrorAsync("That is not a valid date.");
                return;
            }

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Birthdays.Add(new Birthday
                {
                    UserID = user.Id,
                    Date = dt,
                    Age = age
                });
                await uow.CompleteAsync();
            }

            await Context.Channel.SendSuccessAsync($"Added Birthday for {user.Username}.");
        }

        [Command("DeleteUserBirthday"), Summary("Adds a user's birthday.")]
        [Alias("DeleteBDay", "DeleteDOB")]
        [OwnerOnly]
        public async Task DeleteUserBirthday(IGuildUser user)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Birthdays.DeleteUserBirthday(user.Id);
                await uow.CompleteAsync();
            }

            await Context.Channel.SendSuccessAsync($"Deleted Birthday for {user.Username}.");
        }

        [Command("TestBirthdays"), Summary("Tests the birthdays")]
        [OwnerOnly]
        public async Task TestBirthdays()
        {
            List<Birthday> todaysBirthdays = GetBirthdays();

            if (todaysBirthdays != null)
            {
                foreach (Birthday birthday in todaysBirthdays)
                {
                    IUser user = _client.GetUser(birthday.UserID);

                    await Context.Channel.SendSuccessAsync($"Testing Birthdays. {user.Username} is {birthday.Age + 1}.");

                    using (var uow = DBHandler.UnitOfWork())
                    {
                        birthday.Age += 1;
                        uow.Birthdays.Update(birthday);
                        await uow.CompleteAsync();
                    }
                }
            }
        }

        private List<Birthday> GetBirthdays()
        {
            var curDate = DateTime.Now;
            List<Birthday> userBirthdays;

            using (var uow = DBHandler.UnitOfWork())
            {
                userBirthdays = uow.Birthdays.GetAllBirthdays(curDate);
            }

            return userBirthdays;
        }

        [Command("ResetSteamID")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetSteamID(IGuildUser user)
        {
            using (var uow = DBHandler.UnitOfWork())
            {
                if (uow.User.GetSteamID(user.Id) != 0)
                    uow.User.SetSteamID(user.Id, 0);
                else
                    return;
            }

            await Context.Channel.SendSuccessAsync($"Reset the SteamID for {user.NicknameUsername()}");
        }

        [Command("FloraDMDelete"), Alias("FDMD")]
        [RequireContext(ContextType.DM)]
        [OwnerOnly]
        public async Task FloraDMDelete(ulong messageID)
        {
            //Get previous messages
            var Message = await Context.Channel.GetMessageAsync(messageID);

            //Delete message if the owner is flora
            if (Message.Author.Id == _client.CurrentUser.Id)
            {
                await Message.DeleteAsync();
            }
        }

        [Command("FloraDMClear"), Alias("FDMC")]
        [RequireContext(ContextType.DM)]
        [OwnerOnly]
        public async Task FloraDMClear(int count = 1)
        {
            //Get previous messages
            var Messages = await Context.Channel.GetMessagesAsync().Flatten();

            //Filter to flora only
            var Filtered = Messages.Where(x => x.Author.Id == _client.CurrentUser.Id).Take(count);

            //Now we prune
            await Context.Channel.DeleteMessagesAsync(Filtered);
        }

        [Command("RandomSong"), Alias("RS")]
        [RequireContext(ContextType.DM)]
        [OwnerOnly]
        public async Task RandomSong(string person)
        {
            //Trim
            person = person.Trim();

            //Get song list
            var responseArray = getSongList(person);

            if (responseArray == null) return;

            //Got the shit
            var song = responseArray.RandomItem();

            //Get embed from song
            var embed = GenerateSongEmbed(song);

            //Output embed
            await Context.Channel.BlankEmbedAsync(embed);
        }

        [Command("RandomSong"), Alias("RS")]
        [RequireContext(ContextType.DM)]
        [OwnerOnly]
        public async Task RandomSong(string person, int difficulty)
        {
            //Trim
            person = person.Trim();

            //Get song list
            var responseArray = getSongList(person);

            if (responseArray == null) return;

            CloneHeroSongListModel song = null;

            try
            {
                //Got the shit
                song = responseArray.Where(x => x.GuitarDifficulty == difficulty).RandomItem();
            } catch (Exception) { await Context.Channel.SendErrorAsync("No song found."); return; }

            //Get embed from song
            var embed = GenerateSongEmbed(song);

            //Output embed
            await Context.Channel.BlankEmbedAsync(embed);
        }

        private EmbedBuilder GenerateSongEmbed(CloneHeroSongListModel song)
        {
            //Song is selected
            var embed = new EmbedBuilder().WithQuoteColour();

            //If artist one or other
            if (song.ArtistName != "")
            {
                embed = embed.WithTitle($"{song.ArtistName} - {song.SongName}");
            }
            else
            {
                embed = embed.WithTitle($"{song.SongName}");
            }

            //Add fields
            embed = embed.AddField(efb => efb.WithName("Album").WithValue(song.AlbumName).WithIsInline(true));
            embed = embed.AddField(efb => efb.WithName("Genre").WithValue(song.GenreName).WithIsInline(true));
            embed = embed.AddField(efb => efb.WithName("Charter").WithValue(song.CharterName).WithIsInline(true));
            embed = embed.AddField(efb => efb.WithName("Year").WithValue(song.Year).WithIsInline(true));
            embed = embed.AddField(efb => efb.WithName("Guitar Difficulty").WithValue(song.GuitarDifficulty).WithIsInline(true));
            embed = embed.AddField(efb => efb.WithName("Song Length").WithValue(song.SongLength).WithIsInline(true));

            return embed;
        }

        private List<CloneHeroSongListModel> getSongList(string person)
        {
            //Get json
            var json = "";

            using (WebClient wc = new WebClient())
            {
                json = wc.DownloadString(new Uri("https://cynicalpopcorn.me/" + person + ".json"));
            }

            if (json == "") return null;

            //Use json
            return JsonConvert.DeserializeObject<List<CloneHeroSongListModel>>(json);
        }
    }
}
