using System.Xml;
using System.IO;

namespace Prototype1.Foundation.Data.NHibernate
{
    /// <summary>
    /// Writes XML to the given <see cref="TextWriter"/>, omitting declarations 
    /// of namespaces presented in specified <see cref="XmlNamespaceManager"/>.
    /// </summary>
    public class SkipNsXmlTextWriter : XmlTextWriter
    {
        private XmlNamespaceManager _nsmgr;
        private bool _catchNs;
        private string _prefix;
        private string _ns;
        private string _forbiddenNs;

        public SkipNsXmlTextWriter(TextWriter writer, XmlNamespaceManager nsmgr) :
            base(writer)
        {
            _nsmgr = nsmgr;
            _catchNs = false;
            _prefix = null;
            _ns = null;
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            if (_nsmgr != null)
            {
                if (prefix == "xmlns")
                {
                    if (localName != string.Empty && localName != null)
                    {
                        _forbiddenNs = _nsmgr.LookupNamespace(localName);
                        if (_forbiddenNs != null)
                        {
                            _catchNs = true;
                            _prefix = localName;
                            _ns = null;
                            return;
                        }
                    }
                }
                else if ((prefix == null || prefix == string.Empty) && localName == "xmlns")
                {
                    _catchNs = true;
                    _prefix = null;
                    _ns = null;
                    return;
                }
            }
            base.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            if (_catchNs)
            {
                _catchNs = false;
                _ns = _nsmgr.NameTable.Get(_ns);
                if (_prefix != null)
                {
                    if (_forbiddenNs == _ns)
                        // If xmlns:ns="the-ns" where namespace is given
                        return;
                }
                else if (_nsmgr.LookupPrefix(_ns) != null)
                    // If xmlns="the-ns" whrer namespace is given
                    return;

                base.WriteStartAttribute("xmlns", _prefix, "http://www.w3.org/2000/xmlns/");
                base.WriteString(_ns);
            }
            base.WriteEndAttribute();
        }

        public override void WriteString(string text)
        {
            if (_catchNs)
            {
                if (_ns == null)
                    _ns = text;
                else
                    _ns += text;
                return;
            }
            base.WriteString(text);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (_nsmgr != null && ns != string.Empty && ns != null)
            {
                if (prefix != string.Empty && prefix != null)
                {
                    if (_nsmgr.LookupNamespace(prefix) == ns)
                    {
                        base.WriteStartElement(null, prefix + ":" + localName, null);
                        return;
                    }
                }
                else
                {
                    string givenPrefix = _nsmgr.LookupPrefix(ns);
                    if (givenPrefix != null)
                    {
                        base.WriteStartElement(null, givenPrefix + ":" + localName, null);
                        return;
                    }
                }
            }

            base.WriteStartElement(prefix, localName, ns);
        }

    }
}
