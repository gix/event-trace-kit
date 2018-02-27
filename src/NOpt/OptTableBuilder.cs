namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    public sealed class OptTableBuilder
    {
        private readonly List<Option> options = new List<Option>();
        private bool hasUnknown;

        public OptTableBuilder()
        {
            Reset();
        }

        public void Reset()
        {
            options.Clear();
            hasUnknown = false;
        }

        public OptTableBuilder Add(Option option)
        {
            Contract.Requires<ArgumentNullException>(option != null);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddUnknown(OptSpecifier id)
        {
            Contract.Requires<ArgumentException>(id.IsValid);

            options.Add(new UnknownOption(id.Id));
            hasUnknown = true;
            return this;
        }

        public OptTableBuilder AddInput(OptSpecifier id)
        {
            Contract.Requires<ArgumentException>(id.IsValid);

            options.Add(new InputOption(id.Id));
            return this;
        }

        public OptTableBuilder AddGroup(
            OptSpecifier id,
            string name,
            string helpText = null,
            string metaVar = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new GroupOption(
                id.Id,
                name,
                helpText: helpText,
                metaVar: metaVar);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddFlag(
            OptSpecifier id,
            string prefix,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new FlagOption(
                id.Id,
                prefix,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddFlag(
            OptSpecifier id,
            string[] prefixes,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new FlagOption(
                id.Id,
                prefixes,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddJoined(
            OptSpecifier id,
            string prefix,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new JoinedOption(
                id.Id,
                prefix,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddJoined(
            OptSpecifier id,
            string[] prefixes,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new JoinedOption(
                id.Id,
                prefixes,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddSeparate(
            OptSpecifier id,
            string prefix,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new SeparateOption(
                id.Id,
                prefix,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddSeparate(
            OptSpecifier id,
            string[] prefixes,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new SeparateOption(
                id.Id,
                prefixes,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddJoinedOrSeparate(
            OptSpecifier id,
            string prefix,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new JoinedOrSeparateOption(
                id.Id,
                prefix,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddJoinedOrSeparate(
            OptSpecifier id,
            string[] prefixes,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new JoinedOrSeparateOption(
                id.Id,
                prefixes,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddMultiArg(
            OptSpecifier id,
            string prefix,
            string name,
            int argumentCount,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentOutOfRangeException>(argumentCount > 0);

            var option = new MultiArgOption(
                id.Id,
                prefix,
                name,
                argumentCount,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddMultiArg(
            OptSpecifier id,
            string[] prefixes,
            string name,
            int argumentCount,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentOutOfRangeException>(argumentCount > 0);

            var option = new MultiArgOption(
                id.Id,
                prefixes,
                name,
                argumentCount,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddRemainingArgs(
            OptSpecifier id,
            string prefix,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new RemainingArgsOption(
                id.Id,
                prefix,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public OptTableBuilder AddRemainingArgs(
            OptSpecifier id,
            string[] prefixes,
            string name,
            string helpText = null,
            string metaVar = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);

            var option = new RemainingArgsOption(
                id.Id,
                prefixes,
                name,
                helpText: helpText,
                metaVar: metaVar,
                aliasId: aliasId,
                groupId: groupId);
            options.Add(option);
            return this;
        }

        public IList<Option> GetList()
        {
            if (!hasUnknown) {
                int maxId = options.Max(o => o.Id);
                AddUnknown(maxId + 1);
            }
            return options;
        }

        public OptTable CreateTable()
        {
            return new OptTableImpl(GetList());
        }

        private sealed class OptTableImpl : OptTable
        {
            public OptTableImpl(IEnumerable<Option> options)
                : base(options)
            {
            }
        }
    }
}
