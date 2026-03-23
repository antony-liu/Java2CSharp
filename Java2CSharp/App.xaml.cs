using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using HandyControl.Controls;

namespace Java2CSharp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static List<FolderMapping> Folders { get; set; } = new List<FolderMapping>();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadConfig();
            //e.Args;
            //ConsoleManager.OpenConsole();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            //ConsoleManager.CloseConsole();
        }

        private void LoadConfig()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(Environment.CurrentDirectory + "\\config.xml");
            XmlNodeList list = xmlDoc.SelectNodes("/config/folders/folder");
            foreach (XmlNode node in list)
            {
                Folders.Add(new FolderMapping()
                {
                    Name = node.Attributes["name"].Value,
                    Source = node.Attributes["source"].Value,
                    Target = node.Attributes["target"].Value,
                });
            }
        }
    }
}
