namespace Mono.Zeroconf;

using System.Threading.Tasks;

public static class ServiceBrowserExtension
{
    public static async Task Browse(this IServiceBrowser serviceBrowser, uint interfaceIndex, string regtype, string domain)
    {
        await serviceBrowser.Browse(interfaceIndex, AddressProtocol.Any, regtype, domain);
    }

    public static async Task Browse(this IServiceBrowser serviceBrowser, AddressProtocol addressProtocol, string regtype, string domain)
    {
        await serviceBrowser.Browse(0, addressProtocol, regtype, domain);
    }

    public static async Task Browse(this IServiceBrowser serviceBrowser, string regtype, string domain)
    {
        await serviceBrowser.Browse(0, AddressProtocol.Any, regtype, domain);
    }
}