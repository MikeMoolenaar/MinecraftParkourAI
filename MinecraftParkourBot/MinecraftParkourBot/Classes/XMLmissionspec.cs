using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MinecraftBot
{
    public class XMLmissionspec
    {
        public XmlDocument xmlDoc = new XmlDocument();
        public XMLmissionspec(string xmlpath)
        {
            xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(System.IO.File.ReadAllText(xmlpath));
        }

        public override string ToString()
        {
            return xmlDoc.InnerXml;
        }

        public void ChangeSetting(string node, string attribute, string value)
        {
            var xmlnodelist = xmlDoc.GetElementsByTagName(node);
            if (xmlnodelist.Count > 0)
            {
                if (xmlnodelist[0].Attributes[attribute] != null)
                {
                    xmlnodelist[0].Attributes[attribute].Value = value;
                }
                else
                {
                    XmlAttribute xmlAttr = xmlDoc.CreateAttribute(attribute);
                    xmlAttr.Value = value;
                    xmlnodelist[0].Attributes.Append(xmlAttr);
                }

            }
        }

        public void ChangeSetting(string node, string attribute, bool value)
        {
            ChangeSetting(node, attribute, value.ToString().ToLower());
        }
    }
}
