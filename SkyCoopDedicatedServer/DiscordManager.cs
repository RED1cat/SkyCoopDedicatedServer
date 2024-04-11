using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using SkyCoopDedicatedServer;

namespace SkyCoop
{
    static class DiscordManager
    {
        private static string ConfigPath = AppDomain.CurrentDomain.BaseDirectory + @"/" + "botconfig";
        private static string ConfigTemplate = "token=\ninfochannelid=\nfeedchannelid=\nlastmessageid=\ntimetoupdatemessage=5";
        private static bool Ready = false;
        private static bool Connected = false;
        private static DiscordSocketClient _client;
        private static string Token;
        private static ulong InfoChannelId;
        private static ulong FeedChannelId;
        private static ulong Messageid;
        private static int MinuteToUpdateMessage = 5;
        private static int CurrentMinuteToUpdateMessage = 0;

        private static Task Log(LogMessage msg)
        {
            Logger.Log($"[DiscordManager] {msg.Message}",Shared.LoggerColor.Green);

            return Task.CompletedTask;
        }

        public static async Task Init()
        {
            if (!LoadConfig())
                return;

            _client = new DiscordSocketClient();

            _client.Log += Log;

            
            _client.Ready += _client_Ready;
            _client.Connected += _client_Connected;
            _client.Disconnected += _client_Disconnected;

            await _client.LoginAsync(TokenType.Bot, Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private static bool LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                try
                {
                    File.WriteAllText(ConfigPath, ConfigTemplate);
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message, Shared.LoggerColor.Red);
                }
                return false;
            }

            try 
            {
                string[] config = File.ReadAllLines(ConfigPath);

                if (config[0].Split('=')[1].Length <= 1 || config[1].Split('=')[1].Length <= 1 || config[2].Split('=')[1].Length <= 1 || !ulong.TryParse(config[1].Split('=')[1], out InfoChannelId) || !ulong.TryParse(config[2].Split('=')[1], out FeedChannelId))
                {
                    return false;
                }

                Token = config[0].Split('=')[1];
                ulong.TryParse(config[3].Split('=')[1], out Messageid);
                int.TryParse(config[4].Split('=')[1], out MinuteToUpdateMessage);
                CurrentMinuteToUpdateMessage = MinuteToUpdateMessage;

                return true;
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, Shared.LoggerColor.Red);
            }

            return false;
        }

        public static void SaveConfig()
        {
            if (!Ready)
                return;
            try
            { 
                string[] config = File.ReadAllLines(ConfigPath);
                config[1] = $"{config[1].Split('=')[0]}={InfoChannelId}";
                config[2] = $"{config[2].Split('=')[0]}={FeedChannelId}";
                config[3] = $"{config[3].Split('=')[0]}={Messageid}";

                try
                {
                    File.WriteAllLines(ConfigPath, config);
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message, Shared.LoggerColor.Red);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, Shared.LoggerColor.Red);
            }
        }

        private static Task _client_Ready()
        {
            Ready = true;

            if (Program.ServerGettingError)
            {
                Task.Run(ServerError);
                Program.ServerGettingError = false;
            }

            return Task.CompletedTask;
        }

        private static Task _client_Connected()
        {
            Connected = true;

            return Task.CompletedTask;
        }

        private static Task _client_Disconnected(Exception arg)
        {
            Connected = false;

            return Task.CompletedTask;
        }

        public static async Task ServerInfoUpdate(string serverName, int players, int maxPlayers, string serverIp, string serverVersion, bool recreate = false, bool online = true)
        {
            if (!Ready || !Connected)
                return;

            CurrentMinuteToUpdateMessage++;

            if (CurrentMinuteToUpdateMessage >= MinuteToUpdateMessage || !online || recreate)
            {
                CurrentMinuteToUpdateMessage = 0;

                string onlineMsg = ":green_circle: Online";
                if (!online)
                    onlineMsg = ":red_circle: Offline";

                string messageTemplate = $"**{serverName}**\r\nCurrent status: {onlineMsg}\r\nCurrent players: {players}/{maxPlayers}\r\nServer IP address: `{serverIp}`\r\nMod version: **{serverVersion}**\r\nGame version: **2.01-2.02**\r\nLast update message: {DateTime.Now}";


                if (await _client.GetChannelAsync(InfoChannelId) is IMessageChannel chnl) //get channel
                {
                    if (Messageid == 0)
                    {
                        if (await chnl.SendMessageAsync(messageTemplate) is IUserMessage nmsg)
                        {
                            Messageid = nmsg.Id;
                        }
                    }

                    if (await chnl.GetMessageAsync(Messageid) is IUserMessage msg) //get message to edit
                    {
                        if (recreate)
                        {
                            await msg.DeleteAsync();
                            if (await chnl.SendMessageAsync(messageTemplate) is IUserMessage nmsg)
                            {
                                Messageid = nmsg.Id;
                            }
                        }
                        else
                        {
                            await msg.ModifyAsync(m => m.Content = messageTemplate);
                        }
                    }
                }
            }
        }

        public static async Task SendMessage(string Message)
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName
                    },
                    Color = 0xfbff00,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Message:",
                            Value = Message
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }

        public static async Task PlayerJoined(string PlayerName, int Players)
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder() 
                { 
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName 
                    },
                    Color = 0x00FFFF,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = $"Current online: {Players}",
                            Value = $"{PlayerName}\r\nJoined the server"
                        }
                    }
                };

                await chnl.SendMessageAsync(embed:embed.Build());
            }
        }

        public static async Task PlayerLeave(string PlayerName, int Players)
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName 
                    },
                    Color = 0xff0015,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = $"Current online: {Players}",
                            Value = $"{PlayerName}\r\nLeave the server"
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }

        public static async Task TodayStats(string Stats)
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName
                    },
                    Color = 0xfbff00,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Day is over\r\nStats of the day:",
                            Value = Stats
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }

        public static async Task CrashSiteSpawn(string Text)
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName 
                    },
                    Color = 0xfbff00,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Another plane crash on Great Bear Island!",
                            Value = Text
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }
        public static async Task CrashSiteFound()
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName
                    },
                    Color = 0xfbff00,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Crash Site Has Been Found!",
                            Value = "Someone managed to find crash site."
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }

        public static async Task CrashSiteTimeOver()
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName
                    },
                    Color = 0xff0015,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Crash Site Has Not Been Found!",
                            Value = "Time is up, no one has found the crash site."
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }
        public static async Task ServerError()
        {
            if (!Ready || !Connected)
                return;

            if (await _client.GetChannelAsync(FeedChannelId) is IMessageChannel chnl) //get channel
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = MyMod.CustomServerName
                    },
                    Color = 0xff0015,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "The server made an emergency reboot because of an error",
                            Value = ":skull_crossbones:"
                        }
                    }
                };

                await chnl.SendMessageAsync(embed: embed.Build());
            }
        }
    }
}
