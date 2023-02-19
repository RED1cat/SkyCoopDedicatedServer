namespace DedicatedServer
{
    class Program
    {
        public static bool NoGraphics;
        public static XnaMain xnaMain = new XnaMain(); 
        public static void Main(string[] arg)
        {
            if(arg.Length == 0)
            {
                NoGraphics = true;
                ConsoleMain serv = new ConsoleMain();
                serv.Initialize();
            }
            else
            {
                NoGraphics = false;
                xnaMain.Run();
            }
        }
    }
}