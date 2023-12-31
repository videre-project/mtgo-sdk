﻿namespace RemoteNET.Internal.Reflection
{
  public class RemoteEnum
  {
    private readonly RemoteType _remoteType;
    public RemoteApp App => _remoteType?.App;

    public RemoteEnum(RemoteType remoteType) => _remoteType = remoteType;

    public object GetValue(string valueName)
    {
      // NOTE: This is breaking the "RemoteX"/"DynamicX" paradigm because we are
      // effectively returning a DRO here.
      //
      // Unlike RemoteObject which directly uses a remote token + TypeDump to
      // read/write fields/props/methods, RemoteEnum was created after
      // RemoteType was defined and it felt much easier to utilize it.
      //
      // RemoteType itself, as part of the reflection API, returns DROs.
      RemoteFieldInfo verboseField = _remoteType.GetField(valueName) as RemoteFieldInfo;
      return verboseField.GetValue(null);
    }

    public dynamic Dynamify() => new DynamicRemoteEnum(this);
  }
}
