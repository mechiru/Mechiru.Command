using System;

namespace Mechiru.Command
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class OptionAttribute : Attribute
    {
        private readonly object? _default;
        internal readonly bool _hasDefault;

        public bool Required { get; init; }
        public char Short { get; init; }
        public string? Long { get; init; }
        public object? Env { get; init; }
        public string? Help { get; init; }

        public object? Default
        {
            get => _default;
            init
            {
                _hasDefault = true;
                _default = value;
            }
        }
        public Type? Parser { get; init; }
    }
}
