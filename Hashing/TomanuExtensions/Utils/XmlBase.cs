using System.IO;
using System.Xml;

namespace TomanuExtensions.Utils
{
    /// <summary>
    /// Base class suitable for all classes that can be saved and loaded from xml.
    /// </summary>
    public abstract class XmlBase
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlBase()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="a_source"></param>
        public XmlBase(XmlBase a_source)
        {
            MemoryStream ms = new MemoryStream();
            a_source.Save(ms);
            ms.Position = 0;
            Load(ms);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="a_reader"></param>
        public XmlBase(XmlReader a_reader)
        {
            ReadXml(a_reader);
        }

        /// <summary>
        /// Load object from xml file.
        /// </summary>
        /// <param name="a_file_name"></param>
        protected virtual void Load(string a_file_name)
        {
            using (FileStream fs = new FileStream(a_file_name, FileMode.Open, FileAccess.Read))
                Load(fs);
        }

        /// <summary>
        /// Load object from xml stream.
        /// </summary>
        /// <param name="a_stream"></param>
        protected virtual void Load(Stream a_stream)
        {
            XmlReaderExtensions.ReadXml(a_stream, (reader) => ReadXml(reader));
        }

        /// <summary>
        /// Load object from xml.
        /// </summary>
        /// <param name="a_reader"></param>
        protected abstract void ReadXml(XmlReader a_reader);

        /// <summary>
        /// Save object to xml file.
        /// </summary>
        /// <param name="a_file_name"></param>
        public virtual void Save(string a_file_name)
        {
            using (FileStream fs = new FileStream(a_file_name, FileMode.Create))
                Save(fs);
        }

        /// <summary>
        /// Save object to xml stream.
        /// </summary>
        /// <param name="a_stream"></param>
        public virtual void Save(Stream a_stream)
        {
            XmlWriterExtensions.WriteXml(a_stream, (writer) => WriteXml(writer));
        }

        /// <summary>
        /// Save object to xml.
        /// </summary>
        /// <param name="a_writer"></param>
        internal protected abstract void WriteXml(XmlWriter a_writer);

        /// <summary>
        /// Compare objects through xml.
        /// </summary>
        /// <param name="a_obj"></param>
        /// <returns></returns>
        public override bool Equals(object a_obj)
        {
            if (a_obj == null)
                return false;

            XmlBase xml_base = a_obj as XmlBase;
            if (xml_base == null)
                return false;

            MemoryStream ms1 = new MemoryStream();
            xml_base.Save(ms1);

            MemoryStream ms2 = new MemoryStream();
            Save(ms2);

            return ms1.ToArray().AreSame(ms2.ToArray());
        }

        /// <summary>
        /// Calculate hash through xml.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            MemoryStream ms = new MemoryStream();
            Save(ms);
            return ArrayExtensions.GetHashCode(ms.ToArray());
        }
    }
}