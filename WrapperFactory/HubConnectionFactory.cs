using System.Xml;
using Microsoft.AspNetCore.SignalR.Client;

namespace Runner.WrapperFactory
{
    class HubConnectionFactory
    {
        private static string HubConnection = string.Empty;
        static HubConnectionFactory()
        {
            XmlDocument xmlSettings = new XmlDocument();
            xmlSettings.Load("settings.xml");
            XmlNode xmlRootNode = xmlSettings.DocumentElement;
            XmlNodeList lstSettingNode = xmlRootNode.SelectSingleNode($"/Settings").ChildNodes;
            foreach (XmlNode settingNode in lstSettingNode)
            {
                switch (settingNode.Attributes["Name"].Value)
                {
                    case "HubConnection":
                        HubConnection = settingNode.Attributes["Value"].Value;
                        break;
                    default:
                        break;
                }
            }
        }

        public static HubConnection SignalRConnection { get; set; }

        public static void InitHubConnection()
        {
            if (SignalRConnection == null)
            {
                SignalRConnection = new HubConnectionBuilder()
                    .WithUrl(HubConnection)
                    .Build();
            }
        }
    }
}
