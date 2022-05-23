//
// ZeroconfClient.cs
//
// Authors:
//    Aaron Bockover  <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// ReSharper disable InconsistentNaming

namespace MZClient.Lib;

using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf.Abstraction;
using Zeroconf.Providers.DynamicLoad;

public static class MZClient
{
    private const string app_name = "mzclient";
    private static readonly SemaphoreSlim endProgram = new(0, 1);
    private static AddressProtocol address_protocol = AddressProtocol.Any;
    private static uint @interface;
    private static string domain = ServiceBrowserConstants.LocalDomain;
    private static bool resolve_shares;
    private static bool verbose;

    public static async Task<int> MainLib(string [] args)
    {
        var providerFactory = new ProviderFactory();
        await providerFactory.Initialize();
        
        string type = "_workstation._tcp";
        bool show_help = false;
        ArrayList services = new ArrayList();

        for(int i = 0; i < args.Length; i++) {
            if(args[i][0] != '-') {
                continue;
            }

            switch(args[i]) {
                case "-t":
                case "--type":
                    type = args[++i];
                    break;
                case "-r":
                case "--resolve":
                    resolve_shares = true;
                    break;
                case "-p":
                case "--publish":
                    services.Add(args[++i]);
                    break;
                case "-i":
                case "--interface":
                    if (!uint.TryParse (args[++i], out @interface)) {
                        Console.Error.WriteLine ("Invalid interface index, '{0}'", args[i]);
                        show_help = true;
                    }
                    break;
                case "-a":
                case "--aprotocol":
                    string proto = args[++i].ToLower ().Trim ();
                    switch (proto) {
                        case "ipv4": case "4": address_protocol = AddressProtocol.IPv4; break;
                        case "ipv6": case "6": address_protocol = AddressProtocol.IPv6; break;
                        case "any": case "all": address_protocol = AddressProtocol.Any; break;
                        default:
                            Console.Error.WriteLine ("Invalid IP Address Protocol, '{0}'", args[i]);
                            show_help = true;
                            break;
                    }
                    break;
                case "-d":
                case "--domain":
                    domain = args[++i];
                    break;
                case "-h":
                case "--help":
                    show_help = true;
                    break;
                case "-v":
                case "--verbose":
                    verbose = true;
                    break;
            }
        }

        if(show_help) {
            Console.WriteLine("Usage: {0} [-t type] [--resolve] [--publish \"description\"]", app_name);
            Console.WriteLine();
            Console.WriteLine("    -h|--help       shows this help");
            Console.WriteLine("    -v|--verbose    print verbose details of what's happening");
            Console.WriteLine("    -t|--type       uses 'type' as the service type");
            Console.WriteLine("                    (default is '_workstation._tcp')");
            Console.WriteLine("    -r|--resolve    resolve found services to hosts");
            Console.WriteLine("    -d|--domain     which domain to broadcast/listen on");
            Console.WriteLine("    -i|--interface  which network interface index to listen");
            Console.WriteLine("                    on (default is '0', meaning 'all')");
            Console.WriteLine("    -a|--aprotocol  which address protocol to use (Any, IPv4, IPv6)");
            Console.WriteLine("    -p|--publish    publish a service of 'description'");
            Console.WriteLine();
            Console.WriteLine("The -d, -i and -a options are optional. By default {0} will listen", app_name);
            Console.WriteLine("on all network interfaces ('0') on the 'local' domain, and will resolve ");
            Console.WriteLine("all address types, IPv4 and IPv6, as available.");
            Console.WriteLine();
            Console.WriteLine("The service description for publishing has the following syntax.");
            Console.WriteLine("The TXT record is optional.\n");
            Console.WriteLine("    <type> <port> <name> TXT [ <key>='<value>', ... ]\n");
            Console.WriteLine("For example:\n");
            Console.WriteLine("    -p \"_http._tcp 80 Simple Web Server\"");
            Console.WriteLine("    -p \"_daap._tcp 3689 Aaron's Music TXT [ Password='false', \\");
            Console.WriteLine("        Machine Name='Aaron\\'s Box', txtvers='1' ]\"");
            Console.WriteLine();
            return 1;
        }

        IServiceBrowser? browser = null;

        if(services.Count > 0) {
            foreach(string service_description in services) {
                await RegisterService(providerFactory, service_description);
            }
        } else {
            if (verbose) {
                Console.WriteLine ("Creating a ServiceBrowser with the following settings:");
                Console.WriteLine ("  Interface         = {0}", @interface == 0 ? "0 (All)" : @interface.ToString ());
                Console.WriteLine ("  Address Protocol  = {0}", address_protocol);
                Console.WriteLine ("  Domain            = {0}", domain);
                Console.WriteLine ("  Registration Type = {0}", type);
                Console.WriteLine ("  Resolve Shares    = {0}", resolve_shares);
                Console.WriteLine ();
            }

            Console.WriteLine("Hit Ctrl-C when you're bored waiting for responses.");
            Console.WriteLine();

            // Listen for events of some service type
            browser = providerFactory.CreateServiceBrowser();
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
            await browser.Browse(@interface, address_protocol, type, domain);
        }

        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        await endProgram.WaitAsync();

        browser?.Dispose();

        return 0;
    }

