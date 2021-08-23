using System;

namespace Mechiru.Command
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionAttribute : Attribute
    {
        public bool Required { get; init; }
        public char Short { get; init; }
        public string? Long { get; init; }
        public object? Env { get; init; }
        public string? Help { get; init; }
        public object? Default { get; init; }
        public Type? Parser { get; init; }
    }
}
