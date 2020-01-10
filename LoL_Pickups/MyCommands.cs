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

namespace LoL_Pickups
{
    public class MyCommands
    {
        [Command("lg")]
        [Aliases("lastgame")]
        public async Task lastgame(CommandContext ctx, string summonername)
        {
            await ctx.TriggerTypingAsync();
            var riotApi = RiotApi.NewInstance("RGAPI-c0bf4888-0b86-42e9-a5c0-335bc4388701");
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

    }
}
