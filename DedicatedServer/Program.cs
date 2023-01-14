#if (DEDICATED_LINUX)
using System;
namespace DedicatedServer
{
    class Program
    {
        public static void Main(string[] arg)
        {

            if (arg.Length > 0)
            {

            }
            else
            {
                using var game = new SkyCoop.MyMod();
                game.Run();
            }
        }
    }
}
#endif
#if(DEDICATED_WINDOWS)
using var game = new SkyCoop.MyMod();
game.Run();
#endif
