using DevilDaggersInfo.Core.Spawnset;
using DevilDaggersInfo.Tools.EditorFileState;
using DevilDaggersInfo.Tools.Engine.Maths.Numerics;
using DevilDaggersInfo.Tools.Ui.SpawnsetEditor.Utils;
using DevilDaggersInfo.Tools.Utils;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace DevilDaggersInfo.Tools.Ui.SpawnsetEditor;

public static class SettingsWindow
{
	private static void InfoTooltipWhenDisabled(bool disabled, string tooltipText)
	{
		if (disabled)
		{
			ImGui.EndDisabled();
			InfoTooltip(tooltipText);
			ImGui.BeginDisabled(disabled);
		}
	}

	private static void InfoTooltip(string tooltipText)
	{
		ImGui.SameLine();
		ImGui.TextColored(Color.Gray(0.7f), "(?)");
		if (ImGui.IsItemHovered())
			ImGui.SetTooltip(tooltipText);
	}

	public static void Render()
	{
		if (ImGui.Begin("Settings"))
		{
			RenderFormat();
			RenderGameMode();
			RenderRaceDagger();
			RenderArena();
			RenderPractice();

			ImGui.Unindent();
		}

		ImGui.End();
	}

	private static void RenderFormat()
	{
		ImGui.Text("Format");
		InfoTooltip("There is generally no reason to change the spawnset format,\nunless you want to play spawnsets in an old version of the game.\n\nThese options are mainly here for backwards compatibility.");
		ImGui.Separator();
		ImGui.Indent(8);

		ImGui.Text("World version");
		ImGui.SameLine();
		for (int i = 8; i < 10; i++)
		{
			if (ImGui.RadioButton(Inline.Span(i), i == FileStates.Spawnset.Object.WorldVersion) && FileStates.Spawnset.Object.WorldVersion != i)
			{
				FileStates.Spawnset.Update(FileStates.Spawnset.Object with { WorldVersion = i });
				SpawnsetHistoryUtils.Save(SpawnsetEditType.Format);
			}

			if (i < 9)
				ImGui.SameLine();
		}

		ImGui.Text("Spawn version");
		ImGui.SameLine();
		for (int i = 4; i < 7; i++)
		{
			if (ImGui.RadioButton(Inline.Span(i), i == FileStates.Spawnset.Object.SpawnVersion) && FileStates.Spawnset.Object.SpawnVersion != i)
			{
				FileStates.Spawnset.Update(FileStates.Spawnset.Object with { SpawnVersion = i });
				SpawnsetHistoryUtils.Save(SpawnsetEditType.Format);
			}

			if (i < 6)
				ImGui.SameLine();
		}

		ImGui.Text("Supported in game version:");

		SpawnsetSupportedGameVersion supportedGameVersion = FileStates.Spawnset.Object.GetSupportedGameVersion();
		ImGui.Text(EnumUtils.SpawnsetSupportedGameVersionNames[supportedGameVersion]);
	}

	private static void RenderGameMode()
	{
		ImGui.Spacing();
		ImGui.Indent(-8);
		ImGui.Text("Game mode");
		ImGui.Separator();
		ImGui.Indent(8);

		foreach (GameMode gameMode in EnumUtils.GameModes)
		{
			ReadOnlySpan<char> displayGameMode = gameMode switch
			{
				GameMode.Survival => "Survival",
				GameMode.TimeAttack => "Time Attack",
				GameMode.Race => "Race",
				_ => throw new UnreachableException(),
			};

			if (ImGui.RadioButton(displayGameMode, gameMode == FileStates.Spawnset.Object.GameMode) && FileStates.Spawnset.Object.GameMode != gameMode)
			{
				FileStates.Spawnset.Update(FileStates.Spawnset.Object with { GameMode = gameMode });
				SpawnsetHistoryUtils.Save(SpawnsetEditType.GameMode);
			}

			if (gameMode != GameMode.Race)
				ImGui.SameLine();
		}
	}

	private static void RenderRaceDagger()
	{
		ImGui.BeginDisabled(FileStates.Spawnset.Object.GameMode != GameMode.Race);

		ImGui.Spacing();
		ImGui.Indent(-8);
		ImGui.Text("Race dagger");
		InfoTooltipWhenDisabled(FileStates.Spawnset.Object.GameMode != GameMode.Race, "Changing the race dagger values doesn't do anything useful if the game mode is not set to Race.\nSet the game mode to Race to enable these inputs.");
		ImGui.Separator();
		ImGui.Indent(8);

		Vector2 raceDaggerPosition = FileStates.Spawnset.Object.RaceDaggerPosition;
		ImGui.InputFloat2("Position", ref raceDaggerPosition, "%.2f", ImGuiInputTextFlags.CharsDecimal);
		if (ImGui.IsItemDeactivatedAfterEdit())
		{
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { RaceDaggerPosition = raceDaggerPosition });
			SpawnsetHistoryUtils.Save(SpawnsetEditType.RaceDagger);
		}

