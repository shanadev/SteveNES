using NES;
using Serilog;

internal class Program
{

    private static void Main(string[] args)
    {
        string logfilename = "logs/neslog" + DateTime.Now.ToString("s") + ".txt";

        //Log.Logger = new LoggerConfiguration()
        //    .MinimumLevel.Debug()
        //    .WriteTo.File(logfilename)
        //    .CreateLogger();

        //Log.Information("Starting up the NES");
        NESSystem nes = new NESSystem();
    }

}