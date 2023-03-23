using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DiscordWebhook;
using SkyCoop;
#if (!DEDICATED)
using MelonLoader.TinyJSON;
#else
using TinyJSON;
#endif

namespace SkyCoop
{
    internal class DiscordManager
    {
        public static Webhook webhook = null;
        public static bool Initilized = false;
        private static string WebHookName = "Public Server Feed";

        public static void Init()
        {
            string Path = MPSaveManager.GetBaseDirectory() + MPSaveManager.GetSeparator() + "webhook.json";
            if (System.IO.File.Exists(Path))
            {
                string readText = System.IO.File.ReadAllText(Path);
                DataStr.WebhookSettings Data = JSON.Load(readText).Make<DataStr.WebhookSettings>();
                Init(Data.URL, Data.Name);
            }
        }

        public static void Init(string URL, string Name)
        {
            if (Initilized)
            {
                return;
            }
            WebHookName = Name;
            webhook = new Webhook(URL);
            Logger.Log("Webhook started!", Shared.LoggerColor.Magenta);
            Initilized = true;
        }

        public static void SendMessage(string Message)
        {
            if (!Initilized)
            {
                return;
            }
            WebhookObject obj = new WebhookObject()
            {
                username = WebHookName,
                content = Message
            };
            webhook.PostData(obj);
        }

        public static void PlayerJoined(string PlayerName, int Players)
        {
            if (!Initilized)
            {
                return;
            }

            WebhookObject obj = new WebhookObject()
            {
                embeds = new Embed[]
                {
                    new Embed()
                    {
                        author = new Author()
                        {
                            name = "Current online: "+Players,
                        },
                        color = 0x00FFFF,
                        fields = new Field[]
                        {
                            new Field()
                            {
                                name = PlayerName,
                                value = "Joined the server"
                            }
                        }
                    }
                },
                
                
                username = WebHookName,
                content = ""
            };
            webhook.PostData(obj);
        }

        public static void PlayerLeave(string PlayerName, int Players)
        {
            if (!Initilized)
            {
                return;
            }

            WebhookObject obj = new WebhookObject()
            {
                embeds = new Embed[]
                {
                    new Embed()
                    {
                        author = new Author()
                        {
                            name = "Current online: "+Players,
                        },
                        color = 0xff0015,
                        fields = new Field[]
                        {
                            new Field()
                            {
                                name = PlayerName,
                                value = "Leave the server"
                            }
                        }
                    }
                },

                username = WebHookName,
                content = ""
            };
            webhook.PostData(obj);
        }

        public static void TodayStats(string Stats)
        {
            if (!Initilized)
            {
                return;
            }

            WebhookObject obj = new WebhookObject()
            {
                embeds = new Embed[]
                {
                    new Embed()
                    {
                        author = new Author()
                        {
                            name = "Day is over ",
                        },
                        color = 0xfbff00,
                        fields = new Field[]
                        {
                            new Field()
                            {
                                name = "Stats of the day:",
                                value = Stats
                            }
                        }
                    }
                },

                username = WebHookName,
                content = ""
            };
            webhook.PostData(obj);
        }

        public static void CrashSiteSpawn(string Text)
        {
            if (!Initilized)
            {
                return;
            }

            WebhookObject obj = new WebhookObject()
            {
                embeds = new Embed[]
                {
                    new Embed()
                    {
                        author = new Author()
                        {
                            name = "Another plane crash on Great Bear Island!",
                        },
                        color = 0xfbff00,
                        fields = new Field[]
                        {
                            new Field()
                            {
                                name = Text,
                                value = ""
                            }
                        }
                    }
                },

                username = WebHookName,
                content = ""
            };
            webhook.PostData(obj);
        }
        public static void CrashSiteFound()
        {
            if (!Initilized)
            {
                return;
            }

            WebhookObject obj = new WebhookObject()
            {
                embeds = new Embed[]
                {
                    new Embed()
                    {
                        author = new Author()
                        {
                            name = "Crash Site Has Been Found!",
                        },
                        color = 0xfbff00,
                        fields = new Field[]
                        {
                            new Field()
                            {
                                name = "Next one in few hours later.",
                                value = ""
                            }
                        }
                    }
                },

                username = WebHookName,
                content = ""
            };
            webhook.PostData(obj);
        }
    }
}
