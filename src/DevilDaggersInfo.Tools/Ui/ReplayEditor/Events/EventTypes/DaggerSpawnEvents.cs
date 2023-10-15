using DevilDaggersInfo.Core.Replay.Events;
using DevilDaggersInfo.Core.Replay.Events.Enums;
using DevilDaggersInfo.Tools.Utils;
using ImGuiNET;

namespace DevilDaggersInfo.Tools.Ui.ReplayEditor.Events.EventTypes;

public sealed class DaggerSpawnEvents : IEventTypeRenderer<DaggerSpawnEvent>
{
	private static readonly string[] _daggerTypeNamesArray =
	{
		"Lvl1",
		"Lvl2",
		"Lvl3",
		"Lvl3 Homing",
		"Lvl4",
		"Lvl4 Homing",
		"Lvl4 Splash",
	};

	public static IReadOnlyList<EventColumn> EventColumns { get; } = new List<EventColumn>
	{
		EventColumn.Actions,
		EventColumn.Index,
		EventColumn.EntityId,
		new("Type", ImGuiTableColumnFlags.WidthFixed, 96),
		new("?", ImGuiTableColumnFlags.WidthFixed, 32),
		new("Position", ImGuiTableColumnFlags.WidthFixed, 128),
		new("Orientation", ImGuiTableColumnFlags.None, 196),
		new("Shot/Rapid", ImGuiTableColumnFlags.WidthFixed, 80),
	};

	public static void Render(int index, DaggerSpawnEvent e, IReadOnlyList<EntityType> entityTypes)
	{
		EventTypeRendererUtils.NextColumnActions(index);
		EventTypeRendererUtils.NextColumnEventIndex(index);
		EventTypeRendererUtils.NextColumnEntityId(entityTypes, e.EntityId);
		EventTypeRendererUtils.NextColumnInputEnum(index, nameof(DaggerSpawnEvent.DaggerType), ref e.DaggerType, EnumUtils.DaggerTypes, _daggerTypeNamesArray);
		EventTypeRendererUtils.NextColumnInputInt(index, nameof(DaggerSpawnEvent.A), ref e.A);
		EventTypeRendererUtils.NextColumnInputInt16Vec3(index, nameof(DaggerSpawnEvent.Position), ref e.Position);
		EventTypeRendererUtils.NextColumnInputInt16Mat3x3(index, nameof(DaggerSpawnEvent.Orientation), ref e.Orientation);
		EventTypeRendererUtils.NextColumnCheckbox(index, nameof(DaggerSpawnEvent.IsShot), ref e.IsShot, "Shot", "Rapid");
	}
}
