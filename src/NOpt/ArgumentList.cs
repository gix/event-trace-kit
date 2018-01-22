namespace NOpt
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;

    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    internal sealed class ArgumentList : IArgumentList
    {
        private readonly List<Arg> args = new List<Arg>();

        public int Count => args.Count;

        public Arg this[int index]
        {
            get => args[index];
            set => args[index] = value;
        }

        public bool Remove(Arg arg)
        {
            return args.Remove(arg);
        }

        bool ICollection<Arg>.IsReadOnly => false;

        int IList<Arg>.IndexOf(Arg arg)
        {
            return args.IndexOf(arg);
        }

        void IList<Arg>.Insert(int index, Arg arg)
        {
            args.Insert(index, arg);
        }

        void IList<Arg>.RemoveAt(int index)
        {
            args.RemoveAt(index);
        }

        public void Add(Arg arg)
        {
            args.Add(arg);
        }

        public void Clear()
        {
            args.Clear();
        }

        public bool Contains(Arg arg)
        {
            return args.Contains(arg);
        }

        public void CopyTo(Arg[] array, int arrayIndex)
        {
            args.CopyTo(array, arrayIndex);
        }

        public IEnumerable<Arg> Matching(OptSpecifier id)
        {
            return args.Where(arg => arg.Option.Matches(id));
        }

        public bool HasArg(OptSpecifier id)
        {
            return GetLastArg(id) != null;
        }

        public bool HasArgNoClaim(OptSpecifier id)
        {
            return GetLastArgNoClaim(id) != null;
        }

        public bool GetFlag(OptSpecifier id, bool defaultValue = false)
        {
            Arg arg = GetLastArg(id);
            if (arg != null)
                return arg.Option.Matches(id);
            return defaultValue;
        }

        public bool GetFlag(
            OptSpecifier positiveId, OptSpecifier negativeId, bool defaultValue = false)
        {
            Arg arg = GetLastArg(positiveId, negativeId);
            if (arg != null)
                return arg.Option.Matches(positiveId);
            return defaultValue;
        }

        public bool GetFlagNoClaim(
            OptSpecifier positiveId, OptSpecifier negativeId, bool defaultValue = false)
        {
            Arg arg = GetLastArgNoClaim(positiveId, negativeId);
            if (arg != null)
                return arg.Option.Matches(positiveId);
            return defaultValue;
        }

        public Arg GetLastArg(OptSpecifier id)
        {
            Arg lastArg = null;
            foreach (var arg in args) {
                if (arg.Option.Matches(id)) {
                    arg.Claim();
                    lastArg = arg;
                }
            }
            return lastArg;
        }

        public Arg GetLastArg(OptSpecifier id1, OptSpecifier id2)
        {
            Arg lastArg = null;
            foreach (var arg in args) {
                if (arg.Option.Matches(id1) || arg.Option.Matches(id2)) {
                    arg.Claim();
                    lastArg = arg;
                }
            }
            return lastArg;
        }

        public Arg GetLastArg(params OptSpecifier[] ids)
        {
            Arg lastArg = null;
            foreach (var arg in args) {
                if (ids.Any(id => arg.Option.Matches(id))) {
                    arg.Claim();
                    lastArg = arg;
                }
            }
            return lastArg;
        }

        public Arg GetLastArgNoClaim(OptSpecifier id)
        {
            for (int i = args.Count - 1; i >= 0; --i) {
                var arg = args[i];
                if (arg.Option.Matches(id))
                    return arg;
            }
            return null;
        }

        public Arg GetLastArgNoClaim(OptSpecifier id1, OptSpecifier id2)
        {
            for (int i = args.Count - 1; i >= 0; --i) {
                var arg = args[i];
                if (arg.Option.Matches(id1) || arg.Option.Matches(id2))
                    return arg;
            }
            return null;
        }

        public string GetLastArgValue(OptSpecifier id, string defaultValue = null)
        {
            Arg lastArg = GetLastArg(id);
            if (lastArg == null)
                return defaultValue;
            return lastArg.Value;
        }

        public IList<string> GetAllArgValues(OptSpecifier id)
        {
            var allValues = new List<string>();
            foreach (var arg in args) {
                if (arg.Option.Matches(id)) {
                    arg.Claim();
                    allValues.AddRange(arg.Values);
                }
            }
            return allValues;
        }

        public void ClaimAllArgs()
        {
            foreach (var arg in args)
                arg.Claim();
        }

        public void ClaimAllArgs(OptSpecifier id)
        {
            foreach (var arg in args) {
                if (arg.Option.Matches(id))
                    arg.Claim();
            }
        }

        public void RemoveAllArgs(OptSpecifier id)
        {
            args.RemoveAll(a => a.Option.Matches(id));
        }

        public IEnumerator<Arg> GetEnumerator()
        {
            return args.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <devdoc>
        ///   This explicit implementations exists to silence the contract rewriter.
        /// </devdoc>
        Arg IReadOnlyList<Arg>.this[int index] => args[index];

        /// <devdoc>
        ///   This explicit implementations exists to silence the contract rewriter.
        /// </devdoc>
        Arg IList<Arg>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        /// <devdoc>
        ///   This explicit implementations exists to silence the contract rewriter.
        /// </devdoc>
        void ICollection<Arg>.Add(Arg arg)
        {
            Add(arg);
        }

        [ExcludeFromCodeCoverage]
        private sealed class DebuggerProxy
        {
            private readonly ICollection<Arg> collection;

            public DebuggerProxy(ICollection<Arg> collection)
            {
                Contract.Requires<ArgumentNullException>(collection != null);
                this.collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Arg[] Items
            {
                get
                {
                    var items = new Arg[collection.Count];
                    collection.CopyTo(items, 0);
                    return items;
                }
            }
        }
    }
}
