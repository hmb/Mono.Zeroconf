//
// TxtRecord.cs
//
// Author:
//    Aaron Bockover    <abockover@novell.com>
//    Holger Böhnke     <zeroconf@biz.amarin.de>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Mono.Zeroconf.Providers.Avahi;


public class TxtRecord : ITxtRecord
{
    private readonly List<TxtRecordItem> recordItems = new();

    // ReSharper disable once ParameterTypeCanBeEnumerable.Local
    public TxtRecord(byte[][] data)
    {
        foreach (var rawItem in data)
        {
            var itemRegex = new Regex(@"""[^""]*""|[^,]+", RegexOptions.IgnorePatternWhitespace);
            
            foreach (Match itemMatch in itemRegex.Matches(Encoding.UTF8.GetString(rawItem)))
            {
                var item = itemMatch.Groups[0].Value;
                var splitItem = item.Split(new[] { '=' }, 2);

                this.Add(splitItem[0], splitItem.Length == 1 ? string.Empty : splitItem[1]);
            }
        }
    }

    public void Dispose()
    {
    }

    public int Count => this.recordItems.Count;
    public ITxtRecord BaseRecord => this;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public IEnumerator<TxtRecordItem> GetEnumerator()
    {
        return this.recordItems.GetEnumerator();
    }

    public void Add(string key, string value)
    {
        this.recordItems.Add(new TxtRecordItem(key, value));
    }

    public void Add(string key, byte[] value)
    {
        this.recordItems.Add(new TxtRecordItem(key, value));
    }

    public void Add(TxtRecordItem item)
    {
        this.recordItems.Add(item);
    }

    public void Remove(string key)
    {
        var item = this.recordItems.FirstOrDefault(item => item.Key == key);
        if (item != null)
        {
            this.recordItems.Remove(item);
        }
    }

    public TxtRecordItem GetItemAt(int index)
    {
        return this.recordItems[index];
    }

    public TxtRecordItem? FirstOrDefault(string key)
    {
        return this.recordItems.FirstOrDefault(item => item.Key == key);
    }

    internal static byte[][] Render(ITxtRecord record)
    {
        var items = new byte[record.Count][];
        var index = 0;

        foreach (TxtRecordItem item in record)
        {
            var txt = $"{item.Key}={item.ValueString}";
            items[index++] = Encoding.UTF8.GetBytes(txt);
        }

        return items;
    }
}