    private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        endProgram.Release();
    }

    private static async Task RegisterService(IProviderFactory providerFactory, string serviceDescription)
    {
        var match = Regex.Match(serviceDescription, @"(_[a-z]+\._(?:tcp|udp))\s*(\d+)\s*(.*)");
        if(match.Groups.Count < 4) {
            throw new ApplicationException("Invalid service description syntax");
        }

        string type = match.Groups[1].Value.Trim();
        short port = Convert.ToInt16(match.Groups[2].Value);
        string name = match.Groups[3].Value.Trim();

        int txt_pos = name.IndexOf("TXT", StringComparison.InvariantCulture);
        string? txt_data = null;

        if(txt_pos > 0) {
            txt_data = name.Substring(txt_pos).Trim();
            name = name.Substring(0, txt_pos).Trim();

            if(txt_data == String.Empty) {
                txt_data = null;
            }
        }

        var service = providerFactory.CreateRegisterService();
        service.Name = name;
        service.RegType = type;
        service.ReplyDomain = "local.";
        service.Port = port;

        ITxtRecord? record = null;

        if(txt_data != null) {
            var txtMatch = Regex.Match(txt_data, @"TXT\s*\[(.*)\]");

            if(txtMatch.Groups.Count != 2) {
                throw new ApplicationException("Invalid TXT record definition syntax");
            }

            txt_data = txtMatch.Groups[1].Value;

            foreach(string part in Regex.Split(txt_data, @"'\s*,")) {
                string expr = part.Trim();
                if(!expr.EndsWith("'")) {
                    expr += "'";
                }

                Match keyValueMatch = Regex.Match(expr, @"(\w+\s*\w*)\s*=\s*['](.*)[']\s*");
                string key = keyValueMatch.Groups[1].Value.Trim();
                string val = keyValueMatch.Groups[2].Value.Trim();

                if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val)) {
                    throw new ApplicationException("Invalid key = 'value' syntax for TXT record item");
                }

                record ??= providerFactory.CreateTxtRecord();
                record.Add(key, val);
            }
        }

        if(record != null) {
            service.TxtRecord = record;
        }

        Console.WriteLine("*** Registering name = '{0}', type = '{1}', domain = '{2}'",
            service.Name,
            service.RegType,
            service.ReplyDomain);

        service.Response += OnRegisterServiceResponse;
        await service.Register();
    }

    private static async void OnServiceAdded(object? o, ServiceBrowseEventArgs args)
    {
        Console.WriteLine("*** Found name = '{0}', type = '{1}', domain = '{2}'",
            args.Service.Name,
            args.Service.RegType,
            args.Service.ReplyDomain);

        if (!resolve_shares)
        {
            return;
        }

        Console.WriteLine("resolving shares");
        args.Service.Resolved += OnServiceResolved;
        args.Service.ResolveFailure += OnServiceResolveFailure;
        await args.Service.Resolve();
    }

    private static void OnServiceRemoved(object? o, ServiceBrowseEventArgs args)
    {
        Console.WriteLine("*** Lost  name = '{0}', type = '{1}', domain = '{2}'",
            args.Service.Name,
            args.Service.RegType,
            args.Service.ReplyDomain);

        args.Service.Resolved -= OnServiceResolved;
        args.Service.ResolveFailure += OnServiceResolveFailure;
    }

    private static void OnServiceResolved(object? o, ServiceResolvedEventArgs args)
    {
        if (o is not IResolvableService service)
        {
            return;
        }

        Console.Write ("*** Resolved name = '{0}', host ip = '{1}', hostname = {2}, port = '{3}', " +
            "interface = '{4}', address type = '{5}'",
            service.FullName, service.HostEntry.AddressList[0], service.HostEntry.HostName, service.Port,
            service.NetworkInterface, service.AddressProtocol);

        var record = service.TxtRecord;
        var record_count = record?.Count ?? 0;
        if(record_count > 0) {
            Console.Write(", TXT Record = [");
            for(int i = 0, n = record_count; i < n; i++) {
                var item = record!.GetItemAt(i);
                Console.Write("{0} = '{1}'", item.Key, item.ValueString);
                if(i < n - 1) {
                    Console.Write(", ");
                }
            }
            Console.WriteLine("]");
        } else {
            Console.WriteLine();
        }
    }

    private static void OnServiceResolveFailure(object? sender, string error)
    {
        Console.Write($"Error resolving service: {error}");
    }
    
    private static void OnRegisterServiceResponse(object? o, RegisterServiceEventArgs args)
    {
        switch(args.ServiceError) {
            case ServiceErrorCode.NameConflict:
                Console.WriteLine($"*** Name Collision! '{args.Service?.Name}' is already registered");
                break;
            case ServiceErrorCode.None:
                Console.WriteLine($"*** Registered name = '{args.Service?.Name}'");
                break;
            case ServiceErrorCode.Unknown:
                Console.WriteLine($"*** Error registering name = '{args.Service?.Name}'");
                break;
        }
    }
}