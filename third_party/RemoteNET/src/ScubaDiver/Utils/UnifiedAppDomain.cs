﻿using System;
using System.Linq;
using System.Reflection;


namespace ScubaDiver.Utils;

/// <summary>
/// Encapsulates access to all AppDomains in the process
/// </summary>
public class UnifiedAppDomain
{
  private readonly Diver _parentDiver;

  /// <summary>
  /// Parent diver, which is currently running in the app
  /// </summary>
  /// <param name="parentDiver"></param>
  public UnifiedAppDomain(Diver parentDiver)
  {
    _parentDiver = parentDiver;
  }

  private AppDomain[] _domains = new[] { AppDomain.CurrentDomain };

  public AppDomain[] GetDomains()
  {
    if (_domains == null)
    {
      // Using Diver's heap searching abilities to locate all 'System.AppDomain'
      try
      {
        (bool anyErrors, var candidates) = _parentDiver
          .GetHeapObjects(heapObjType =>
              heapObjType == typeof(AppDomain).FullName, true);

        if(anyErrors)
        {
          throw new Exception("GetHeapObjects returned anyErrors: True");
        }

        _domains = candidates
          .Select(cand =>
              _parentDiver
                .GetObject(cand.Address, false, cand.Type, cand.HashCode)
                .instance)
          .Cast<AppDomain>().ToArray();
        Logger.Debug("[Diver][UnifiedAppDomain] All assemblies were retrieved from all AppDomains");
      }
      catch (Exception ex)
      {
        Logger.Debug("[Diver][UnifiedAppDomain] Failed to search heap for Runtime Assemblies. Error: " + ex.Message);

        // Fallback - Just return all assemblies in the current AppDomain.
        // Obviously, it's not ALL of them but sometimes it's good enough.
        _domains = new[] { AppDomain.CurrentDomain };
      }
    }
    return _domains;
  }

  public Assembly GetAssembly(string name)
  {
    return _domains.SelectMany(domain => domain.GetAssemblies())
      .Where(asm => asm.GetName().Name == name)
      .SingleOrDefault();
  }

  public Assembly[] GetAssemblies()
  {
    return _domains.SelectMany(domain => domain.GetAssemblies()).ToArray();
  }

  public Type ResolveType(string typeFullName, string assembly = null)
  {
    // TODO: Nullable gets a special case but in general we should switch to a
    //       recursive type-resolution to account for types like:
    //           Dictionary<FirstAssembly.FirstType, SecondAssembly.SecondType>
    if (typeFullName.StartsWith("System.Nullable`1[["))
    {
      return ResolveNullableType(typeFullName, assembly);
    }

    if (typeFullName.Contains('<') && typeFullName.EndsWith(">"))
    {
      string genericParams = typeFullName.Substring(typeFullName.LastIndexOf('<'));
      int numOfParams = genericParams.Split(',').Length;

      string nonGenericPart = typeFullName.Substring(0,typeFullName.LastIndexOf('<'));
      // TODO: Does this event work? it turns List<int> and List<string> both to List`1?
      typeFullName = $"{nonGenericPart}`{numOfParams}";
    }


    foreach (Assembly assm in GetAssemblies())
    {
      Type t = assm.GetType(typeFullName, throwOnError: false);
      if (t != null)
      {
        return t;
      }
    }
    throw new Exception($"Could not find type '{typeFullName}' in any of the known assemblies");
  }

  private Type ResolveNullableType(string typeFullName, string assembly)
  {
    // Remove prefix: "System.Nullable`1[["
    string innerTypeName = typeFullName.Substring("System.Nullable`1[[".Length);
    // Remove suffix: "]]"
    innerTypeName = innerTypeName.Substring(0, innerTypeName.Length - 2);
    // Type name is everything before the first comma (affter that we have some assembly info)
    innerTypeName = innerTypeName.Substring(0, innerTypeName.IndexOf(',')).Trim();

    Type innerType = ResolveType(innerTypeName);
    if(innerType == null)
      return null;

    Type nullable = typeof(Nullable<>);
    return nullable.MakeGenericType(innerType);
  }
}
