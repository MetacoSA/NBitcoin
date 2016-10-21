using System.Diagnostics;
using System.Xml;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class XmlNodeExtensions
    {
        public static XmlNode CreateChildNode(this XmlNode a_node, string a_name)
        {
            var document = (a_node is XmlDocument ? (XmlDocument)a_node : a_node.OwnerDocument);
            XmlNode node = document.CreateElement(a_name);
            a_node.AppendChild(node);
            return node;
        }
    }
}