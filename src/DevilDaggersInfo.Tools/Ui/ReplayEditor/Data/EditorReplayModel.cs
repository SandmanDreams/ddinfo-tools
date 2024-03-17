using DevilDaggersInfo.Core.Replay;
using DevilDaggersInfo.Core.Replay.Events.Data;
using DevilDaggersInfo.Core.Spawnset;
using DevilDaggersInfo.Tools.Ui.ReplayEditor.Events;
using System.Diagnostics;
using System.Security.Cryptography;

namespace DevilDaggersInfo.Tools.Ui.ReplayEditor.Data;

public record EditorReplayModel
{
	private ReplayEventsData? _replayEventsDataCache;

	private readonly List<EditorEvent> _boidSpawnEvents = [];
	private readonly List<EditorEvent> _daggerSpawnEvents = [];
	private readonly List<EditorEvent> _entityOrientationEvents = [];
	private readonly List<EditorEvent> _entityPositionEvents = [];
	private readonly List<EditorEvent> _entityTargetEvents = [];
	private readonly List<EditorEvent> _gemEvents = [];
	private readonly List<EditorEvent> _hitEvents = [];
	private readonly List<EditorEvent> _leviathanSpawnEvents = [];
	private readonly List<EditorEvent> _pedeSpawnEvents = [];
	private readonly List<EditorEvent> _spiderEggSpawnEvents = [];
	private readonly List<EditorEvent> _spiderSpawnEvents = [];
	private readonly List<EditorEvent> _squidSpawnEvents = [];
	private readonly List<EditorEvent> _thornSpawnEvents = [];
	private readonly List<EditorEvent> _transmuteEvents = [];

	private EditorReplayModel(int version, long timestampSinceGameRelease, float time, float startTime, int daggersFired, int deathType, int gems, int daggersHit, int kills, int playerId, string username, SpawnsetBinary spawnset)
	{
		Version = version;
		TimestampSinceGameRelease = timestampSinceGameRelease;
		Time = time;
		StartTime = startTime;
		DaggersFired = daggersFired;
		DeathType = deathType;
		Gems = gems;
		DaggersHit = daggersHit;
		Kills = kills;
		PlayerId = playerId;
		Username = username;
		Spawnset = spawnset;
	}

	// Data found in local replay header.
	public int Version { get; set; }
	public long TimestampSinceGameRelease { get; set; }
	public float Time { get; set; }
	public float StartTime { get; set; }
	public int DaggersFired { get; set; }
	public int DeathType { get; set; }
	public int Gems { get; set; }
	public int DaggersHit { get; set; }
	public int Kills { get; set; }
	public int PlayerId { get; set; }
	public string Username { get; set; }
	public SpawnsetBinary Spawnset { get; set; }

	// TODO: Invalidate events data cache when any of the below properties are changed or values are added/removed/moved from lists.

	// Embedded inputs data.
	public float LookSpeed { get; set; }

	// TODO: Use IReadOnlyList<> instead of List<>.
	public List<InputsEventData> InputsEvents { get; } = [];

	// All other events.
	public IReadOnlyList<EditorEvent> BoidSpawnEvents => _boidSpawnEvents;
	public IReadOnlyList<EditorEvent> DaggerSpawnEvents => _daggerSpawnEvents;
	public IReadOnlyList<EditorEvent> EntityOrientationEvents => _entityOrientationEvents;
	public IReadOnlyList<EditorEvent> EntityPositionEvents => _entityPositionEvents;
	public IReadOnlyList<EditorEvent> EntityTargetEvents => _entityTargetEvents;
	public IReadOnlyList<EditorEvent> GemEvents => _gemEvents;
	public IReadOnlyList<EditorEvent> HitEvents => _hitEvents;
	public IReadOnlyList<EditorEvent> LeviathanSpawnEvents => _leviathanSpawnEvents;
	public IReadOnlyList<EditorEvent> PedeSpawnEvents => _pedeSpawnEvents;
	public IReadOnlyList<EditorEvent> SpiderEggSpawnEvents => _spiderEggSpawnEvents;
	public IReadOnlyList<EditorEvent> SpiderSpawnEvents => _spiderSpawnEvents;
	public IReadOnlyList<EditorEvent> SquidSpawnEvents => _squidSpawnEvents;
	public IReadOnlyList<EditorEvent> ThornSpawnEvents => _thornSpawnEvents;
	public IReadOnlyList<EditorEvent> TransmuteEvents => _transmuteEvents;

	public int TickCount => InputsEvents.Count;

