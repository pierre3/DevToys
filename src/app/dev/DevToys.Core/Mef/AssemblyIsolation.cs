﻿using System.Reflection;
using System.Runtime.Loader;

namespace DevToys.Core.Mef;

// See https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext
internal sealed class AssemblyIsolation : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    internal AssemblyIsolation(string entryPointAssemblyPath)
    {
        _resolver = new AssemblyDependencyResolver(entryPointAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}
