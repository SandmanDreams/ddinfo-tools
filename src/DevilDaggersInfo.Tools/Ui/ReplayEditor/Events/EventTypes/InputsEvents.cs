using DevilDaggersInfo.Core.Replay.Events.Data;
using DevilDaggersInfo.Tools.Ui.ReplayEditor.Data;
using ImGuiNET;

namespace DevilDaggersInfo.Tools.Ui.ReplayEditor.Events.EventTypes;

public sealed class InputsEvents : IEventTypeRenderer<InputsEventData>
{
	public static int ColumnCount => 9;

	public static void SetupColumns()
	{
		ImGui.TableSetupColumn("Left", ImGuiTableColumnFlags.WidthFixed, 64);
		ImGui.TableSetupColumn("Right", ImGuiTableColumnFlags.WidthFixed, 64);
		ImGui.TableSetupColumn("Forward", ImGuiTableColumnFlags.WidthFixed, 64);
		ImGui.TableSetupColumn("Backward", ImGuiTableColumnFlags.WidthFixed, 64);
		ImGui.TableSetupColumn("Jump", ImGuiTableColumnFlags.WidthFixed, 96);
		ImGui.TableSetupColumn("Shoot", ImGuiTableColumnFlags.WidthFixed, 96);
		ImGui.TableSetupColumn("Shoot Homing", ImGuiTableColumnFlags.WidthFixed, 96);
		ImGui.TableSetupColumn("Mouse X", ImGuiTableColumnFlags.WidthFixed, 64);
		ImGui.TableSetupColumn("Mouse Y", ImGuiTableColumnFlags.WidthFixed, 64);
	}

	public static void Render(InputsEventData e, EditorReplayModel replay)
	{
		EventTypeRendererUtils.NextColumnBool(e.Left, "On", "Off");
		EventTypeRendererUtils.NextColumnBool(e.Right, "On", "Off");
		EventTypeRendererUtils.NextColumnBool(e.Forward, "On", "Off");
		EventTypeRendererUtils.NextColumnBool(e.Backward, "On", "Off");
		EventTypeRendererUtils.NextColumn(e.Jump);
		EventTypeRendererUtils.NextColumn(e.Shoot);
		EventTypeRendererUtils.NextColumn(e.ShootHoming);
		EventTypeRendererUtils.NextColumn(e.MouseX);
		EventTypeRendererUtils.NextColumn(e.MouseY);
	}
}
