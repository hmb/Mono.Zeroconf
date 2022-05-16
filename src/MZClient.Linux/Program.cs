using System.Threading.Tasks;

namespace MZClient.Linux;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Lib.MZClient.MainLib(args);
    }    
}