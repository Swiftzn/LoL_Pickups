using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using System.IO;

namespace LoL_Pickups
{
    public class MyCommands
    {
        [Command("lg")]
        [Aliases("lastgame")]
        public async Task lastgame(CommandContext ctx, string summonername)
        {
            await ctx.Message.DeleteAsync();
            await ctx.TriggerTypingAsync();
            var riotApi = RiotApi.NewInstance("--RIOT_API-TOKEN--");
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
                await ctx.RespondAsync($"{ctx.Member.DisplayName}, your last game was with {champ.Name()} and your KDA was {k}/{d}/{a} ({kda:0.00}) with {cs} CS");
            }
        }

        [Command("register")]
        public async Task Register(CommandContext ctx)
        {
            await ctx.Message.DeleteAsync();
            var interactivity = ctx.Client.GetInteractivityModule();
            var dmchannel = await ctx.Client.CreateDmAsync(ctx.Member);
            await dmchannel.SendMessageAsync($"What is your Summoner Name (lolapi token : {Cfg.Lolapi})");
            var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.Member.Id & xm.Channel == dmchannel, TimeSpan.FromMinutes(1));
            if (msg == null)
            {
                await dmchannel.SendMessageAsync("You took too long to reply please use !register command again");
            }
            else
            {

                var riotApi = RiotApi.NewInstance("--RIOT_API-TOKEN--");
                var summonerData = await riotApi.SummonerV4.GetBySummonerNameAsync(Region.EUW, msg.Message.Content);
                var token = GetToken();
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Please verify your account.",
                    Color = DiscordColor.Red
                };
                embed.AddField("Summoner Name", $"{summonerData.Name}");
                embed.AddField("Verification Code", $"{token}", true);
                var msgid = await dmchannel.SendMessageAsync(embed: embed);
                var emoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
                await msgid.CreateReactionAsync(emoji);
                var em = await interactivity.WaitForReactionAsync(xe => xe == emoji, ctx.User, TimeSpan.FromMinutes(5));
                if ( em != null )
                {
                    var activationCode = await riotApi.ThirdPartyCodeV4.GetThirdPartyCodeBySummonerIdAsync(Region.EUW, summonerData.Id);
                    if (activationCode == token)
                    {
                        await dmchannel.SendMessageAsync($"Activation successfull. {activationCode} / {token}");

                    }
                    else
                    {
                        await dmchannel.SendMessageAsync($"Activation failed. {activationCode} / {token}");
                    }
                }
                else
                {
                    await dmchannel.SendMessageAsync($"Summoner not found. {em}");
                }
            }
        }
        public string GetToken()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", ""); // Remove period.
            return path;
        }
    }
}