	public ReplayEventsData Cache => _replayEventsDataCache ??= CompileEventsData();

	private void InvalidateCache()
	{
		_replayEventsDataCache = null;
	}

	public static EditorReplayModel CreateDefault()
	{
		return new(
			version: 1,
			timestampSinceGameRelease: 0,
			time: 0,
			startTime: 0,
			daggersFired: 0,
			deathType: 0,
			gems: 0,
			daggersHit: 0,
			kills: 0,
			playerId: 0,
			username: string.Empty,
			spawnset: SpawnsetBinary.CreateDefault());
	}

	public static EditorReplayModel CreateFromLeaderboardReplay(int playerId, string username, ReplayEventsData replayEventsData)
	{
		EditorReplayModel replay = CreateDefault();
		replay.PlayerId = playerId;
		replay.Username = username;
		replay.AddReplayEventsData(replayEventsData);
		return replay;
	}

	public static EditorReplayModel CreateFromLocalReplay(ReplayBinary<LocalReplayBinaryHeader> localReplay)
	{
		EditorReplayModel replay = new(
			version: localReplay.Header.Version,
			timestampSinceGameRelease: localReplay.Header.TimestampSinceGameRelease,
			time: localReplay.Header.Time,
			startTime: localReplay.Header.StartTime,
			daggersFired: localReplay.Header.DaggersFired,
			deathType: localReplay.Header.DeathType,
			gems: localReplay.Header.Gems,
			daggersHit: localReplay.Header.DaggersHit,
			kills: localReplay.Header.Kills,
			playerId: localReplay.Header.PlayerId,
			username: localReplay.Header.Username,
			spawnset: localReplay.Header.Spawnset);
		replay.AddReplayEventsData(localReplay.EventsData);
		return replay;
	}

	private void AddReplayEventsData(ReplayEventsData replayEventsData)
	{
		int currentTick = 0;
		int entityId = 1;
		foreach (IEventData eventData in replayEventsData.Events.Select(e => e.Data))
		{
			switch (eventData)
			{
				case InputsEventData inputsEventData:
					InputsEvents.Add(inputsEventData);
					currentTick++;
					break;
				case InitialInputsEventData initialInputsEventData:
					InputsEvents.Add(new(
						Left: initialInputsEventData.Left,
						Right: initialInputsEventData.Right,
						Forward: initialInputsEventData.Forward,
						Backward: initialInputsEventData.Backward,
						Jump: initialInputsEventData.Jump,
						Shoot: initialInputsEventData.Shoot,
						ShootHoming: initialInputsEventData.ShootHoming,
						MouseX: initialInputsEventData.MouseX,
						MouseY: initialInputsEventData.MouseY));
					LookSpeed = initialInputsEventData.LookSpeed;
					currentTick++;
					break;
				case BoidSpawnEventData boidSpawnEventData: _boidSpawnEvents.Add(new(currentTick, entityId++, boidSpawnEventData)); break;
				case DaggerSpawnEventData daggerSpawnEventData: _daggerSpawnEvents.Add(new(currentTick, entityId++, daggerSpawnEventData)); break;
				case EntityOrientationEventData entityOrientationEventData: _entityOrientationEvents.Add(new(currentTick, null, entityOrientationEventData)); break;
				case EntityPositionEventData entityPositionEventData: _entityPositionEvents.Add(new(currentTick, null, entityPositionEventData)); break;
				case EntityTargetEventData entityTargetEventData: _entityTargetEvents.Add(new(currentTick, null, entityTargetEventData)); break;
				case GemEventData gemEventData: _gemEvents.Add(new(currentTick, null, gemEventData)); break;
				case HitEventData hitEventData: _hitEvents.Add(new(currentTick, null, hitEventData)); break;
				case LeviathanSpawnEventData leviathanSpawnEventData: _leviathanSpawnEvents.Add(new(currentTick, entityId++, leviathanSpawnEventData)); break;
				case PedeSpawnEventData pedeSpawnEventData: _pedeSpawnEvents.Add(new(currentTick, entityId++, pedeSpawnEventData)); break;
				case SpiderEggSpawnEventData spiderEggSpawnEventData: _spiderEggSpawnEvents.Add(new(currentTick, entityId++, spiderEggSpawnEventData)); break;
				case SpiderSpawnEventData spiderSpawnEventData: _spiderSpawnEvents.Add(new(currentTick, entityId++, spiderSpawnEventData)); break;
				case SquidSpawnEventData squidSpawnEventData: _squidSpawnEvents.Add(new(currentTick, entityId++, squidSpawnEventData)); break;
				case ThornSpawnEventData thornSpawnEventData: _thornSpawnEvents.Add(new(currentTick, entityId++, thornSpawnEventData)); break;
				case TransmuteEventData transmuteEventData: _transmuteEvents.Add(new(currentTick, null, transmuteEventData)); break;
			}
		}
	}

