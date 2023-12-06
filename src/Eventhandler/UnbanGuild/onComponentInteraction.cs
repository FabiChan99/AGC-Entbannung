﻿#region

using AGC_Entbannungssystem.Helpers;
using AGC_Entbannungssystem.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Exceptions;
using Sentry;

#endregion

namespace AGC_Entbannungssystem.Eventhandler.UnbanGuild;

[EventHandler]
public class onComponentInteraction : ApplicationCommandsModule
{
    [Event]
    public async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            string cid = e.Interaction.Data.CustomId;
            if (cid == "open_appealticketinfo")
            {
                DiscordGuild mainGuild = await client.GetGuildAsync(GlobalProperties.MainGuildId);
                bool isBanned = false;
                try
                {
                    await mainGuild.GetBanAsync(e.User.Id);
                    isBanned = true;
                }
                catch (NotFoundException)
                {
                    // ignored
                    isBanned = false;
                }
                catch (Exception exception)
                {
                    var embed = new DiscordEmbedBuilder();
                    embed.WithTitle("Fehler!");
                    embed.WithDescription(
                        "Es ist ein Fehler aufgetreten. Bitte versuche es später erneut. Der Fehler wurde automatisch an den Entwickler weitergeleitet.");
                    embed.WithColor(DiscordColor.Red);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                    await ErrorReporting.SendErrorToDev(client, e.User, exception);
                }
                if (e.User.Id == GlobalProperties.BotOwnerId)
                {
                    // application test
                    isBanned = true;
                }
                if (!isBanned)
                {
                    var embed = new DiscordEmbedBuilder();
                    embed.WithTitle("Nicht gebannt!");
                    embed.WithDescription(
                        "Wie es scheint, bist du nicht auf AGC gebannt. Diese Überprüfung ist automatisiert. Du kannst also keinen Entbannungsantrag stellen.");
                    embed.WithColor(DiscordColor.Red);
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                    return;
                }

                var rb = new DiscordInteractionResponseBuilder();
                var button = new DiscordButtonComponent(ButtonStyle.Success, "open_appealticket_confirm",
                    "Ich habe alles gelesen und verstanden!",
                    emoji: new DiscordComponentEmoji("✅"));
                rb.AddComponents(button);
                rb.AddEmbeds(MessageGenerator.UnbanNoteGenerate());
                rb.AsEphemeral();
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, rb);
                try
                {
                    ulong logChannelId = ulong.Parse(BotConfigurator.GetConfig("MainConfig", "LogChannelId"));
                    var logChannel = await client.GetChannelAsync(logChannelId);
                    await logChannel.SendMessageAsync(
                        $"{e.User.Mention} ({e.User.Id}) hat die Antragshinweise **geöffnet** - {DateTime.Now.Timestamp(TimestampFormat.ShortDateTime)}");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
            else if (cid == "open_appealticket_confirm")
            {
                // TODO: check if user has already an open ticket
                // TODO: check if user is "still" banned
                ulong logChannelId = ulong.Parse(BotConfigurator.GetConfig("MainConfig", "LogChannelId"));
                var logChannel = await client.GetChannelAsync(logChannelId);
                await logChannel.SendMessageAsync(
                    $"{e.User.Mention} ({e.User.Id}) hat die Antragshinweise **akzeptiert** - {DateTime.Now.Timestamp(TimestampFormat.ShortDateTime)}");
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Ticket wird erstellt..."));
                var appealrole = e.Guild.GetRole(ulong.Parse(BotConfigurator.GetConfig("MainConfig", "AppealRoleId")));
                await Task.Delay(1000);
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Prüfe auf offenes Ticket..."));
                DiscordMember member = await e.Guild.GetMemberAsync(e.User.Id);
                if (member.Roles.Contains(appealrole))
                {
                    var embed = new DiscordEmbedBuilder();
                    embed.WithTitle("Fehler!");
                    embed.WithDescription(
                        "Du hast bereits ein offenes Ticket. Bitte nutze dieses, um einen Entbannungsantrag zu stellen.");
                    embed.WithColor(DiscordColor.Red);
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
                await Task.Delay(1000);
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Erstelle Ticket..."));
                await logChannel.SendMessageAsync(
                    $"$new {e.User.Id}");
                await Task.Delay(500);
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Ticket erstellt!"));
                
            }



            await Task.CompletedTask;
        });
    }
}