# README

## Generate C# classes

```bash
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Accounts.User.xml               --output avahi/Accounts.User.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Accounts.xml                    --output avahi/Accounts.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.AddressResolver.xml       --output avahi/Avahi.AddressResolver.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.DomainBrowser.xml         --output avahi/Avahi.DomainBrowser.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.EntryGroup.xml            --output avahi/Avahi.EntryGroup.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.HostNameResolver.xml      --output avahi/Avahi.HostNameResolver.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.RecordBrowser.xml         --output avahi/Avahi.RecordBrowser.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.Server.xml                --output avahi/Avahi.Server.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.ServiceBrowser.xml        --output avahi/Avahi.ServiceBrowser.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.ServiceResolver.xml       --output avahi/Avahi.ServiceResolver.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.Avahi.ServiceTypeBrowser.xml    --output avahi/Avahi.ServiceTypeBrowser.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.bolt.xml                        --output avahi/bolt.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.fwupd.xml                       --output avahi/fwupd.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.PackageKit.Transaction.xml      --output avahi/PackageKit.Transaction.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.PackageKit.xml                  --output avahi/PackageKit.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.UPower.Device.xml               --output avahi/UPower.Device.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.UPower.KbdBacklight.xml         --output avahi/UPower.KbdBacklight.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.UPower.Wakeups.xml              --output avahi/UPower.Wakeups.cs
dotnet dotnet-dbus.dll codegen /usr/share/dbus-1/interfaces/org.freedesktop.UPower.xml                      --output avahi/UPower.cs
```