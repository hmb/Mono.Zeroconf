namespace Zeroconf.Bonjour;

public static class BonjourInit
{
    public static void Initialize()
    {
        var error = Native.DNSServiceCreateConnection(out var sdRef);
            
        if(error != ServiceError.NoError) {
            throw new ServiceErrorException(error);
        }
            
        sdRef.Deallocate();
    }
}