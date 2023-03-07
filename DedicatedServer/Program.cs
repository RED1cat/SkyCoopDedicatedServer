namespace DedicatedServer
{
    class Program
    {
        public static bool NoGraphics;
        public static XnaMain xnaMain = new XnaMain();

        public static void Main(string[] arg)
        {
#if (WINDOWS_DEBUG)
            bool ForceXNA = true;
#else
            bool ForceXNA = false;
#endif            
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