using DevilDaggersInfo.App.Engine.Maths;
using DevilDaggersInfo.App.Engine.Maths.Numerics;
using DevilDaggersInfo.App.Extensions;
using DevilDaggersInfo.App.User.Settings;
using DevilDaggersInfo.App.User.Settings.Model;
using DevilDaggersInfo.Common;
using DevilDaggersInfo.Core.Spawnset;
using DevilDaggersInfo.Core.Spawnset.View;
using DevilDaggersInfo.Core.Wiki;
using ImGuiNET;
using System.Numerics;

namespace DevilDaggersInfo.App.Ui.Practice;

public static class PracticeWindow
{
	private const float _timerStartTolerance = 0.00001f;
	private const int _templateWidth = 360;
	private const int _templateDescriptionHeight = 48;

	private static readonly Vector2 _templateContainerSize = new(400, 480);
	private static readonly Vector2 _templateListSize = new(380, 380);

	private static readonly List<Template> _noFarmTemplates = new()
	{
		new("First Spider I & Squid II", EnemiesV3_2.Squid2.Color.ToEngineColor(), HandLevel.Level1, 8, 39),
		new("First Centipede", EnemiesV3_2.Centipede.Color.ToEngineColor(), HandLevel.Level2, 25, 114),
		new("Centipede & first triple Spider Is", EnemiesV3_2.Spider1.Color.ToEngineColor(), HandLevel.Level3, 11, 174),
		new("Six Squid Is", EnemiesV3_2.Squid3.Color.ToEngineColor(), HandLevel.Level3, 57, 229),
		new("Gigapedes", EnemiesV3_2.Gigapede.Color.ToEngineColor(), HandLevel.Level3, 81, 259),
		new("Leviathan", EnemiesV3_2.Leviathan.Color.ToEngineColor(), HandLevel.Level4, 105, 350),
		new("Post Orb", EnemiesV3_2.TheOrb.Color.ToEngineColor(), HandLevel.Level4, 129, 397),
	};

	private static readonly List<float> _endLoopTimerStarts = new();

	private static State _state = State.Default;

	static PracticeWindow()
	{
		const int endLoopTemplateWaveCount = 33;
		SpawnsView spawnsView = new(ContentManager.Content.DefaultSpawnset, GameVersion.V3_2, endLoopTemplateWaveCount);
		for (int i = 0; i < endLoopTemplateWaveCount; i++)
		{
			float timerStart;
			if (i == 0)
				timerStart = spawnsView.Waves[i][0].Seconds;
			else
				timerStart = spawnsView.Waves[i - 1][^1].Seconds + 0.1f; // Make sure we don't accidentally include the last enemy of the previous wave.

			_endLoopTimerStarts.Add(timerStart);
		}
	}

	public static void Render()
	{
		ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Constants.MinWindowSize);
		ImGui.Begin("Practice", ImGuiWindowFlags.NoCollapse);
		ImGui.PopStyleVar();

		ImGui.Text("Use these templates to practice specific sections of the game. Click on a template to install it.");
		ImGui.Spacing();

		ImGui.BeginChild("No farm templates", _templateContainerSize, true);
		ImGui.Text("No farm templates");

