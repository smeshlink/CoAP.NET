/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CoAP.EndPoint;
using CoAP.Log;
using CoAP.Util;

namespace CoAP
{
    /// <summary>
    /// This class provides link format definitions as specified in
    /// draft-ietf-core-link-format-06
    /// </summary>
    public static class LinkFormat
    {
        /// <summary>
        /// Name of the attribute Resource Type
        /// </summary>
        public static readonly String ResourceType = "rt";
        /// <summary>
        /// Name of the attribute Interface Description
        /// </summary>
        public static readonly String InterfaceDescription = "if";
        /// <summary>
        /// Name of the attribute Content Type
        /// </summary>
        public static readonly String ContentType = "ct";
        /// <summary>
        /// Name of the attribute Max Size Estimate
        /// </summary>
        public static readonly String MaxSizeEstimate = "sz";
        /// <summary>
        /// Name of the attribute Title
        /// </summary>
        public static readonly String Title = "title";
        /// <summary>
        /// Name of the attribute Observable
        /// </summary>
        public static readonly String Observable = "obs";
        /// <summary>
        /// Name of the attribute link
        /// </summary>
        public static readonly String Link = "href";

        /// <summary>
        /// The string as the delimiter between resources
        /// </summary>
        public static readonly String Delimiter = ",";
        /// <summary>
        /// The string to separate attributes
        /// </summary>
        public static readonly String Separator = ";";

        public static readonly Regex DelimiterRegex = new Regex("\\s*" + Delimiter + "+\\s*");
        public static readonly Regex SeparatorRegex = new Regex("\\s*" + Separator + "+\\s*");

        public static readonly Regex ResourceNameRegex = new Regex("</[^>]*>");
        public static readonly Regex AttributeNameRegex = new Regex("\\w+");
        public static readonly Regex QuotedString = new Regex("\\G\".*?\"");
        public static readonly Regex Cardinal = new Regex("\\G\\d+");

        private static ILogger log = LogManager.GetLogger(typeof(LinkFormat));

        public static String Serialize(Resource resource, IEnumerable<Option> query, Boolean recursive)
        {
            StringBuilder linkFormat = new StringBuilder();

            // skip hidden and empty root in recursive mode, always skip non-matching resources
            if ((!resource.Hidden && (resource.Name.Length > 0) || !recursive) 
                && Matches(resource, query))
            {
                linkFormat.Append("<")
                    .Append(resource.Path)
                    .Append(">");

                foreach (LinkAttribute attr in resource.Attributes)
                {
                    linkFormat.Append(Separator);
                    attr.Serialize(linkFormat);
                }
            }

            if (recursive)
            {
                foreach (Resource sub in resource.GetSubResources())
                {
                    String next = Serialize(sub, query, true);

                    if (next.Length > 0)
                    {
                        if (linkFormat.Length > 3)
                            linkFormat.Append(Delimiter);
                        linkFormat.Append(next);
                    }
                }
            }

            return linkFormat.ToString();
        }

        public static RemoteResource Deserialize(String linkFormat)
        {
            RemoteResource root = new RemoteResource(String.Empty);
            Scanner scanner = new Scanner(linkFormat);

            String path = null;
            while ((path = scanner.Find(ResourceNameRegex)) != null)
            {
                path = path.Substring(2, path.Length - 3);

                // Retrieve specified resource, create if necessary
                RemoteResource resource = new RemoteResource(path);

                LinkAttribute attr = null;
                while (scanner.Find(DelimiterRegex, 1) == null && (attr = ParseAttribute(scanner)) != null)
                {
                    AddAttribute(resource.Attributes, attr);
                }

                root.AddSubResource(resource);
            }

            return root;
        }

        private static LinkAttribute ParseAttribute(Scanner scanner)
        {
            String name = scanner.Find(AttributeNameRegex);
            if (name == null)
                return null;
            else
            {
                Object value = null;
                // check for name-value-pair
                if (scanner.Find(new Regex("="), 1) == null)
                    // flag attribute
                    value = true;
                else
                {
                    String s = null;
                    if ((s = scanner.Find(QuotedString)) != null)
                        // trim " "
                        value = s.Substring(1, s.Length - 2);
                    else if ((s = scanner.Find(Cardinal)) != null)
                        value = Int32.Parse(s);
                    // TODO what if both pattern failed?
                }
                return new LinkAttribute(name, value);
            }
        }

        private static Boolean Matches(Resource resource, IEnumerable<Option> query)
        {
            if (resource == null)
                return false;

            if (query == null)
                return true;

            foreach (Option q in query)
            {
                String s = q.StringValue;
                Int32 delim = s.IndexOf('=');
                if (delim == -1)
                {
                    // flag attribute
                    if (resource.GetAttributes(s).Count > 0)
                        return true;
                }
                else
                {
                    String attrName = s.Substring(0, delim);
                    String expected = s.Substring(delim + 1);

                    if (attrName.Equals(LinkFormat.Link))
                    {
                        if (expected.EndsWith("*"))
                            return resource.Path.StartsWith(expected.Substring(0, expected.Length - 1));
                        else
                            return resource.Path.Equals(expected);
                    }
                    
                    foreach (LinkAttribute attr in resource.GetAttributes(attrName))
                    {
                        String actual = attr.Value.ToString();

                        // get prefix length according to "*"
                        Int32 prefixLength = expected.IndexOf('*');
                        if (prefixLength >= 0 && prefixLength < actual.Length)
                        {
                            // reduce to prefixes
                            expected = expected.Substring(0, prefixLength);
                            actual = actual.Substring(0, prefixLength);
                        }

                        // handle case like rt=[Type1 Type2]
                        if (actual.IndexOf(' ') > -1)
                        {
                            foreach (String part in actual.Split(' '))
                            {
                                if (part.Equals(expected))
                                    return true;
                            }
                        }

                        if (expected.Equals(actual))
                            return true;
                    }
                }
            }

            return false;
        }

        internal static Boolean AddAttribute(ICollection<LinkAttribute> attributes, LinkAttribute attrToAdd)
        {
            if (IsSingle(attrToAdd.Name))
            {
                foreach (LinkAttribute attr in attributes)
                {
                    if (attr.Name.Equals(attrToAdd.Name))
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("Found existing singleton attribute: " + attr.Name);
                        return false;
                    }
                }
            }

            // special rules
            if (attrToAdd.Name.Equals(ContentType) && attrToAdd.IntValue < 0)
                return false;
            if (attrToAdd.Name.Equals(MaxSizeEstimate) && attrToAdd.IntValue < 0)
                return false;

            attributes.Add(attrToAdd);
            return true;
        }

        private static Boolean IsSingle(String name)
        {
            return name.Equals(Title) || name.Equals(MaxSizeEstimate) || name.Equals(Observable);
        }
    }
}
