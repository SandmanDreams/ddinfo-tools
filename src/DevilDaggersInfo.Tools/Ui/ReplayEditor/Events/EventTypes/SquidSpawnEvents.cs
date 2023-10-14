using DevilDaggersInfo.Core.Replay.Events;
using DevilDaggersInfo.Core.Replay.Events.Enums;
using DevilDaggersInfo.Tools.Utils;

namespace DevilDaggersInfo.Tools.Ui.ReplayEditor.Events.EventTypes;

public sealed class SquidSpawnEvents : IEventTypeRenderer<SquidSpawnEvent>
{
	private static readonly string[] _squidTypeNamesArray = EnumUtils.SquidTypeNames.Values.ToArray();

	public static void Render(int index, SquidSpawnEvent e, IReadOnlyList<EntityType> entityTypes)
	{
		EventTypeRendererUtils.NextColumnText(Inline.Span(index));
		EventTypeRendererUtils.EntityColumn(entityTypes, e.EntityId);
		EventTypeRendererUtils.NextColumnInputEnum(index, nameof(SquidSpawnEvent.SquidType), ref e.SquidType, EnumUtils.SquidTypes, _squidTypeNamesArray);
		EventTypeRendererUtils.NextColumnInputInt(index, nameof(SquidSpawnEvent.A), ref e.A);
		EventTypeRendererUtils.NextColumnInputVector3(index, nameof(SquidSpawnEvent.Position), ref e.Position, "%.2f");
		EventTypeRendererUtils.NextColumnInputVector3(index, nameof(SquidSpawnEvent.Direction), ref e.Direction, "%.2f");
		EventTypeRendererUtils.NextColumnInputFloat(index, nameof(SquidSpawnEvent.RotationInRadians), ref e.RotationInRadians, "%.2f");
	}
}
