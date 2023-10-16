using DevilDaggersInfo.Core.Replay;
using DevilDaggersInfo.Core.Replay.Events;
using DevilDaggersInfo.Core.Replay.Events.Data;
using DevilDaggersInfo.Core.Replay.Events.Enums;
using DevilDaggersInfo.Tools.Engine.Maths.Numerics;
using DevilDaggersInfo.Tools.Ui.ReplayEditor.Utils;
using ImGuiNET;
using System.Numerics;

namespace DevilDaggersInfo.Tools.Ui.ReplayEditor;

public static class ReplayInputsChild
{
	private static int _startTick;

	public static void Render(ReplayEventsData eventsData, float startTime)
	{
		const int maxTicks = 60;
		const int height = 64;

		if (ImGui.BeginChild("TickNavigation", new(448 + 8, height)))
		{
			const int padding = 4;
			ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(padding));

			Vector2 iconSize = new(16);
			if (ImGuiImage.ImageButton("Start", Root.InternalResources.ArrowStartTexture.Handle, iconSize))
				_startTick = 0;
			ImGui.SameLine();
			if (ImGuiImage.ImageButton("Back", Root.InternalResources.ArrowLeftTexture.Handle, iconSize))
				_startTick = Math.Max(0, _startTick - maxTicks);
			ImGui.SameLine();
			if (ImGuiImage.ImageButton("Forward", Root.InternalResources.ArrowRightTexture.Handle, iconSize))
				_startTick = Math.Min(eventsData.TickCount - maxTicks, _startTick + maxTicks);
			ImGui.SameLine();
			if (ImGuiImage.ImageButton("End", Root.InternalResources.ArrowEndTexture.Handle, iconSize))
				_startTick = eventsData.TickCount - maxTicks;

			_startTick = Math.Max(0, Math.Min(_startTick, eventsData.TickCount - maxTicks));
			int endTick = Math.Min(_startTick + maxTicks - 1, eventsData.TickCount);

			ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(padding));
			ImGui.Text(Inline.Span($"Showing {_startTick} - {endTick} of {eventsData.TickCount} ticks\n{TimeUtils.TickToTime(_startTick, startTime):0.0000} - {TimeUtils.TickToTime(endTick, startTime):0.0000}"));
		}

		ImGui.EndChild(); // TickNavigation

		if (!ImGui.BeginTable("ReplayInputsTable", 2, ImGuiTableFlags.BordersInnerH))
			return;

		ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 128);
		ImGui.TableSetupColumn("Inputs", ImGuiTableColumnFlags.None, 384);
		ImGui.TableHeadersRow();

		int i = -1;
		foreach (ReplayEvent e in eventsData.Events)
		{
			if (e.Data is not InputsEventData and not InitialInputsEventData)
				continue;

			i++;
			if (i > _startTick + maxTicks)
				break;

			if (i < _startTick)
				continue;

			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(Inline.Span($"{TimeUtils.TickToTime(i, startTime):0.0000} ({i})"));

			ImGui.TableNextColumn();

			if (e.Data is InputsEventData inputsEvent)
				RenderInputsEvent(inputsEvent.Left, inputsEvent.Right, inputsEvent.Forward, inputsEvent.Backward, inputsEvent.Jump, inputsEvent.Shoot, inputsEvent.ShootHoming, inputsEvent.MouseX, inputsEvent.MouseY, null);
			else if (e.Data is InitialInputsEventData initialInputsEvent)
				RenderInputsEvent(initialInputsEvent.Left, initialInputsEvent.Right, initialInputsEvent.Forward, initialInputsEvent.Backward, initialInputsEvent.Jump, initialInputsEvent.Shoot, initialInputsEvent.ShootHoming, initialInputsEvent.MouseX, initialInputsEvent.MouseY, initialInputsEvent.LookSpeed);
		}

		ImGui.EndTable();
	}

	private static void RenderInputsEvent(
		bool left,
		bool right,
		bool forward,
		bool backward,
		JumpType jump,
		ShootType shoot,
		ShootType shootHoming,
		short mouseX,
		short mouseY,
		float? lookSpeed)
	{
		if (lookSpeed.HasValue)
			ImGui.TextColored(Color.White, Inline.Span($"Look Speed: {lookSpeed.Value}"));

		ImGui.TextColored(forward ? Color.Red : Color.White, "W");
		ImGui.SameLine();
		ImGui.TextColored(left ? Color.Red : Color.White, "A");
		ImGui.SameLine();
		ImGui.TextColored(backward ? Color.Red : Color.White, "S");
		ImGui.SameLine();
		ImGui.TextColored(right ? Color.Red : Color.White, "D");
		ImGui.SameLine();
		ImGui.TextColored(GetJumpTypeColor(jump), "[Space]");
		ImGui.SameLine();
		ImGui.TextColored(GetShootTypeColor(shoot), "[LMB]");
		ImGui.SameLine();
		ImGui.TextColored(GetShootTypeColor(shootHoming), "[RMB]");
		ImGui.SameLine();
		ImGui.TextColored(mouseX == 0 ? Color.White : Color.Red, Inline.Span($"X:{mouseX}"));
		ImGui.SameLine();
		ImGui.TextColored(mouseY == 0 ? Color.White : Color.Red, Inline.Span($"Y:{mouseY}"));

		static Color GetJumpTypeColor(JumpType jumpType) => jumpType switch
		{
			JumpType.Hold => Color.Orange,
			JumpType.StartedPress => Color.Red,
			_ => Color.White,
		};

		static Color GetShootTypeColor(ShootType shootType) => shootType switch
		{
			ShootType.Hold => Color.Orange,
			ShootType.Release => Color.Red,
			_ => Color.White,
		};
	}
}
