using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    // https://forum.unity.com/threads/finally-a-serializable-dictionary-for-unity-extracted-from-system-collections-generic.335797/
    [Serializable]
    public sealed class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary {
        [SerializeField, HideInInspector] int[] _Buckets;
        [SerializeField, HideInInspector] int _Count;
        [SerializeField, HideInInspector] int _Version;
        [SerializeField, HideInInspector] int _FreeList;
        [SerializeField, HideInInspector] int _FreeCount;

        [Serializable]
        internal struct Entry {
            public int hashcode;
            public int next;

            public TKey key;
            public TValue value;
        }
        [SerializeField] internal Entry[] entries;

#if UNITY_EDITOR
#pragma warning disable IDE0051
        [SerializeField] TKey _candidateKey;
        [SerializeField] TValue _candidateValue;

        private void ClearCandidate() {
            _candidateKey = default(TKey);
            _candidateValue = default(TValue);
        }

        private void AddCandidate() {
            Add(_candidateKey, _candidateValue);
        }

        private bool ContainsCandidate() {
            bool ret = ContainsKey(_candidateKey);

            return ret;
        }
#pragma warning restore IDE0051
#endif

        readonly IEqualityComparer<TKey> _Comparer;

        // Mainly for debugging purposes - to get the key-value pairs display
        public Dictionary<TKey, TValue> AsDictionary {
            get { return new Dictionary<TKey, TValue>(this); }
        }

        public int Count {
            get { return _Count - _FreeCount; }
        }

        public TValue this[TKey key, TValue defaultValue] {
            get {
                int index = FindIndex(key);
                if (index >= 0)
                    return entries[index].value;
                return defaultValue;
            }
        }

        public TValue this[TKey key] {
            get {
                int index = FindIndex(key);
                if (index >= 0)
                    return entries[index].value;
                throw new KeyNotFoundException(key.ToString());
            }

            set { Insert(key, value, false); }
        }

        public SerializableDictionary()
            : this(0, null) {
            UnityEngine.Debug.Log("Test 444");
        }

        public SerializableDictionary(int capacity)
            : this(capacity, null) {
        }

        public SerializableDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer) {
        }

        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");

            Initialize(capacity);

            _Comparer = (comparer ?? EqualityComparer<TKey>.Default);
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null) {
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : this((dictionary != null) ? dictionary.Count : 0, comparer) {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            foreach (KeyValuePair<TKey, TValue> current in dictionary)
                Add(current.Key, current.Value);
        }

        public bool ContainsValue(TValue value) {
            if (value == null) {
                for (int i = 0; i < _Count; i++) {
                    if (entries[i].hashcode >= 0 && entries[i].value == null)
                        return true;
                }
            } else {
                var defaultComparer = EqualityComparer<TValue>.Default;
                for (int i = 0; i < _Count; i++) {
                    if (entries[i].hashcode >= 0 && defaultComparer.Equals(entries[i].value, value))
                        return true;
                }
            }
            return false;
        }

        public bool ContainsKey(TKey key) {
            return FindIndex(key) >= 0;
        }

        public void Clear() {
            if (_Count <= 0)
                return;

            for (int i = 0; i < _Buckets.Length; i++)
                _Buckets[i] = -1;

            Array.Clear(entries, 0, _Count);

            _FreeList = -1;
            _Count = 0;
            _FreeCount = 0;
            _Version++;
        }

        public void Add(TKey key, TValue value) {
            Insert(key, value, true);
        }

        private void Resize(int newSize, bool forceNewHashCodes) {
            int[] bucketsCopy = new int[newSize];
            for (int i = 0; i < bucketsCopy.Length; i++)
                bucketsCopy[i] = -1;

            var entriesCopy = new Entry[newSize];

            Array.Copy(entries, 0, entriesCopy, 0, _Count);

            if (forceNewHashCodes) {
                for (int i = 0; i < _Count; i++) {
                    if (entriesCopy[i].hashcode != -1)
                        entriesCopy[i].hashcode = (_Comparer.GetHashCode(entriesCopy[i].key) & 2147483647);
                }
            }

            for (int i = 0; i < _Count; i++) {
                int index = entriesCopy[i].hashcode % newSize;
                entriesCopy[i].next = bucketsCopy[index];
                bucketsCopy[index] = i;
            }

            _Buckets = bucketsCopy;
            entries = entriesCopy;
        }

        private void Resize() {
            Resize(PrimeHelper.ExpandPrime(_Count), false);
        }

        public bool Remove(TKey key) {
            if (key == null)
                throw new ArgumentNullException("key");

            if (_Buckets != null) {
                int hash = _Comparer.GetHashCode(key) & 2147483647;
                int bucket = hash % _Buckets.Length;
                int last = -1;
                for (int i = _Buckets[bucket]; i >= 0; last = i, i = entries[i].next) {
                    if (entries[i].hashcode == hash && _Comparer.Equals(entries[i].key, key)) {
                        if (last < 0) {
                            _Buckets[bucket] = entries[i].next;
                        } else {
                            entries[last].next = entries[i].next;
                        }

                        entries[i].hashcode = -1;
                        entries[i].next = _FreeList;
                        entries[i].key = default;
                        entries[i].value = default;
                        _FreeList = i;
                        _FreeCount++;
                        _Version++;
                        return true;
                    }
                }
            }

            return false;
        }

        private void Insert(TKey key, TValue value, bool add) {
            if (key == null)
                throw new ArgumentNullException("key");

            if (_Buckets == null)
                Initialize(0);

            int hash = _Comparer.GetHashCode(key) & 2147483647;
            int index = hash % _Buckets.Length;
            int num1 = 0;

            for (int i = _Buckets[index]; i >= 0; i = entries[i].next) {
                if (entries[i].hashcode == hash && _Comparer.Equals(entries[i].key, key)) {
                    if (add)
                        throw new ArgumentException("Key already exists: " + key);

                    entries[i].value = value;
                    _Version++;
                    return;
                }
                num1++;
            }

            int num2;
            if (_FreeCount > 0) {
                num2 = _FreeList;
                _FreeList = entries[num2].next;
                _FreeCount--;
            } else {
                if (_Count == entries.Length) {
                    Resize();
                    index = hash % _Buckets.Length;
                }
                num2 = _Count;
                _Count++;
            }

            entries[num2].hashcode = hash;
            entries[num2].next = _Buckets[index];
            entries[num2].key = key;
            entries[num2].value = value;
            _Buckets[index] = num2;
            _Version++;
        }

        private void Initialize(int capacity) {
            int prime = PrimeHelper.GetPrime(capacity);

            _Buckets = new int[prime];
            for (int i = 0; i < _Buckets.Length; i++)
                _Buckets[i] = -1;

            entries = new Entry[prime];

            _FreeList = -1;
        }

        private int FindIndex(TKey key) {
            if (key == null)
                throw new ArgumentNullException("key");

            if (_Buckets != null) {
                int hash = _Comparer.GetHashCode(key) & 2147483647;

                Debug.Log(hash + " % " + _Buckets.Length);
                for (int i = _Buckets[hash % _Buckets.Length]; i >= 0; i = entries[i].next) {
                    if (entries[i].hashcode == hash && _Comparer.Equals(entries[i].key, key))
                        return i;
                }
            }
            return -1;
        }

        public bool TryGetValue(TKey key, out TValue value) {
            int index = FindIndex(key);
            if (index >= 0) {
                value = entries[index].value;
                return true;
            }
            value = default;
            return false;
        }

        private static class PrimeHelper {
            public static readonly int[] Primes = new int[]
            {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
            };

            public static bool IsPrime(int candidate) {
                if ((candidate & 1) != 0) {
                    int num = (int)Math.Sqrt((double)candidate);
                    for (int i = 3; i <= num; i += 2) {
                        if (candidate % i == 0) {
                            return false;
                        }
                    }
                    return true;
                }
                return candidate == 2;
            }

            public static int GetPrime(int min) {
                if (min < 0)
                    throw new ArgumentException("min < 0");

                for (int i = 0; i < PrimeHelper.Primes.Length; i++) {
                    int prime = PrimeHelper.Primes[i];
                    if (prime >= min)
                        return prime;
                }
                for (int i = min | 1; i < 2147483647; i += 2) {
                    if (PrimeHelper.IsPrime(i) && (i - 1) % 101 != 0)
                        return i;
                }
                return min;
            }

            public static int ExpandPrime(int oldSize) {
                int num = 2 * oldSize;
                if (num > 2146435069 && 2146435069 > oldSize) {
                    return 2146435069;
                }
                return GetPrime(num);
            }
        }

        public ICollection<TKey> Keys {
            get {
                return entries.Take(Count).Select(x => x.key).ToArray();
            }
        }

        public ICollection<TValue> Values {
            get {
                return entries.Take(Count).Select(x => x.value).ToArray();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            int index = FindIndex(item.Key);
            return index >= 0 &&
                EqualityComparer<TValue>.Default.Equals(entries[index].value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException(string.Format("index = {0} array.Length = {1}", index, array.Length));

            if (array.Length - index < Count)
                throw new ArgumentException(string.Format("The number of elements in the dictionary ({0}) is greater than the available space from index to the end of the destination array {1}.", Count, array.Length));

            for (int i = 0; i < _Count; i++) {
                if (entries[i].hashcode >= 0)
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
            }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool IsFixedSize { get; }
        ICollection IDictionary.Keys { get; }
        ICollection IDictionary.Values { get; }
        public bool IsSynchronized { get; }
        public object SyncRoot { get; }

        public bool IsCompatibleKey(object obj) {
            if (obj == null) return false;

            return obj is TKey;
        }

        public object this[object key] {
            get {
                if (IsCompatibleKey(key)) {
                    int i = FindIndex((TKey)key);

                    if (i >= 0) {
                        return entries[i].value;
                    }
                }

                return null;
            }

            set {
                if (key == null) {
                    throw new NullReferenceException("Key cannot be null");
                }

                try {
                    TKey tkey = (TKey)key;

                    try {
                        this[tkey] = (TValue)value;
                    } catch (InvalidCastException) {
                        throw new ArgumentException("Invalid value type. Expected value type " + typeof(TValue));
                    }
                } catch (InvalidCastException) {
                    throw new ArgumentException("Invalid key type. Expected key type " + typeof(TKey));
                }
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            return Remove(item.Key);
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(object key, object value) {
            if (key == null) {
                throw new NullReferenceException("Key cannot be null");
            }

            try {
                TKey tkey = (TKey)key;

                try {
                    Add(tkey, (TValue)value);
                } catch (InvalidCastException) {
                    throw new ArgumentException("Invalid value type. Expected value type " + typeof(TValue));
                }
            } catch (InvalidCastException) {
                throw new ArgumentException("Invalid key type. Expected key type " + typeof(TKey));
            }
        }

        public bool Contains(object key) {
            if (IsCompatibleKey(key)) {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() {
            throw new NotImplementedException();
        }

        public void Remove(object key) {
            if (IsCompatibleKey(key)) {
                Remove((TKey)key);
            }
        }

        public void CopyTo(Array array, int index) {
            throw new NotSupportedException();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            private readonly SerializableDictionary<TKey, TValue> _Dictionary;
            private readonly int _Version;
            private int _Index;
            private KeyValuePair<TKey, TValue> _Current;

            public KeyValuePair<TKey, TValue> Current {
                get { return _Current; }
            }

            internal Enumerator(SerializableDictionary<TKey, TValue> dictionary) {
                _Dictionary = dictionary;
                _Version = dictionary._Version;
                _Current = default;
                _Index = 0;
            }

            public bool MoveNext() {
                if (_Version != _Dictionary._Version)
                    throw new InvalidOperationException(string.Format("Enumerator version {0} != Dictionary version {1}", _Version, _Dictionary._Version));

                while (_Index < _Dictionary._Count) {
                    if (_Dictionary.entries[_Index].hashcode >= 0) {
                        _Current = new KeyValuePair<TKey, TValue>(_Dictionary.entries[_Index].key, _Dictionary.entries[_Index].value);
                        _Index++;
                        return true;
                    }
                    _Index++;
                }

                _Index = _Dictionary._Count + 1;
                _Current = default;
                return false;
            }

            void IEnumerator.Reset() {
                if (_Version != _Dictionary._Version)
                    throw new InvalidOperationException(string.Format("Enumerator version {0} != Dictionary version {1}", _Version, _Dictionary._Version));

                _Index = 0;
                _Current = default;
            }

            object IEnumerator.Current {
                get { return Current; }
            }

            public void Dispose() {
            }
        }
    }
}