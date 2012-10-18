/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
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
using CoAP.Util;

namespace CoAP
{
    /// <summary>
    /// This class describes the functionality of a Token Manager.
    /// 
    /// Its purpose is to manage tokens used for keeping state of
    /// transactions and block-wise transfers. Communication layers use
    /// a TokenManager to acquire token where needed and release
    /// them after completion of the task.
    /// </summary>
    public class TokenManager
    {
        /// <summary>
        /// The empty token, used as default value
        /// </summary>
        public static readonly Option EmptyToken = Option.Create(OptionType.Token, new Byte[0]);

        private Int32 _nextValue = 0;
        private List<Option> _acquiredTokens = new List<Option>();

        /// <summary>
        /// Returns an unique token.
        /// </summary>
        /// <param name="preferEmptyToken">If set to true, the caller will receive the empty token if it is available. This is useful for reducing datagram sizes in transactions that are expected to complete in short time. On the other hand, empty tokens are not preferred in block-wise transfers, as the empty token is then not available for concurrent transactions.</param>
        /// <returns></returns>
        public Option AcquireToken(Boolean preferEmptyToken)
        {
            Option token = null;
            if (preferEmptyToken && !IsAcquired(EmptyToken))
            {
                token = EmptyToken;
            }
            else
            {
                token = Option.Create(OptionType.Token, ++this._nextValue);
            }

            if (!AcquireToken(token))
            {
                Log.Warning(this, "Token already acquired: {0}\n", token.ToString());
            }

            return token;
        }

        /// <summary>
        /// Returns an unique token.
        /// </summary>
        /// <returns></returns>
        public Option AcquireToken()
        {
            return AcquireToken(false);
        }

        /// <summary>
        /// Checks if a token is acquired by this manager.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns>True iff the token is currently in use</returns>
        public Boolean IsAcquired(Option token)
        {
            return this._acquiredTokens.Contains(token);
        }

        /// <summary>
        /// Releases an acquired token and makes it available for reuse.
        /// </summary>
        /// <param name="token">The token to release</param>
        public void ReleaseToken(Option token)
        {
            if (!this._acquiredTokens.Remove(token))
            {
                Log.Warning(this, "Token to release is not acquired: {0}\n", token.ToString());
            }
        }

        private Boolean AcquireToken(Option token)
        {
            if (IsAcquired(token))
                return false;
            this._acquiredTokens.Add(token);
            return true;
        }
    }
}
