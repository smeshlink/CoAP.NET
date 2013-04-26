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
using CoAP.Log;

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
        public static readonly Byte[] EmptyToken = new Byte[0];
        private static ILogger log = LogManager.GetLogger(typeof(TokenManager));
        private static TokenManager instance = new TokenManager();

        private Int64 _currentToken;
        private List<Byte[]> _acquiredTokens = new List<Byte[]>();

        private TokenManager()
        {
            _currentToken = (Int64)(new Random().NextDouble() * 0x1001);
        }

        public static TokenManager Instance
        {
            get { return instance; }
        }

        private Byte[] NextToken()
        {
            _currentToken++;

            Int64 temp = _currentToken;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(CoapConstants.TokenLength))
            {
                while (temp > 0 && ms.Length < CoapConstants.TokenLength)
                {
                    ms.WriteByte((Byte)(temp & 0xff));
                    temp >>= 8;
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Returns an unique token.
        /// </summary>
        /// <param name="preferEmptyToken">If set to true, the caller will receive the empty token if it is available. This is useful for reducing datagram sizes in transactions that are expected to complete in short time. On the other hand, empty tokens are not preferred in block-wise transfers, as the empty token is then not available for concurrent transactions.</param>
        /// <returns></returns>
        public Byte[] AcquireToken(Boolean preferEmptyToken)
        {
            Byte[] token = null;
            if (preferEmptyToken && !IsAcquired(EmptyToken))
                token = EmptyToken;
            else
            {
                do 
                {
                    token = NextToken();
                } while (IsAcquired(token));
            }

            lock (this)
            {
                _acquiredTokens.Add(token);
            }

            return token;
        }

        /// <summary>
        /// Returns an unique token.
        /// </summary>
        /// <returns></returns>
        public Byte[] AcquireToken()
        {
            return AcquireToken(false);
        }

        /// <summary>
        /// Checks if a token is acquired by this manager.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns>True iff the token is currently in use</returns>
        public Boolean IsAcquired(Byte[] token)
        {
            lock (this)
            {
                return _acquiredTokens.Contains(token);
            }
        }

        /// <summary>
        /// Releases an acquired token and makes it available for reuse.
        /// </summary>
        /// <param name="token">The token to release</param>
        public void ReleaseToken(Byte[] token)
        {
            lock (this)
            {
                if (!_acquiredTokens.Remove(token))
                {
                    if (log.IsWarnEnabled)
                        log.Warn("Token to release is not acquired: " + Option.Hex(token));
                }
            }
        }
    }
}
