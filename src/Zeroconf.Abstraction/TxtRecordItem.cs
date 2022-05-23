//
// TxtRecordItem.cs
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

namespace Zeroconf.Abstraction;

using System.Text;

// TODO create an interface for this as well, perhaps move into core lib
public class TxtRecordItem
{
    public TxtRecordItem(string key, byte [] valueRaw)
    {
        this.Key = key;
        this.ValueRaw = valueRaw;
        this.ValueString = Encoding.UTF8.GetString(this.ValueRaw);
    }
        
    public TxtRecordItem(string key, string valueString)
    {
        this.Key = key;
        this.ValueString = valueString;
        this.ValueRaw = Encoding.UTF8.GetBytes(valueString);
    }
        
    public string Key { get; }
    public byte[] ValueRaw { get; }
    public string ValueString { get; }
    
    public override string ToString()
    {
        return $"{this.Key} = {this.ValueString}";
    }
}