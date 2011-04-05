﻿namespace RoliSoft.TVShowTracker.Remote
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    using Newtonsoft.Json;

    using RoliSoft.TVShowTracker.Remote.Objects;

    /// <summary>
    /// Provides method proxies for the lab.rolisoft.net API.
    /// </summary>
    public static class API
    {
        private static readonly RSACryptoServiceProvider _rsa = new RSACryptoServiceProvider();
        private static readonly SymmetricAlgorithm _algo      = new RijndaelManaged
            {
                BlockSize = 256
            };
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

        /// <summary>
        /// Initializes the <see cref="API"/> class.
        /// </summary>
        static API()
        {
            _rsa.FromXmlString(Properties.Resources.APIPublicKey);
        }

        #region Remote method invocation
        /// <summary>
        /// Invokes a remote method.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        public static T InvokeRemoteMethod<T>(string func, params object[] args) where T : IRemoteObject, new()
        {
            return InternalInvokeRemoteMethod<T>(func, args);
        }

        /// <summary>
        /// Invokes a remote method, with encryption enabled.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        public static T InvokeSecureRemoteMethod<T>(string func, params object[] args) where T : IRemoteObject, new()
        {
            return InternalInvokeRemoteMethod<T>(func, args, true);
        }

        /// <summary>
        /// Invokes a remote method, with user authentication.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        public static T InvokeAuthedRemoteMethod<T>(string func, string user, string pass, params object[] args) where T : IRemoteObject, new()
        {
            return InternalInvokeRemoteMethod<T>(func, args, user: user, pass: pass);
        }

        /// <summary>
        /// Invokes a remote method, with user authentication and encryption enabled.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        public static T InvokeAuthedSecureRemoteMethod<T>(string func, string user, string pass, params object[] args) where T : IRemoteObject, new()
        {
            return InternalInvokeRemoteMethod<T>(func, args, true, user, pass);
        }

        /// <summary>
        /// Invokes a remote method.
        /// </summary>
        /// <typeparam name="T">The answer type of the method.</typeparam>
        /// <param name="func">The name of the method.</param>
        /// <param name="args">The arguments of the method.</param>
        /// <param name="secure">if set to <c>true</c> encryption will be enabled.</param>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        /// <returns>Answer deserialized to type <c>T</c>.</returns>
        private static T InternalInvokeRemoteMethod<T>(string func, object[] args, bool secure = false, string user = null, string pass = null) where T : IRemoteObject, new()
        {
            T obj;
            byte[] key = null, iv = null;
            var sw = Stopwatch.StartNew();

            try
            {
                var post = Utils.EscapeUTF8(JsonConvert.SerializeObject(new Request(func, args), Formatting.None, _settings));
                var head = new Dictionary<string, string>
                    {
                        { "X-UUID", "{0}/{1}/{2}".FormatWith(Utils.GetUUID(), Environment.UserDomainName, Environment.UserName) }
                    };

                if (secure)
                {
                    key = new byte[32];
                    Utils.CryptoRand.GetBytes(key);

                    iv = new byte[32];
                    Utils.CryptoRand.GetBytes(iv);

                    var tmp = Encoding.UTF8.GetBytes(post);
                       post = Convert.ToBase64String(_algo.CreateEncryptor(key, iv).TransformFinalBlock(tmp, 0, tmp.Length)).TrimEnd('=');

                    head["X-Key"] = Convert.ToBase64String(_rsa.Encrypt(Utils.Combine(iv, key), true)).TrimEnd('=');
                }

                if (!string.IsNullOrWhiteSpace(user))
                {
                    head["X-UUID"] += "/" + user;

                    if (!string.IsNullOrEmpty(pass))
                    {
                        var auth = new HMACSHA256(Encoding.UTF8.GetBytes(Signature.Software)).ComputeHash(Encoding.UTF8.GetBytes(user + "\0" + pass));
                        head["X-Auth"] = Convert.ToBase64String(new HMACSHA256(auth).ComputeHash(Encoding.UTF8.GetBytes(post))).TrimEnd('=');
                    }
                }

                var resp = Utils.GetURL(
                    url:       "http://lab.rolisoft.net/api/",
                  //url:       "http://home.rolisoft.net/api/",
                    postData:  post,
                    userAgent: "{0}/{1}".FormatWith(Signature.Software, Signature.Version),
                    timeout:   120000,
                    headers:   head
                );

                if (secure)
                {
                    var tmp = Convert.FromBase64String(resp);
                       resp = Encoding.UTF8.GetString(_algo.CreateDecryptor(key, iv).TransformFinalBlock(tmp, 0, tmp.Length));
                }

                obj = JsonConvert.DeserializeObject<T>(resp);
                obj.Success = string.IsNullOrWhiteSpace(obj.Error);
            }
            catch (Exception ex)
            {
                obj = new T
                    {
                        Success = false,
                        Error   = ex.Message
                    };
            }

            sw.Stop();
            obj.Time = sw.Elapsed.TotalSeconds;

            return obj;
        }
        #endregion

        #region API methods
        /// <summary>
        /// Adds two numbers together. [API test]
        /// </summary>
        /// <param name="number1">The first number.</param>
        /// <param name="number2">The second number.</param>
        /// <returns>Sum of the two numbers.</returns>
        public static Generic<double> Add(double number1, double number2)
        {
            return InvokeRemoteMethod<Generic<double>>("Add", number1, number2);
        }

        /// <summary>
        /// Calculates the arithmetic mean of the specified numbers. [API test]
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <returns>Arithmetic mean of the numbers.</returns>
        public static Generic<double> Mean(params double[] numbers)
        {
            return InvokeRemoteMethod<Generic<double>>("Mean", numbers);
        }

        /// <summary>
        /// Check for software update.
        /// </summary>
        /// <returns>URL of the new version, if available.</returns>
        public static UpdateCheck CheckForUpdate()
        {
            return InvokeRemoteMethod<UpdateCheck>("CheckForUpdate");
        }

        /// <summary>
        /// Gets a list of known shows on the server.
        /// </summary>
        /// <returns>List of shows.</returns>
        public static ListOfShows GetListOfShows()
        {
            return InvokeRemoteMethod<ListOfShows>("GetListOfShows");
        }

        /// <summary>
        /// Gets the informations available on the server about the specified show.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="filter">The name of the fields to return or <c>null</c> for all available.</param>
        /// <returns>Informations about the show.</returns>
        public static RemoteShowInfo GetShowInfo(string name, IEnumerable<string> filter = null)
        {
            return InvokeRemoteMethod<RemoteShowInfo>("GetShowInfo", name, filter);
        }

        /// <summary>
        /// Sends the specified show information to the remote database.
        /// By caching little bits of information at lab.rolisoft.net, users can later retrieve it when
        /// additional information is required for a show, without putting a huge load on TVRage's server.
        /// </summary>
        /// <param name="info">The show object.</param>
        /// <param name="hash">The checksum of the object.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General SetShowInfo(object info, string hash)
        {
            return InvokeRemoteMethod<General>("SetShowInfo", info, hash);
        }

        /// <summary>
        /// Gets the informations available on the server about the specified shows.
        /// </summary>
        /// <param name="shows">The name of the shows.</param>
        /// <param name="filter">The name of the fields to return or <c>null</c> for all available.</param>
        /// <returns>Informations about the shows.</returns>
        public static MultipleShowInfo GetMultipleShowInfo(IEnumerable<string> shows, IEnumerable<string> filter = null)
        {
            return InvokeRemoteMethod<MultipleShowInfo>("GetMultipleShowInfo", shows, filter);
        }

        /// <summary>
        /// Reports the unhandled exception anonymously.
        /// </summary>
        /// <param name="stacktrace">The stacktrace of the error.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General ReportError(string stacktrace)
        {
            return InvokeSecureRemoteMethod<General>("ReportError", stacktrace);
        }

        /// <summary>
        /// Reports the unhandled exception anonymously.
        /// </summary>
        /// <param name="type">The type of the feedback.</param>
        /// <param name="name">The name of the user.</param>
        /// <param name="email">The email of the user.</param>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General SendFeedback(string type, string name, string email, string message)
        {
            return InvokeSecureRemoteMethod<General>("SendFeedback", type, name, email, message);
        }
        
        /// <summary>
        /// Sends a database change to the remote server.
        /// </summary>
        /// <param name="change">The database change.</param>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General SendDatabaseChange(ShowInfoChange change, string user, string pass)
        {
            return InvokeAuthedRemoteMethod<General>("SendDatabaseChange", user, pass, change);
        }

        /// <summary>
        /// Sends multiple database changes to the remote server.
        /// </summary>
        /// <param name="changes">The list of database changes.</param>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static General SendDatabaseChanges(IEnumerable<ShowInfoChange> changes, string user, string pass)
        {
            return InvokeAuthedRemoteMethod<General>("SendDatabaseChanges", user, pass, changes);
        }

        /// <summary>
        /// Retrieves multiple database changes from the remote server.
        /// </summary>
        /// <param name="time">The last date when sync occurred.</param>
        /// <param name="user">The username.</param>
        /// <param name="pass">The password.</param>
        /// <returns><c>true</c> if operation was successful.</returns>
        public static ShowInfoChangeList GetDatabaseChanges(long time, string user, string pass)
        {
            return InvokeAuthedRemoteMethod<ShowInfoChangeList>("GetDatabaseChanges", user, pass, time);
        }
        #endregion
    }
}