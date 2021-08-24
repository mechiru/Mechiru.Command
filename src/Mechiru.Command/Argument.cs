using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mechiru.Command
{
    public interface IDefault { public object Default(); }
    public interface IParser { public object Parse(string s); }

    internal interface IArg { }
    internal sealed record ArgDefault(object Value) : IArg;
    internal sealed record ArgValueless : IArg;
    internal sealed record ArgValue(string Value) : IArg;
    internal sealed record ArgArray(IReadOnlyList<string> Value) : IArg;

    internal sealed record ArgSpec(
        PropertyInfo Property,
        OptionAttribute Option,
        string LowerName,
        string UpperName
    )
    {
        public ArgSpec(PropertyInfo prop, OptionAttribute opt) : this(prop, opt, prop.Name.ToLower(), prop.Name.ToUpper()) { }
    }

    internal static class EnumerableArgSpecExt
    {
        public static ArgSpec Find(this IEnumerable<ArgSpec> self, string name)
        {
            foreach (ArgSpec spec in self)
            {
                if (name.Length == 1 && spec.Option.Short != default && name[0] == spec.Option.Short) return spec;
                if (name == spec.Option.Long) return spec;
                if (name == spec.LowerName) return spec;
            }
            throw new ArgumentException($"unknown option name: `{name}`");
        }
    }
}
