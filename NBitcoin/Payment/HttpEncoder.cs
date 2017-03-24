//
// Authors:
//   Patrik Torstensson (Patrik.Torstensson@labs2.com)
//   Wictor Wilén (decode/encode functions) (wictor@ibizkit.se)
//   Tim Coleman (tim@timcoleman.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)

//   Marek Habersack <mhabersack@novell.com>
//
// (C) 2005-2010 Novell, Inc (http://novell.com/)
//

using NBitcoin.DataEncoders;
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
#if NET_4_0 && !MOBILE
using System.Web.Configuration;
#endif

namespace System.Web.Util
{
#if NET_4_0
	public
#endif
	internal class HttpEncoder
	{
		static char[] hexChars = "0123456789abcdef".ToCharArray();
		static object entitiesLock = new object();
#if NET_4_0
		static Lazy <HttpEncoder> defaultEncoder;
		static Lazy <HttpEncoder> currentEncoderLazy;
#else
		static HttpEncoder defaultEncoder;
#endif
		static HttpEncoder currentEncoder;

		static HttpEncoder()
		{
#if NET_4_0
			defaultEncoder = new Lazy <HttpEncoder> (() => new HttpEncoder ());
			currentEncoderLazy = new Lazy <HttpEncoder> (new Func <HttpEncoder> (GetCustomEncoderFromConfig));
#else
			defaultEncoder = new HttpEncoder();
			currentEncoder = defaultEncoder;
#endif
		}

		public HttpEncoder()
		{
		}

#if NET_4_0
		protected internal virtual void HtmlAttributeEncode (string value, TextWriter output)
		{

			if (output == null)
				throw new ArgumentNullException ("output");

			if (String.IsNullOrEmpty (value))
				return;

			output.Write (HtmlAttributeEncode (value));
		}

		protected internal virtual void HtmlDecode (string value, TextWriter output)
		{
			if (output == null)
				throw new ArgumentNullException ("output");

			output.Write (HtmlDecode (value));
		}

		protected internal virtual void HtmlEncode (string value, TextWriter output)
		{
			if (output == null)
				throw new ArgumentNullException ("output");

			output.Write (HtmlEncode (value));
		}

		protected internal virtual byte[] UrlEncode (byte[] bytes, int offset, int count)
		{
			return UrlEncodeToBytes (bytes, offset, count);
		}

		static HttpEncoder GetCustomEncoderFromConfig ()
		{
#if MOBILE
			return defaultEncoder.Value;
#else
			var cfg = HttpRuntime.Section;
			string typeName = cfg.EncoderType;

			if (String.Compare (typeName, "System.Web.Util.HttpEncoder", StringComparison.OrdinalIgnoreCase) == 0)
				return Default;
			
			Type t = Type.GetType (typeName, false);
			if (t == null)
				throw new ConfigurationErrorsException (String.Format ("Could not load type '{0}'.", typeName));
			
			if (!typeof (HttpEncoder).IsAssignableFrom (t))
				throw new ConfigurationErrorsException (
					String.Format ("'{0}' is not allowed here because it does not extend class 'System.Web.Util.HttpEncoder'.", typeName)
				);

			return Activator.CreateInstance (t, false) as HttpEncoder;
#endif
		}
#endif

		internal static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");

			int blen = bytes.Length;
			if(blen == 0)
				return new byte[0];

			if(offset < 0 || offset >= blen)
				throw new ArgumentOutOfRangeException("offset");

			if(count < 0 || count > blen - offset)
				throw new ArgumentOutOfRangeException("count");

			MemoryStream result = new MemoryStream(count);
			int end = offset + count;
			for(int i = offset; i < end; i++)
				UrlEncodeChar((char)bytes[i], result, false);

			return result.ToArray();
		}

		internal static bool NotEncoded(char c)
		{
			//query strings are allowed to contain both ? and / characters, see section 3.4 of http://www.ietf.org/rfc/rfc3986.txt, which is basically the spec written by Tim Berners-Lee and friends governing how the web should operate.
			//pchar         = unreserved / pct-encoded / sub-delims / ":" / "@"
			//query         = *( pchar / "/" / "?" )
			return (c == '!' || c == '(' || c == ')' || c == '*' || c == '-' || c == '.' || c == '_' || c == '?' || c == '/' || c == ':'
#if !NET_4_0
 || c == '\''
#endif
);
		}

		internal static void UrlEncodeChar(char c, Stream result, bool isUnicode)
		{
			if(c > 255)
			{
				//FIXME: what happens when there is an internal error?
				//if (!isUnicode)
				//	throw new ArgumentOutOfRangeException ("c", c, "c must be less than 256");
				int idx;
				int i = (int)c;

				result.WriteByte((byte)'%');
				result.WriteByte((byte)'u');
				idx = i >> 12;
				result.WriteByte((byte)hexChars[idx]);
				idx = (i >> 8) & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
				idx = (i >> 4) & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
				idx = i & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
				return;
			}

			if(c > ' ' && NotEncoded(c))
			{
				result.WriteByte((byte)c);
				return;
			}
			if((c < '0') ||
				(c < 'A' && c > '9') ||
				(c > 'Z' && c < 'a') ||
				(c > 'z'))
			{
				if(isUnicode && c > 127)
				{
					result.WriteByte((byte)'%');
					result.WriteByte((byte)'u');
					result.WriteByte((byte)'0');
					result.WriteByte((byte)'0');
				}
				else
					result.WriteByte((byte)'%');

				int idx = ((int)c) >> 4;
				result.WriteByte((byte)hexChars[idx]);
				idx = ((int)c) & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
			}
			else
				result.WriteByte((byte)c);
		}
	}
}