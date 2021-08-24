using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Mechiru.Command
{
    public readonly struct ArgumentParser
    {
        public static T ParseDefault<T>() => new ArgumentParser().Parse<T>(Environment.GetCommandLineArgs().Skip(1));

        public T Parse<T>(IEnumerable<string> args)
        {
            var type = typeof(T);
            var props = type.GetProperties();

            // TODO: check specs(duplicate, etc...)?

            var ctor = type.GetConstructor(props.Select(p => p.PropertyType).ToArray());
            var param = ctor?.GetParameters().Select(p => (p.GetCustomAttribute(typeof(OptionAttribute)) as OptionAttribute)!);
            if (ctor is not null && param is not null)
            {
                var specs = props.Zip(param).Select(ps => new ArgSpec(ps.First, ps.Second)).ToArray();
                var cmdArgs = ParseInner(args, specs);
                object[] ctorParams = cmdArgs.Select(arg => ParseArg(arg.Key.Property.PropertyType, arg.Value, arg.Key.Option.Parser?.As<IParser>())).ToArray();
                return (T)ctor.Invoke(ctorParams);
            }
            else
            {
                var specs = props
                    .Select(prop => (prop, attr: prop.GetCustomAttribute(typeof(OptionAttribute)) as OptionAttribute))
                    .Where(prop => prop.attr is not null)
                    .Select(prop => new ArgSpec(prop.prop, prop.attr!))
                    .ToArray();

                var cmdArgs = ParseInner(args, specs);

                var instance = Activator.CreateInstance<T>();
                foreach (var ((prop, opt, _, _), arg) in cmdArgs)
                    prop.SetValue(instance, ParseArg(prop.PropertyType, arg, opt.Parser?.As<IParser>()));
                return instance;
            }
        }

        internal ReadOnlyDictionary<ArgSpec, IArg> ParseInner(IEnumerable<string> args, ArgSpec[] specs)
        {
            using var iter = args.GetEnumerator();
            var cmdArgs = new Dictionary<ArgSpec, IArg>();

            while (iter.MoveNext())
            {
                // argument name
                int start = iter.Current switch
                {
                    var s when s.StartsWith("--") && s.Length > 2 => 2,
                    var s when s.StartsWith('-') && s.Length > 1 => 1,
                    _ => throw new ArgumentException($"unrecognized option name: `{iter.Current}`")
                };
                string name = iter.Current.AsSpan()[start..].ToString();

                var spec = specs.Find(name);
                var propType = spec.Property.PropertyType;

                // argument value
                IArg? cmdArg;
                if (propType == typeof(bool) || propType == typeof(bool?))
                {
                    if (!iter.MoveNext() || iter.Current.StartsWith('-')) cmdArg = new ArgValueless();
                    else cmdArg = new ArgValue(iter.Current);
                }
                else if (propType.IsArray)
                {
                    var values = new List<string>();
                    while (iter.MoveNext()) if (!iter.Current.StartsWith('-')) values.Add(iter.Current);
                    cmdArg = new ArgArray(values);
                }
                else
                {
                    if (!iter.MoveNext()) throw new ArgumentException($"not found option value: `{name}`");
                    cmdArg = new ArgValue(iter.Current);
                }

                if (!cmdArgs.TryAdd(spec, cmdArg)) throw new ArgumentException($"already presents option: `{name}`");
            }

            foreach (var spec in specs)
            {
                if (cmdArgs.TryGetValue(spec, out _)) continue;
                IArg? cmdArg = null;
                string? value = spec.Option.Env switch
                {
                    null or false => null,
                    true => Environment.GetEnvironmentVariable(spec.UpperName),
                    string name => Environment.GetEnvironmentVariable(name),
                    _ => throw new ArgumentException($"`Env` must be a value of type bool or string: `{spec.Option.Env.GetType().FullName}`")
                };
                if (value is not null) cmdArg = new ArgValue(value);
                else if (spec.Option.Default is not null) cmdArg = new ArgDefault(spec.Option.Default);
                else if (spec.Option.Required) throw new ArgumentException($"required option not specified: `{spec.Option.Long ?? spec.LowerName}`");
                if (cmdArg is not null) cmdArgs.Add(spec, cmdArg);
            }

            return new ReadOnlyDictionary<ArgSpec, IArg>(cmdArgs);
        }

        internal static object ParseArg(Type type, IArg arg, IParser? parser) => arg switch
        {
            ArgDefault @default => ParseArgDefault(type, @default, parser),
            ArgValueless less => ParseArgValueless(type, less),
            ArgValue value => ParseArgValue(type, value, parser),
            ArgArray array => ParseArgArray(type, array, parser),
            _ => throw new ArgumentException(null, nameof(arg))
        };

        internal static object ParseArgDefault(Type type, ArgDefault arg, IParser? parser) => arg.Value switch
        {
            Type ty when ty.IsAssignableTo(typeof(IDefault)) => ty.As<IDefault>()!.Default(),
            string value => parser is not null ? parser.Parse(value) : GetTypeParser(type)(value),
            var value => value
        };

        internal static object ParseArgValueless(Type type, ArgValueless arg) =>
            type == typeof(bool) || type == typeof(bool?) ? true : throw new ArgumentException($"`ArgValueless` is supported only for the bool type: {type.FullName}");

        internal static object ParseArgValue(Type type, ArgValue arg, IParser? parser) =>
            type.IsArray
                ? ParseArgArrayValue(type, arg.Value, parser)
                : parser is not null ? parser.Parse(arg.Value) : GetTypeParser(type)(arg.Value);


        internal static object ParseArgArray(Type type, ArgArray arg, IParser? parser) => ParseArgArrayValue(type, arg.Value, parser);

        internal static object ParseArgArrayValue(Type type, object args, IParser? parser)
        {
            if (!type.IsArray) throw new ArgumentException(null, nameof(type));

            if (args is string s)
            {
                if (parser is not null) return parser.Parse(s);
                args = s.Split(" ");
            }

            if (args is IReadOnlyList<string> xs)
            {
                if (parser is not null)
                {
                    if (xs.Count != 1) throw new ArgumentException($"if custom parser is specified, only one argument is accepted: {xs.Count}");
                    return parser.Parse(xs[0]);
                }
                var elemType = type.GetElementType()!;
                var elemParser = GetTypeParser(elemType);
                var array = Array.CreateInstance(elemType, xs.Count);
                for (int i = 0; i < xs.Count; i++) array.SetValue(elemParser(xs[i]), i);
                return array;
            }

            throw new ArgumentException(null, nameof(args));
        }

        internal static Func<string, object> GetTypeParser(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string)) return s => s;
            if (type.IsEnum) return s => int.TryParse(s, out int i) ? Enum.ToObject(type, i) : Enum.Parse(type, s);
            // primitive types
            if (type == typeof(bool)) return s => bool.Parse(s);
            if (type == typeof(byte)) return s => byte.Parse(s);
            if (type == typeof(sbyte)) return s => sbyte.Parse(s);
            if (type == typeof(char)) return s => char.Parse(s);
            if (type == typeof(decimal)) return s => decimal.Parse(s);
            if (type == typeof(double)) return s => double.Parse(s);
            if (type == typeof(float)) return s => float.Parse(s);
            if (type == typeof(int)) return s => int.Parse(s);
            if (type == typeof(uint)) return s => uint.Parse(s);
            if (type == typeof(long)) return s => long.Parse(s);
            if (type == typeof(ulong)) return s => ulong.Parse(s);
            if (type == typeof(short)) return s => short.Parse(s);
            if (type == typeof(ushort)) return s => ushort.Parse(s);
            // time related types
            if (type == typeof(TimeSpan)) return s => TimeSpan.Parse(s);
            if (type == typeof(DateTime)) return s => DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
            if (type == typeof(DateTimeOffset)) return s => DateTimeOffset.Parse(s, null, DateTimeStyles.RoundtripKind);
            // custom object
            var ctor = type.GetTypeInfo().GetConstructor(new[] { typeof(string) }) ?? throw new ArgumentException($"does not support the target type: `{type.FullName}`");
            return s => ctor.Invoke(new object?[] { s });
        }
    }

    internal static class TypeExt
    {
        public static T? As<T>(this Type self) => self.IsAssignableTo(typeof(T)) ? (T)Activator.CreateInstance(self)! : default;
    }
}
