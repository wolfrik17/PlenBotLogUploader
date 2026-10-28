using Hardstuck.Http;
using Newtonsoft.Json;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.DiscordApi;
using PlenBotLogUploader.DpsReport;
using PlenBotLogUploader.DpsReport.ExtraJson;
using PlenBotLogUploader.Teams;
using PlenBotLogUploader.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TextTableFormatter;

namespace PlenBotLogUploader.Services;

internal static class DiscordWebhookService
{
    private static readonly TableVisibleBorders TableBorders = TableVisibleBorders.HEADER_ONLY;
    private static readonly CellStyle TableCellCenterAlign = new(CellHorizontalAlignment.Center);
    private static readonly CellStyle TableCellRightAlign = new(CellHorizontalAlignment.Right);
    private static readonly TableBordersStyle TableStyle = TableBordersStyle.HORIZONTAL_ONLY;

    internal static async Task ExecuteAllActiveDiscordWebhooksAsync(ILogUploaderController controller, DpsReportJson reportJson, List<LogPlayer> players)
    {
        var allWebhooks = DiscordWebhooks.All;
        if (allWebhooks.Count == 0)
        {
            return;
        }

        if (reportJson.Encounter.BossId == 1) // WvW
        {
            await ExecuteWvWWebhooksAsync(controller, allWebhooks, reportJson, players);
        }
        else // PvE / Normal Bosses
        {
            await ExecutePvEWebhooksAsync(controller, allWebhooks, reportJson, players);
        }

        controller?.AddToText(">:> All active webhooks executed.");
    }

