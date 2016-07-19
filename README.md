# Managed.Reflection

Managed.Reflection is a fully managed replacement of System.Reflection and System.Reflection.Emit. Unlike System.Reflection, it is not tied to the runtime it is running on.

It targets [.NET Standard 1.3](https://docs.microsoft.com/dotnet/articles/standard/library), which means it runs on .NET Core 1.0 and .NET Framework 4.6 and up.

It can generate .pdb files in [Portable PDB](https://github.com/dotnet/roslyn/blob/portable-pdb/docs/specs/PortablePdb-Metadata.md) format.
