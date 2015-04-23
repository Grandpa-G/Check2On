using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Permissions;
using System.Net.Mail;

using Terraria;
using TShockAPI;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TerrariaApi.Server;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO;

namespace Check2On
{
    [ApiVersion(1, 17)]
    public class Check2On : TerrariaPlugin
    {
        private Config Check2OnConfig;

        public override string Name
        {
            get { return "Check2On"; }
        }
        public override string Author
        {
            get { return "Granpa-G"; }
        }
        public override string Description
        {
            get { return "Takes action when to accounts are logged in at the same time."; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public Check2On(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnLogin;

            Commands.ChatCommands.Add(new Command("Check2On.allow", Check2OnOptions, "2on"));

            var path = Path.Combine(TShock.SavePath, "Check2On.json");
            (Check2OnConfig = Config.Read(path)).Write(path);

        }

        protected override void Dispose(bool disposing)
        {

            if (disposing)
            {
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnLogin;
            }
            base.Dispose(disposing);
        }
        private static void OnLeave(LeaveEventArgs args)
        {
        }

        private void OnLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
             var player = args.Player.UserAccountName;
            if (player == null)
                return;

            foreach (TSPlayer tsPlayer in TShock.Players.Where(p => null != p))
            {
                if (args.Player.UserAccountName.Equals(tsPlayer.UserAccountName))
                {
                    switch (Check2OnConfig.action)
                    {
                        case 0:     // do nothing
                            break;
                        case 1:     // warn admin
                            TShock.Log.ConsoleInfo(String.Format("2nd login for user:{0} player:{1} on:{2} {3}", tsPlayer.UserAccountName, tsPlayer.Name, tsPlayer.IP, tsPlayer.Group.Name));
                            args.Player.SendSuccessMessage(String.Format("2nd login for user:{0} player:{1} on:{2} {3}", tsPlayer.UserAccountName, tsPlayer.Name, tsPlayer.IP, tsPlayer.Group.Name));
                            if (Check2OnConfig.email)
                                sendMail(String.Format("2nd login for user:{0} player:{1} on:{2} group:{3}", tsPlayer.UserAccountName, tsPlayer.Name, tsPlayer.IP, tsPlayer.Group.Name));
                            break;
                        case 2:     // kick player
                            TShock.Log.ConsoleInfo(String.Format("2nd login kicked  user:{0} player:{1} on:{2} {3}", tsPlayer.UserAccountName, tsPlayer.Name, tsPlayer.IP, tsPlayer.Group.Name));
                            args.Player.SendSuccessMessage(String.Format("2nd login kicked user:{0} player:{1} on:{2} {3}", tsPlayer.UserAccountName, tsPlayer.Name, tsPlayer.IP, tsPlayer.Group.Name));
                            TShock.Utils.ForceKick(tsPlayer, "Your user account is currently in use.", false, true);
                            if (Check2OnConfig.email)
                                sendMail(String.Format("2nd login kicked  user:{0} player:{1} on:{2} group:{3}", tsPlayer.UserAccountName, tsPlayer.Name, tsPlayer.IP, tsPlayer.Group.Name));
                            break;
                    }
                    break;
                }
            }

        }

        private void Check2OnOptions(CommandArgs args)
        {
            bool help = false;

            Check2OnListArguments arguments = new Check2OnListArguments(args.Parameters.ToArray());
            if (arguments.Contains("-help"))
                help = true;

            if (help)
            {
                args.Player.SendMessage("Syntax: /2on [-help] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage("   -r        reload configuration file", Color.LightSalmon);
                args.Player.SendMessage("   -test     test email configuration", Color.LightSalmon);
                args.Player.SendMessage("   -help     this information", Color.LightSalmon);
                return;
            }

            if (arguments.Contains("-test"))
            {
                if (sendMail("test message from Check2On"))
                    args.Player.SendSuccessMessage("send test mail success");
                else
                args.Player.SendSuccessMessage("send test mail failure");

            }
            if (arguments.Contains("-r"))
            {
                var path = Path.Combine(TShock.SavePath, "Check2On.json");
                Check2OnConfig = Config.Read(path);
                args.Player.SendSuccessMessage("Check2On options reloaded.");
            }
        }

        private bool sendMail(string message)
        {
            if (Check2OnConfig.host.Length == 0)
                return false;

            try
            {
                SmtpClient client = new SmtpClient();
                client.Port = Check2OnConfig.port;
                client.Host = Check2OnConfig.host;
                client.EnableSsl = Check2OnConfig.EnableSsl;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(Check2OnConfig.credentialsUser, Check2OnConfig.credentialsPassword);

                MailMessage mm = new MailMessage(Check2OnConfig.sendFrom, Check2OnConfig.sendTo, message, message);
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                client.Send(mm);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
 
                return false;
            }
            return true;
        }
    }
    #region application specific commands
    public class Check2OnListArguments : InputArguments
    {
        public string Verbose
        {
            get { return GetValue("-verbose"); }
        }
        public string VerboseShort
        {
            get { return GetValue("-v"); }
        }

        public string Help
        {
            get { return GetValue("-help"); }
        }


        public Check2OnListArguments(string[] args)
            : base(args)
        {
        }

        protected bool GetBoolValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                bool res;
                bool.TryParse(_parsedArguments[adjustedKey], out res);
                return res;
            }
            return false;
        }
    }
    #endregion

}
