using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using TShockAPI;
using Color = Microsoft.Xna.Framework.Color;

namespace DiscordBridge
{
	public partial class DiscordBridge
	{
		private void doDiscord(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendInfoMessage("{0} by {1}",
					TShock.Utils.ColorTag($"Discord Bridge v{Version}", new Color(0, 255, 185)),
					TShock.Utils.ColorTag(Author, Color.OrangeRed));

				if (Client.ConnectionState == ConnectionState.Connected && Client.Guilds.Any())
				{
					e.Player.SendInfoMessage("Your messages are being broadcasted to Discord Server(s): {0}",
						String.Join(", ", Client.Guilds.Select(s => TShock.Utils.ColorTag(s.Name, new Color(0, 255, 0)))));
				}
				else
				{
					e.Player.SendInfoMessage("This bot is currently not connected to any server.");
					e.Player.SendInfoMessage("Type /discord accept-invite <inviteId> to connect the bot to one.");
				}

				return;
			}

			string command = e.Parameters[0].ToLowerInvariant();
			e.Parameters.RemoveAt(0);

			var commandList = new Dictionary<string, Action>
			{
				["accept-invite"] = () => acceptInvite(e),
				["connect"] = () => connect(e),
				["disconnect"] = () => disconnect(e),
				["reload-config"] = () => reloadConfig(e),
				["set-token" ] = () => setToken(e)
			};

			// Must be added separately after the list has been declared
			commandList.Add("help", () => help(commandList.Keys, e));

			switch (command)
			{
				case "-h":
				case "-?":
				case "help":
					commandList["help"].Invoke();
					return;

				case "-c":
				case "connect":
					if (!e.Player.HasPermission(Permissions.Reconnect))
					{
						e.Player.SendErrorMessage("You do not have access to this command.");
						return;
					}

					commandList["connect"].Invoke();
					return;

				case "-d":
				case "disconnect":
					if (!e.Player.HasPermission(Permissions.Reconnect))
					{
						e.Player.SendErrorMessage("You do not have access to this command.");
						return;
					}

					commandList["disconnect"].Invoke();
					return;

				case "-i":
				case "accept-invite":
					if (!e.Player.HasPermission(Permissions.AcceptInvite))
					{
						e.Player.SendErrorMessage("You do not have access to this command.");
						return;
					}

					commandList["accept-invite"].Invoke();
					return;

				case "-r":
				case "reload":
				case "reload-config":
					if (!e.Player.HasPermission(Permissions.ReloadConfig))
					{
						e.Player.SendErrorMessage("You do not have access to this command.");
						return;
					}

					commandList["reload-config"].Invoke();
					return;

				case "-t":
				case "token":
				case "set-token":
					if (!e.Player.HasPermission(Permissions.SetBotToken))
					{
						e.Player.SendErrorMessage("You do not have access to this command.");
						return;
					}

					commandList["set-token"].Invoke();
					return;

				default:
					e.Player.SendErrorMessage($"Invalid command! Type {Commands.Specifier}discord help for a list of available commands.");
					break;
			}
		}

		private void help(IEnumerable<string> commands, CommandArgs e)
		{
			// Todo: Add actual help for each specific command
			e.Player.SendInfoMessage($"Command syntax: {Commands.Specifier}discord <command> [params...]");
			e.Player.SendInfoMessage($"Available commands: {String.Join(", ", commands)}.");
		}

		private void acceptInvite(CommandArgs e)
		{
			// Apparently bot accounts cannot accept invites, so this feature is useless. Gotta use the OAUTH2 API link generation instead
		  e.Player.SendInfoMessage(" * As the Discord unnoficial application API dictates, bots are unable to accept invites.");
		  e.Player.SendInfoMessage(" * Copy the following link to your clipboard and access it with a web browser to register your bot instead.");
		  e.Player.SendInfoMessage(" * https://discordapp.com/oauth2/authorize?client_id=157730590492196864&scope=bot&permissions=0");
		  e.Player.SendInfoMessage(" * Replace client_id with your bot application ID. This is NOT your bot token, but the application client ID itself.");
		}

		private async void connect(CommandArgs e)
		{
			bool force = String.Equals(e.Parameters.FirstOrDefault(), "-f", StringComparison.OrdinalIgnoreCase);
			switch (Client.ConnectionState)
			{
				case ConnectionState.Disconnected:
					await Client.StartUp();
					if (Client.ConnectionState == ConnectionState.Connected)
						e.Player.SendSuccessMessage(" * Discord Bridge bot connected.");
					else
						e.Player.SendErrorMessage(" * Discord Bridge bot failed to connect. Check your internet connection and try again.");

					return;

				case ConnectionState.Connecting:
					if (!force)
						e.Player.SendInfoMessage("The discord bot is already trying to connect.");
					else
					{
						await Client.StopAsync();
						goto case ConnectionState.Disconnected;
					}

					return;

				case ConnectionState.Connected:
					if (force)
					{
						await Client.StopAsync();
						goto case ConnectionState.Disconnected;
					}

					e.Player.SendInfoMessage($"The discord bot is currently connected. Reconnect? ({Commands.Specifier}y OR {Commands.Specifier}n)");
					e.Player.AddResponse("y", async (o) =>
					{
						await Client.StopAsync();
						await Client.StartUp();
						e.Player.AwaitingResponse.Remove("n");
						e.Player.SendSuccessMessage(" * Discord Bridge bot reconnected.");
					});
					e.Player.AddResponse("n", (o) =>
					{
						e.Player.AwaitingResponse.Remove("y");
						e.Player.SendInfoMessage(" * Discord Bridge bot restart has been aborted.");
					});
					return;

				case ConnectionState.Disconnecting:
					e.Player.SendInfoMessage("The discord bot is currently disconnecting. Wait for it to finish and then run this command.");
					return;
			}		
		}

		private async void disconnect(CommandArgs e)
		{
			e.Player.SendInfoMessage(" * Disconnecting...");
			await Client.StopAsync();
			if (Client.ConnectionState == ConnectionState.Disconnected)
				e.Player.SendInfoMessage($"The discord bot is now offline. Use {Commands.Specifier}discord connect to bring it back up.");
			else
				e.Player.SendErrorMessage("The discord bot could not be disconnected at the moment. Try again later, or shut down the server if you really need to bring it down.");
		}

		private void reloadConfig(CommandArgs e)
		{
			try
			{
				Config = ConfigFile.Read();
				ChatHandler.Config = Config;
				e.Player.SendSuccessMessage("Discord bot has reloaded its configuration successfully.");
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(ex.Message);
				e.Player.SendErrorMessage("Unable to parse the config file! Please fix the following issues and run this command again:");
				e.Player.SendErrorMessage($" * {ex.Message}");
			}
		}

		private async void setToken(CommandArgs e)
		{
			if (e.Parameters.Count < 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {Commands.Specifier}discord set-token <bot token>");
				e.Player.SendInfoMessage(" * This will (re)start the bot and have it connect using the provided token.");
				return;
			}

			Config.BotToken = e.Parameters[0];
			
			if (Client.ConnectionState == ConnectionState.Connected || Client.ConnectionState == ConnectionState.Connecting)
				await Client.LogoutAsync();

			await Client.StartUp();
			if (Client.ConnectionState == ConnectionState.Connected)
				e.Player.SendSuccessMessage(" * Discord Bridge bot connected.");
			else
				e.Player.SendErrorMessage(" * Discord Bridge bot failed to connect. Check your internet connection and try again.");
		}
	}
}