    private static async Task ExecuteWvWWebhooksAsync(ILogUploaderController controller, Dictionary<int, DiscordWebhookData> allWebhooks, DpsReportJson reportJson, List<LogPlayer> players)
    {
        var extraJsonFightName = reportJson.ExtraJson is null ? reportJson.Encounter.Boss : reportJson.ExtraJson.FightName;
        var extraJson = reportJson.ExtraJson is null ? "" : $"Recorded by: {reportJson.ExtraJson.RecordedByAccountName}\nDuration: {reportJson.ExtraJson.Duration}\nElite Insights version: {reportJson.ExtraJson.EliteInsightsVersion}";
        var icon = "";
        var bossData = Bosses.GetBossDataFromId(1);
        if (bossData is not null)
        {
            icon = bossData.Icon;
        }
        const int colour = 16752238;
        var discordContentEmbedThumbnail = new DiscordApiJsonContentEmbedThumbnail
        {
            Url = icon,
        };
        var timestampDateTime = reportJson.ExtraJson?.TimeStart ?? DateTime.UtcNow;
        var timestamp = timestampDateTime.ToString("o");
        var discordContentEmbed = new DiscordApiJsonContentEmbed
        {
            Title = extraJsonFightName,
            Url = reportJson.ConfigAwarePermalink,
            Description = $"{extraJson}\narcdps version: {reportJson.Evtc.Type}{reportJson.Evtc.Version}",
            Colour = colour,
            TimeStamp = timestamp,
            Thumbnail = discordContentEmbedThumbnail,
        };
        var discordContentEmbedSpacer = new DiscordApiJsonContentEmbed
        {
            Title = "Log summary",
            Description = DiscordApiJsonContent.Spacer,
            Colour = colour,
            TimeStamp = timestamp,
        };

        var discordContentEmbedSquadAndPlayers = new List<DiscordApiJsonContentEmbedField>();
        var discordContentEmbedSquad = new List<DiscordApiJsonContentEmbedField>();
        var discordContentEmbedPlayers = new List<DiscordApiJsonContentEmbedField>();
        var discordContentEmbedNone = new List<DiscordApiJsonContentEmbedField>();

        if (reportJson.ExtraJson is not null)
        {
            var squadPlayers = reportJson.ExtraJson.Players.Count(x => !x.FriendlyNpc && !x.NotInSquad);
            var squadDamage = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad)
                .Select(x => x.DpsTargets.Sum(y => y.Sum(z => z.Damage)))
                .Sum();
            var squadDps = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad)
                .Select(x => x.DpsTargets.Sum(y => y.Sum(z => z.Dps)))
                .Sum();
            var squadDowns = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad)
                .Select(x => x.Defenses[0].DownCount)
                .Sum();
            var squadDeaths = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad)
                .Select(x => x.Defenses[0].DeadCount)
                .Sum();

            var squadSummary = new TextTable(5, TableStyle, TableBorders);
            squadSummary.SetColumnWidthRange(0, 4, 4);
            squadSummary.SetColumnWidthRange(1, 13, 13);
            squadSummary.SetColumnWidthRange(2, 11, 11);
            squadSummary.SetColumnWidthRange(3, 8, 8);
            squadSummary.SetColumnWidthRange(4, 8, 8);
            squadSummary.AddCell("#", TableCellCenterAlign);
            squadSummary.AddCell("DMG", TableCellCenterAlign);
            squadSummary.AddCell("DPS", TableCellCenterAlign);
            squadSummary.AddCell("Downs", TableCellCenterAlign);
            squadSummary.AddCell("Deaths", TableCellCenterAlign);
            squadSummary.AddCell($"{squadPlayers}", TableCellCenterAlign);
            squadSummary.AddCell($"{squadDamage.ParseAsK()}", TableCellCenterAlign);
            squadSummary.AddCell($"{squadDps.ParseAsK()}", TableCellCenterAlign);
            squadSummary.AddCell($"{squadDowns}", TableCellCenterAlign);
            squadSummary.AddCell($"{squadDeaths}", TableCellCenterAlign);

            var squadField = new DiscordApiJsonContentEmbedField
            {
                Name = "Squad summary:",
                Value = $"```{squadSummary.Render()}```",
            };

            var enemyField = new DiscordApiJsonContentEmbedField
            {
                Name = "Enemy summary:",
                Value = "```Summary could not have been generated.\nToggle detailed WvW to enable this feature.```",
            };

            if (reportJson.ExtraJson.Targets.Length > 1)
            {
                var enemyPlayers = reportJson.ExtraJson.Targets.Length - 1;
                var enemyDamage = reportJson.ExtraJson.Targets.Where(x => !x.IsFake).Select(x => x.DpsAll[0].Damage).Sum();
                var enemyDps = reportJson.ExtraJson.Targets.Where(x => !x.IsFake).Select(x => x.DpsAll[0].Dps).Sum();
                var enemyDowns = reportJson.ExtraJson.Players.Where(x => !x.FriendlyNpc && !x.NotInSquad).Select(x => x.StatsTargets.Select(y => y[0].Downed).Sum()).Sum();
                var enemyDeaths = reportJson.ExtraJson.Players.Where(x => !x.FriendlyNpc && !x.NotInSquad).Select(x => x.StatsTargets.Select(y => y[0].Killed).Sum()).Sum();

                var enemySummary = new TextTable(5, TableStyle, TableBorders);
                enemySummary.SetColumnWidthRange(0, 4, 4);
                enemySummary.SetColumnWidthRange(1, 13, 13);
                enemySummary.SetColumnWidthRange(2, 11, 11);
                enemySummary.SetColumnWidthRange(3, 8, 8);
                enemySummary.SetColumnWidthRange(4, 8, 8);
                enemySummary.AddCell("#", TableCellCenterAlign);
                enemySummary.AddCell("DMG", TableCellCenterAlign);
                enemySummary.AddCell("DPS", TableCellCenterAlign);
                enemySummary.AddCell("Downs", TableCellCenterAlign);
                enemySummary.AddCell("Deaths", TableCellCenterAlign);
                enemySummary.AddCell($"{enemyPlayers}", TableCellCenterAlign);
                enemySummary.AddCell($"{enemyDamage.ParseAsK()}", TableCellCenterAlign);
                enemySummary.AddCell($"{enemyDps.ParseAsK()}", TableCellCenterAlign);
                enemySummary.AddCell($"{enemyDowns}", TableCellCenterAlign);
                enemySummary.AddCell($"{enemyDeaths}", TableCellCenterAlign);

                enemyField = new DiscordApiJsonContentEmbedField
                {
                    Name = "Enemy summary:",
                    Value = $"```{enemySummary.Render()}```",
                };
            }

            var damageStats = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad && x.DpsTargets.Sum(y => y[0].Damage) > 0)
                .OrderByDescending(x => x.DpsTargets.Sum(y => y[0].Damage))
                .Take(10)
                .ToArray();
            var damageSummary = new TextTable(4, TableStyle, TableBorders);
            damageSummary.SetColumnWidthRange(0, 3, 3);
            damageSummary.SetColumnWidthRange(1, 22, 22);
            damageSummary.SetColumnWidthRange(2, 11, 11);
            damageSummary.SetColumnWidthRange(3, 9, 9);
            damageSummary.AddCell("#", TableCellCenterAlign);
            damageSummary.AddCell("Name");
            damageSummary.AddCell("DMG", TableCellRightAlign);
            damageSummary.AddCell("DPS", TableCellRightAlign);
            var rank = 0;
            foreach (var player in damageStats)
            {
                rank++;
                damageSummary.AddCell($"{rank}", TableCellCenterAlign);
                damageSummary.AddCell($"{player.Name} ({player.ProfessionShort})");
                damageSummary.AddCell($"{player.DpsTargets.Sum(y => y[0].Damage).ParseAsK()}", TableCellRightAlign);
                damageSummary.AddCell($"{player.DpsTargets.Sum(y => y[0].Dps).ParseAsK()}", TableCellRightAlign);
            }
            var damageField = new DiscordApiJsonContentEmbedField
            {
                Name = "Damage summary:",
                Value = $"```{damageSummary.Render()}```",
            };

            var cleansesStats = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad && x.Support[0].CondiCleanseTotal > 0)
                .OrderByDescending(x => x.Support[0].CondiCleanseTotal)
                .Take(10)
                .ToArray();
            var cleansesSummary = new TextTable(3, TableStyle, TableBorders);
            cleansesSummary.SetColumnWidthRange(0, 3, 3);
            cleansesSummary.SetColumnWidthRange(1, 29, 29);
            cleansesSummary.SetColumnWidthRange(2, 14, 14);
            cleansesSummary.AddCell("#", TableCellCenterAlign);
            cleansesSummary.AddCell("Name");
            cleansesSummary.AddCell("Cleanses", TableCellRightAlign);
            rank = 0;
            foreach (var player in cleansesStats)
            {
                rank++;
                cleansesSummary.AddCell($"{rank}", TableCellCenterAlign);
                cleansesSummary.AddCell($"{player.Name} ({player.ProfessionShort})");
                cleansesSummary.AddCell($"{player.Support[0].CondiCleanseTotal}", TableCellRightAlign);
            }
            var cleansesField = new DiscordApiJsonContentEmbedField
            {
                Name = "Cleanses summary:",
                Value = $"```{cleansesSummary.Render()}```",
            };

            var boonStripsStats = reportJson.ExtraJson.Players
                .Where(x => !x.FriendlyNpc && !x.NotInSquad && x.Support[0].BoonStrips > 0)
                .OrderByDescending(x => x.Support[0].BoonStrips)
                .Take(10)
                .ToArray();
            var boonStripsSummary = new TextTable(3, TableStyle, TableBorders);
            boonStripsSummary.SetColumnWidthRange(0, 3, 3);
            boonStripsSummary.SetColumnWidthRange(1, 29, 29);
            boonStripsSummary.SetColumnWidthRange(2, 14, 14);
            boonStripsSummary.AddCell("#", TableCellCenterAlign);
            boonStripsSummary.AddCell("Name");
            boonStripsSummary.AddCell("Strips", TableCellRightAlign);
            rank = 0;
            foreach (var player in boonStripsStats)
            {
                rank++;
                boonStripsSummary.AddCell($"{rank}", TableCellCenterAlign);
                boonStripsSummary.AddCell($"{player.Name} ({player.ProfessionShort})");
                boonStripsSummary.AddCell($"{player.Support[0].BoonStrips}", TableCellRightAlign);
            }
            var boonStripsField = new DiscordApiJsonContentEmbedField
            {
                Name = "Boon strips summary:",
                Value = $"```{boonStripsSummary.Render()}```",
            };

            discordContentEmbedSquadAndPlayers.AddRange(squadField, enemyField, damageField, cleansesField, boonStripsField);
            discordContentEmbedSquad.AddRange(squadField, enemyField);
            discordContentEmbedPlayers.AddRange(damageField, cleansesField, boonStripsField);
        }

        var discordContentWvW = new DiscordApiJsonContent
        {
            Embeds = [discordContentEmbed],
        };
        discordContentWvW.Embeds[0].Fields = discordContentEmbedNone;
        var jsonContentWvWNone = JsonConvert.SerializeObject(discordContentWvW);

        discordContentWvW.Embeds.Add(discordContentEmbedSpacer);
        discordContentWvW.Embeds[1].Fields = discordContentEmbedSquadAndPlayers;
        var jsonContentWvWSquadAndPlayers = JsonConvert.SerializeObject(discordContentWvW);
        discordContentWvW.Embeds[1].Fields = discordContentEmbedSquad;
        var jsonContentWvWSquad = JsonConvert.SerializeObject(discordContentWvW);
        discordContentWvW.Embeds[1].Fields = discordContentEmbedPlayers;
        var jsonContentWvWPlayers = JsonConvert.SerializeObject(discordContentWvW);

        await SendLogViaWebhooksAsync(controller, allWebhooks, reportJson.Encounter.Success ?? false,
            reportJson.Encounter.BossId, false, false, bossData, players,
            jsonContentWvWNone, jsonContentWvWSquad, jsonContentWvWPlayers, jsonContentWvWSquadAndPlayers);
    }

    private static async Task ExecutePvEWebhooksAsync(ILogUploaderController controller, Dictionary<int, DiscordWebhookData> allWebhooks, DpsReportJson reportJson, List<LogPlayer> players)
    {
        var bossName = $"{reportJson.Encounter.Boss}{(reportJson.ChallengeMode ? " CM" : "")}";
        var successString = reportJson.Encounter.Success ?? false ? ":white_check_mark:" : "❌";
        var lastTarget = reportJson.ExtraJson is null ? "" : $"\n{reportJson.ExtraJson.GetLastPhaseName()} ({reportJson.ExtraJson.GetLastPhaseTargets()})";
        var extraJson = reportJson.ExtraJson is null ? "" : $"Recorded by: {reportJson.ExtraJson.RecordedByAccountName}\nDuration: {reportJson.ExtraJson.Duration}{lastTarget}\nElite Insights version: {reportJson.ExtraJson.EliteInsightsVersion}\n";
        var icon = "";
        var bossData = Bosses.GetBossDataFromId(reportJson.Encounter.BossId);
        if (bossData is not null)
        {
            bossName = bossData.FightName(reportJson);
            icon = bossData.Icon;
        }
        var colour = reportJson.Encounter.Success ?? false ? 32768 : 16711680;
        var discordContentEmbedThumbnail = new DiscordApiJsonContentEmbedThumbnail
        {
            Url = icon,
        };
        var timestampDateTime = reportJson.ExtraJson?.TimeStart ?? DateTime.UtcNow;
        var timestamp = timestampDateTime.ToString("o");
        var discordContentEmbed = new DiscordApiJsonContentEmbed
        {
            Title = bossName,
            Url = reportJson.ConfigAwarePermalink,
            Description = $"{extraJson}Result: {successString}\narcdps version: {reportJson.Evtc.Type}{reportJson.Evtc.Version}",
            Colour = colour,
            TimeStamp = timestamp,
            Thumbnail = discordContentEmbedThumbnail,
        };
        var discordContentEmbedSpacer = new DiscordApiJsonContentEmbed
        {
            Title = "Log summary",
            Description = DiscordApiJsonContent.Spacer,
            Colour = colour,
            TimeStamp = timestamp,
        };

        var discordContentEmbedSquadAndPlayers = new List<DiscordApiJsonContentEmbedField>();
        var discordContentEmbedSquad = new List<DiscordApiJsonContentEmbedField>();
        var discordContentEmbedPlayers = new List<DiscordApiJsonContentEmbedField>();
        var discordContentEmbedNone = new List<DiscordApiJsonContentEmbedField>();

        if (reportJson.Players.Values.Count <= 10)
        {
            if (reportJson.ExtraJson is null)
            {
                var playerNames = new TextTable(2, TableStyle, TableBorders);
                playerNames.SetColumnWidthRange(0, 21, 21);
                playerNames.SetColumnWidthRange(1, 20, 20);
                playerNames.AddCell("Character");
                playerNames.AddCell("Account name");
                foreach (var player in reportJson.Players.Values)
                {
                    playerNames.AddCell($"{player.CharacterName}");
                    playerNames.AddCell($"{player.DisplayName}");
                }
                var squadEmbedField = new DiscordApiJsonContentEmbedField
                {
                    Name = "Players in squad/group:",
                    Value = $"```{playerNames.Render()}```",
                };
                discordContentEmbedSquadAndPlayers.Add(squadEmbedField);
                discordContentEmbedSquad.Add(squadEmbedField);
            }
            else
            {
                var playerNames = new TextTable(2, TableStyle, TableBorders);
                playerNames.SetColumnWidthRange(0, 23, 23);
                playerNames.SetColumnWidthRange(1, 23, 23);
                playerNames.AddCell("Character");
                playerNames.AddCell("Account name");
                foreach (var player in reportJson.ExtraJson.Players.Where(x => !x.FriendlyNpc).OrderBy(x => x.Name))
                {
                    playerNames.AddCell($"{player.Name}");
                    playerNames.AddCell($"{player.Account}");
                }
                var squadEmbedField = new DiscordApiJsonContentEmbedField
                {
                    Name = "Players in squad/group:",
                    Value = $"```{playerNames.Render()}```",
                };
                discordContentEmbedSquadAndPlayers.Add(squadEmbedField);
                discordContentEmbedSquad.Add(squadEmbedField);

                var numberOfRealTargets = reportJson.ExtraJson.Targets.Count(x => !x.IsFake);
                var targetDps = reportJson.ExtraJson.GetPlayerTargetDps();
                var damageStats = reportJson.ExtraJson.Players
                    .Where(x => !x.FriendlyNpc)
                    .Select(x => new
                    {
                        Player = x,
                        DPS = numberOfRealTargets > 0 ? targetDps[x] : x.DpsAll[0].Dps,
                    })
                    .OrderByDescending(x => x.DPS)
                    .Take(10)
                    .ToArray();

                var dpsTargetSummary = new TextTable(3, TableStyle, TableVisibleBorders.HEADER_AND_FOOTER);
                dpsTargetSummary.SetColumnWidthRange(0, 5, 5);
                dpsTargetSummary.SetColumnWidthRange(1, 31, 31);
                dpsTargetSummary.SetColumnWidthRange(2, 10, 10);
                dpsTargetSummary.AddCell("#", TableCellCenterAlign);
                dpsTargetSummary.AddCell("Name");
                dpsTargetSummary.AddCell("DPS", TableCellRightAlign);
                var rank = 0;
                foreach (var player in damageStats)
                {
                    rank++;
                    dpsTargetSummary.AddCell($"{rank}", TableCellCenterAlign);
                    dpsTargetSummary.AddCell($"{player.Player.Name} ({player.Player.ProfessionShort})");
                    dpsTargetSummary.AddCell($"{player.DPS.ParseAsK()}", TableCellRightAlign);
                }
                dpsTargetSummary.AddCell("");
                dpsTargetSummary.AddCell("Total");
                var totalDps = damageStats.Select(x => x.DPS).Sum();
                dpsTargetSummary.AddCell($"{totalDps.ParseAsK()}", TableCellRightAlign);

                var playersEmbedField = new DiscordApiJsonContentEmbedField
                {
                    Name = "DPS target summary:",
                    Value = $"```{dpsTargetSummary.Render()}```",
                };
                discordContentEmbedSquadAndPlayers.Add(playersEmbedField);
                discordContentEmbedPlayers.Add(playersEmbedField);
            }
        }

        var discordContent = new DiscordApiJsonContent
        {
            Embeds = [discordContentEmbed],
        };
        discordContent.Embeds[0].Fields = discordContentEmbedNone;
        var jsonContentNone = JsonConvert.SerializeObject(discordContent);

        discordContent.Embeds.Add(discordContentEmbedSpacer);
        discordContent.Embeds[1].Fields = discordContentEmbedSquadAndPlayers;
        var jsonContentSquadAndPlayers = JsonConvert.SerializeObject(discordContent);
        discordContent.Embeds[1].Fields = discordContentEmbedSquad;
        var jsonContentSquad = JsonConvert.SerializeObject(discordContent);
        discordContent.Embeds[1].Fields = discordContentEmbedPlayers;
        var jsonContentPlayers = JsonConvert.SerializeObject(discordContent);

        await SendLogViaWebhooksAsync(controller, allWebhooks, reportJson.Encounter.Success ?? false,
            reportJson.Encounter.BossId, reportJson.ChallengeMode, reportJson.LegendaryChallengeMode,
            bossData, players, jsonContentNone, jsonContentSquad, jsonContentPlayers, jsonContentSquadAndPlayers);
    }

    private static async Task SendLogViaWebhooksAsync(ILogUploaderController controller, Dictionary<int, DiscordWebhookData> allWebhooks,
        bool success, int bossId, bool isCm, bool isLegendaryCm, BossData bossData, List<LogPlayer> players,
        string jsonContentNone, string jsonContentSquad, string jsonContentPlayers, string jsonContentSquadAndPlayers)
    {
        foreach (var webhook in allWebhooks.Values)
        {
            if (!webhook.Active
                || (webhook.SuccessFailToggle == DiscordWebhookDataSuccessToggle.OnSuccessOnly && !success)
                || (webhook.SuccessFailToggle == DiscordWebhookDataSuccessToggle.OnFailOnly && success)
                || (!webhook.IncludeNormalLogs && !isCm)
                || (!webhook.IncludeChallengeModeLogs && isCm && !isLegendaryCm)
                || (!webhook.IncludeLegendaryChallengeModeLogs && isLegendaryCm)
                || webhook.BossesDisable.Contains(bossId)
                || (!webhook.AllowUnknownBossIds && bossData is null)
                || !(webhook.Team?.IsSatisfied(players) ?? true))
            {
                continue;
            }

            try
            {
                var jsonToSend = webhook.SummaryType switch
                {
                    DiscordWebhookDataLogSummaryType.None => jsonContentNone,
                    DiscordWebhookDataLogSummaryType.SquadOnly => jsonContentSquad,
                    DiscordWebhookDataLogSummaryType.PlayersOnly => jsonContentPlayers,
                    _ => jsonContentSquadAndPlayers,
                };

                using var content = new StringContent(jsonToSend, Encoding.UTF8, "application/json");
                using var response = await controller.HttpClientController.PostAsync(webhook.Url, content);
            }
            catch (UriFormatException ex)
            {
                controller?.AddToText($">:> An error has occurred while processing URL for the webhook \"{webhook.Name}\": {ex.Message}");
            }
            catch (Exception ex)
            {
                controller?.AddToText($">:> An error has occurred while processing the webhook \"{webhook.Name}\": {ex.Message}");
            }
        }
    }

    internal static async Task ExecuteSessionWebhooksAsync(ILogUploaderController controller, List<DpsReportJson> reportJson, LogSessionSettings logSessionSettings)
    {
        var allWebhooks = DiscordWebhooks.All;
        IEnumerable<DiscordWebhookData> webhooksToExecute = logSessionSettings.UseSelectedWebhooksInstead
            ? logSessionSettings.SelectedWebhooks
            : logSessionSettings.ExcludeSelectedWebhooksInstead
                ? allWebhooks.Values.Except(logSessionSettings.SelectedWebhooks)
                : allWebhooks.Values.Where(x => x.Active);

        foreach (var webhook in webhooksToExecute)
        {
            var satisfyingLogs = reportJson.Where(x => webhook.Team?.IsSatisfied(x.GetLogPlayers()) ?? true).ToList();
            var discordEmbeds = SessionTextConstructor.ConstructSessionEmbeds(satisfyingLogs, logSessionSettings);
            var embedsList = webhook.SuccessFailToggle switch
            {
                DiscordWebhookDataSuccessToggle.OnSuccessOnly => discordEmbeds.Success,
                DiscordWebhookDataSuccessToggle.OnFailOnly => discordEmbeds.Failure,
                _ => discordEmbeds.SuccessFailure,
            };
            var fullEmbeds = new List<DiscordApiJsonContentEmbed>();
            if (discordEmbeds.Summary is not null)
            {
                fullEmbeds.Add(discordEmbeds.Summary);
            }
            if (embedsList is not null)
            {
                fullEmbeds.AddRange(embedsList);
            }
            await SendDiscordMessageWebhooksAsync(controller, webhook, fullEmbeds, logSessionSettings.ContentText);
        }

        controller?.AddToText(">:> Session webhooks executed.");
    }

    private static async Task SendDiscordMessageWebhooksAsync(ILogUploaderController controller, DiscordWebhookData webhook, List<DiscordApiJsonContentEmbed> discordEmbeds, string contentText)
    {
        try
        {
            var jsonContent = JsonConvert.SerializeObject(new DiscordApiJsonContent
            {
                Content = contentText,
                Embeds = discordEmbeds,
            });

            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            await controller.HttpClientController.PostAsync(webhook.Url, content);
        }
        catch (Exception ex)
        {
            controller?.AddToText($">:> Unable to execute webhook \"{webhook.Name}\" for log session: {ex.Message}");
        }
    }
}
