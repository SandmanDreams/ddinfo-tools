using DevilDaggersInfo.Core.Replay;
using DevilDaggersInfo.Core.Replay.Events.Data;
using DevilDaggersInfo.Tools.Utils;
using ImGuiNET;

namespace DevilDaggersInfo.Tools.Ui.ReplayEditor.Events.EventTypes;

public sealed class SquidSpawnEvents : IEventTypeRenderer<SquidSpawnEventData>
{
	private static readonly string[] _squidTypeNamesArray = EnumUtils.SquidTypeNames.Values.ToArray();

	public static int ColumnCount => 8;
	public static int ColumnCountData => 5;

	public static void SetupColumns()
	{
		EventTypeRendererUtils.SetupColumnActions();
		EventTypeRendererUtils.SetupColumnIndex();
		EventTypeRendererUtils.SetupColumnEntityId();
		SetupColumnsData();
	}

	public static void SetupColumnsData()
	{
		ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 80);
		ImGui.TableSetupColumn("?", ImGuiTableColumnFlags.WidthFixed, 32);
		ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 192);
		ImGui.TableSetupColumn("Direction", ImGuiTableColumnFlags.WidthFixed, 192);
		ImGui.TableSetupColumn("Rotation", ImGuiTableColumnFlags.WidthFixed, 64);
	}

	public static void Render(int eventIndex, int entityId, SquidSpawnEventData e, ReplayEventsData replayEventsData)
	{
		EventTypeRendererUtils.NextColumnActions(eventIndex);
		EventTypeRendererUtils.NextColumnEventIndex(eventIndex);
		EventTypeRendererUtils.NextColumnEntityId(replayEventsData, entityId);
		RenderData(eventIndex, e, replayEventsData);
	}

	public static void RenderData(int eventIndex, SquidSpawnEventData e, ReplayEventsData replayEventsData)
	{
		EventTypeRendererUtils.NextColumnInputByteEnum(eventIndex, nameof(SquidSpawnEventData.SquidType), ref e.SquidType, EnumUtils.SquidTypes, _squidTypeNamesArray);
		EventTypeRendererUtils.NextColumnInputInt(eventIndex, nameof(SquidSpawnEventData.A), ref e.A);
		EventTypeRendererUtils.NextColumnInputVector3(eventIndex, nameof(SquidSpawnEventData.Position), ref e.Position, "%.2f");
		EventTypeRendererUtils.NextColumnInputVector3(eventIndex, nameof(SquidSpawnEventData.Direction), ref e.Direction, "%.2f");
		EventTypeRendererUtils.NextColumnInputFloat(eventIndex, nameof(SquidSpawnEventData.RotationInRadians), ref e.RotationInRadians, "%.2f");
	}

	public static void RenderEdit(int eventIndex, SquidSpawnEventData e, ReplayEventsData replayEventsData)
	{
		const float leftColumnWidth = 120;
		const float rightColumnWidth = 160;
		const float tableWidth = leftColumnWidth + rightColumnWidth;

		if (ImGui.BeginChild(Inline.Span($"SquidSpawnEdit{eventIndex}"), default, ImGuiChildFlags.AutoResizeY))
		{
			if (ImGui.BeginTable("Left", 2, ImGuiTableFlags.None, new(tableWidth, 0)))
			{
				ImGui.TableSetupColumn("LeftText", ImGuiTableColumnFlags.WidthFixed, leftColumnWidth);
				ImGui.TableSetupColumn("LeftInput", ImGuiTableColumnFlags.None, rightColumnWidth);

				ImGui.TableNextRow();

				ImGui.TableNextColumn();
				ImGui.Text("Type");
				ImGui.TableNextColumn();
				EventTypeRendererUtils.InputByteEnum(eventIndex, nameof(SquidSpawnEventData.SquidType), ref e.SquidType, EnumUtils.SquidTypes, _squidTypeNamesArray);

				ImGui.TableNextColumn();
				ImGui.Text("?");
				ImGui.TableNextColumn();
				EventTypeRendererUtils.InputInt(eventIndex, nameof(SquidSpawnEventData.A), ref e.A);

				ImGui.EndTable();
			}

			ImGui.SameLine();

			if (ImGui.BeginTable("Right", 2, ImGuiTableFlags.None, new(tableWidth, 0)))
			{
				ImGui.TableSetupColumn("RightText", ImGuiTableColumnFlags.WidthFixed, leftColumnWidth);
				ImGui.TableSetupColumn("RightInput", ImGuiTableColumnFlags.None, rightColumnWidth);

				ImGui.TableNextRow();

				ImGui.TableNextColumn();
				ImGui.Text("Position");
				ImGui.TableNextColumn();
				EventTypeRendererUtils.InputVector3(eventIndex, nameof(SquidSpawnEventData.Position), ref e.Position, "%.2f");

				ImGui.TableNextColumn();
				ImGui.Text("Direction");
				ImGui.TableNextColumn();
				EventTypeRendererUtils.InputVector3(eventIndex, nameof(SquidSpawnEventData.Direction), ref e.Direction, "%.2f");

				ImGui.TableNextColumn();
				ImGui.Text("Rotation");
				ImGui.TableNextColumn();
				EventTypeRendererUtils.InputFloat(eventIndex, nameof(SquidSpawnEventData.RotationInRadians), ref e.RotationInRadians, "%.2f");

				ImGui.EndTable();
			}
		}

		ImGui.EndChild();
	}
}