		ImGui.EndDisabled();
	}

	private static void RenderArena()
	{
		ImGui.Spacing();
		ImGui.Indent(-8);
		ImGui.Text("Arena");
		ImGui.Separator();
		ImGui.Indent(8);

		float shrinkStart = FileStates.Spawnset.Object.ShrinkStart;
		if (ImGui.SliderFloat("Shrink start", ref shrinkStart, 0, 150, "%.1f"))
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { ShrinkStart = shrinkStart });
		if (ImGui.IsItemDeactivatedAfterEdit())
			SpawnsetHistoryUtils.Save(SpawnsetEditType.ShrinkStart);

		float shrinkEnd = FileStates.Spawnset.Object.ShrinkEnd;
		if (ImGui.SliderFloat("Shrink end", ref shrinkEnd, 0, 150, "%.1f"))
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { ShrinkEnd = shrinkEnd });
		if (ImGui.IsItemDeactivatedAfterEdit())
			SpawnsetHistoryUtils.Save(SpawnsetEditType.ShrinkEnd);

		float shrinkRate = FileStates.Spawnset.Object.ShrinkRate;
		if (ImGui.SliderFloat("Shrink rate", ref shrinkRate, 0, 10, "%.3f"))
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { ShrinkRate = shrinkRate });
		if (ImGui.IsItemDeactivatedAfterEdit())
			SpawnsetHistoryUtils.Save(SpawnsetEditType.ShrinkRate);

		float brightness = FileStates.Spawnset.Object.Brightness;
		if (ImGui.SliderFloat("Brightness", ref brightness, 0, 500, "%.1f"))
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { Brightness = brightness });
		if (ImGui.IsItemDeactivatedAfterEdit())
			SpawnsetHistoryUtils.Save(SpawnsetEditType.Brightness);
	}

	private static void RenderPractice()
	{
		ImGui.BeginDisabled(FileStates.Spawnset.Object.SpawnVersion <= 4);

		ImGui.Spacing();
		ImGui.Indent(-8);
		ImGui.Text("Practice");
		InfoTooltipWhenDisabled(FileStates.Spawnset.Object.SpawnVersion <= 4, "Practice values are not supported in spawn version 4.");
		ImGui.Separator();
		ImGui.Indent(8);

		for (int i = 0; i < EnumUtils.HandLevels.Count; i++)
		{
			HandLevel level = EnumUtils.HandLevels[i];
			if (ImGui.RadioButton(Inline.Span($"Lvl {(int)level}"), level == FileStates.Spawnset.Object.HandLevel) && FileStates.Spawnset.Object.HandLevel != level)
			{
				FileStates.Spawnset.Update(FileStates.Spawnset.Object with { HandLevel = level });
				SpawnsetHistoryUtils.Save(SpawnsetEditType.HandLevel);
			}

			if (level != HandLevel.Level4)
				ImGui.SameLine();
		}

		int additionalGems = FileStates.Spawnset.Object.AdditionalGems;
		ImGui.InputInt((FileStates.Spawnset.Object.HandLevel == HandLevel.Level2)? Inline.Span($"Added gems ({additionalGems+10})") : "Added gems", ref additionalGems, 1);
		if (ImGui.IsItemDeactivatedAfterEdit())
		{
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { AdditionalGems = additionalGems });
			SpawnsetHistoryUtils.Save(SpawnsetEditType.AdditionalGems);
		}

		ImGui.EndDisabled();

		ImGui.BeginDisabled(FileStates.Spawnset.Object.SpawnVersion <= 5);

		float timerStart = FileStates.Spawnset.Object.TimerStart;
		ImGui.InputFloat("Timer start", ref timerStart, 1, 5, "%.4f");
		if (ImGui.IsItemDeactivatedAfterEdit())
		{
			FileStates.Spawnset.Update(FileStates.Spawnset.Object with { TimerStart = timerStart });
			SpawnsetHistoryUtils.Save(SpawnsetEditType.TimerStart);
		}

		ImGui.EndDisabled();
	}
}
