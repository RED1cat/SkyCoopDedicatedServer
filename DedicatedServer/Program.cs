namespace DedicatedServer
{
    class Program
    {
        public static bool NoGraphics;
        public static XnaMain xnaMain = new XnaMain();
#if (WINDOWS_DEBUG)
        static bool ForceXNA = true;
#else
        static bool ForceXNA = false;
#endif

        public static void Main(string[] arg)
        {
            if (ForceXNA)
            {
                NoGraphics = false;
                xnaMain.Run();
            } else
            {
                if (arg.Length == 0)
                {
                    NoGraphics = true;
                    ConsoleMain serv = new ConsoleMain();
                    serv.Initialize();
                } else
                {
                    NoGraphics = false;
                    xnaMain.Run();
                }
            }
        }
    }
}