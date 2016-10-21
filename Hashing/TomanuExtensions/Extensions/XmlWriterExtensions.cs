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
    public static class XmlWriterExtensions
    {
        public static void WriteHexElement(this XmlWriter a_writer, string a_name, byte a_byte)
        {
            a_writer.WriteElementString(a_name, Hex.ByteToHex(a_byte));
        }

        public static void WriteHexElement(this XmlWriter a_writer, string a_name, ushort a_byte)
        {
            a_writer.WriteElementString(a_name, Hex.UShortToHex(a_byte));
        }

        public static void WriteHexElement(this XmlWriter a_writer, string a_name, uint a_byte)
        {
            a_writer.WriteElementString(a_name, Hex.UIntToHex(a_byte));
        }

        public static void WriteElement<T>(this XmlWriter a_writer, string a_name, T a_obj)
        {
            a_writer.WriteElementString(a_name, a_obj.ToString());
        }

        public static void WriteElement(this XmlWriter a_writer, string a_name, double a_value)
        {
            a_writer.WriteElementString(a_name, a_value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteElementSize(this XmlWriter a_writer, string a_name, Size a_size)
        {
            a_writer.WriteStartElement(a_name);
            a_writer.WriteAttribute("Width", a_size.Width);
            a_writer.WriteAttribute("Height", a_size.Height);
            a_writer.WriteEndElement();
        }

        public static void WriteElementRectangle(this XmlWriter a_writer, string a_name,
            Rectangle a_rect)
        {
            a_writer.WriteStartElement(a_name);
            a_writer.WriteAttribute("Left", a_rect.Left);
            a_writer.WriteAttribute("Top", a_rect.Top);
            a_writer.WriteAttribute("Width", a_rect.Width);
            a_writer.WriteAttribute("Height", a_rect.Height);
            a_writer.WriteEndElement();
        }

        public static void WriteElementEnum<T>(this XmlWriter a_writer, string a_name, Enum a_enum)
        {
            a_writer.WriteElementString(a_name, a_enum.ToString().Replace(", ", " "));
        }

        public static void WriteHexAttribute(this XmlWriter a_writer, string a_name, byte a_byte)
        {
            a_writer.WriteAttributeString(a_name, Hex.ByteToHex(a_byte));
        }

        public static void WriteHexAttribute(this XmlWriter a_writer, string a_name, ushort a_byte)
        {
            a_writer.WriteAttributeString(a_name, Hex.UShortToHex(a_byte));
        }

        public static void WriteHexAttribute(this XmlWriter a_writer, string a_name, uint a_byte)
        {
            a_writer.WriteAttributeString(a_name, Hex.UIntToHex(a_byte));
        }

        public static void WriteAttribute<T>(this XmlWriter a_writer, string a_name, T a_obj)
        {
            a_writer.WriteAttributeString(a_name, a_obj.ToString());
        }

        public static void WriteAttributeEnum<T>(this XmlWriter a_writer, string a_name, Enum a_enum)
        {
            a_writer.WriteAttributeString(a_name, a_enum.ToString().Replace(", ", " "));
        }

        /// <summary>
        /// Helper for xml writing.
        /// </summary>
        /// <param name="a_stream"></param>
        /// <param name="a_write_func"></param>
        public static void WriteXml(Stream a_stream, Action<XmlWriter> a_write_func)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter writer = new NoNamespacesXmlWriter(XmlWriter.Create(a_stream, settings)))
            {
                writer.WriteStartDocument();
                a_write_func(writer);
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Helper for xml writing.
        /// </summary>
        /// <param name="a_file_name"></param>
        /// <param name="a_write_func"></param>
        public static void WriteXml(string a_file_name, Action<XmlWriter> a_write_func)
        {
            using (var fs = new FileStream(a_file_name, FileMode.Create))
                WriteXml(fs, a_write_func);
        }
    }
}