namespace Zeroconf.Avahi;

public static class PublishFlagsExtension
{
    public static uint ToNativeAvahi(this PublishFlags publishFlags)
    {
        return (uint)publishFlags;
    }
}