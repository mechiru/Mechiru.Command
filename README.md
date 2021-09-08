# Mechiru.Command

[![ci](https://github.com/mechiru/Mechiru.Command/workflows/ci/badge.svg)](https://github.com/mechiru/Mechiru.Command/actions?query=workflow:ci)
[![nuget](https://img.shields.io/nuget/v/Mechiru.Command.svg)](https://www.nuget.org/packages/Mechiru.Command/)

Mechiru.Command is the simplest command line argument parser.<br>
🚧 This library is implemented for container applications and has some limitations. 🚧

```csharp
record Opt(
    [Option] string Foo,
    [Option] bool Bar
);

Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ dotnet run --foo hello --bar
Opt { Foo = hello, Bar = True }
```


### TargetFrameworks
.NET 5+


### Notice
You can also use the standard argument parser instead of relying on a third party library.<br>
Check [this documentation](https://docs.microsoft.com/en-us/archive/msdn-magazine/2019/march/net-parse-the-command-line-with-system-commandline) for details.


### Features:
- Support environment variables [commandlineparser/commandline#677](https://github.com/commandlineparser/commandline/issues/677)
- Default value from function through interface
- Record class friendly


### Examples


#### Required arguments
```csharp
record Opt([Option(Required = true)] string Foo);
Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ dotnet run
Unhandled exception. System.ArgumentException: required option not specified: `foo`
   at Mechiru.Command.ArgumentParser.ParseInner(IEnumerable`1 args, ArgSpec[] specs)
   at Mechiru.Command.ArgumentParser.Parse[T](IEnumerable`1 args)
   at Mechiru.Command.ArgumentParser.ParseDefault[T]()
   at test.Program.Main(String[] args) in /tmp/test/Program.cs:line 12
```


#### Environment variables
```csharp
record Opt([Option(Env = true)] string Foo);
Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ FOO=fuga dotnet run
Opt { Foo = fuga }
```

You can also override the name of the environment variable:
```csharp
record Opt([Option(Env = "MY_FOO")] string Foo);
Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ MY_FOO=my-fuga dotnet run
Opt { Foo = my-fuga }
```


#### Default values
```csharp
record Opt([Option(Default = "hoge")] string Foo);
Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ dotnet run
Opt { Foo = hoge }
```

You can also set default value from object:
```csharp
record Opt([Option(Default = 20)] int Age);
Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ dotnet run
Opt { Age = 20 }
```

You can also set default value from function through interface:
```csharp
record Opt([Option(Default = typeof(MyDefaultValue))] string Foo);

class MyDefaultValue : IDefault
{
    public object Default() => "value from function";
} 

Console.WriteLine(ArgumentParser.ParseDefault<Opt>());
```

```console
$ dotnet run
Opt { Foo = value from function }
```


#### Custom parsing
```csharp
record Opt([Option(Parser = typeof(JsonArrayParser))] string[] Values);

class JsonArrayParser : IParser
{
    public object Parse(string s) => JsonSerializer.Deserialize<string[]>(s)!;
}

var opt = ArgumentParser.ParseDefault<Opt>();
Console.WriteLine(string.Join(',', opt.Values));
```

```console
$ dotnet run --values '["foo", "fuga", "hoge"]'
foo,fuga,hoge
```

Other examples can be found [here](tests/Mechiru.Command.Tests/Example.cs).


### Lisence
This library is under the MIT License.
