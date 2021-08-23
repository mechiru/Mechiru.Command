using System;
using System.Linq;
using Xunit;

namespace Mechiru.Command.Tests
{
    sealed record Opt1
    {
        [Option]
        public bool? Optional { get; init; }

        [Option(Required = true)]
        public bool Required { get; init; }
    }

    sealed record Opt2
    {
        [Option(Short = 'o', Long = "out")]
        public string? Output { get; init; }
    }

    sealed record Opt3
    {
        [Option(Required = true, Env = true)]
        public string Env1 { get; init; } = null!;

        [Option(Required = true, Short = 'e', Long = "my-env", Env = "MY_ENV")]
        public string Env2 { get; init; } = null!;
    }

    sealed record Opt4
    {
        [Option(Default = 123)]
        public int Int1 { get; init; }

        [Option(Default = "123")]
        public int Int2 { get; init; }
    }

    sealed record Opt5
    {
        [Option(Required = true, Env = true)]
        public int[] Ints { get; init; } = null!;
    }

    enum Mode : byte
    {
        Abort,
        Ignore,
    }

    sealed record Opt6
    {
        [Option]
        public Mode Mode1 { get; init; }

        [Option]
        public Mode Mode2 { get; init; }
    }

    sealed record Opt7
    {
        [Option]
        public TimeSpan TimeSpan { get; init; }

        [Option]
        public DateTime DateTime { get; init; }

        [Option]
        public DateTimeOffset DateTimeOffset { get; init; }

        [Option]
        public Guid Guid { get; init; }

        [Option]
        public Uri? Uri { get; init; }
    }

    public sealed class Example
    {
        [Fact]
        public void Opt1_Simple()
        {
            var opt = new ArgumentParser().Parse<Opt1>(new[] { "--optional", "true", "--required" });
            Assert.Equal(opt, new Opt1 { Optional = true, Required = true });
        }

        [Fact]
        public void Opt1_Required()
        {
            Assert.Throws<ArgumentException>(() => new ArgumentParser().Parse<Opt1>(new[] { "--optional", "true" }));
        }

        [Fact]
        public void Opt2_Short()
        {
            var opt = new ArgumentParser().Parse<Opt2>(new[] { "--o", "/tmp/out.txt" });
            Assert.Equal(opt, new Opt2 { Output = "/tmp/out.txt" });
        }

        [Fact]
        public void Opt2_Long()
        {
            var opt = new ArgumentParser().Parse<Opt2>(new[] { "--out", "/tmp/out.txt" });
            Assert.Equal(opt, new Opt2 { Output = "/tmp/out.txt" });
        }

        [Fact]
        public void Opt3_ByArguments()
        {
            Environment.SetEnvironmentVariable("ENV1", "env1-from-env-var");
            var opt = new ArgumentParser().Parse<Opt3>(new[] { "--env1", "env1", "--my-env", "env2" });
            Assert.Equal(opt, new Opt3 { Env1 = "env1", Env2 = "env2" });
            Environment.SetEnvironmentVariable("ENV1", null);
        }

        [Fact]
        public void Opt3_ByEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("ENV1", "env1-from-env-var");
            Environment.SetEnvironmentVariable("MY_ENV", "env2-from-env-var");
            var opt = new ArgumentParser().Parse<Opt3>(Array.Empty<string>());
            Assert.Equal(opt, new Opt3 { Env1 = "env1-from-env-var", Env2 = "env2-from-env-var" });
            Environment.SetEnvironmentVariable("ENV1", null);
            Environment.SetEnvironmentVariable("MY_ENV", null);
        }

        [Fact]
        public void Opt4_Default()
        {
            var opt = new ArgumentParser().Parse<Opt4>(Array.Empty<string>());
            Assert.Equal(opt, new Opt4 { Int1 = 123, Int2 = 123 });
        }

        [Fact]
        public void Opt5_Array_ByArguments()
        {
            var opt = new ArgumentParser().Parse<Opt5>(new[] { "--ints", "1", "2", "3" });
            Assert.True(opt.Ints.SequenceEqual(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void Opt5_ByEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("INTS", "1 2 3");
            var opt = new ArgumentParser().Parse<Opt5>(Array.Empty<string>());
            Assert.True(opt.Ints.SequenceEqual(new[] { 1, 2, 3 }));
            Environment.SetEnvironmentVariable("INTS", null);
        }

        [Fact]
        public void Opt6_Enum()
        {
            var opt = new ArgumentParser().Parse<Opt6>(new[] { "--mode1", "0", "--mode2", "Ignore" });
            Assert.Equal(opt, new Opt6 { Mode1 = Mode.Abort, Mode2 = Mode.Ignore });
        }

        [Fact]
        public void Opt7_CustomType()
        {
            var opt = new ArgumentParser().Parse<Opt7>(new[]
            {
                "--timespan", "1",
                "--datetime", "2021-08-22T09:05:07Z",
                "--datetimeoffset", "2021-08-22T09:05:07Z",
                "--guid", "1D381F5A-99D8-4F7E-9AFA-72C72AAFB555",
                "--uri", "https://www.google.com"
            });
            Assert.Equal(opt, new Opt7
            {
                TimeSpan = new TimeSpan(1, 0, 0, 0, 0),
                DateTime = new DateTime(2021, 8, 22, 9, 5, 7, DateTimeKind.Utc),
                DateTimeOffset = new DateTimeOffset(2021, 8, 22, 9, 5, 7, TimeSpan.Zero),
                Guid = new Guid("1D381F5A-99D8-4F7E-9AFA-72C72AAFB555"),
                Uri = new Uri("https://www.google.com")
            });
        }
    }
}
