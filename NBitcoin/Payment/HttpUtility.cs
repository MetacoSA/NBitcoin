// 
// System.Web.HttpUtility
//
// Authors:
//   Patrik Torstensson (Patrik.Torstensson@labs2.com)
//   Wictor Wilén (decode/encode functions) (wictor@ibizkit.se)
//   Tim Coleman (tim@timcoleman.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2005-2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Web.Util;
using NBitcoin.DataEncoders;

namespace System.Web.NBitcoin
{


	public static class HttpUtility
	{

		#region Methods
		public static string UrlDecode(string str)
		{
			return UrlDecode(str, Encoding.UTF8);
		}

		static void WriteCharBytes(IList buf, char ch, Encoding e)
		{
			if (ch > 255)
			{
				foreach (byte b in e.GetBytes(new char[] { ch }))
					buf.Add(b);
			}
			else
				buf.Add((byte)ch);
		}

		public static string UrlDecode(string s, Encoding e)
		{
			if (null == s)
				return null;

			if (s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
				return s;

			if (e == null)
				e = Encoding.UTF8;

			long len = s.Length;
			var bytes = new List<byte>();
			int xchar;
			char ch;

			for (int i = 0; i < len; i++)
			{
				ch = s[i];
				if (ch == '%' && i + 2 < len && s[i + 1] != '%')
				{
					if (s[i + 1] == 'u' && i + 5 < len)
					{
						// unicode hex sequence
						xchar = GetChar(s, i + 2, 4);
						if (xchar != -1)
						{
							WriteCharBytes(bytes, (char)xchar, e);
							i += 5;
						}
						else
							WriteCharBytes(bytes, '%', e);
					}
					else if ((xchar = GetChar(s, i + 1, 2)) != -1)
					{
						WriteCharBytes(bytes, (char)xchar, e);
						i += 2;
					}
					else
					{
						WriteCharBytes(bytes, '%', e);
					}
					continue;
				}

				if (ch == '+')
					WriteCharBytes(bytes, ' ', e);
				else
					WriteCharBytes(bytes, ch, e);
			}

			byte[] buf = bytes.ToArray();
			bytes = null;
			return e.GetString(buf, 0, buf.Length);

		}

		static int GetInt(byte b)
		{
			char c = (char)b;
			if (c >= '0' && c <= '9')
				return c - '0';

			if (c >= 'a' && c <= 'f')
				return c - 'a' + 10;

			if (c >= 'A' && c <= 'F')
				return c - 'A' + 10;

			return -1;
		}

		static int GetChar(string str, int offset, int length)
		{
			int val = 0;
			int end = length + offset;
			for (int i = offset; i < end; i++)
			{
				char c = str[i];
				if (c > 127)
					return -1;

				int current = GetInt((byte)c);
				if (current == -1)
					return -1;
				val = (val << 4) + current;
			}

			return val;
		}

		public static string UrlEncode(string str)
		{
			return UrlEncode(str, Encoding.UTF8);
		}

		public static string UrlEncode(string s, Encoding Enc)
		{
			if (s == null)
				return null;

			if (s == String.Empty)
				return String.Empty;

			bool needEncode = false;
			int len = s.Length;
			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				if ((c < '0') || (c < 'A' && c > '9') || (c > 'Z' && c < 'a') || (c > 'z'))
				{
					if (HttpEncoder.NotEncoded(c))
						continue;

					needEncode = true;
					break;
				}
			}

			if (!needEncode)
				return s;

			// avoided GetByteCount call
			byte[] bytes = new byte[Enc.GetMaxByteCount(s.Length)];
			int realLen = Enc.GetBytes(s, 0, s.Length, bytes, 0);
			return Encoders.ASCII.EncodeData(UrlEncodeToBytes(bytes, 0, realLen));
		}

		public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
		{
			if (bytes == null)
				return null;
#if NET_4_0
			return HttpEncoder.Current.UrlEncode (bytes, offset, count);
#else
			return HttpEncoder.UrlEncodeToBytes(bytes, offset, count);
#endif
		}

#if NET_4_0
		public static string HtmlEncode (object value)
		{
			if (value == null)
				return null;

#if !MOBILE
			IHtmlString htmlString = value as IHtmlString;
			if (htmlString != null)
				return htmlString.ToHtmlString ();
#endif

			return HtmlEncode (value.ToString ());
		}

		public static string JavaScriptStringEncode (string value)
		{
			return JavaScriptStringEncode (value, false);
		}

		public static string JavaScriptStringEncode (string value, bool addDoubleQuotes)
		{
			if (String.IsNullOrEmpty (value))
				return addDoubleQuotes ? "\"\"" : String.Empty;

			int len = value.Length;
			bool needEncode = false;
			char c;
			for (int i = 0; i < len; i++)
			{
				c = value [i];

				if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)
				{
					needEncode = true;
					break;
				}
			}

			if (!needEncode)
				return addDoubleQuotes ? "\"" + value + "\"" : value;

			var sb = new StringBuilder ();
			if (addDoubleQuotes)
				sb.Append ('"');

			for (int i = 0; i < len; i++)
			{
				c = value [i];
				if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 39 || c == 60 || c == 62)
					sb.AppendFormat ("\\u{0:x4}", (int)c);
				else switch ((int)c)
				{
						case 8:
							sb.Append ("\\b");
							break;

						case 9:
							sb.Append ("\\t");
							break;

						case 10:
							sb.Append ("\\n");
							break;

						case 12:
							sb.Append ("\\f");
							break;

						case 13:
							sb.Append ("\\r");
							break;

						case 34:
							sb.Append ("\\\"");
							break;

						case 92:
							sb.Append ("\\\\");
							break;

						default:
							sb.Append (c);
							break;
					}
			}

			if (addDoubleQuotes)
				sb.Append ('"');

			return sb.ToString ();
		}
#endif

		#endregion // Methods
	}
}