		ImGui.BeginChild("No farm template description", _templateListSize with { Y = _templateDescriptionHeight });
		ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + _templateWidth);
		ImGui.Text("The amount of gems is based on how many gems you would have at that point, without farming, without losing gems, and without any homing usage.");
		ImGui.PopTextWrapPos();
		ImGui.EndChild();

		ImGui.BeginChild("No farm template list", _templateListSize);
		foreach (Template noFarmTemplate in _noFarmTemplates)
			RenderTemplate(noFarmTemplate);

		ImGui.EndChild();
		ImGui.EndChild();

		ImGui.SameLine();
		ImGui.BeginChild("End loop templates", _templateContainerSize, true);
		ImGui.Text("End loop templates");

		ImGui.BeginChild("End loop template description", _templateListSize with { Y = _templateDescriptionHeight });
		ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + _templateWidth);
		ImGui.Text("The amount of homing for the end loop waves is set to 0. Use one that is realistic for you.");
		ImGui.PopTextWrapPos();
		ImGui.EndChild();

		ImGui.BeginChild("End loop template list", _templateListSize);
		for (int i = 0; i < _endLoopTimerStarts.Count; i++)
			RenderEndLoopTemplate(i, _endLoopTimerStarts[i]);

		ImGui.EndChild();
		ImGui.EndChild();

		ImGui.SameLine();
		ImGui.BeginChild("Custom templates", _templateContainerSize, true);
		ImGui.Text("Custom templates");

		ImGui.BeginChild("Custom template description", _templateListSize with { Y = _templateDescriptionHeight });
		ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + _templateWidth);
		ImGui.Text("You can make your own templates and save them. Your custom templates are saved locally on your computer.");
		ImGui.PopTextWrapPos();
		ImGui.EndChild();

		ImGui.BeginChild("Custom template list", _templateListSize);
		for (int i = 0; i < UserSettings.Model.PracticeTemplates.Count; i++)
			RenderCustomTemplate(UserSettings.Model.PracticeTemplates[i]);

		ImGui.EndChild();
		ImGui.EndChild();

		ImGui.BeginChild("Input values", new(400, 192), true);

		ImGui.Spacing();
		ImGui.Image((IntPtr)Root.InternalResources.IconHandTexture.Handle, new(16), Vector2.Zero, Vector2.One, _state.HandLevel.GetColor());
		ImGui.SameLine();
		foreach (HandLevel level in Enum.GetValues<HandLevel>())
		{
			if (ImGui.RadioButton($"Lvl {(int)level}", level == _state.HandLevel) && _state.HandLevel != level)
				_state.HandLevel = level;

			if (level != HandLevel.Level4)
				ImGui.SameLine();
		}

		(Texture gemOrHomingTexture, Color tintColor) = _state.HandLevel is HandLevel.Level3 or HandLevel.Level4 ? (Root.GameResources.IconMaskHomingTexture, Color.White) : (Root.GameResources.IconMaskGemTexture, Color.Red);
		ImGui.Spacing();
		ImGui.Image((IntPtr)gemOrHomingTexture.Handle, new(16), Vector2.UnitY, Vector2.UnitX, tintColor);
		ImGui.SameLine();
		ImGui.InputInt("Added gems", ref _state.AdditionalGems, 1);

		ImGui.Spacing();
		ImGui.Image((IntPtr)Root.GameResources.IconMaskStopwatchTexture.Handle, new(16), Vector2.UnitY, Vector2.UnitX);
		ImGui.SameLine();
		ImGui.InputFloat("Timer start", ref _state.TimerStart, 1, 5, "%.4f");

		for (int i = 0; i < 8; i++)
			ImGui.Spacing();

		if (ImGui.Button("Apply", new(80, 30)))
			Apply();

		ImGui.SameLine();
		if (ImGui.Button("Save", new(80, 30)))
		{
			UserSettingsModel.UserSettingsPracticeTemplate newTemplate = new(_state.HandLevel, _state.AdditionalGems, _state.TimerStart);
			if (!UserSettings.Model.PracticeTemplates.Contains(newTemplate))
			{
				UserSettings.Model = UserSettings.Model with
				{
					PracticeTemplates = UserSettings.Model.PracticeTemplates
						.Append(newTemplate)
						.OrderBy(pt => pt.TimerStart)
						.ThenBy(pt => pt.HandLevel)
						.ThenBy(pt => pt.AdditionalGems)
						.ToList(),
				};
			}
		}

		ImGui.EndChild();

		ImGui.SameLine();
		ImGui.BeginChild("Current spawnset", new(400, 192), true);

		ImGui.Text("Current spawnset");

		ImGui.BeginChild("Current practice values", new(400, 64));
		if (SurvivalFileWatcher.Exists)
		{
			ImGui.Text(SurvivalFileWatcher.HandLevel.ToString());
			ImGui.Text(SurvivalFileWatcher.AdditionalGems.ToString());
			ImGui.Text(SurvivalFileWatcher.TimerStart.ToString(StringFormats.TimeFormat));
		}
		else
		{
			ImGui.Text("<No spawnset enabled>");
		}

		ImGui.EndChild();

		ImGui.BeginDisabled(!SurvivalFileWatcher.Exists);
		if (ImGui.Button("Delete spawnset (restore default)", new(0, 30)))
			DeleteModdedSpawnset();

		ImGui.EndDisabled();

		ImGui.EndChild();

		ImGui.End();

		if (ImGui.IsKeyPressed(ImGuiKey.Escape) || ImGui.IsKeyPressed((ImGuiKey)526))
			UiRenderer.Layout = LayoutType.Main;
	}

	private static void RenderTemplate(Template template)
	{
		(byte backgroundAlpha, byte textAlpha) = GetAlpha(template.IsEqual(_state));

		string timerText = template.TimerStart.ToString(StringFormats.TimeFormat);
		(string gemsOrHomingText, Color gemColor) = template.HandLevel switch
		{
			HandLevel.Level3 => ($"{template.AdditionalGems} homing", HandLevel.Level3.GetColor()),
			HandLevel.Level4 => ($"{template.AdditionalGems} homing", HandLevel.Level4.GetColor()),
			_ => ($"{template.AdditionalGems} gems", Color.Red),
		};

		ImGuiExt.ButtonWrapper(
			template.Name,
			new(_templateWidth, 48),
			template.Color with { A = backgroundAlpha },
			template.Color with { A = 31 },
			() =>
			{
				_state = new(template.HandLevel, template.AdditionalGems, template.TimerStart);
				Apply();
			},
			template.Color with { A = textAlpha },
			template.Name,
			Color.White with { A = textAlpha },
			timerText,
			template.HandLevel.GetColor() with { A = textAlpha },
			template.HandLevel.ToString(),
			gemColor with { A = textAlpha },
			gemsOrHomingText);
	}

	private static void RenderEndLoopTemplate(int waveIndex, float timerStart)
	{
		(byte backgroundAlpha, byte textAlpha) = GetAlpha(IsEqual(_state, timerStart));

		string timerText = timerStart.ToString(StringFormats.TimeFormat);
		string name = $"Wave {waveIndex + 1}";
		Color color = waveIndex % 3 == 2 ? EnemiesV3_2.Ghostpede.Color.ToEngineColor() : EnemiesV3_2.Gigapede.Color.ToEngineColor();

		ImGuiExt.ButtonWrapper(
			name,
			new(_templateWidth, 30),
			color with { A = backgroundAlpha },
			color with { A = 31 },
			() =>
			{
				_state = new(HandLevel.Level4, 0, timerStart);
				Apply();
			},
			color with { A = textAlpha },
			name,
			Color.White with { A = textAlpha },
			timerText);

		static bool IsEqual(State state, float timerStart)
		{
			return state is { HandLevel: HandLevel.Level4, AdditionalGems: 0 } && Math.Abs(state.TimerStart - timerStart) < _timerStartTolerance;
		}
	}

	private static void RenderCustomTemplate(UserSettingsModel.UserSettingsPracticeTemplate customTemplate)
	{
		Color color = Color.White;
		string uniqueName = $"{customTemplate.HandLevel}-{customTemplate.AdditionalGems}-{customTemplate.TimerStart.ToString(StringFormats.TimeFormat)}";
		(byte backgroundAlpha, byte textAlpha) = GetAlpha(_state.IsEqual(customTemplate));

		string timerText = customTemplate.TimerStart.ToString(StringFormats.TimeFormat);
		(string gemsOrHomingText, Color gemColor) = customTemplate.HandLevel switch
		{
			HandLevel.Level3 => ($"{customTemplate.AdditionalGems} homing", HandLevel.Level3.GetColor()),
			HandLevel.Level4 => ($"{customTemplate.AdditionalGems} homing", HandLevel.Level4.GetColor()),
			_ => ($"{customTemplate.AdditionalGems} gems", Color.Red),
		};

		ImGuiExt.ButtonWrapper(
			uniqueName,
			new(_templateWidth - 56, 48),
			color with { A = backgroundAlpha },
			color with { A = 31 },
			() =>
			{
				_state = new(customTemplate.HandLevel, customTemplate.AdditionalGems, customTemplate.TimerStart);
				Apply();
			},
			color with { A = textAlpha },
			"Custom template",
			Color.White with { A = textAlpha },
			timerText,
			customTemplate.HandLevel.GetColor() with { A = textAlpha },
			customTemplate.HandLevel.ToString(),
			gemColor with { A = textAlpha },
			gemsOrHomingText);

		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Button, Color.Red with { A = 159 });
		ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red);
		ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red with { A = 223 });
		ImGui.PushID(customTemplate.ToString());
		if (ImGui.ImageButton((IntPtr)Root.InternalResources.BinTexture.Handle, new(24), Vector2.Zero, Vector2.One, 12))
		{
			UserSettings.Model = UserSettings.Model with
			{
				PracticeTemplates = UserSettings.Model.PracticeTemplates.Where(pt => customTemplate != pt).ToList(),
			};
		}

		ImGui.PopID();
		ImGui.PopStyleColor(3);
	}

	private static (byte BackgroundAlpha, byte TextAlpha) GetAlpha(bool isActive)
	{
		return isActive ? ((byte)48, (byte)255) : ((byte)16, (byte)191);
	}

	private static void Apply()
	{
		_state.TimerStart = Math.Clamp(_state.TimerStart, 0, 1400);

		SpawnsetBinary spawnset = ContentManager.Content.DefaultSpawnset;
		float shrinkStart = MathUtils.Lerp(spawnset.ShrinkStart, spawnset.ShrinkEnd, _state.TimerStart / ((spawnset.ShrinkStart - spawnset.ShrinkEnd) / spawnset.ShrinkRate));

		SpawnsetBinary generatedSpawnset = spawnset.GetWithHardcodedEndLoop(70).GetWithTrimmedStart(_state.TimerStart) with
		{
			HandLevel = _state.HandLevel,
			AdditionalGems = _state.AdditionalGems,
			TimerStart = _state.TimerStart,
			SpawnVersion = 6,
			ShrinkStart = shrinkStart,
		};
		File.WriteAllBytes(UserSettings.ModsSurvivalPath, generatedSpawnset.ToBytes());
	}

	private static void DeleteModdedSpawnset()
	{
		if (File.Exists(UserSettings.ModsSurvivalPath))
			File.Delete(UserSettings.ModsSurvivalPath);
	}

	private struct State
	{
		public HandLevel HandLevel;
		public int AdditionalGems;
		public float TimerStart;

		public State(HandLevel handLevel, int additionalGems, float timerStart)
		{
			HandLevel = handLevel;
			AdditionalGems = additionalGems;
			TimerStart = timerStart;
		}

		public static State Default => new(HandLevel.Level1, 0, 0);

		public bool IsEqual(UserSettingsModel.UserSettingsPracticeTemplate practiceTemplate)
		{
			return HandLevel == practiceTemplate.HandLevel && AdditionalGems == practiceTemplate.AdditionalGems && Math.Abs(TimerStart - practiceTemplate.TimerStart) < _timerStartTolerance;
		}
	}

	private readonly record struct Template(string Name, Color Color, HandLevel HandLevel, int AdditionalGems, float TimerStart)
	{
		public bool IsEqual(State state)
		{
			return HandLevel == state.HandLevel && AdditionalGems == state.AdditionalGems && Math.Abs(TimerStart - state.TimerStart) < _timerStartTolerance;
		}
	}
}
