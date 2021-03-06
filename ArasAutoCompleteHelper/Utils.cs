﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace Aras.AutoComplete
{
  internal static class Utils
  {
    public static string Attribute(this XmlElement elem, string localName, string defaultValue = null)
    {
      if (elem != null && elem.HasAttribute(localName))
      {
        return elem.Attributes[localName].Value;
      }
      else
      {
        return defaultValue;
      }
    }
    public static XmlElement Element(this XmlNode node, string localName)
    {
      return node.ChildNodes.OfType<XmlElement>().SingleOrDefault(e => e.LocalName == localName);
    }
    public static string Element(this XmlNode node, string localName, string defaultValue)
    {
      var elem = node.ChildNodes.OfType<XmlElement>().SingleOrDefault(e => e.LocalName == localName);
      if (elem == null) return defaultValue;
      return elem.InnerText;
    }
    public static IEnumerable<XmlElement> ElementsByXPath(this XmlNode node, string xPath)
    {
      return node.SelectNodes(xPath).OfType<XmlElement>();
    }

    public enum XmlState
    {
      Attribute,
      AttributeStart,
      AttributeValue,
      CData,
      Comment,
      Tag,
      Other
    }

    [DebuggerStepThrough()]
    public static XmlState ProcessFragment(string fragment, Func<XmlReader, int, bool> processor)
    {
      var lineOffsets = new List<int>() {0};
      var state = XmlState.Other;
      var line = 1;
      var lastTag = new KeyValuePair<int, int>(0, 0);

      for (var i = 0; i < fragment.Length; i++)
      {
        switch (fragment[i])
        {
          case '\r':
            if (i + 1 < fragment.Length && fragment[i + 1] == '\n') i++;
            line++;
            lineOffsets.Add(i + 1);
            if (state == XmlState.Tag) state = XmlState.Attribute;
            break;
          case '\n':
            line++;
            lineOffsets.Add(i + 1);
            if (state == XmlState.Tag) state = XmlState.Attribute;
            break;
          default:
            switch (state)
            {
              case XmlState.Attribute:
                if (fragment[i] == '=')
                {
                  i++;
                  state = XmlState.AttributeValue;
                }
                break;
              case XmlState.AttributeValue:
                if (fragment[i] == '"')
                {
                  state = XmlState.Tag;
                }
                break;
              case XmlState.CData:
                if (i + 2 < fragment.Length && fragment[i] == ']' && fragment[i + 1] == ']' && fragment[i + 2] == '>')
                {
                  i += 2;
                  state = XmlState.Other;
                }
                break;
              case XmlState.Comment:
                if (i + 2 < fragment.Length && fragment[i] == '-' && fragment[i + 1] == '-' && fragment[i + 2] == '>')
                {
                  i += 2;
                  state = XmlState.Other;
                }
                break;
              case XmlState.Tag:
                if (char.IsWhiteSpace(fragment[i]))
                {
                  state = XmlState.Attribute;
                }
                else if (fragment[i] == '>')
                {
                  state = XmlState.Other;
                }
                break;
              case XmlState.Other:
                if (fragment[i] == '<')
                {
                  if (i + 3 < fragment.Length
                    && fragment[i + 1] == '!'
                    && fragment[i + 2] == '-'
                    && fragment[i + 3] == '-')
                  {
                    i += 3;
                    state = XmlState.Comment;
                  }
                  if (i + 8 < fragment.Length
                    && fragment[i + 1] == '!'
                    && fragment[i + 2] == '['
                    && fragment[i + 3] == 'C'
                    && fragment[i + 4] == 'D'
                    && fragment[i + 5] == 'A'
                    && fragment[i + 6] == 'T'
                    && fragment[i + 7] == 'A'
                    && fragment[i + 8] == '[')
                  {
                    i += 8;
                    state = XmlState.CData;
                  }
                  else
                  {
                    state = XmlState.Tag;
                    lastTag = new KeyValuePair<int, int>(line, i - lineOffsets.Last() + 2);
                  }
                }
                break;
            }
            break;
        } 
      }

      const string __noName = "___NO_NAME___";
      const string __eof = "{`EOF`}";

      switch (state)
      {
        case XmlState.Attribute:
          if (char.IsWhiteSpace(fragment[fragment.Length - 1]))
          {
            state = XmlState.AttributeStart;
            fragment += ">";
          }
          else
          {
            fragment += "=\"\">";
          }
          break;
        case XmlState.AttributeValue:
          if (fragment[fragment.Length - 1] == '=') fragment += '"';
          fragment += "\">";
          break;
        case XmlState.CData:
          fragment += "]]>";
          break;
        case XmlState.Comment:
          fragment += "-->";
          break;
        case XmlState.Tag:
          if (fragment[fragment.Length - 1] == '<') fragment += __noName;
          fragment += ">";
          break;
      }
      fragment += "<!--" + __eof + "-->";
      
      var settings = new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment };
      var textReader = new System.IO.StringReader(fragment);
      var reader = XmlReader.Create(textReader, settings);
      var lineInfo = reader as IXmlLineInfo;

      try
      {
        bool keepGoing = true;
        while (keepGoing && reader.Read() && !(reader.NodeType == XmlNodeType.Comment && reader.Value == __eof))
        {
          if (reader.LocalName != __noName)
          {
            keepGoing = processor.Invoke(reader, lineOffsets[lineInfo.LineNumber - 1]
                                                + lineInfo.LinePosition
                                                - (reader.NodeType == XmlNodeType.Element ? 2 : 1));
            if (reader.NodeType == XmlNodeType.Element
              && lineInfo.LineNumber == lastTag.Key
              && lineInfo.LinePosition == lastTag.Value)
            {
              switch (state)
              {
                case XmlState.Attribute:
                case XmlState.AttributeValue:
                  reader.MoveToAttribute(reader.AttributeCount - 1);
                  keepGoing = processor.Invoke(reader, lineOffsets[lineInfo.LineNumber - 1] + lineInfo.LinePosition - 1);
                  break;
              }
            }
          }
        }
      }
      catch (XmlException)
      {
        // Do Nothing
      }

      return state;
    }
  }
}
