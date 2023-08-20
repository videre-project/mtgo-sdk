/** @file
  Copyright (c) 2023, Cory Bennett. All rights reserved.
  SPDX-License-Identifier: BSD-3-Clause
**/

using System.Diagnostics;
using System.Reflection;

using RemoteNET;


namespace MTGOInjector;

public class BaseClient
{
  protected virtual Process ClientProcess { get; private set; } = default!;
  public readonly RemoteApp Client;

  /// <summary>
  /// A list of non-system modules loaded by the client.
  /// </summary>
  public IEnumerable<ProcessModule> ClientModules =>
    ClientProcess.Modules
      .Cast<ProcessModule>()
      .Where(m =>
        new string[] { "\\Windows\\", "\\ProgramData\\" }
          .All(s => m.FileName.Contains(s) == false));

  public bool Is_Reconnect { get; private set; } = false;

  public BaseClient()
  {
    Client = GetClientHandle();
  }

  private RemoteApp GetClientHandle()
  {
    // Check if the client injector is already loaded
    Is_Reconnect = ClientModules
      .Any(m => m.FileName.Contains("UnmanagedAdapterDLL"));

    // Connect to the target process
    var Client = RemoteApp.Connect(ClientProcess);

    // Verify that the injected assembly is loaded and reponding
    if (Client.Communicator.CheckAliveness() is false)
      throw new Exception("RemoteNET Diver is not responding to requests.");

    return Client;
  }

  public virtual void Dispose()
  {
    Client.Dispose();
    ClientProcess.Kill();
  }

  //
  // ManagedRemoteApp wrapper methods
  //

  public dynamic GetInstance(string queryPath)
  {
    return GetInstances(queryPath).Single();
  }

  public IEnumerable<dynamic> GetInstances(string queryPath)
  {
    IEnumerable<CandidateObject> queryRefs = Client.QueryInstances(queryPath);
    foreach (var candidate in queryRefs)
    {
      var queryObject = Client.GetRemoteObject(candidate);
      yield return queryObject.Dynamify();
    }
  }

  public Type GetInstanceType(string queryPath)
  {
    return Client.GetRemoteType(queryPath)
      ?? throw new Exception($"Type not found: {queryPath}");
    // return GetInstanceTypes(queryPath).Single();
  }

  public IEnumerable<Type> GetInstanceTypes(string queryPath)
  {
    IEnumerable<CandidateType> queryRefs = Client.QueryTypes(queryPath);
    foreach (var candidate in queryRefs)
    {
      var queryObject = Client.GetRemoteType(candidate);
      yield return queryObject;
    }
  }

  public MethodInfo GetInstanceMethod(string queryPath, string methodName)
  {
    return GetInstanceMethods(queryPath, methodName).Single();
  }

  public IEnumerable<MethodInfo> GetInstanceMethods(string queryPath,
                                                    string methodName)
  {
    Type type = GetInstanceType(queryPath);
    var methods = type.GetMethods((BindingFlags)0xffff)
      .Where(mInfo => mInfo.Name == methodName);

    return methods;
  }

  public dynamic CreateInstance(string queryPath)
  {
    RemoteActivator activator = Client.Activator;
    RemoteObject queryObject = activator.CreateInstance(queryPath);
    return queryObject.Dynamify();
  }

  //
  // Reflection wrapper methods
  //

  public MethodInfo? GetMethod(string queryPath,
                               string methodName,
                               Type[]? genericTypes=null)
  {
    var remoteType = GetInstanceType(queryPath);
    var remoteMethod = remoteType.GetMethod(methodName);

    // Fills in a generic method if generic types are specified
    if (genericTypes is not null)
      return remoteMethod!.MakeGenericMethod(genericTypes);

    return remoteMethod;
  }

  /// <summary>
  /// Invokes a static method on the target process.
  /// </summary>
  public dynamic InvokeMethod(string queryPath,
                              string methodName,
                              Type[]? genericTypes=null,
                              params object[]? args)
  {
    var remoteMethod = GetMethod(queryPath, methodName, genericTypes);
#pragma warning disable CS8603
    return remoteMethod!.Invoke(null, args);
#pragma warning restore CS8603
  }

  //
  // HookingManager wrapper methods
  //

  public void HookInstanceMethod(string queryPath,
                                 string methodName,
                                 string hookName,
                                 HookAction callback)
  {
    MethodInfo method = GetInstanceMethod(queryPath, methodName);
    switch (hookName)
    {
      //
      // FIXME: prefix/postfix patches break on subsequent client connections.
      //
      case "prefix":
        Client.Harmony.Patch(method, prefix: callback);
        break;
      case "postfix":
        Client.Harmony.Patch(method, postfix: callback);
        break;
      case "finalizer":
        Client.Harmony.Patch(method, finalizer: callback);
        break;
      default:
        throw new Exception($"Unknown hook type: {hookName}");
    }
  }

  // TODO: Add unhooking methods + unhook all methods on exit
  //       (or just unhook all methods on exit)

  // public void UnhookInstanceMethod(string queryPath,
  //                                  string methodName,
  //                                  string hookName)
}