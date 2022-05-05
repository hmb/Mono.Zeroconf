using System.Runtime.InteropServices;

namespace Mono.Zeroconf.Providers.AvahiDBus.NDesk.DBus;

public static class UnixUserInfo
{
    private const string LIBC = "libc";

    // getuid(2)
    //    uid_t getuid(void);
    [DllImport (LIBC, SetLastError=true)]
    private static extern uint getuid ();

    public static long GetRealUserId()
    {
        return getuid();
    }
}