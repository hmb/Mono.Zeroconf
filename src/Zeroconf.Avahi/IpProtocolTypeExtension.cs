namespace Zeroconf.Avahi;

public static class IpProtocolTypeExtension
{
    public static int ToNativeAvahi(this IpProtocolType ipProtocolType)
    {
        return (int)ipProtocolType;
    }
}