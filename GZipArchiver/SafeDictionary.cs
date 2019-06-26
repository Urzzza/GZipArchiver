using System.Linq;
using System.Collections.Generic;

namespace GZipArchiver
{
    public class SafeDictionary<TKey, TValue>
    {
        private readonly object lockObj = new object();
        private readonly Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get
            {
                lock (lockObj)
                {
                    return dictionary[key];
                }
            }
            set
            {
                lock (lockObj)
                {
                    dictionary[key] = value;
                }
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                lock (lockObj)
                {
                    return dictionary.Keys.ToList();
                }
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (lockObj)
            {
                if (dictionary.TryGetValue(key, out value))
                {
                    dictionary.Remove(key);
                    return true;
                }

                return false;
            }
        }
    }
}