﻿using System;
using System.Net;
using System.Reflection;


namespace ScubaDiver;

public class RegisteredEventHandlerInfo 
{
  /// <summary>
  /// Event handler that was registered on the event
  /// </summary>
  public Delegate RegisteredProxy { get; set; }

  // Note that this object might be pinned or unpinned when this info object is
  // created. By holding a reference to it within this class we don't care
  // if it moves or not as we will always be able to safely access it.
  public object Target { get; set; }

  public EventInfo EventInfo { get; set; }

  /// <summary>
  /// Endpoint listening for invocations of the event
  /// </summary>
  public IPEndPoint Endpoint { get; set; }
}
