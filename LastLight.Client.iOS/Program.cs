using System;
using Foundation;
using UIKit;
using LastLight.Client.Core;

namespace LastLight.Client.iOS
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        private static LastLight.Client.Core.Game1 game;

        internal static void RunGame()
        {
            game = new LastLight.Client.Core.Game1();
            game.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(Program));
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
    }
}
