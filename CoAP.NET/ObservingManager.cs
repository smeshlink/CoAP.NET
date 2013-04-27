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
using CoAP.EndPoint;
using CoAP.Layers;
using CoAP.Log;

namespace CoAP
{
    /// <summary>
    /// Manages observationships.
    /// </summary>
    public class ObservingManager
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(ObservingManager));
        private static readonly ObservingManager instance = new ObservingManager();

        private IDictionary<String, IDictionary<String, Observationship>> _observersByResource
            = new Dictionary<String, IDictionary<String, Observationship>>();
        private IDictionary<String, IDictionary<String, Observationship>> _observersByClient
            = new Dictionary<String, IDictionary<String, Observationship>>();
        private Int32 _checkInterval = CoapConstants.ObservingRefreshInterval;
        private IDictionary<String, Int32> _intervalByResource = new Dictionary<String, Int32>();
        private IDictionary<String, Request> _observeRequests = new Dictionary<String, Request>();
        private Object _sync = new Byte[0];

        public static ObservingManager Instance
        {
            get { return instance; }
        }

        private ObservingManager()
        { }

        public void NotifyObservers(LocalResource resource)
        {
            String path = resource.Path;
            lock (_sync)
            {
                if (_observersByResource.ContainsKey(path))
                {
                    IDictionary<String, Observationship> resourceObservers = _observersByResource[path];
                    if (resourceObservers.Count > 0)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug(String.Format("Notifying observers: {0} @ {1}", resourceObservers.Count, path));

                        Int32 check = _intervalByResource.ContainsKey(path) ? (_intervalByResource[path] - 1) : _checkInterval;

                        if (check <= 0)
                        {
                            _intervalByResource[path] = _checkInterval;
                            if (log.IsDebugEnabled)
                                log.Debug("Refreshing observationship: " + path);
                        }
                        else
                            _intervalByResource[path] = check;

                        foreach (Observationship observer in resourceObservers.Values)
                        {
                            Request request = observer.request;
                            request.Type = check <= 0 ? MessageType.CON : MessageType.NON;
                            resource.DoGet(request);
                            PrepareResponse(request);
                            if (request.PeerAddress == null)
                                request.HandleResponse(request.Response);
                            else
                                request.Response.Send();
                        }
                    }
                }
            }
        }

        public Boolean IsObserved(String clientID, LocalResource resource)
        {
            return _observersByClient.ContainsKey(clientID)
                && _observersByClient[clientID].ContainsKey(resource.Path);
        }

        public void AddObserver(Request request, LocalResource resource)
        {
            request.IsObserving = true;
            Observationship shipToAdd = new Observationship(request);

            lock (_sync)
            {
                // get clients map for the given resource path
                IDictionary<String, Observationship> resourceObservers = SafeGet(_observersByResource, resource.Path);
                // get resource map for given client address
                IDictionary<String, Observationship> clientObservers = SafeGet(_observersByClient, request.PeerAddress.ToString());

                // save relationship for notifications triggered by resource
                resourceObservers[request.PeerAddress.ToString()] = shipToAdd;
                // save relationship for actions triggered by client
                clientObservers[resource.Path] = shipToAdd;
            }

            if (log.IsInfoEnabled)
                log.Info(String.Format("Established observing relationship: {0} @ {1}",
                    request.PeerAddress, resource.Path));

            // update response
            PrepareResponse(request);
        }

        public void RemoveObserver(String clientID)
        {
            if (_observersByClient.ContainsKey(clientID))
            {
                foreach (IDictionary<String, Observationship> observers in _observersByResource.Values)
                {
                    observers.Remove(clientID);
                }
                _observersByClient.Remove(clientID);

                if (log.IsInfoEnabled)
                    log.Info("Terminated all observing relationships for client: " + clientID);
            }
        }

        public void RemoveObserver(String clientID, Int32 messageId)
        {
            Observationship shipToRemove = null;
            IDictionary<String, Observationship> clientObservers = null;

            if (_observersByClient.ContainsKey(clientID))
            {
                clientObservers = _observersByClient[clientID];
                foreach (Observationship ship in clientObservers.Values)
                {
                    if (messageId == ship.lastMessageID && clientID.Equals(ship.clientID))
                    {
                        shipToRemove = ship;
                        break;
                    }
                }
            }

            if (shipToRemove == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(String.Format("Cannot find observing relationship: {0}|{1}", clientID, messageId));
            }
            else
            {
                IDictionary<String, Observationship> resourceObservers = _observersByResource[shipToRemove.resourcePath];

                // FIXME Inconsistent state check
                if (resourceObservers == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error(String.Format("FIXME: ObservingManager has clientObservee, but no resourceObservers ({0} @ {1})", clientID, shipToRemove.resourcePath));
                }
                else if (resourceObservers.Remove(clientID) && clientObservers.Remove(shipToRemove.resourcePath))
                {
                    if (log.IsInfoEnabled)
                        log.Info(String.Format("Terminated observing relationship by RST: {0} @ {1}", clientID, shipToRemove.resourcePath));
                    return;
                }
            }

            if (log.IsWarnEnabled)
                log.Warn(String.Format("Cannot find observing relationship by MID: {0}|{1}", clientID, messageId));
        }

        public void RemoveObserver(String clientID, LocalResource resource)
        {
            IDictionary<String, Observationship> resourceObservers = null;
            IDictionary<String, Observationship> clientObservers = null;
            String path = resource.Path;

            if (_observersByResource.ContainsKey(path))
                resourceObservers = _observersByResource[path];
            if (_observersByClient.ContainsKey(clientID))
                clientObservers = _observersByClient[clientID];

            if (resourceObservers != null && clientObservers != null)
            {
                lock (_sync)
                {
                    if (resourceObservers.Remove(clientID) && clientObservers.Remove(path))
                    {
                        if (log.IsInfoEnabled)
                            log.Info(String.Format("Terminated observing relationship by GET: {0} @ {1}", clientID, path));
                        return;
                    }
                }
            }

            if (log.IsWarnEnabled)
                log.Warn(String.Format("Cannot find observing relationship: {0} @ {1}", clientID, path));
        }

        public void RemoveObservers(LocalResource resource)
        {
            NotifyObservers(resource);

            String path = resource.Path;
            IDictionary<String, Observationship> resourceObservers = null;
            if (_observersByResource.ContainsKey(path))
                resourceObservers = _observersByResource[path];
            if (resourceObservers != null)
            {
                lock (_sync)
                {
                    if (log.IsInfoEnabled)
                        log.Info(String.Format("Terminated {0} observing relationship @ {1}", resourceObservers.Count, path));
                    _observersByResource[path] = new Dictionary<String, Observationship>();
                }
            }
        }

        private void PrepareResponse(Request request)
        {
            // consecutive response require new MID that must be stored for RST matching
            if (request.Response.ID == Message.InvalidID)
                request.Response.ID = MessageLayer.NextMessageID();

            // 16-bit second counter
            Int32 secs = (Int32)((DateTime.Now - request.StartTime).TotalMilliseconds / 1000) & 0xFFFF;
            request.Response.SetOption(Option.Create(OptionType.Observe, secs));

            // store ID for RST matching
            UpdateLastMessageID(request.PeerAddress.ToString(), request.UriPath, request.Response.ID);
        }

        private void UpdateLastMessageID(String clientID, String path, Int32 id)
        {
            if (_observersByClient.ContainsKey(clientID))
            {
                IDictionary<String, Observationship> clientObservers = _observersByClient[clientID];
                if (clientObservers.ContainsKey(path))
                {
                    Observationship shipToUpdate = clientObservers[path];
                    shipToUpdate.lastMessageID = id;
                    if (log.IsDebugEnabled)
                        log.Debug(String.Format("Updated last message ID for observing relationship: {0} @ {1}",
                            clientID, shipToUpdate.resourcePath));
                    return;
                }
            }
            if (log.IsWarnEnabled)
                log.Warn(String.Format("Cannot find observing relationship to update message ID: {0} @ {1}",
                    clientID, path));
        }

        private static IDictionary<String, Observationship> SafeGet(IDictionary<String, IDictionary<String, Observationship>> map, String key)
        {
            if (map.ContainsKey(key))
                return map[key];
            else
            {
                IDictionary<String, Observationship> dict = new Dictionary<String, Observationship>();
                map[key] = dict;
                return dict;
            }
        }

        public Boolean HasSubscription(String key)
        {
            return _observeRequests.ContainsKey(key);
        }

        public void AddSubscription(Request request)
        {
            if (HasSubscription(request.SequenceKey) && log.IsWarnEnabled)
                log.Warn(String.Format("Observe subspricption will be overwritten: {0}", request.SequenceKey));
            if (log.IsInfoEnabled)
                log.Info(String.Format("Adding Observe subscription for {0}: {1}", request.UriPath, request.SequenceKey));
            _observeRequests[request.SequenceKey] = request;
        }

        public Request GetSubscriptionRequest(String key)
        {
            return _observeRequests.ContainsKey(key) ? _observeRequests[key] : null;
        }

        public void CancelSubscription(String key)
        {
            if (log.IsInfoEnabled)
                log.Info(String.Format("Cancelling Observe subscription {0}", key));
            _observeRequests.Remove(key);
        }

        class Observationship
        {
            public String clientID;
            public String resourcePath;
            public Request request;
            public Int32 lastMessageID;

            public Observationship(Request request)
            {
                request.ID = Message.InvalidID;
                this.clientID = request.PeerAddress.ToString();
                this.resourcePath = request.UriPath;
                this.request = request;
                this.lastMessageID = Message.InvalidID;
            }
        }
    }
}
