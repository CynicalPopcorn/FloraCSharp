﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloraCSharp.Extensions
{
    public static class FloraExtensions
    {
        private static Random _random = new Random();

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithErrorColour().WithDescription(text).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static T RandomItem<T>(this IEnumerable<T> list)
        {
            return list.ElementAt(_random.Next(0, list.Count()));
        }

        public static string NicknameUsername(this IGuildUser user)
        {
            return user?.Nickname ?? user.Username;
        }

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string text)
            => ch.SendMessageAsync("", embed: new EmbedBuilder().WithErrorColour().WithDescription(text).Build());

        public static Task<IUserMessage> SendSuccessAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColour().WithDescription(text).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendPictureAsync(this IMessageChannel ch, string title, string text, string pic, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColour().WithDescription(text).WithTitle(title);
            if (pic != null && Uri.IsWellFormedUriString(pic, UriKind.Absolute))
                eb.WithImageUrl(pic);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendPictureAsync(this IMessageChannel ch, string pic)
        {
            var eb = new EmbedBuilder().WithOkColour();
            if (pic != null && Uri.IsWellFormedUriString(pic, UriKind.Absolute))
                eb.WithImageUrl(pic);
            return ch.SendMessageAsync("", embed: eb.Build());
        }

        public static Task<IUserMessage> SendSuccessAsync(this IMessageChannel ch, string text)
            => ch.SendMessageAsync("", embed: new EmbedBuilder().WithOkColour().WithDescription(text).Build());

        public static EmbedBuilder WithOkColour(this EmbedBuilder eb)
            => eb.WithColor(3800852);

        public static EmbedBuilder WithErrorColour(this EmbedBuilder eb)
            => eb.WithColor(16711731);

        public static EmbedBuilder WithQuoteColour(this EmbedBuilder eb)
            => eb.WithColor(16758465);

        public static EmbedBuilder WithDnDColour(this EmbedBuilder eb)
            => eb.WithColor(2003199);

        public static IMessage DeleteAfter(this IUserMessage msg, int seconds)
        {
            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000);
                try { await msg.DeleteAsync().ConfigureAwait(false); }
                catch { }
            });
            return msg;
        }

        public static Task<IUserMessage> BlankEmbedAsync(this IMessageChannel ch, Embed embed)
            => ch.SendMessageAsync("", false, embed);

        public static string FirstCharToLower(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToLower() + input.Substring(1);
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(this long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
