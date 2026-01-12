using UtilityPack;

namespace LeadHype.Api;

public class DI
{
    static DI()
    {
        AssemblyName = typeof(DI).Assembly.GetName().Name;
    }

    public static string AssemblyName { get; set; }
    public static IServiceProvider ServiceProvider { get; set; }
    public static ILogFactory Logger { get; set; }
    
    public static void Build(string basePath)
    {
        Logger = new BaseLogFactory([
            new FileLogger(Path.Combine(basePath, "Data", "Logs")),
        ]);
    }
}