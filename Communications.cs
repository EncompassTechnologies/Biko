using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Reflection;

namespace Communications
{
    public static class Settings
    {
        public static string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public static string Url
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.url"];
            }
        }

        public static string UserName
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.userName"];
            }
        }

        public static string Password
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.password"];
            }
        }

        public static string Inbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.inbox"];
            }
        }

        public static string Outbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.outbox"];
            }
        }

        public static string OpenDSDInbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.opendsdinbox"];
            }
        }

        public static string OpenDSDOutbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.opendsdoutbox"];
            }
        }

        public static string Sentbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.sentbox"];
            }
        }

        public static string Receivedbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.receivedbox"];
            }
        }

        public static string Rejectedbox
        {
            get
            {
                return ConfigurationManager.AppSettings["encxs.connection.rejectbox"];
            }
        }
    }
}
