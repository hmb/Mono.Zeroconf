//
// ProviderFactory.cs
//
// Authors:
//    Aaron Bockover    <abockover@novell.com>
//    Holger Böhnke     <zeroconf@biz.amarin.de>
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
// Copyright (C) 2022 Holger Böhnke, (http://www.amarin.de)
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

namespace Mono.Zeroconf.Providers.DynamicLoad;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Zeroconf.Providers.Abstraction;

public class ProviderFactory : IProviderFactory
{
    private IZeroconfProviderObjectTypes[]? providerObjectTypes;
    private IZeroconfProviderObjectTypes? selectedProviderObjectTypes;

    private IZeroconfProviderObjectTypes DefaultProviderObjectTypes
    {
        get
        {
            if (this.providerObjectTypes == null || this.providerObjectTypes.Length == 0)
            {
                throw new Exception(
                    "No Zeroconf providers could be found or initialized. Necessary daemon may not be running.");
            }

            return this.providerObjectTypes[0];
        }
    }

    private IZeroconfProviderObjectTypes SelectedProviderObjectTypes
    {
        get => this.selectedProviderObjectTypes ?? this.DefaultProviderObjectTypes;
        set => this.selectedProviderObjectTypes = value;
    }

    public Task StartAsync()
    {
        this.providerObjectTypes = LoadProvidersFromFilesystem();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    public IServiceBrowser CreateServiceBrowser()
    {
        var browser = (IServiceBrowser?)Activator.CreateInstance(this.SelectedProviderObjectTypes.ServiceBrowser);
        
        if (browser == null)
        {
            throw new ProviderObjectCreateException("The ServiceBrowser could not be created");
        }

        return browser;
    }
    
    public IRegisterService CreateRegisterService()
    {
        var registerService = (IRegisterService?)Activator.CreateInstance(this.SelectedProviderObjectTypes.RegisterService);
        
        if (registerService == null)
        {
            throw new ProviderObjectCreateException("The RegisterService could not be created");
        }

        return registerService;
    }

    public ITxtRecord CreateTxtRecord()
    {
        var txtRecord = (ITxtRecord?)Activator.CreateInstance(this.SelectedProviderObjectTypes.TxtRecord);
        
        if (txtRecord == null)
        {
            throw new ProviderObjectCreateException("The TxtRecord could not be created");
        }

        return txtRecord;
    }

    private static IZeroconfProviderObjectTypes[] LoadProvidersFromFilesystem()
    {
        var envPath = Environment.GetEnvironmentVariable("MONO_ZEROCONF_PROVIDERS");
        var directories = new List<string>();
        
        if (!string.IsNullOrEmpty(envPath))
        {
            directories.AddRange(envPath.Split(':').Where(Directory.Exists));
        }

        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        
        directories.Add(Path.GetDirectoryName(assemblyPath) ?? Directory.GetCurrentDirectory());

        var providerList = new List<IZeroconfProviderObjectTypes>();
        
        foreach (var directory in directories)
        {
            foreach (var fileName in Directory.GetFiles(directory, "Mono.Zeroconf.Providers.*.dll"))
            {
                if (Path.GetFileName(fileName) == Path.GetFileName(assemblyPath))
                {
                    // Console.WriteLine($"provider {fileName} is own assembly");
                    continue;
                }
                
                Console.WriteLine($"load provider {fileName}");

                var providerAssembly = Assembly.LoadFile(fileName);

                if (providerAssembly.GetCustomAttributes(false).FirstOrDefault(a => a is ZeroconfProviderAttribute) is
                    not ZeroconfProviderAttribute attribute)
                {
                    // Console.WriteLine($"provider has no ZeroconfProviderAttribute {fileName}");
                    continue;
                }
                
                Console.WriteLine($"create instance {attribute.ZeroconfProviderObjectTypes}");
                var provider = (IZeroconfProviderObjectTypes?)Activator.CreateInstance(attribute.ZeroconfProviderObjectTypes);

                if (provider == null)
                {
                    // Console.WriteLine($"create instance failed");
                    continue;
                }
                            
                try
                {
                    // Console.WriteLine($"init provider {provider}");
                    provider.Initialize();
                    providerList.Add(provider);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        if (providerList.Count == 0)
        {
            throw new Exception(
                "No Zeroconf providers could be found or initialized. Necessary daemon may not be running.");
        }

        return providerList.ToArray();
    }
}