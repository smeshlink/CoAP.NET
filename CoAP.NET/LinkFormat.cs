/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using CoAP.Util;
using CoAP.Log;

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
        /// The string as the delimiter between resources
        /// </summary>
        public static readonly String Delimiter = ",";
        /// <summary>
        /// The string to separate attributes
        /// </summary>
        public static readonly String Separator = ";";

        public static readonly Regex AttributeNameRegex = new Regex("</.*?>");
        public static readonly Regex QuotedString = new Regex("\\G\".*?\"");
        public static readonly Regex Cardinal = new Regex("\\G\\d+");

        private static ILogger log = LogManager.GetLogger(typeof(LinkFormat));
    }
}
