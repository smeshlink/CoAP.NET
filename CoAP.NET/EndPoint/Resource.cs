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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CoAP.Util;

namespace CoAP.EndPoint
{
    /// <summary>
    /// This class describes the functionality of a CoAP resource.
    /// </summary>
    public abstract class Resource
    {
        private Int32 _totalSubResourceCount;
        private String _resourceIdentifier;
        /// <summary>
        /// Contains the resource's attributes as specified by CoRE Link Format
        /// </summary>
        protected IDictionary<String, LinkAttribute> _attributes;
        /// <summary>
        /// The current resource's parent
        /// </summary>
        protected Resource _parent;
        /// <summary>
        /// The current resource's sub-resources
        /// </summary>
        protected IDictionary<String, Resource> _subResources;
        /// <summary>
        /// Determines whether the resource is hidden from a resource discovery
        /// </summary>
        protected Boolean _hidden;

        /// <summary>
        /// Initialize a resource.
        /// </summary>
        public Resource() : this(null) { }

        /// <summary>
        /// Initialize a resource.
        /// </summary>
        /// <param name="resourceIdentifier">The identifier of this resource</param>
        public Resource(String resourceIdentifier) : this(resourceIdentifier, false) { }

        /// <summary>
        /// Initialize a resource.
        /// </summary>
        /// <param name="resourceIdentifier">The identifier of this resource</param>
        /// <param name="hidden">True if this resource is hidden</param>
        public Resource(String resourceIdentifier, Boolean hidden)
        {
            this._resourceIdentifier = resourceIdentifier;
            this._hidden = hidden;
            this._attributes = new HashMap<String, LinkAttribute>();
        }

