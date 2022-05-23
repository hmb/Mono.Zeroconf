namespace Zeroconf.Avahi;

public static class AvahiInit
{
    public static async Task Initialize()
    {
        await DBusManager.Initialize();
    }
}