	private List<EditorEvent> GetEventsAtTick(int tickIndex)
	{
		return BoidSpawnEvents
			.Concat(DaggerSpawnEvents)
			.Concat(EntityOrientationEvents)
			.Concat(EntityPositionEvents)
			.Concat(EntityTargetEvents)
			.Concat(GemEvents)
			.Concat(HitEvents)
			.Concat(LeviathanSpawnEvents)
			.Concat(PedeSpawnEvents)
			.Concat(SpiderEggSpawnEvents)
			.Concat(SpiderSpawnEvents)
			.Concat(SquidSpawnEvents)
			.Concat(ThornSpawnEvents)
			.Concat(TransmuteEvents)
			.Where(e => e.TickIndex == tickIndex)
			.ToList();
	}

	private ReplayEventsData CompileEventsData()
	{
		ReplayEventsData replayEventsData = new();

		List<EditorEvent> eventsThisTick = [];

		for (int i = 0; i < InputsEvents.Count; i++)
		{
			eventsThisTick.Clear();
			eventsThisTick.AddRange(GetEventsAtTick(i));
			eventsThisTick.Sort((a, b) => (a.EntityId ?? -1).CompareTo(b.EntityId ?? -1));
			foreach (EditorEvent editorEvent in eventsThisTick)
				replayEventsData.AddEvent(editorEvent.Data);

			if (i == 0)
				replayEventsData.AddEvent(new InitialInputsEventData(InputsEvents[i].Left, InputsEvents[i].Right, InputsEvents[i].Forward, InputsEvents[i].Backward, InputsEvents[i].Jump, InputsEvents[i].Shoot, InputsEvents[i].ShootHoming, InputsEvents[i].MouseX, InputsEvents[i].MouseY, LookSpeed));
			else
				replayEventsData.AddEvent(InputsEvents[i]);
		}

		replayEventsData.AddEvent(new EndEventData());

		return replayEventsData;
	}

	public void AddEmptyEvent(int tickIndex, EventType eventType)
	{
		int entityId = 1;

		for (int i = tickIndex; i >= 0; i--)
		{
			// Find all spawn events with this tick index.
			// If there are none, continue.
			// If we find one, use that entityId + 1.
			// If we find multiple, use the highest entityId + 1.
			int? highestEntityId = HighestEntityIdInList(GetEventsAtTick(i));
			if (highestEntityId.HasValue)
			{
				entityId = highestEntityId.Value + 1;
				break;
			}

			static int? HighestEntityIdInList(List<EditorEvent> events)
			{
				if (events.Count == 0)
					return null;

				int? highestEntityId = null;
				for (int i = 0; i < events.Count; i++)
				{
					if (!events[i].EntityId.HasValue)
						continue;

					if (!highestEntityId.HasValue || events[i].EntityId > highestEntityId)
						highestEntityId = events[i].EntityId;
				}

				return highestEntityId;
			}
		}

		// TODO: Shift entity ids of events with higher entity ids than the added event.
		Action action = eventType switch
		{
			EventType.BoidSpawn => () => _boidSpawnEvents.Add(new(tickIndex, entityId, BoidSpawnEventData.CreateDefault())),
			EventType.LeviathanSpawn => () => _leviathanSpawnEvents.Add(new(tickIndex, entityId, LeviathanSpawnEventData.CreateDefault())),
			EventType.PedeSpawn => () => _pedeSpawnEvents.Add(new(tickIndex, entityId, PedeSpawnEventData.CreateDefault())),
			EventType.SpiderEggSpawn => () => _spiderEggSpawnEvents.Add(new(tickIndex, entityId, SpiderEggSpawnEventData.CreateDefault())),
			EventType.SpiderSpawn => () => _spiderSpawnEvents.Add(new(tickIndex, entityId, SpiderSpawnEventData.CreateDefault())),
			EventType.SquidSpawn => () => _squidSpawnEvents.Add(new(tickIndex, entityId, SquidSpawnEventData.CreateDefault())),
			EventType.ThornSpawn => () => _thornSpawnEvents.Add(new(tickIndex, entityId, ThornSpawnEventData.CreateDefault())),
			EventType.DaggerSpawn => () => _daggerSpawnEvents.Add(new(tickIndex, entityId, DaggerSpawnEventData.CreateDefault())),
			EventType.EntityOrientation => () => _entityOrientationEvents.Add(new(tickIndex, null, EntityOrientationEventData.CreateDefault())),
			EventType.EntityPosition => () => _entityPositionEvents.Add(new(tickIndex, null, EntityPositionEventData.CreateDefault())),
			EventType.EntityTarget => () => _entityTargetEvents.Add(new(tickIndex, null, EntityTargetEventData.CreateDefault())),
			EventType.Gem => () => _gemEvents.Add(new(tickIndex, null, GemEventData.CreateDefault())),
			EventType.Hit => () => _hitEvents.Add(new(tickIndex, null, HitEventData.CreateDefault())),
			EventType.Transmute => () => _transmuteEvents.Add(new(tickIndex, null, TransmuteEventData.CreateDefault())),
			EventType.InitialInputs => throw new UnreachableException($"Event type not supported by timeline editor: {eventType}"),
			EventType.Inputs => throw new UnreachableException($"Event type not supported by timeline editor: {eventType}"),
			EventType.End => throw new UnreachableException($"Event type not supported by timeline editor: {eventType}"),
			_ => throw new UnreachableException($"Unknown event type: {eventType}"),
		};

		action();

		InvalidateCache();
	}

