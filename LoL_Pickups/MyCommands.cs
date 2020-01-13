using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;

namespace LoL_Pickups
{
    public class MyCommands
    {
        [Command("lg")]
        [Aliases("lastgame")]
        public async Task lastgame(CommandContext ctx, string summonername)
        {
            await ctx.TriggerTypingAsync();
            var riotApi = RiotApi.NewInstance("Riot API Token");
            var summonerData = await riotApi.SummonerV4.GetBySummonerNameAsync(Region.EUW, summonername);
            var matchlist = await riotApi.MatchV4.GetMatchlistAsync(Region.EUW, summonerData.AccountId, endIndex: 1);
            var matchDataTasks = matchlist.Matches.Select(matchMetadata => riotApi.MatchV4.GetMatchAsync(Region.EUW, matchMetadata.GameId)).ToArray();
            var matchDatas = await Task.WhenAll(matchDataTasks);

            for (var i = 0; i < matchDatas.Count(); i++)
            {
                var matchData = matchDatas[i];
                var participantIdData = matchData.ParticipantIdentities.First(pi => summonerData.Id.Equals(pi.Player.SummonerId));
                var participant = matchData.Participants.First(p => p.ParticipantId == participantIdData.ParticipantId);

                var win = participant.Stats.Win;
                var champ = (Champion)participant.ChampionId;
                var k = participant.Stats.Kills;
                var d = participant.Stats.Deaths;
                var a = participant.Stats.Assists;
                var cs = (participant.Stats.NeutralMinionsKilled + participant.Stats.TotalMinionsKilled);
                var kda = (k + a) / (float)d;
                await ctx.RespondAsync($"Your last game was with {champ.Name()} and your KDA was {k}/{d}/{a} ({kda:0.00}) with {cs} CS");
            }
        }

        [Command("register")]
        public async Task Register(CommandContext ctx)
        {
            await ctx.Message.DeleteAsync();
            var interactivity = ctx.Client.GetInteractivityModule();
            await ctx.Member.SendMessageAsync("What is your Summoner Name");
            var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.Member.Id, TimeSpan.FromMinutes(1));
            var riotApi = RiotApi.NewInstance("Riot API Token");
            var summonerData = await riotApi.SummonerV4.GetBySummonerNameAsync(Region.EUW, msg.Message.Content);
            var embed = new DiscordEmbedBuilder
            {
                Title = "Summoner Info",
                Color = DiscordColor.Red
            };
            embed.AddField("Summoner Name", $"{summonerData.Name}");
            embed.AddField("Summoner Level", $"{summonerData.SummonerLevel}");
            await ctx.Member.SendMessageAsync(embed: embed);
        }
    }
}
