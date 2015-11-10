using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using BookSleeve;
using ProtoBuf;


namespace RedisExample.Console
{
    public class RedisKeys
    {
        private const string SampleKey = "SampleKey:{0}";


        public static string GetSampleKey(int id)
        {
            return string.Format(SampleKey, id);
        }
    }

    public class RedisClient
    {
        private const int RedisDb = 0;

        public static void RedisSet<T>(string key, T[] value) where T : class
        {
            RedisSet(key, value, TimeSpan.MaxValue);
        }

        public static void RedisSet<T>(string key, T value) where T : class
        {
            RedisSet(key, value, TimeSpan.MaxValue);
        }

        public static void RedisSet<T>(string key, T value, TimeSpan expires) where T : class
        {
            try
            {
                if (value == null) return;

                var redis = RedisConnectionGateway.Current.GetConnection();

                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, value);
                    byte[] raw = ms.ToArray();
                    redis.Strings.Set(RedisDb, key, raw, (long)expires.TotalSeconds);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Redis Error:"+ex);
            }
        }

        public static void RedisUpdate<T>(string key, T value, TimeSpan expires) where T : class
        {
            try
            {
                var redis = RedisConnectionGateway.Current.GetConnection();
                var wait = redis.Keys.Remove(RedisDb, key);
                redis.Wait(wait);
                RedisSet(key, value, expires);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Redis Error:" + ex);
            }
        }

        public static void RedisRemove(string key)
        {
            var redis = RedisConnectionGateway.Current.GetConnection();
            redis.Keys.Remove(RedisDb, key);
        }



        public static T RedisGet<T>(string key) where T : class
        {
           
           
                var redis = RedisConnectionGateway.Current.GetConnection();

                var wait = redis.Strings.Get(RedisDb, key);

                var bytes = redis.Wait(wait);

                if (bytes == null) return null;
                using (var ms = new MemoryStream(bytes))
                {
                    var t = Serializer.Deserialize<T>(ms);
                    return t;
                }
            

        }

        public static IEnumerable<T> RedisGetArray<T>(string key) where T : class
        {
            var redis = RedisConnectionGateway.Current.GetConnection();

            var wait = redis.Strings.Get(RedisDb, key);

            var bytes = redis.Wait(wait);

            return bytes != null ? Serializer.Deserialize<T[]>(new MemoryStream(bytes)) : null;

        }


    }

    public sealed class RedisConnectionGateway
    {
        private const string RedisConnectionFailed = "Redis connection failed.";
        private RedisConnection _connection;
        private static volatile RedisConnectionGateway _instance;

        private static readonly object SyncLock = new object();
        private static readonly object SyncConnectionLock = new object();

        public static RedisConnectionGateway Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new RedisConnectionGateway();
                        }
                    }
                }

                return _instance;
            }
        }

        private RedisConnectionGateway()
        {
            _connection = GetNewConnection();
        }

        private static RedisConnection GetNewConnection()
        {
            var redisServer = ConfigurationManager.AppSettings["RedisServer"];
            // local instance doesn't authenticate
            if (redisServer == "127.0.0.1") return new RedisConnection(redisServer, syncTimeout: 5000, ioTimeout: 5000);
            var redisPassword = ConfigurationManager.AppSettings["RedisPassword"];
            var redisServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["RedisServerPort"]);
            return new RedisConnection(redisServer, redisServerPort, syncTimeout: 5000, ioTimeout: 5000, password: redisPassword);

        }

        public RedisConnection GetConnection()
        {
            lock (SyncConnectionLock)
            {
                if (_connection == null)
                    _connection = GetNewConnection();

                if (_connection.State == RedisConnectionBase.ConnectionState.Opening)
                    return _connection;

                if (_connection.State == RedisConnectionBase.ConnectionState.Closing || _connection.State == RedisConnectionBase.ConnectionState.Closed)
                {
                    try
                    {
                        _connection = GetNewConnection();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(RedisConnectionFailed, ex);
                    }
                }

                if (_connection.State == RedisConnectionBase.ConnectionState.New)
                {
                    try
                    {
                        var openAsync = _connection.Open();
                        _connection.Wait(openAsync);
                    }
                    catch (SocketException ex)
                    {
                        throw new Exception(RedisConnectionFailed, ex);
                    }
                }

                return _connection;
            }
        }
    }

  

   
}