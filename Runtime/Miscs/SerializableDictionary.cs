using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, ISerializable {
        [Serializable]
        public struct Entry {
            public int hashCode;    // Lower 31 bits of hash code, -1 if unused
            public int next;        // Index of next entry, -1 if last
            public TKey key;           // Key of entry
            public TValue value;         // Value of entry
        }

        [SerializeField] private int[] buckets;
        [SerializeField] private Entry[] entries;
        [SerializeField] private int count;
        [SerializeField] private int version;
        [SerializeField] private int freeList;
        [SerializeField] private int freeCount;
        private IEqualityComparer<TKey> comparer;
        [NonSerialized] private KeyCollection keys;
        [NonSerialized] private ValueCollection values;
        private object _syncRoot;

        [SerializeField] internal TKey _candidateKey = default;
        [SerializeField] internal TValue _candidateValue = default;

#if UNITY_EDITOR
        internal void AddCandidate() {
            Add(_candidateKey, _candidateValue);
        }

        private static readonly Type stringType = typeof(string);
        internal void ClearCandidate() {
            _candidateValue = default;

            if (_candidateKey.GetType() == stringType) {
                _candidateKey = (TKey)(object)string.Empty;
            } else {
                _candidateKey = default;
            }
        }

        internal bool ContainsCandidate() {
            return ContainsKey(_candidateKey);
        }

        internal List<int> GetIndexLookupList() {
            List<int> ret = new List<int>(Count);

            int index = 0;

            while (index < count) {
                if (entries[index].hashCode >= 0) {
                    ret.Add(index);
                    index++;
                    continue;
                }
                index++;
            }

            return ret;
        }

        internal List<object> GetKeyLookupList() {
            List<object> ret = new List<object>(Count);

            int index = 0;

            while (index < count) {
                if (entries[index].hashCode >= 0) {
                    ret.Add(entries[index].key);
                    index++;
                    continue;
                }
                index++;
            }

            return ret;
        }
#endif

        // constants for serialization
        private const string VersionName = "Version";
        private const string HashSizeName = "HashSize";  // Must save buckets.Length
        private const string KeyValuePairsName = "KeyValuePairs";
        private const string ComparerName = "Comparer";

        public SerializableDictionary() : this(0, null) {
        }

        public SerializableDictionary(int capacity) : this(capacity, null) { }

        public SerializableDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity");

            Initialize(capacity);

            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null) { }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) :
            this(dictionary != null ? dictionary.Count : 0, comparer) {

            if (dictionary == null) {
                throw new ArgumentNullException("dictionary");
            }

            foreach (KeyValuePair<TKey, TValue> pair in dictionary) {
                Add(pair.Key, pair.Value);
            }
        }

        public IEqualityComparer<TKey> Comparer {
            get {
                return comparer;
            }
        }

        public int Count {
            get { return count - freeCount; }
        }

        public KeyCollection Keys {
            get {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys {
            get {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
            get {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }

        public ValueCollection Values {
            get {
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values {
            get {
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
            get {
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        public TValue this[TKey key] {
            get {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;

                throw new KeyNotFoundException("Key " + key + " was not found");
            }
            set {
                Insert(key, value, false);
            }
        }

        public void Add(TKey key, TValue value) {
            Insert(key, value, true);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair) {
            int i = FindEntry(keyValuePair.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value)) {
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair) {
            int i = FindEntry(keyValuePair.Key);
            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value)) {
                Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        public void Clear() {
            if (count > 0) {
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }

        public bool ContainsKey(TKey key) {
            return FindEntry(key) >= 0;
        }

        public bool ContainsValue(TValue value) {
            if (value == null) {
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0 && entries[i].value == null) return true;
                }
            } else {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value)) return true;
                }
            }
            return false;
        }

        private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            if (index < 0 || index > array.Length) {
                throw new ArgumentOutOfRangeException("argument");
            }

            if (array.Length - index < Count) {
                throw new ArgumentException("Array size was too small");
            }

            int count = this.count;
            Entry[] entries = this.entries;
            for (int i = 0; i < count; i++) {
                if (entries[i].hashCode >= 0) {
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info == null) {
                throw new ArgumentNullException("info");
            }
            info.AddValue(VersionName, version);

#if FEATURE_RANDOMIZED_STRING_HASHING
            info.AddValue(ComparerName, HashHelpers.GetEqualityComparerForSerialization(comparer), typeof(IEqualityComparer<TKey>));
#else
            info.AddValue(ComparerName, comparer, typeof(IEqualityComparer<TKey>));
#endif

            info.AddValue(HashSizeName, buckets == null ? 0 : buckets.Length); //This is the length of the bucket array.
            if (buckets != null) {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        private int FindEntry(TKey key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            if (buckets != null) {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;

                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next) {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                        return i;
                }
            }
            return -1;
        }

        private void Initialize(int capacity) {
            int size = PrimeHelper.GetPrime(capacity);

            buckets = new int[size];

            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new Entry[size];
            freeList = -1;
        }

        private void Insert(TKey key, TValue value, bool add) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            if (buckets == null) Initialize(0);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;

#if FEATURE_RANDOMIZED_STRING_HASHING
            int collisionCount = 0;
#endif

            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next) {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) {
                    if (add) {
                        throw new ArgumentException("Key " + key + " are already exists");
                    }
                    entries[i].value = value;
                    version++;
                    return;
                }

#if FEATURE_RANDOMIZED_STRING_HASHING
                collisionCount++;
#endif
            }
            int index;
            if (freeCount > 0) {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            } else {
                if (count == entries.Length) {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }

            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
            version++;
        }

        private void Resize() {
            Resize(PrimeHelper.ExpandPrime(count), false);
        }

        private void Resize(int newSize, bool forceNewHashCodes) {
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            if (forceNewHashCodes) {
                for (int i = 0; i < count; i++) {
                    if (newEntries[i].hashCode != -1) {
                        newEntries[i].hashCode = (comparer.GetHashCode(newEntries[i].key) & 0x7FFFFFFF);
                    }
                }
            }
            for (int i = 0; i < count; i++) {
                if (newEntries[i].hashCode >= 0) {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        public bool Remove(TKey key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            if (buckets != null) {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next) {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) {
                        if (last < 0) {
                            buckets[bucket] = entries[i].next;
                        } else {
                            entries[last].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default;
                        entries[i].value = default;
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value) {
            int i = FindEntry(key);
            if (i >= 0) {
                value = entries[i].value;
                return true;
            }
            value = default;
            return false;
        }

        internal TValue GetValueOrDefault(TKey key) {
            int i = FindEntry(key);
            if (i >= 0) {
                return entries[i].value;
            }
            return default;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly {
            get { return false; }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index) {
            CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1) {
                throw new ArgumentException("Multiple dimensional array are not supported");
            }

            if (array.GetLowerBound(0) != 0) {
                throw new ArgumentException("Array got non-zero lower bound");
            }

            if (index < 0 || index > array.Length) {
                throw new ArgumentOutOfRangeException("index");
            }

            if (array.Length - index < Count) {
                throw new ArgumentException("Array size too small");
            }

            KeyValuePair<TKey, TValue>[] pairs = array as KeyValuePair<TKey, TValue>[];
            if (pairs != null) {
                CopyTo(pairs, index);
            } else if (array is DictionaryEntry[]) {
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                Entry[] entries = this.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) {
                        dictEntryArray[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                    }
                }
            } else {
                object[] objects = array as object[];
                if (objects == null) {
                    throw new ArgumentException("Invalid array type");
                }

                try {
                    int count = this.count;
                    Entry[] entries = this.entries;
                    for (int i = 0; i < count; i++) {
                        if (entries[i].hashCode >= 0) {
                            objects[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                        }
                    }
                } catch (ArrayTypeMismatchException) {
                    throw new ArgumentException("Invalid array type");
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        bool ICollection.IsSynchronized {
            get { return false; }
        }

        object ICollection.SyncRoot {
            get {
                if (_syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        bool IDictionary.IsFixedSize {
            get { return false; }
        }

        bool IDictionary.IsReadOnly {
            get { return false; }
        }

        ICollection IDictionary.Keys {
            get { return (ICollection)Keys; }
        }

        ICollection IDictionary.Values {
            get { return (ICollection)Values; }
        }

        object IDictionary.this[object key] {
            get {
                if (IsCompatibleKey(key)) {
                    int i = FindEntry((TKey)key);
                    if (i >= 0) {
                        return entries[i].value;
                    }
                }
                return null;
            }
            set {
                if (key == null) {
                    throw new ArgumentNullException("key");
                }

                try {
                    TKey tempKey = (TKey)key;
                    try {
                        this[tempKey] = (TValue)value;
                    } catch (InvalidCastException) {
                        throw new ArgumentException("Wrong value type. Expected type: " + typeof(TValue));
                    }
                } catch (InvalidCastException) {
                    throw new ArgumentException("Wrong key type. Expected type: " + typeof(TKey));
                }
            }
        }

        private static bool IsCompatibleKey(object key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }
            return (key is TKey);
        }

        void IDictionary.Add(object key, object value) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            try {
                TKey tempKey = (TKey)key;

                try {
                    Add(tempKey, (TValue)value);
                } catch (InvalidCastException) {
                    throw new ArgumentException("Wrong value type. Expected type: " + typeof(TValue));
                }
            } catch (InvalidCastException) {
                throw new ArgumentException("Wrong key type. Expected type: " + typeof(TKey));
            }
        }

        bool IDictionary.Contains(object key) {
            if (IsCompatibleKey(key)) {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return new Enumerator(this, Enumerator.DictEntry);
        }

        void IDictionary.Remove(object key) {
            if (IsCompatibleKey(key)) {
                Remove((TKey)key);
            }
        }

        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>,
            IDictionaryEnumerator {
            private SerializableDictionary<TKey, TValue> dictionary;
            private int version;
            private int index;
            private KeyValuePair<TKey, TValue> current;
            private int getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(SerializableDictionary<TKey, TValue> dictionary, int getEnumeratorRetType) {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext() {
                if (version != dictionary.version) {
                    throw new InvalidOperationException("Enumerator mismatch version");
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)index < (uint)dictionary.count) {
                    if (dictionary.entries[index].hashCode >= 0) {
                        current = new KeyValuePair<TKey, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }

                index = dictionary.count + 1;
                current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey, TValue> Current {
                get { return current; }
            }

            public void Dispose() {
            }

            object IEnumerator.Current {
                get {
                    if (index == 0 || (index == dictionary.count + 1)) {
                        throw new InvalidOperationException("Enumerator operation ran into issue");
                    }

                    if (getEnumeratorRetType == DictEntry) {
                        return new System.Collections.DictionaryEntry(current.Key, current.Value);
                    } else {
                        return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    }
                }
            }

            void IEnumerator.Reset() {
                if (version != dictionary.version) {
                    throw new InvalidOperationException("Enumerator mismatch version");
                }

                index = 0;
                current = new KeyValuePair<TKey, TValue>();
            }

            DictionaryEntry IDictionaryEnumerator.Entry {
                get {
                    if (index == 0 || (index == dictionary.count + 1)) {
                        throw new InvalidOperationException("Enumerator operation ran into issue");
                    }

                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key {
                get {
                    if (index == 0 || (index == dictionary.count + 1)) {
                        throw new InvalidOperationException("Enumerator operation ran into issue");
                    }

                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value {
                get {
                    if (index == 0 || (index == dictionary.count + 1)) {
                        throw new InvalidOperationException("Enumerator operation ran into issue");
                    }

                    return current.Value;
                }
            }
        }

        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey> {
            private SerializableDictionary<TKey, TValue> dictionary;

            public KeyCollection(SerializableDictionary<TKey, TValue> dictionary) {
                if (dictionary == null) {
                    throw new ArgumentNullException("dictionary");
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator() {
                return new Enumerator(dictionary);
            }

            public void CopyTo(TKey[] array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < dictionary.Count) {
                    throw new ArgumentException("Array size too small");
                }

                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].key;
                }
            }

            public int Count {
                get { return dictionary.Count; }
            }

            bool ICollection<TKey>.IsReadOnly {
                get { return true; }
            }

            void ICollection<TKey>.Add(TKey item) {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear() {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Contains(TKey item) {
                return dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item) {
                throw new NotSupportedException();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            void ICollection.CopyTo(Array array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1) {
                    throw new ArgumentException("Multiple dimensional array are not supported");
                }

                if (array.GetLowerBound(0) != 0) {
                    throw new ArgumentException("Array got non-zero lower bound");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < dictionary.Count) {
                    throw new ArgumentException("Array size too small");
                }

                TKey[] keys = array as TKey[];
                if (keys != null) {
                    CopyTo(keys, index);
                } else {
                    object[] objects = array as object[];
                    if (objects == null) {
                        throw new ArgumentException("Invalid array type");
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try {
                        for (int i = 0; i < count; i++) {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].key;
                        }
                    } catch (ArrayTypeMismatchException) {
                        throw new ArgumentException("Invalid array type");
                    }
                }
            }

            bool ICollection.IsSynchronized {
                get { return false; }
            }

            object ICollection.SyncRoot {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TKey>, System.Collections.IEnumerator {
                private SerializableDictionary<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TKey currentKey;

                internal Enumerator(SerializableDictionary<TKey, TValue> dictionary) {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentKey = default;
                }

                public void Dispose() {
                }

                public bool MoveNext() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("Enumerator mismatch version");
                    }

                    while ((uint)index < (uint)dictionary.count) {
                        if (dictionary.entries[index].hashCode >= 0) {
                            currentKey = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = dictionary.count + 1;
                    currentKey = default;
                    return false;
                }

                public TKey Current {
                    get {
                        return currentKey;
                    }
                }

                object IEnumerator.Current {
                    get {
                        if (index == 0 || (index == dictionary.count + 1)) {
                            throw new InvalidOperationException("Enumerator operation ran into issue");
                        }

                        return currentKey;
                    }
                }

                void System.Collections.IEnumerator.Reset() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("Enumerator mismatch version");
                    }

                    index = 0;
                    currentKey = default;
                }
            }
        }

        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue> {
            private SerializableDictionary<TKey, TValue> dictionary;

            public ValueCollection(SerializableDictionary<TKey, TValue> dictionary) {
                if (dictionary == null) {
                    throw new ArgumentNullException("dictionary");
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator() {
                return new Enumerator(dictionary);
            }

            public void CopyTo(TValue[] array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < Count) {
                    throw new ArgumentException("Array size too small");
                }

                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].value;
                }
            }

            public int Count {
                get { return dictionary.Count; }
            }

            bool ICollection<TValue>.IsReadOnly {
                get { return true; }
            }

            void ICollection<TValue>.Add(TValue item) {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Remove(TValue item) {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear() {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item) {
                return dictionary.ContainsValue(item);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            void ICollection.CopyTo(Array array, int index) {
                if (array == null) {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1) {
                    throw new ArgumentException("Multiple dimensional array are not supported");
                }

                if (array.GetLowerBound(0) != 0) {
                    throw new ArgumentException("Array got non-zero lower bound");
                }

                if (index < 0 || index > array.Length) {
                    throw new ArgumentOutOfRangeException("index");
                }

                if (array.Length - index < Count) {
                    throw new ArgumentException("Array size too small");
                }

                TValue[] values = array as TValue[];
                if (values != null) {
                    CopyTo(values, index);
                } else {
                    object[] objects = array as object[];
                    if (objects == null) {
                        throw new ArgumentException("Invalid array type");
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try {
                        for (int i = 0; i < count; i++) {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].value;
                        }
                    } catch (ArrayTypeMismatchException) {
                        throw new ArgumentException("Invalid array type");
                    }
                }
            }

            bool ICollection.IsSynchronized {
                get { return false; }
            }

            object ICollection.SyncRoot {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TValue>, System.Collections.IEnumerator {
                private SerializableDictionary<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TValue currentValue;

                internal Enumerator(SerializableDictionary<TKey, TValue> dictionary) {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default;
                }

                public void Dispose() {
                }

                public bool MoveNext() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("Enumerator mismatch version");
                    }

                    while ((uint)index < (uint)dictionary.count) {
                        if (dictionary.entries[index].hashCode >= 0) {
                            currentValue = dictionary.entries[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentValue = default;
                    return false;
                }

                public TValue Current {
                    get {
                        return currentValue;
                    }
                }

                object IEnumerator.Current {
                    get {
                        if (index == 0 || (index == dictionary.count + 1)) {
                            throw new InvalidOperationException("Enumerator operation ran into issue");
                        }

                        return currentValue;
                    }
                }

                void System.Collections.IEnumerator.Reset() {
                    if (version != dictionary.version) {
                        throw new InvalidOperationException("Enumerator mismatch version");
                    }
                    index = 0;
                    currentValue = default;
                }
            }
        }
    }
}