namespace Zeroconf.Avahi;

public static class IpProtocolTypeExtension
{
    public static int ToNativeAvahiProtocolType(this IpProtocolType ipProtocolType)
    {
        return (int)ipProtocolType;
    }
}