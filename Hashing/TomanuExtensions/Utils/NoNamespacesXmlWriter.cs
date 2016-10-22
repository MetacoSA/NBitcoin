using System.Xml;

namespace TomanuExtensions.Utils
{
    public class NoNamespacesXmlWriter : XmlWriter
    {
        private XmlWriter m_writer;

        public NoNamespacesXmlWriter(XmlWriter a_writer)
        {
            if (a_writer is NoNamespacesXmlWriter)
                m_writer = (a_writer as NoNamespacesXmlWriter).m_writer;
            else
                m_writer = a_writer;
        }

        public override void Close()
        {
            m_writer.Close();
        }

        public override void Flush()
        {
            m_writer.Flush();
        }

        public override string LookupPrefix(string a_ns)
        {
            if (a_ns == "http://www.w3.org/2001/XMLSchema")
                return "";
            else if (a_ns == "http://www.w3.org/2001/XMLSchema-instance")
                return "";

            return m_writer.LookupPrefix(a_ns);
        }

        public override void WriteBase64(byte[] a_buffer, int a_index, int a_count)
        {
            m_writer.WriteBase64(a_buffer, a_index, a_count);
        }

        public override void WriteCData(string a_text)
        {
            m_writer.WriteCData(a_text);
        }

        public override void WriteCharEntity(char a_ch)
        {
            m_writer.WriteCharEntity(a_ch);
        }

        public override void WriteChars(char[] a_buffer, int a_index, int a_count)
        {
            m_writer.WriteChars(a_buffer, a_index, a_count);
        }

        public override void WriteComment(string a_text)
        {
            m_writer.WriteComment(a_text);
        }

        public override void WriteDocType(string a_name, string a_pubid,
            string a_sysid, string a_subset)
        {
            m_writer.WriteDocType(a_name, a_pubid, a_sysid, a_subset);
        }

        public override void WriteEndAttribute()
        {
            m_writer.WriteEndAttribute();
        }

        public override void WriteEndDocument()
        {
            m_writer.WriteEndDocument();
        }

        public override void WriteEndElement()
        {
            m_writer.WriteEndElement();
        }

        public override void WriteEntityRef(string a_name)
        {
            m_writer.WriteEntityRef(a_name);
        }

        public override void WriteFullEndElement()
        {
            m_writer.WriteFullEndElement();
        }

        public override void WriteProcessingInstruction(string a_name, string a_text)
        {
            m_writer.WriteProcessingInstruction(a_name, a_text);
        }

        public override void WriteRaw(string a_data)
        {
            m_writer.WriteRaw(a_data);
        }

        public override void WriteRaw(char[] a_buffer, int a_index, int a_count)
        {
            m_writer.WriteRaw(a_buffer, a_index, a_count);
        }

        public override void WriteStartAttribute(string a_prefix, string a_localName, string a_ns)
        {
            m_writer.WriteStartAttribute(a_prefix, a_localName, a_ns);
        }

        public override void WriteStartDocument(bool a_standalone)
        {
            m_writer.WriteStartDocument(a_standalone);
        }

        public override void WriteStartDocument()
        {
            m_writer.WriteStartDocument();
        }

        public override void WriteStartElement(string a_prefix, string a_local_name, string a_ns)
        {
            m_writer.WriteStartElement(a_prefix, a_local_name, a_ns);
        }

        public override WriteState WriteState
        {
            get
            {
                return m_writer.WriteState;
            }
        }

        public override void WriteString(string a_text)
        {
            m_writer.WriteString(a_text);
        }

        public override void WriteSurrogateCharEntity(char a_lowChar, char a_highChar)
        {
            m_writer.WriteSurrogateCharEntity(a_lowChar, a_highChar);
        }

        public override void WriteWhitespace(string a_ws)
        {
            m_writer.WriteWhitespace(a_ws);
        }

        public override bool Equals(object a_obj)
        {
            return m_writer.Equals(a_obj);
        }

        public override int GetHashCode()
        {
            return m_writer.GetHashCode();
        }

        public override string ToString()
        {
            return m_writer.ToString();
        }

        public override string XmlLang
        {
            get
            {
                return m_writer.XmlLang;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                return m_writer.XmlSpace;
            }
        }

        public override XmlWriterSettings Settings
        {
            get
            {
                return m_writer.Settings;
            }
        }
    }
}