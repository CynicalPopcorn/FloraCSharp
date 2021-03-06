﻿using Discord;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FloraCSharp.Extensions;
using System.Threading.Tasks;

namespace FloraCSharp.Services
{
    class EliteColours
    {
        Dictionary<int, ulong> EliteRoleIds = new Dictionary<int, ulong>()
        {
            //True Weebs
            { 1, 330094882670772234 }, //Pink
            { 2, 330093327347220490 }, //Red
            { 3, 330092923171635230 }, //Blue 
            { 4, 330095123264438272 }, //Orange
            { 5, 330096661940666368 }, //Purple
            { 6, 354727337230729216 }, //Yellow
            { 7, 364191035250704394 }, //Teal

            //Senpais
            { 8, 464837386682105857 }, //Pastel Green
            { 9, 464838092453314601 }, //Pastel Purple
            { 10, 464837905097949187 }, //Pastel blue

            //Traps
            { 11, 464842610478022656 }, //Discord Light
            { 12, 464842828036440064 }, //Discord Dark
            { 13, 464844707386884097 }, //Gold
            { 14, 464846041729204224 }, //Emerald
            { 15, 464846484236664833 } //Royal Blue
        };

        public async Task GiveEliteColour(IGuildUser Sender, IMessageChannel Channel, int Colour)
        {
            foreach (ulong RoleID in Sender.RoleIds)
            {
                IRole role = Sender.Guild.GetRole(RoleID);

                if (EliteRoleIds.ContainsValue(role.Id))
                {
                    await Sender.RemoveRoleAsync(role);
                }
            }

            if (Colour != 0)
            {
                //Get role from ID which we grab from the Int to ID Dictionary
                IRole role = Sender.Guild.GetRole(EliteRoleIds[Colour]);

                await Sender.AddRoleAsync(role);
            }

            await Channel.SendSuccessAsync("Success!");
        }
    }
}
