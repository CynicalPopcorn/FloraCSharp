﻿using FloraCSharp.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloraCSharp.Services.Database.Repos.Impl
{
    public class GuildRepository : Repository<Guild>, IGuildRepository
    {
        public GuildRepository(DbContext context) : base(context)
        {
        }

        public Guild GetOrCreateGuild(ulong GuildID, ulong DelChannelID = 0, bool enabled = false)
        {
            Guild toReturn;
            toReturn = _set.FirstOrDefault(x => x.GuildID == GuildID);

            if (toReturn == null)
            {
                _set.Add(toReturn = new Guild()
                {
                    GuildID = GuildID,
                    DeleteLogChannel = DelChannelID,
                    DeleteLogEnabled = enabled
                });
                _context.SaveChanges();
            }

            return toReturn;
        }

        public bool IsDeleteLoggingEnabled(ulong GuildID)
        {
            Guild G = GetOrCreateGuild(GuildID);
            return G.DeleteLogEnabled;
        }

        public void SetGuildDelChannel(ulong GuildID, ulong ChannelID)
        {
            Guild G = GetOrCreateGuild(GuildID);
            G.DeleteLogChannel = ChannelID;

            _set.Update(G);
            _context.SaveChanges();
        }

        public void SetGuildDelLogEnabled(ulong GuildID, bool isEnabled)
        {
            Guild G = GetOrCreateGuild(GuildID);
            G.DeleteLogEnabled = isEnabled;

            _set.Update(G);
            _context.SaveChanges();
        }
    }
}
