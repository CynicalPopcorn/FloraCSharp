﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloraCSharp.Services
{
    public class RequireSuperSenpai : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            IGuildUser user = (IGuildUser)context.User;

            if (user.RoleIds.Contains((ulong)392049363440107520))
                return PreconditionResult.FromSuccess();
            else
                return PreconditionResult.FromError("You must be a trap to run this command.");
        }
    }
}
