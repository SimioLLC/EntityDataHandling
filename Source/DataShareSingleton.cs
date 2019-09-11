using SimioAPI.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityDataSharing
{
    /// <summary>
    /// A singleton class to hold our data
    /// </summary>
    public class DataShareSingleton
    {
        private static DataShareSingleton _instance;

        /// <summary>
        /// A place to store information at runtime
        /// </summary>
        public ConcurrentDictionary<string,object> RuntimeInfoDict { get; set; }
    
        /// <summary>
        /// The singleton constructor
        /// </summary>
        private DataShareSingleton() { RuntimeInfoDict = new ConcurrentDictionary<string, object>(); }

        /// <summary>
        /// The singleton pattern implemented.
        /// </summary>
        public static DataShareSingleton Instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new DataShareSingleton();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get runtime info according to case-insensitive key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public object GetRuntimeInfo(string key, out object info)
        {
            if (!RuntimeInfoDict.TryGetValue(key.ToLower(), out info))
                return null;

            return info;
        }

        /// <summary>
        /// Store or replace runtime info
        /// </summary>
        /// <param name="key"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool PutRuntimeInfo(string key, object info)
        {
            RuntimeInfoDict.AddOrUpdate(key.ToLower(), info, (k,v) => info);

            return true;
        }
    }
}
