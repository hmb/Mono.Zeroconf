namespace Zeroconf.Abstraction;

using System.Threading.Tasks;

public static class ServiceBrowserExtension
{
    public static async Task Browse(this IServiceBrowser serviceBrowser, uint interfaceIndex, string regtype, string domain)
    {
        await serviceBrowser.Browse(interfaceIndex, IpProtocolType.Any, regtype, domain);
    }

    public static async Task Browse(this IServiceBrowser serviceBrowser, IpProtocolType ipProtocolType, string regtype, string domain)
    {
        await serviceBrowser.Browse(ZeroconfConstants.InterfaceIndexAny, ipProtocolType, regtype, domain);
    }

    public static async Task Browse(this IServiceBrowser serviceBrowser, string regtype, string domain)
    {
        await serviceBrowser.Browse(ZeroconfConstants.InterfaceIndexAny, IpProtocolType.Any, regtype, domain);
    }
}