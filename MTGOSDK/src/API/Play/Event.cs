/** @file
  Copyright (c) 2023, Cory Bennett. All rights reserved.
  SPDX-License-Identifier: Apache-2.0
**/

using System.Linq;

using MTGOSDK.API.Collection;
using MTGOSDK.API.Play.Leagues;
using MTGOSDK.API.Play.Tournaments;
using MTGOSDK.API.Users;
using MTGOSDK.Core.Reflection;

using WotC.MtGO.Client.Model;
using WotC.MtGO.Client.Model.Collection;
using WotC.MtGO.Client.Model.Play;
using WotC.MtGO.Client.Model.Play.Enums;


namespace MTGOSDK.API.Play;
using static MTGOSDK.API.Events;

public abstract class Event<I> : DLRWrapper<IPlayerEvent>
{
  /// <summary>
  /// The internal reference for the binding type for the wrapped object.
  /// </summary>
  internal override Type type => typeof(I);

  public sealed class Default(dynamic playerEvent) : Event<IPlayerEvent>
  {
    /// <summary>
    /// Stores an internal reference to the IPlayerEvent object.
    /// </summary>
    internal override dynamic obj => Bind<IPlayerEvent>(playerEvent);

    //
    // IPlayerEvent wrapper events
    //

    public EventProxy IsLocalUserJoinedChanged =
      new(/* IPlayerEvent */ playerEvent, nameof(IsLocalUserJoinedChanged));

    public EventProxy IsAcceptingNewPlayersChanged =
      new(/* IPlayerEvent */ playerEvent, nameof(IsAcceptingNewPlayersChanged));
  }

  //
  // IPlayerEvent wrapper properties
  //

  /// <summary>
  /// The event's tournament ID.
  /// </summary>
  public int Id => @base.EventId;

  /// <summary>
  /// The event's session token.
  /// </summary>
  public Guid Token => Cast<Guid>(Unbind(@base).EventToken);

  /// <summary>
  /// The event type (e.g. League, Tournament, Match, etc.).
  /// </summary>
  public string EventType => this.GetType().Name;

  /// <summary>
  /// A class describing the event format (e.g. Standard, Modern, Legacy, etc.).
  /// </summary>
  public PlayFormat Format => new(@base.PlayFormat);

  /// <summary>
  /// The name of this event (e.g. "Standard Preliminary").
  /// </summary>
  public string Description => @base.Description;

  /// <summary>
  /// The total number of players registered for the event.
  /// </summary>
  public int TotalPlayers => Enumerable.Count<IUser>(@base.JoinedUsers);

  /// <summary>
  /// The current players registered for the event.
  /// </summary>
  public IEnumerable<User> Players => Map<User>(@base.JoinedUsers);

  /// <summary>
  /// The user's registered deck for the event.
  /// </summary>
  public Deck RegisteredDeck => new(@base.DeckUsedToJoin);

  /// <summary>
  /// The number of minutes each player has in each match.
  /// </summary>
  public int MinutesPerPlayer => @base.MinutesPerPlayer;

  /// <summary>
  /// The minimum number of players required to start the event.
  /// </summary>
  public int MinimumPlayers => @base.MinimumPlayers;

  /// <summary>
  /// The maximum number of players allowed to join the event.
  /// </summary>
  public int MaximumPlayers => @base.MaximumPlayers;

  /// <summary>
  /// Whether the event has ended.
  /// </summary>
  public bool IsCompleted => @base.IsCompleted;

  /// <summary>
  /// Whether the event has finished and is no longer active.
  /// </summary>
  public bool IsRemoved => @base.WasEventRemovedFromSystem;

  /// <summary>
  /// Whether the user has joined the event.
  /// </summary>
  public bool HasJoined => @base.IsLocalUserJoined;

  /// <summary>
  /// Whether the user is currently participating in the event.
  /// </summary>
  public bool IsParticipant => @base.IsLocalUserParticipant;

  /// <summary>
  /// Whether the user has been eliminated from the event.
  /// </summary>
  public bool IsEliminated => @base.IsLocalUserEliminated;

  //
  // IPlayerEvent wrapper methods
  //

  public static dynamic FromPlayerEvent(dynamic playerEvent)
  {
    switch (playerEvent.GetType().Name)
    {
      case "FilterableLeague" or "League":
        return new League(playerEvent);
      case "FilterableMatch" or "Match":
        return new Match(playerEvent);
      case "FilterableTournament" or "Tournament":
        return new Tournament(playerEvent);
      case "FilterableQueue" or "Queue":
        return new Queue(playerEvent);
      default:
        return new Event<IPlayerEvent>.Default(playerEvent);
    }
  }

  public override string ToString() => $"{Description} #{Id}";
}
