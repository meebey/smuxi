using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using Smuxi.Engine;
using Smuxi.Common;
namespace Smuxi.Frontend.Wpf
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class Frontend : Application
	{
#if LOG4NET

        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

#endif





        public string Name { get; private set; }

        public string UIName { get; private set; }

        public Version Version { get; private set; }

        public Version EngineVersion { get; set; }

        public string VersionString { get; private set; }

        public Session Session { get; set; }

        public FrontendManager FrontendManager { get; private set; }

        public Config Config { get; private set; }

        public UserConfig UserConfig { get; set; }

        public FrontendConfig FrontendConfig { get; private set; }



        public new MainWindow MainWindow { get { return base.MainWindow as MainWindow; } }

        public new static Frontend Current { get { return Application.Current as Frontend; } }





        public Window StartSplashScreen()
        {

            Uri splashUri = new Uri("/SplashScreenWindow.xaml", UriKind.Relative);

            Window splash = (Window)LoadComponent(splashUri);

            splash.Show();

            return splash;

        }





        private void StartApplication(object sender, StartupEventArgs e)
        {

#if LOG4NET

            log4net.Config.BasicConfigurator.Configure();

#endif


            // Splash screen should be shown as soon as the application begins

            Window ss = StartSplashScreen();



            // Now we can continue to initialize the application as needed

            System.Threading.Thread.CurrentThread.Name = "Main";



            {

                Assembly asm = Assembly.GetAssembly(typeof(Frontend));

                AssemblyName asm_name = asm.GetName(false);

                AssemblyProductAttribute pr = (AssemblyProductAttribute)asm.

                    GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];

                Version = asm_name.Version;

                VersionString = String.Format("{0} - {1} frontend {2}", pr.Product, UIName, Version);

            }

#if LOG4NET

            _Logger.Info(VersionString + " starting");

#endif



            FrontendConfig = new FrontendConfig(UIName);

            // loading and setting defaults

            FrontendConfig.Load();

            FrontendConfig.Save();



            if (FrontendConfig.IsCleanConfig)
            {

                ss.Close();

                RunInitWizard();

            }
            else
            {

                if (((string)FrontendConfig["Engines/Default"]).Length == 0)
                {

                    //InitLocalEngine();

                }
                else
                {

                    // there is a default engine set, means we want a remote engine

                    ss.Close();

                    RunEngineManagerDialog();

                }

            }

            // Now that everything is setup, we can destroy the splash screen

            // and start the application

            ss.Close();

        }

        public void InitLocalEngine()
        {

            Engine.Engine.Init();

            EngineVersion = Engine.Engine.Version;

            Session = new Engine.Session(Engine.Engine.Config,

                                         Engine.Engine.ProtocolManagerFactory,

                                         "local");

            Session.RegisterFrontendUI(MainWindow.UI);

            UserConfig = Session.UserConfig;

            ConnectEngineToGUI();

        }



        public void ConnectEngineToGUI()
        {

            FrontendManager = Session.GetFrontendManager(MainWindow.UI);

            FrontendManager.Sync();



            if (UserConfig.IsCaching)
            {

                // when our UserConfig is cached, we need to invalidate the cache

                FrontendManager.ConfigChangedDelegate = new SimpleDelegate(UserConfig.ClearCache);

            }

            MainWindow.Show();

            MainWindow.ApplyConfig(UserConfig);

            // make sure entry got attention :-P

            MainWindow.Entry.Focus();

        }





        private void EndApplication(object sender, ExitEventArgs e)
        {



        }



        private void RunInitWizard()
        {

        }



        private void RunEngineManagerDialog()
        {

        }
	}
}
