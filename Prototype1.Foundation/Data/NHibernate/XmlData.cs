using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Prototype1.Foundation.Data.NHibernate
{
    [Serializable]
        public class XmlData
        {
            private string _stringData = null;
            XmlDocument _doc = null;
            private XmlElement _xmlData = null;
            private XmlNamespaceManager _nsmgr = null;

            public XmlData()
            {
            }

            public XmlData(XmlNamespaceManager nsmgr)
            {
                _nsmgr = nsmgr;
            }

            [XmlIgnore]
            public string String
            {
                get
                {
                    if (_stringData == null)
                        XmlToString();
                    return _stringData;
                }
                set
                {
                    _stringData = value;
                    _xmlData = null;
                    Unsubscribe();
                }
            }

            //[XmlAnyElement, SkipNavigableRoot]
            [XmlAnyElement]
            public XmlElement Xml
            {
                get
                {
                    if (_xmlData == null)
                        StringToXml();
                    return _xmlData;
                }
            }

            [XmlIgnore]
            public XmlDocument Doc
            {
                get { return _doc; }
            }

            public void StringToXml()
            {
                // Unsubscribe from events
                Unsubscribe();

                // Create new document
                if (_nsmgr != null)
                    _doc = new XmlDocument(_nsmgr.NameTable);
                else
                    _doc = new XmlDocument();

                _xmlData = _doc.CreateElement("xml");

                if (_stringData != null && _stringData != string.Empty)
                {
                    // Load XML from string
                    XmlParserContext context = new XmlParserContext(null, _nsmgr, null, XmlSpace.Default);
                    XmlTextReader reader = new XmlTextReader(_stringData, XmlNodeType.Element, context);
                    do
                    {
                        XmlNode nextChild = _doc.ReadNode(reader);
                        if (nextChild != null)
                            _xmlData.AppendChild(nextChild);
                        else
                            break;
                    }
                    while (true);
                }

                // Subscibe document change events
                Subscribe();
            }

            public void XmlToString()
            {
                if (_xmlData != null)
                {
                    StringWriter sw = new StringWriter();
                    SkipNsXmlTextWriter xtw = new SkipNsXmlTextWriter(sw, _nsmgr);
                    _xmlData.WriteContentTo(xtw);
                    xtw.Close();
                    _stringData = sw.ToString();
                }
                else
                    _stringData = string.Empty;
            }

            [XmlIgnore]
            public XmlNamespaceManager NamespaceManager
            {
                get { return _nsmgr; }
                set { _nsmgr = value; }
            }

            public string ValuleOf(string xpath)
            {
                return ValuleOf(xpath, NamespaceManager, "");
            }

            public string ValuleOf(string xpath, XmlNamespaceManager nsmgr)
            {
                return ValuleOf(xpath, nsmgr, "");
            }

            protected string ValuleOf(string xpath, XmlNamespaceManager nsmgr, string defaultValue)
            {
                if (Xml.FirstChild == null)
                    return defaultValue;

                XmlNode node = Xml.SelectSingleNode(xpath, nsmgr);

                if (node == null)
                    return defaultValue;

                if (node is XmlElement)
                    return node.InnerText;

                if (node is XmlAttribute)
                    return node.Value;


                return node.Value;
            }

            public static explicit operator string(XmlData data)
            {
                return data.String;
            }

            public static explicit operator XmlElement(XmlData data)
            {
                return data.Xml;
            }


            private void Subscribe()
            {
                _doc.NodeChanged += new XmlNodeChangedEventHandler(OnDocumentChange);
                _doc.NodeInserted += new XmlNodeChangedEventHandler(OnDocumentChange);
                _doc.NodeRemoved += new XmlNodeChangedEventHandler(OnDocumentChange);
            }

            private void Unsubscribe()
            {
                if (_doc != null)
                {
                    _doc.NodeChanged -= new XmlNodeChangedEventHandler(OnDocumentChange);
                    _doc.NodeInserted -= new XmlNodeChangedEventHandler(OnDocumentChange);
                    _doc.NodeRemoved -= new XmlNodeChangedEventHandler(OnDocumentChange);
                }
            }

            private void OnDocumentChange(object sender, XmlNodeChangedEventArgs e)
            {
                string json = string.Empty;
                _stringData = null;
            }
        }
}