        /// <summary>
        /// Adds sub-resources to this resource from a link format string.
        /// </summary>
        /// <param name="linkFormat">The link format representation of the resources</param>
        public void AddLinkFormat(String linkFormat)
        {
            String[] resources = linkFormat.Split(new String[] { LinkFormat.Delimiter }, StringSplitOptions.RemoveEmptyEntries);

            List<String> reses = new List<String>(resources.Length);
            foreach (String s in resources)
            {
                if (s.StartsWith("</"))
                    reses.Add(s);
                else
                    reses[reses.Count - 1] += s;
            }

            foreach (String s in reses)
            {
                String res = s.Trim();
                String[] entries = res.Split(new String[] { LinkFormat.Separator }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (entries.Length > 0)
                {
                    Match match = LinkFormat.AttributeNameRegex.Match(entries[0]);
                    if (null != match)
                    {
                        // trim </...>
                        String identifier = match.Value;
                        identifier = identifier.Substring(2, identifier.Length - 3);
                        // Retrieve specified resource, create if necessary
                        Resource resource = SubResource(identifier, true);
                        // Read link format attributes
                        if (entries.Length > 1)
                            resource.ReadAttribute(entries[1]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets sub-resources of this resource.
        /// </summary>
        /// <returns></returns>
        public Resource[] GetSubResources()
        {
            if (null == this._subResources)
                return new Resource[0];

            Resource[] resources = new Resource[this._subResources.Count];
            this._subResources.Values.CopyTo(resources, 0);
            return resources;
        }

        /// <summary>
        /// Adds a resource as a sub-resource of this resource.
        /// </summary>
        /// <param name="resource">The sub-resource to be added</param>
        public void AddSubResource(Resource resource)
        {
            if (null == resource)
                return;

            if (null == this._subResources)
                this._subResources = new HashMap<String, Resource>();

            _subResources[resource.Identifier] = resource;

            resource._parent = this;

            // update number of sub-resources in the tree
            Resource p = resource._parent;
            while (p != null)
            {
                ++p._totalSubResourceCount;
                p = p._parent;
            }
        }

        /// <summary>
        /// Removes a sub-resource from this resource by its identifier.
        /// </summary>
        /// <param name="identifier">The identifier of the sub-resource</param>
        public void RemoveSubResource(String identifier)
        {
            RemoveSubResource(SubResource(identifier));
        }

        /// <summary>
        /// Removes a sub-resource from this resource.
        /// </summary>
        /// <param name="resource">The sub-resource to be removed</param>
        public void RemoveSubResource(Resource resource)
        {
            if (null == resource)
                return;

            if (_subResources.Remove(resource.Identifier))
            {
                Resource p = resource._parent;
                while (p != null)
                {
                    --p._totalSubResourceCount;
                    p = p._parent;
                }

                resource._parent = null;
            }
        }

        /// <summary>
        /// Removes this resource from its parent.
        /// </summary>
        public void Remove()
        {
            if (this._parent != null)
            {
                this._parent.RemoveSubResource(this);
            }
        }

        /// <summary>
        /// Sets an attribute to this resource.
        /// </summary>
        /// <param name="name">The name of the attribute</param>
        /// <param name="value">The value of the attribute</param>
        public void SetAttributeValue(String name, Object value)
        {
            LinkAttribute attr = new LinkAttribute(name, value);
            _attributes[name] = attr;
        }

        /// <summary>
        /// Gets the attribute's value by its name.
        /// </summary>
        /// <param name="name">The name of the attribute</param>
        /// <returns>The value of the attribute if exists, otherwise null</returns>
        public Object GetAttributeValue(String name)
        {
            LinkAttribute attr = _attributes[name];
            return null == attr ? null : attr.Value;
        }

        /// <summary>
        /// Gets the identifier (relative URI to its parent) of this resource.
        /// </summary>
        public String Identifier
        {
            get { return GetIdentifier(false); }
        }

        /// <summary>
        /// Gets the URI of this resource.
        /// </summary>
        public String Path
        {
            get { return GetIdentifier(true); }
        }

        /// <summary>
        /// Gets or sets the type attribute of this resource.
        /// </summary>
        public String Type
        {
            get
            {
                Object val = GetAttributeValue(LinkFormat.ResourceType);
                return null == val ? null : val.ToString();
            }
            set
            {
                SetAttributeValue(LinkFormat.ResourceType, value);
            }
        }

        /// <summary>
        /// Gets or sets the title attribute of this resource.
        /// </summary>
        public String Title
        {
            get
            {
                Object val = GetAttributeValue(LinkFormat.Title);
                return null == val ? null : val.ToString();
            }
            set
            {
                SetAttributeValue(LinkFormat.Title, value);
            }
        }

        /// <summary>
        /// Gets or sets the interface description attribute of this resource.
        /// </summary>
        public String InterfaceDescription
        {
            get
            {
                Object val = GetAttributeValue(LinkFormat.InterfaceDescription);
                return null == val ? null : val.ToString();
            }
            set
            {
                SetAttributeValue(LinkFormat.InterfaceDescription, value);
            }
        }

        /// <summary>
        /// Gets or sets the content type code attribute of this resource.
        /// </summary>
        public Int32 ContentTypeCode
        {
            get
            {
                Object val = GetAttributeValue(LinkFormat.ContentType);
                return val is Int32 ? (Int32)val : 0;
            }
            set
            {
                SetAttributeValue(LinkFormat.ContentType, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum size estimate attribute of this resource.
        /// </summary>
        public Int32 MaximumSizeEstimate
        {
            get
            {
                Object val = GetAttributeValue(LinkFormat.MaxSizeEstimate);
                return val is Int32 ? (Int32)val : 0;
            }
            set
            {
                SetAttributeValue(LinkFormat.MaxSizeEstimate, value);
            }
        }

        /// <summary>
        /// Gets or sets the observable attribute of this resource.
        /// </summary>
        public Boolean Observable
        {
            get
            {
                Object val = GetAttributeValue(LinkFormat.Observable);
                return val is Boolean ? (Boolean)val : false;
            }
            set
            {
                SetAttributeValue(LinkFormat.Observable, value);
            }
        }

        /// <summary>
        /// Gets the total count of sub-resources, including children and children's children...
        /// </summary>
        public Int32 TotalSubResourceCount
        {
            get { return this._totalSubResourceCount; }
        }

        /// <summary>
        /// Gets the count of sub-resources of this resource.
        /// </summary>
        public Int32 SubResourceCount
        {
            get { return null == this._subResources ? 0 : this._subResources.Count; }
        }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
        protected abstract Resource CreateInstance();

        private void ReadAttribute(String attrString)
        {
            this._attributes.Clear();
            String[] entries = attrString.Split(new String[] { LinkFormat.Separator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in entries)
            {
                String entry = s.Trim();
                LinkAttribute attr = LinkAttribute.Parse(entry);
                if (attr != null)
                    this._attributes[attr.Name] = attr;
            }
        }

        private Resource SubResource(String identifier)
        {
            return SubResource(identifier, false);
        }

        private Resource SubResource(String identifier, Boolean create)
        {
            Int32 pos = identifier.IndexOf('/');
            String head, tail;

            if (pos >= 0 && pos < identifier.Length - 1)
            {
                head = identifier.Substring(0, pos);
                tail = identifier.Substring(pos + 1);
            }
            else
            {
                head = identifier;
                tail = null;
            }

            Resource resource = null;
            if (null != this._subResources && this._subResources.ContainsKey(head))
                resource = this._subResources[head];

            if (resource == null && create)
            {
                resource = CreateInstance();
                resource._resourceIdentifier = head;
                AddSubResource(resource);
            }

            if (resource != null && tail != null)
                return resource.SubResource(tail, create);
            else
                return resource;
        }

        private String GetIdentifier(Boolean absolute)
        {
            if (absolute && null != this._parent)
            {
                return this._parent.GetIdentifier(absolute) + "/" + this._resourceIdentifier;
            }
            else
            {
                return this._resourceIdentifier;
            }
        }
    }
}
