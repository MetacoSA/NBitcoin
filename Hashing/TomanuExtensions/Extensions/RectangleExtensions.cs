using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class RectangleExtensions
    {
        public static Rectangle Scale(this Rectangle a_rect, int a_ratio)
        {
            return new Rectangle(a_rect.Left * a_ratio, a_rect.Top * a_ratio,
                a_rect.Width * a_ratio, a_rect.Height * a_ratio);
        }

        /// <summary>
        /// Round mode: Math.Round()
        /// </summary>
        /// <param name="a_rect"></param>
        /// <param name="a_ratio"></param>
        /// <returns></returns>
        public static Rectangle Scale(this Rectangle a_rect, double a_ratio)
        {
             return Rectangle.FromLTRB(
               (a_rect.Left * a_ratio).Round(),
               (a_rect.Top * a_ratio).Round(),
               (a_rect.Right * a_ratio).Round(),
               (a_rect.Bottom * a_ratio).Round());
        }

        public static IEnumerable<Point> EnumPixels(this Rectangle a_rect)
        {
            for (int y = a_rect.Top; y < a_rect.Bottom; y++)
            {
                for (int x = a_rect.Left; x < a_rect.Right; x++)
                {
                    yield return new Point(x, y);
                }
            }
        }

        public static XElement GetAsXml(this Rectangle a_rect, string a_name)
        {
            return new XElement(a_name,
                new XElement("Left", a_rect.Left),
                new XElement("Top", a_rect.Top),
                new XElement("Width", a_rect.Width),
                new XElement("Height", a_rect.Height));
        }

        public static Rectangle FromXml(XElement a_element)
        {
            return new Rectangle(
                a_element.Element("Left").Value.ToInt(),
                a_element.Element("Top").Value.ToInt(),
                a_element.Element("Width").Value.ToInt(),
                a_element.Element("Height").Value.ToInt());
        }
    }
}