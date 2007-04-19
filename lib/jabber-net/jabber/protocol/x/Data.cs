/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net can be used under either JOSL or the GPL.
 * See LICENSE.txt for details.
 * --------------------------------------------------------------------------*/
using System;

using System.Xml;

using bedrock.util;

namespace jabber.protocol.x
{
    /// <summary>
    /// XData types.
    /// </summary>
    [SVN(@"$Id: Data.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public enum XDataType
    {
        /// <summary>
        /// This packet contains a form to fill out. Display it to the user (if your program can).
        /// </summary>
        form,
        /// <summary>
        /// The form is filled out, and this is the data that is being returned from the form.
        /// </summary>
        submit,
        /// <summary>
        /// Data results being returned from a search, or some other query.
        /// </summary>
        result,
        /// <summary>
        /// A form was cancelled.
        /// </summary>
        cancel
    }

    /// <summary>
    /// jabber:x:data support, as in http://www.xmpp.org/extensions/xep-0004.html.
    /// </summary>
    [SVN(@"$Id: Data.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class Data : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Data(XmlDocument doc) : base("x", URI.XDATA, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Data(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }


        /// <summary>
        /// Form instructions.
        /// </summary>
        public string Instructions
        {
            get { return GetElem("instructions"); }
            set { SetElem("instructions", value); }
        }

        /// <summary>
        /// The form title, for display at the top of a window.
        /// </summary>
        public string Title
        {
            get { return GetElem("title"); }
            set { SetElem("title", value); }
        }

        /// <summary>
        /// Type of this XData.
        /// </summary>
        public XDataType Type
        {
            get { return (XDataType)GetEnumAttr("type", typeof(XDataType)); }
            set { SetAttribute("type", value.ToString());}
        }

        /// <summary>
        /// List of form fields
        /// </summary>
        /// <returns></returns>
        public Field[] GetFields()
        {
            XmlNodeList nl = GetElementsByTagName("field", URI.XDATA);
            Field[] fields = new Field[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                fields[i] = (Field) n;
                i++;
            }
            return fields;
        }

        /// <summary>
        /// Add a form field
        /// </summary>
        /// <returns></returns>
        public Field AddField()
        {
            Field f = new Field(this.OwnerDocument);
            AddChild(f);
            return f;
        }

        /// <summary>
        /// Add a form field
        /// </summary>
        /// <param name="var">Variable name</param>
        /// <param name="typ">Field Type</param>
        /// <param name="label">Field label</param>
        /// <param name="val">Field value</param>
        /// <param name="desc">Description</param>
        /// <returns></returns>
        public Field AddField(string var, FieldType typ, string label, string val, string desc)
        {
            Field f = new Field(this.OwnerDocument);
            if (var != null)
                f.Var = var;
            if (label != null)
                f.Label = label;
            f.Type = typ;
            if (val != null)
                f.Val = val;
            if (desc != null)
                f.Desc = desc;

            AddChild(f);
            return f;
        }

        /// <summary>
        /// Get a field with the specified variable name.
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public Field GetField(string var)
        {
            XmlNodeList nl = GetElementsByTagName("field", URI.XDATA);
            foreach (XmlNode n in nl)
            {
                Field f = (Field) n;
                if (f.Var == var)
                    return f;
            }
            return null;
        }
    }

    /// <summary>
    /// Types of fields.  This enum doesn't exactly match the XEP,
    /// since most of the field types aren't valid identifiers in C#.
    /// </summary>
    [SVN(@"$Id: Data.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public enum FieldType
    {
        /// <summary>
        /// Single-line text, and default.
        /// </summary>
        text_single,
        /// <summary>
        /// Password-style single line text.  Text obscured by *'s.
        /// </summary>
        text_private,
        /// <summary>
        /// Multi-line text
        /// </summary>
        text_multi,
        /// <summary>
        /// Multi-select list
        /// </summary>
        list_multi,
        /// <summary>
        /// Single-select list
        /// </summary>
        list_single,
        /// <summary>
        /// Checkbox
        /// </summary>
        boolean,
        /// <summary>
        /// Fixed text.
        /// </summary>
        Fixed,
        /// <summary>
        /// Hidden field.  Value is returned to sender as sent.
        /// </summary>
        hidden,
        /// <summary>
        /// Jabber ID.
        /// </summary>
        jid_single,
        /// <summary>
        /// A list of jabber ID's.
        /// </summary>
        jid_multi
    }

    /// <summary>
    /// Form field.
    /// </summary>
    [SVN(@"$Id: Data.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class Field : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Field(XmlDocument doc) : base("field", URI.XDATA, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Field(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Field type.
        /// </summary>
        public FieldType Type
        {
            get
            {
                switch (GetAttribute("type"))
                {
                    case "text-single":
                        return FieldType.text_single;
                    case "text-private":
                        return FieldType.text_private;
                    case "text-multi":
                        return FieldType.text_multi;
                    case "list-multi":
                        return FieldType.list_multi;
                    case "list-single":
                        return FieldType.list_single;
                    case "boolean":
                        return FieldType.boolean;
                    case "fixed":
                        return FieldType.Fixed;
                    case "hidden":
                        return FieldType.hidden;
                    case "jid-single":
                        return FieldType.jid_single;
                    case "jid-multi":
                        return FieldType.jid_multi;
                    default:
                        throw new ArgumentException("Unknown x:data field type: " + GetAttribute("type"));
                }
            }
            set
            {
                switch (value)
                {
                    case FieldType.text_single:
                        SetAttribute("type", "text-single");
                        break;
                    case FieldType.text_private:
                        SetAttribute("type", "text-private");
                        break;
                    case FieldType.text_multi:
                        SetAttribute("type", "text-multi");
                        break;
                    case FieldType.list_multi:
                        SetAttribute("type", "list-multi");
                        break;
                    case FieldType.list_single:
                        SetAttribute("type", "list-single");
                        break;
                    case FieldType.boolean:
                        SetAttribute("type", "boolean");
                        break;
                    case FieldType.Fixed:
                        SetAttribute("type", "fixed");
                        break;
                    case FieldType.hidden:
                        SetAttribute("type", "hidden");
                        break;
                    case FieldType.jid_single:
                        SetAttribute("type", "jid-single");
                        break;
                    case FieldType.jid_multi:
                        SetAttribute("type", "jid-multi");
                        break;
                    default:
                        throw new ArgumentException("Unknown x:data field type: " + value);
                }
            }
        }

        /// <summary>
        /// Field label.  Will return Var if no label is found.
        /// </summary>
        public string Label
        {
            get
            {
                string lbl = GetAttribute("label");
                if (lbl == null)
                    lbl = Var;
                return lbl;
            }
            set { SetAttribute("label", value); }
        }

        /// <summary>
        /// Field variable name.
        /// </summary>
        public string Var
        {
            get { return GetAttribute("var"); }
            set { SetAttribute("var", value); }
        }

        /// <summary>
        /// Is this a required field?
        /// </summary>
        public bool IsRequired
        {
            get { return this["required"] != null; }
            set
            {
                if (value)
                    this.SetElem("required", null);
                else
                    this.RemoveElem("required");
            }
        }

        /// <summary>
        /// The field value.
        /// </summary>
        public string Val
        {
            get { return GetElem("value"); }
            set { SetElem("value", value); }
        }

        /// <summary>
        /// Value for type='boolean' fields
        /// </summary>
        public bool BoolVal
        {
            get
            {
                string sval = Val;
                return !((sval == null) || (sval == "0"));
            }
            set
            {
                Val = value ? "1" : "0";
            }
        }

        /// <summary>
        /// Values for type='list-multi' fields
        /// </summary>
        public string[] Vals
        {
            get
            {
                XmlNodeList nl = GetElementsByTagName("value", URI.XDATA);
                string[] results = new string[nl.Count];
                int i=0;
                foreach (XmlElement el in nl)
                {
                    results[i++] = el.InnerText;
                }
                return results;
            }
            set
            {
                RemoveElems("value", URI.XDATA);
                foreach (string s in value)
                {
                    XmlElement val = this.OwnerDocument.CreateElement("value", URI.XDATA);
                    val.InnerText = s;
                    this.AppendChild(val);
                }
            }
        }

        /// <summary>
        /// Is the given value in Vals?
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool IsValSet(string val)
        {
            XmlNodeList nl = GetElementsByTagName("value", URI.XDATA);
            foreach (XmlElement el in nl)
            {
                if (el.InnerText == val)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Add a value to a multi-value field.
        /// </summary>
        /// <param name="newvalue"></param>
        public void AddValue(string newvalue)
        {
            XmlElement val = this.OwnerDocument.CreateElement("value", URI.XDATA);
            val.InnerText = newvalue;
            this.AppendChild(val);
        }

        /// <summary>
        /// The field description
        /// </summary>
        public string Desc
        {
            get { return GetElem("desc"); }
            set { SetElem("desc", value); }
        }

        /// <summary>
        /// List of field options
        /// </summary>
        /// <returns></returns>
        public Option[] GetOptions()
        {
            XmlNodeList nl = GetElementsByTagName("option", URI.XDATA);
            Option[] options = new Option[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                options[i] = (Option) n;
                i++;
            }
            return options;
        }

        /// <summary>
        /// Add a field option
        /// </summary>
        /// <returns></returns>
        public Option AddOption()
        {
            Option o = new Option(this.OwnerDocument);
            AddChild(o);
            return o;
        }

        /// <summary>
        /// Add a field option, with a value
        /// </summary>
        /// <param name="val">Value of the option</param>
        /// <returns></returns>
        public Option AddOption(String val)
        {
            Option o = new Option(this.OwnerDocument);
            AddChild(o);
            o.Val = val;
            return o;
        }

        /// <summary>
        /// Add a field option, with a value
        /// </summary>
        /// <param name="label">Label for the option</param>
        /// <param name="val">Value of the option</param>
        /// <returns></returns>
        public Option AddOption(String label, String val)
        {
            Option o = new Option(this.OwnerDocument);
            AddChild(o);
            o.Val = val;
            o.Label = label;
            return o;
        }
    }

    /// <summary>
    /// Field options, for list-single and list-multi type fields.
    /// </summary>
    [SVN(@"$Id: Data.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class Option : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Option(XmlDocument doc) : base("option", URI.XDATA, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Option(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Option label
        /// </summary>
        public string Label
        {
            get { return GetAttribute("label"); }
            set { SetAttribute("label", value); }
        }

        /// <summary>
        /// The option value.
        /// </summary>
        public string Val
        {
            get { return GetElem("value"); }
            set { SetElem("value", value); }
        }

        /// <summary>
        /// Return the label for this option, so that a ComboBox.ObjectCollection can manage these directly.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string l = Label;
            if (l != "")
                return l;
            return Val;
        }

    }
}
