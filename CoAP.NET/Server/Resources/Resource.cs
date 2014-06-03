/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CoAP.Net;
using CoAP.Observe;
using CoAP.Log;

namespace CoAP.Server.Resources
{
    /// <summary>
    /// Basic implementation of a resource.
    /// Extend this class to write your own resources.
    /// </summary>
    public class Resource : IResource
    {
        static readonly IEnumerable<IEndPoint> EmptyEndPoints = new IEndPoint[0];
        static readonly ILogger log = LogManager.GetLogger(typeof(Resource));
        readonly ResourceAttributes _attributes = new ResourceAttributes();
        private String _name;
        private String _path = String.Empty;
        private Boolean _visible;
        private Boolean _observable;
        private MessageType _observeType;
        private IResource _parent;
        private IDictionary<String, IResource> _children
            = new ConcurrentDictionary<String, IResource>();
        private IDictionary<ObserveRelation, Boolean> _observeRelations
            = new ConcurrentDictionary<ObserveRelation, Boolean>();
        private ObserveNotificationOrderer _notificationOrderer
            = new ObserveNotificationOrderer();

        /// <summary>
        /// Constructs a new resource with the specified name.
        /// </summary>
        /// <param name="name">the name</param>
        public Resource(String name)
            : this(name, true)
        { }

        /// <summary>
        /// Constructs a new resource with the specified name
        /// and makes it visible to clients if the flag is true.
        /// </summary>
        /// <param name="name">the name</param>
        /// <param name="visible">if the resource is visible or not</param>
        public Resource(String name, Boolean visible)
        {
            _name = name;
            _visible = visible;
        }

        /// <inheritdoc/>
        public String Name
        {
            get { return _name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                lock (this)
                {
                    IResource parent = _parent;
                    lock (parent)
                    {
                        parent.Remove(this);
                        _name = value;
                        parent.Add(this);
                    }
                    AdjustChildrenPath();
                }
            }
        }

        /// <inheritdoc/>
        public String Path
        {
            get { return _path; }
            set
            {
                lock (this)
                {
                    _path = value;
                    AdjustChildrenPath();
                }
            }
        }

        /// <inheritdoc/>
        public Uri Uri
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets or sets a value indicating if the resource is visible to remote CoAP clients.
        /// </summary>
        public Boolean Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        /// <inheritdoc/>
        public virtual Boolean Cachable
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets a value indicating if this resource is observable by remote CoAP clients.
        /// </summary>
        public Boolean Observable
        {
            get { return _observable; }
            set { _observable = value; }
        }

        /// <summary>
        /// Gets or sets the type of the notifications that will be sent.
        /// </summary>
        public MessageType ObserveType
        {
            get { return _observeType; }
            set
            {
                if (value == MessageType.ACK || value == MessageType.RST)
                    throw new ArgumentException(
                        "Only CON and NON notifications are allowed or Unknown for no changes by the framework", "value");
                _observeType = value;
            }
        }

        /// <inheritdoc/>
        public ResourceAttributes Attributes
        {
            get { return _attributes; }
        }

        /// <inheritdoc/>
        public IEnumerable<IEndPoint> EndPoints
        {
            get { return _parent == null ? EmptyEndPoints : _parent.EndPoints; }
        }

        /// <inheritdoc/>
        public IResource Parent
        {
            get { return _parent; }
            set
            {
                if (_parent != value)
                {
                    lock (this)
                    {
                        if (_parent != null)
                            _parent.Remove(this);
                        _parent = value;
                        if (value != null)
                            _path = value.Path + value.Name + "/";
                        AdjustChildrenPath();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IResource> Children
        {
            get { return _children.Values; }
        }

        /// <inheritdoc/>
        public void Add(IResource child)
        {
            if (child.Name == null)
                throw new ArgumentException("Child must have a name", "child");
            _children[child.Name] = child;
            child.Parent = this;
        }

        /// <inheritdoc/>
        public Boolean Remove(IResource child)
        {
            IResource toRemove;
            if (_children.TryGetValue(child.Name, out toRemove)
                && toRemove == child)
            {
                _children.Remove(child.Name);
                child.Parent = null;
                child.Path = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public IResource GetChild(String name)
        {
            IResource child;
            _children.TryGetValue(name, out child);
            return child;
        }

        /// <inheritdoc/>
        public void AddObserveRelation(ObserveRelation relation)
        {
            _observeRelations[relation] = true;
        }

        /// <inheritdoc/>
        public void RemoveObserveRelation(ObserveRelation relation)
        {
            _observeRelations.Remove(relation);
        }

        /// <inheritdoc/>
        public void HandleRequest(Exchange exchange)
        {
            CoapExchange ce = new CoapExchange(exchange, this);
            switch (exchange.Request.Code)
            {
                case Code.GET:
                    DoGet(ce);
                    break;
                case Code.POST:
                    DoPost(ce);
                    break;
                case Code.PUT:
                    DoPut(ce);
                    break;
                case Code.DELETE:
                    DoDelete(ce);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the GET request in the given CoAPExchange.
        /// </summary>
        protected virtual void DoGet(CoapExchange exchange)
        {
            exchange.Respond(Code.MethodNotAllowed);
        }

        /// <summary>
        /// Handles the POST request in the given CoAPExchange.
        /// </summary>
        protected virtual void DoPost(CoapExchange exchange)
        {
            exchange.Respond(Code.MethodNotAllowed);
        }

        /// <summary>
        /// Handles the PUT request in the given CoAPExchange.
        /// </summary>
        protected virtual void DoPut(CoapExchange exchange)
        {
            exchange.Respond(Code.MethodNotAllowed);
        }

        /// <summary>
        /// Handles the DELETE request in the given CoAPExchange.
        /// </summary>
        protected virtual void DoDelete(CoapExchange exchange)
        {
            exchange.Respond(Code.MethodNotAllowed);
        }

        internal void CheckObserveRelation(Exchange exchange, Response response)
        {
            /*
             * If the request for the specified exchange tries to establish an observer
             * relation, then the ServerMessageDeliverer must have created such a relation
             * and added to the exchange. Otherwise, there is no such relation.
             * Remember that different paths might lead to this resource.
             */

            ObserveRelation relation = exchange.Relation;
            if (relation == null)
                return; // because request did not try to establish a relation

            if (Code.IsSuccess(response.Code))
            {
                response.SetOption(Option.Create(OptionType.Observe, _notificationOrderer.Current));

                if (!relation.Established)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Successfully established observe relation between " + relation.Source + " and resource " + Uri);
                    relation.Established = true;
                    AddObserveRelation(relation);
                }
                else if (_observeType != MessageType.Unknown)
                {
                    // The resource can control the message type of the notification
                    response.Type = _observeType;
                }
            } // ObserveLayer takes care of the else case
        }

        private void AdjustChildrenPath()
        {
            String childpath = _path + _name + "/";
            foreach (IResource child in _children.Values)
            {
                child.Path = childpath;
            }
        }
    }
}