	public void RemoveEvent(EditorEvent editorEvent)
	{
		// TODO: Shift entity ids of events with higher entity ids than the removed event.
		switch (editorEvent.Data)
		{
			case BoidSpawnEventData: _boidSpawnEvents.Remove(editorEvent); return;
			case DaggerSpawnEventData: _daggerSpawnEvents.Remove(editorEvent); return;
			case EntityOrientationEventData: _entityOrientationEvents.Remove(editorEvent); return;
			case EntityPositionEventData: _entityPositionEvents.Remove(editorEvent); return;
			case EntityTargetEventData: _entityTargetEvents.Remove(editorEvent); return;
			case GemEventData: _gemEvents.Remove(editorEvent); return;
			case HitEventData: _hitEvents.Remove(editorEvent); return;
			case LeviathanSpawnEventData: _leviathanSpawnEvents.Remove(editorEvent); return;
			case PedeSpawnEventData: _pedeSpawnEvents.Remove(editorEvent); return;
			case SpiderEggSpawnEventData: _spiderEggSpawnEvents.Remove(editorEvent); return;
			case SpiderSpawnEventData: _spiderSpawnEvents.Remove(editorEvent); return;
			case SquidSpawnEventData: _squidSpawnEvents.Remove(editorEvent); return;
			case ThornSpawnEventData: _thornSpawnEvents.Remove(editorEvent); return;
			case TransmuteEventData: _transmuteEvents.Remove(editorEvent); return;
		}

		InvalidateCache();
	}

	public ReplayBinary<LocalReplayBinaryHeader> ToLocalReplay()
	{
		LocalReplayBinaryHeader header = new(
			Version,
			TimestampSinceGameRelease,
			Time,
			StartTime,
			DaggersFired,
			DeathType,
			Gems,
			DaggersHit,
			Kills,
			PlayerId,
			Username,
			new byte[10],
			Spawnset.ToBytes());

		return new(header, Cache);
	}

	public byte[] ToHash()
	{
		using MemoryStream ms = new();
		using BinaryWriter bw = new(ms);

		bw.Write(LookSpeed);
		bw.Write(InputsEvents.Count);
		for (int i = 0; i < InputsEvents.Count; i++)
			InputsEvents[i].Write(bw);

		WriteList(bw, _boidSpawnEvents);
		WriteList(bw, _daggerSpawnEvents);
		WriteList(bw, _entityOrientationEvents);
		WriteList(bw, _entityPositionEvents);
		WriteList(bw, _entityTargetEvents);
		WriteList(bw, _gemEvents);
		WriteList(bw, _hitEvents);
		WriteList(bw, _leviathanSpawnEvents);
		WriteList(bw, _pedeSpawnEvents);
		WriteList(bw, _spiderEggSpawnEvents);
		WriteList(bw, _spiderSpawnEvents);
		WriteList(bw, _squidSpawnEvents);
		WriteList(bw, _thornSpawnEvents);
		WriteList(bw, _transmuteEvents);

		return MD5.HashData(ms.ToArray());

		static void WriteList(BinaryWriter bw, List<EditorEvent> events)
		{
			bw.Write(events.Count);
			for (int i = 0; i < events.Count; i++)
				events[i].Data.Write(bw);
		}
	}
}
