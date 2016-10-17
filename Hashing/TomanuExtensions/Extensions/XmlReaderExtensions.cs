using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Xml;
using TomanuExtensions.Utils;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class XmlReaderExtensions
    {
        public static byte ReadElementContentAsHexByte(this XmlReader a_reader, string a_name)
        {
            return Hex.HexToByte(a_reader.ReadElementString(a_name));
        }

        public static ushort ReadElementContentAsHexUShort(this XmlReader a_reader, string a_name)
        {
            return Hex.HexToUShort(a_reader.ReadElementString(a_name));
        }

        public static bool ReadElementContentAsBoolean(this XmlReader a_reader, string a_name)
        {
            return Boolean.Parse(a_reader.ReadElementString(a_name));
        }

        public static string ReadElementContentAsString(this XmlReader a_reader, string a_name)
        {
            return a_reader.ReadElementContentAsString(a_name, "");
        }

        public static double ReadElementContentAsDouble(this XmlReader a_reader, string a_name)
        {
            return Double.Parse(a_reader.ReadElementString(a_name.Replace(',', '.')),
                CultureInfo.InvariantCulture);
        }

        public static uint ReadElementContentAsHexUInt(this XmlReader a_reader, string a_name)
        {
            return Hex.HexToUInt(a_reader.ReadElementString(a_name));
        }

        public static byte ReadElementContentAsByte(this XmlReader a_reader, string a_name)
        {
            return Byte.Parse(a_reader.ReadElementString(a_name));
        }

        public static ushort ReadElementContentAsUShort(this XmlReader a_reader, string a_name)
        {
            return UInt16.Parse(a_reader.ReadElementString(a_name));
        }

        public static uint ReadElementContentAsUInt(this XmlReader a_reader, string a_name)
        {
            return UInt32.Parse(a_reader.ReadElementString(a_name));
        }

        public static Size ReadElementContentAsSize(this XmlReader a_reader, string a_name)
        {
            Size size = new Size(
                a_reader.GetAttributeInt("Width"),
                a_reader.GetAttributeInt("Height"));
            a_reader.MoveToNextElement(a_name);
            return size;
        }

        public static Rectangle ReadElementContentAsRectangle(this XmlReader a_reader, string a_name)
        {
            Rectangle rect = new Rectangle(
                a_reader.GetAttributeInt("Left"),
                a_reader.GetAttributeInt("Top"),
                a_reader.GetAttributeInt("Width"),
                a_reader.GetAttributeInt("Height"));
            a_reader.MoveToNextElement(a_name);
            return rect;
        }

        public static ulong ReadElementContentAsULong(this XmlReader a_reader, string a_name)
        {
            return UInt64.Parse(a_reader.ReadElementString(a_name));
        }

        public static int ReadElementContentAsInt(this XmlReader a_reader, string a_name)
        {
            return Int32.Parse(a_reader.ReadElementString(a_name));
        }

        public static Guid ReadElementContentAsGuid(this XmlReader a_reader, string a_name)
        {
            return Guid.Parse(a_reader.ReadElementString(a_name));
        }

        public static T ReadElementContentAsEnum<T>(this XmlReader a_reader, string a_name)
        {
            return (T)Enum.Parse(typeof(T), a_reader.ReadElementString(a_name).Replace(" ", ", "));
        }

        public static string GetAttributeDef(this XmlReader a_reader, string a_name,
            string a_default = "")
        {
            if (a_reader.MoveToAttribute(a_name))
                return a_reader.GetAttribute(a_name);
            else
                return a_default;
        }

        public static byte GetAttributeHexByte(this XmlReader a_reader, string a_name)
        {
            return Hex.HexToByte(a_reader.GetAttribute(a_name));
        }

        public static ushort GetAttributeHexUShort(this XmlReader a_reader, string a_name)
        {
            return Hex.HexToUShort(a_reader.GetAttribute(a_name));
        }

        public static uint GetAttributeHexUInt(this XmlReader a_reader, string a_name)
        {
            return Hex.HexToUInt(a_reader.GetAttribute(a_name));
        }

        public static byte GetAttributeByte(this XmlReader a_reader, string a_name)
        {
            return Byte.Parse(a_reader.GetAttribute(a_name));
        }

        public static ushort GetAttributeUShort(this XmlReader a_reader, string a_name)
        {
            return UInt16.Parse(a_reader.GetAttribute(a_name));
        }

        public static uint GetAttributeUInt(this XmlReader a_reader, string a_name)
        {
            return UInt32.Parse(a_reader.GetAttribute(a_name));
        }

        public static ulong GetAttributeULong(this XmlReader a_reader, string a_name)
        {
            return UInt64.Parse(a_reader.GetAttribute(a_name));
        }

        public static int GetAttributeInt(this XmlReader a_reader, string a_name)
        {
            return Int32.Parse(a_reader.GetAttribute(a_name));
        }

        public static int GetAttributeIntDef(this XmlReader a_reader, string a_name, int a_default = 0)
        {
            if (a_reader.MoveToAttribute(a_name))
                return Int32.Parse(a_reader.GetAttribute(a_name));
            else
                return a_default;
        }

        public static bool GetAttributeBool(this XmlReader a_reader, string a_name)
        {
            return Boolean.Parse(a_reader.GetAttribute(a_name));
        }

        public static bool GetAttributeBoolDef(this XmlReader a_reader, string a_name,
            bool a_default = false)
        {
            if (a_reader.MoveToAttribute(a_name))
                return Boolean.Parse(a_reader.GetAttribute(a_name));
            else
                return a_default;
        }

        public static Guid GetAttributeGuid(this XmlReader a_reader, string a_name)
        {
            return Guid.Parse(a_reader.GetAttribute(a_name));
        }

        public static T GetAttributeEnum<T>(this XmlReader a_reader, string a_name)
        {
            return (T)Enum.Parse(typeof(T), a_reader.GetAttribute(a_name).Replace(" ", ", "));
        }

        /// <summary>
        /// Use to start read xml.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_type"></param>
        /// <param name="a_stream"></param>
        /// <param name="a_read_func"></param>
        public static void ReadXml(Stream a_stream, Action<XmlReader> a_read_func)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            using (XmlReader reader = XmlReader.Create(a_stream, settings))
            {
                reader.Read();

                if (reader.NodeType == XmlNodeType.XmlDeclaration)
                    reader.Skip();

                a_read_func(reader);
            }
        }

        /// <summary>
        /// Move to next element. Current element must be empty.
        /// </summary>
        /// <param name="a_reader"></param>
        /// <param name="a_name"></param>
        public static void MoveToNextElement(this XmlReader a_reader, string a_name)
        {
            if (a_reader.NodeType == XmlNodeType.Attribute)
                a_reader.MoveToElement();

            if (a_reader.IsEmptyElement)
                a_reader.ReadStartElement(a_name);
            else
            {
                a_reader.ReadStartElement(a_name);

                if (a_reader.IsStartElement())
                    throw new XmlException();

                a_reader.ReadEndElement();
            }
        }
    }
}