namespace NOpt
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    /// <summary>
    ///   Represents an ordered collection of command-line arguments.
    /// </summary>
    [ContractClass(typeof(IArgumentListContract))]
    public interface IArgumentList : IList<Arg>, IReadOnlyList<Arg>
    {
        /// <summary>
        ///   Returns all arguments matching the specified id.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <returns>
        ///   An IEnumerable {Arg} that contains all arguments matching the specified
        ///   id.
        /// </returns>
        IEnumerable<Arg> Matching(OptSpecifier id);

        /// <summary>
        ///   Determines whether the argument list has any arguments matching the
        ///   specified <paramref name="id"/> and claims all such arguments.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if any argument matches <paramref name="id"/>,
        ///   otherwise <see langword="false"/>.
        /// </returns>
        bool HasArg(OptSpecifier id);

        /// <summary>
        ///   Determines whether the argument list has any arguments matching
        ///   the specified <paramref name="id"/> without claiming any.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if any argument matches <paramref name="id"/>,
        ///   otherwise <see langword="false"/>.
        /// </returns>
        bool HasArgNoClaim(OptSpecifier id);

        /// <summary>
        ///   Finds the last argument matching the specified id and returns
        ///   <see langword="true"/> if found, or a default value if the option
        ///   is unmatched. Claims all matching arguments.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <param name="defaultValue">
        ///   The value to return if no matching argument is found.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the option is matched; otherwise
        ///   <paramref name="defaultValue"/>.
        /// </returns>
        bool GetFlag(OptSpecifier id, bool defaultValue = false);

        /// <summary>
        ///   Finds the last argument matching any of the specified ids and returns
        ///   <see langword="true"/> if it matches the positive option,
        ///   <see langword="false"/> if it matches the negation, or a default
        ///   value if neither option is matched. Claims all matching arguments.
        /// </summary>
        /// <param name="positiveId">
        ///   The positive option id.
        /// </param>
        /// <param name="negativeId">
        ///   The negative option id.
        /// </param>
        /// <param name="defaultValue">
        ///   The value to return if no matching argument is found.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the positive option is matched,
        ///   <see langword="false"/> if its negation is matched, and
        ///   <paramref name="defaultValue"/> if neither option is matched. If
        ///   both the option and its negation are matched, the last one is used.
        /// </returns>
        bool GetFlag(OptSpecifier positiveId, OptSpecifier negativeId, bool defaultValue = true);

        /// <summary>
        ///   Finds the last argument matching any of the specified ids and returns
        ///   <see langword="true"/> if it matches the positive option,
        ///   <see langword="false"/> if it matches the negation, or a default
        ///   value if neither option is matched. Claims no arguments.
        /// </summary>
        /// <param name="positiveId">
        ///   The positive option id.
        /// </param>
        /// <param name="negativeId">
        ///   The negative option id.
        /// </param>
        /// <param name="defaultValue">
        ///   The value to return if no matching argument is found.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the positive option is matched,
        ///   <see langword="false"/> if its negation is matched, and
        ///   <paramref name="defaultValue"/> if neither option is matched. If
        ///   both the option and its negation are matched, the last one is used.
        /// </returns>
        bool GetFlagNoClaim(OptSpecifier positiveId, OptSpecifier negativeId, bool defaultValue = true);

        /// <summary>
        ///   Gets the last argument matching the specified <paramref name="id"/>.
        ///   Claims all matching arguments.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <returns>
        ///   The last argument or <see langword="null"/> if no argument matches
        ///   <paramref name="id"/>.
        /// </returns>
        Arg GetLastArg(OptSpecifier id);

        /// <summary>
        ///   Gets the last argument matching any of the specified ids. Claims
        ///   all matching arguments.
        /// </summary>
        /// <param name="id1">
        ///   The first option id to look for.
        /// </param>
        /// <param name="id2">
        ///   The second option id to look for.
        /// </param>
        /// <returns>
        ///   The last argument or <see langword="null"/> if no argument matches
        ///   any id.
        /// </returns>
        Arg GetLastArg(OptSpecifier id1, OptSpecifier id2);

        /// <summary>
        ///   Gets the last argument matching any of the specified ids. Claims
        ///   all matching arguments.
        /// </summary>
        /// <param name="ids">
        ///   The option ids to look for.
        /// </param>
        /// <returns>
        ///   The last argument or <see langword="null"/> if no argument matches
        ///   any id.
        /// </returns>
        Arg GetLastArg(params OptSpecifier[] ids);

        /// <summary>
        ///   Gets the last argument matching the specified <paramref name="id"/>
        ///   without claiming any arguments.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <returns>
        ///   The last matching argument or <see langword="null"/> if no argument
        ///   matches <paramref name="id"/>.
        /// </returns>
        Arg GetLastArgNoClaim(OptSpecifier id);

        /// <summary>
        ///   Gets the last argument matching any of the specified ids without
        ///   claiming it.
        /// </summary>
        /// <param name="id1">
        ///   The first option id to look for.
        /// </param>
        /// <param name="id2">
        ///   The second option id to look for.
        /// </param>
        /// <returns>
        ///   The last matching argument or <see langword="null"/> if no argument
        ///   matches.
        /// </returns>
        Arg GetLastArgNoClaim(OptSpecifier id1, OptSpecifier id2);

        /// <summary>
        ///   Gets the value of the last argument matching the specified id, or
        ///   a default value if no argument is found.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <param name="defaultValue">
        ///   The value to return if no matching argument is found.
        /// </param>
        /// <returns>
        ///   The value of the last matching argument; otherwise,
        ///   <paramref name="defaultValue"/>.
        /// </returns>
        string GetLastArgValue(OptSpecifier id, string defaultValue = null);

        /// <summary>
        ///   Gets the values of all arguments matching the specified id.
        /// </summary>
        /// <param name="id">
        ///   The option id to look for.
        /// </param>
        /// <returns>
        ///   A list of values of all matching arguments.
        /// </returns>
        IList<string> GetAllArgValues(OptSpecifier id);

        /// <summary>Claims all arguments.</summary>
        void ClaimAllArgs();

        /// <summary>Claims all arguments matching the specified id.</summary>
        /// <param name="id">The option id to claim.</param>
        void ClaimAllArgs(OptSpecifier id);

        /// <summary>Removes all arguments matching the specified id.</summary>
        /// <param name="id">The option id to remove.</param>
        void RemoveAllArgs(OptSpecifier id);

        /// <summary>
        ///   Gets the number of arguments contained in the <see cref="IArgumentList"/>.
        /// </summary>
        /// <returns>
        ///   The number of arguments contained in the <see cref="IArgumentList"/>.
        /// </returns>
        new int Count { get; }

        /// <summary>
        ///   Gets or sets the argument at the specified index.
        /// </summary>
        /// <param name="index">
        ///   The zero-based index of the argument to get or set.
        /// </param>
        /// <returns>
        ///   The argument at the specified index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is not a valid index in the
        ///   <see cref="IArgumentList"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        new Arg this[int index] { get; set; }

        /// <summary>
        ///   Adds the specified argument to the list.
        /// </summary>
        /// <param name="arg">
        ///   The argument to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="arg"/> is <see langword="null"/>.
        /// </exception>
        new void Add(Arg arg);
    }

    /// <summary>Contract for <see cref="IArgumentList"/>.</summary>
    [ContractClassFor(typeof(IArgumentList))]
    internal abstract class IArgumentListContract : IArgumentList
    {
        IEnumerator<Arg> IEnumerable<Arg>.GetEnumerator()
        {
            return default(IEnumerator<Arg>);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }

        void ICollection<Arg>.Add(Arg item)
        {
        }

        void ICollection<Arg>.Clear()
        {
        }

        bool ICollection<Arg>.Contains(Arg item)
        {
            return default(bool);
        }

        void ICollection<Arg>.CopyTo(Arg[] array, int arrayIndex)
        {
        }

        bool ICollection<Arg>.Remove(Arg item)
        {
            return default(bool);
        }

        int ICollection<Arg>.Count => default(int);

        bool ICollection<Arg>.IsReadOnly => default(bool);

        int IList<Arg>.IndexOf(Arg item)
        {
            return default(int);
        }

        void IList<Arg>.Insert(int index, Arg item)
        {
        }

        void IList<Arg>.RemoveAt(int index)
        {
        }

        Arg IList<Arg>.this[int index]
        {
            get { return default(Arg); }
            set { }
        }

        int IReadOnlyCollection<Arg>.Count => default(int);

        Arg IReadOnlyList<Arg>.this[int index] => default(Arg);

        IEnumerable<Arg> IArgumentList.Matching(OptSpecifier id)
        {
            Contract.Ensures(Contract.Result<IEnumerable<Arg>>() != null);
            return default(IEnumerable<Arg>);
        }

        bool IArgumentList.HasArg(OptSpecifier id)
        {
            return default(bool);
        }

        bool IArgumentList.HasArgNoClaim(OptSpecifier id)
        {
            return default(bool);
        }

        bool IArgumentList.GetFlag(
            OptSpecifier id, bool defaultValue)
        {
            return default(bool);
        }

        bool IArgumentList.GetFlag(
            OptSpecifier positiveId, OptSpecifier negativeId, bool defaultValue)
        {
            return default(bool);
        }

        bool IArgumentList.GetFlagNoClaim(
            OptSpecifier positiveId, OptSpecifier negativeId, bool defaultValue)
        {
            return default(bool);
        }

        Arg IArgumentList.GetLastArg(OptSpecifier id)
        {
            return default(Arg);
        }

        Arg IArgumentList.GetLastArg(OptSpecifier id1, OptSpecifier id2)
        {
            return default(Arg);
        }

        Arg IArgumentList.GetLastArg(params OptSpecifier[] ids)
        {
            Contract.Requires<ArgumentNullException>(ids != null);
            return default(Arg);
        }

        Arg IArgumentList.GetLastArgNoClaim(OptSpecifier id)
        {
            return default(Arg);
        }

        string IArgumentList.GetLastArgValue(OptSpecifier id, string defaultValue)
        {
            return default(string);
        }

        Arg IArgumentList.GetLastArgNoClaim(OptSpecifier id1, OptSpecifier id2)
        {
            return default(Arg);
        }

        IList<string> IArgumentList.GetAllArgValues(OptSpecifier id)
        {
            Contract.Ensures(Contract.Result<IList<string>>() != null);
            return default(IList<string>);
        }

        void IArgumentList.ClaimAllArgs()
        {
        }

        void IArgumentList.ClaimAllArgs(OptSpecifier id)
        {
        }

        void IArgumentList.RemoveAllArgs(OptSpecifier id)
        {
        }

        int IArgumentList.Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return default(int);
            }
        }

        Arg IArgumentList.this[int index]
        {
            get
            {
                Contract.Requires<ArgumentOutOfRangeException>(
                    index >= 0 && index < ((IList<Arg>)this).Count);
                Contract.Ensures(Contract.Result<Arg>() != null);
                return default(Arg);
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(
                    index >= 0 && index < ((IList<Arg>)this).Count);
                Contract.Requires<ArgumentNullException>(value != null);
            }
        }

        void IArgumentList.Add(Arg arg)
        {
            Contract.Requires<ArgumentNullException>(arg != null);
        }
    }
}
