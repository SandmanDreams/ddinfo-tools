using DevilDaggersInfo.App.Engine.Maths.Numerics;
using DevilDaggersInfo.App.Ui.SpawnsetEditor.Arena.EditorChildren;
using DevilDaggersInfo.App.Ui.SpawnsetEditor.State;
using DevilDaggersInfo.App.Ui.SpawnsetEditor.Utils;
using DevilDaggersInfo.Core.Spawnset;
using ImGuiNET;
using System.Numerics;

namespace DevilDaggersInfo.App.Ui.SpawnsetEditor.Arena.EditorStates;

public class ArenaDaggerState : IArenaState
{
	private bool _settingRaceDagger;
	private Vector2? _position;

	public void Handle(ArenaMousePosition mousePosition)
	{
		if (SpawnsetState.Spawnset.GameMode != GameMode.Race)
			return;

		if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
		{
			_settingRaceDagger = true;
			_position = GetSnappedDaggerPosition();
		}
		else if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
		{
			if (_settingRaceDagger)
				_position = GetSnappedDaggerPosition();
		}
		else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
		{
			if (!_position.HasValue)
				return;

			Vector2 tileCoordinate = _position.Value / ArenaChild.TileSize;
			Vector2 daggerPosition = new(SpawnsetState.Spawnset.TileToWorldCoordinate(tileCoordinate.X), SpawnsetState.Spawnset.TileToWorldCoordinate(tileCoordinate.Y));

			SpawnsetState.Spawnset = SpawnsetState.Spawnset with { RaceDaggerPosition = daggerPosition };
			SpawnsetHistoryUtils.Save(SpawnsetEditType.RaceDagger);

			Reset();
		}

		Vector2 GetSnappedDaggerPosition()
		{
			return ArenaEditingUtils.Snap(mousePosition.Real, DaggerChild.Snap * ArenaChild.TileSize);
		}
	}

	public void HandleOutOfRange(ArenaMousePosition mousePosition)
	{
		Reset();
	}

	private void Reset()
	{
		_settingRaceDagger = false;
		_position = null;
	}

	public void Render(ArenaMousePosition mousePosition)
	{
		if (!_position.HasValue)
			return;

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 origin = ImGui.GetCursorScreenPos();
		Vector2 center = origin + _position.Value + ArenaChild.HalfTileSizeAsVector2;
		drawList.AddImage((IntPtr)Root.InternalResources.IconDaggerTexture.Handle, center - new Vector2(8), center + new Vector2(8), Vector2.UnitX, Vector2.UnitY, ImGui.GetColorU32(Color.HalfTransparentWhite));
	}
}
