using DevilDaggersInfo.Api.App.CustomLeaderboards;
using DevilDaggersInfo.App.Networking;
using DevilDaggersInfo.App.Networking.TaskHandlers;
using DevilDaggersInfo.App.User.Cache;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace DevilDaggersInfo.App.Ui.CustomLeaderboards.LeaderboardList;

public static class LeaderboardListChild
{
	public const int PageSize = 20;

	private static readonly string[] _categoryNames = Enum.GetValues<CustomLeaderboardCategory>().Select(et => et.ToString()).ToArray();

	private static int _categoryIndex;
	private static bool _featuredOnly;
	private static string _spawnsetFilter = string.Empty;
	private static string _authorFilter = string.Empty;

	private static readonly List<GetCustomLeaderboardForOverview> _customLeaderboards = new();

	private static CustomLeaderboardCategory Category => Enum.Parse<CustomLeaderboardCategory>(_categoryNames[_categoryIndex]);

	public static bool IsLoading { get; private set; }
	public static int PageIndex { get; private set; }
	public static List<GetCustomLeaderboardForOverview> PagedCustomLeaderboards { get; private set; } = new();
	public static LeaderboardListSorting Sorting { get; set; }
	public static bool SortAscending { get; set; }

	public static int TotalEntries => _customLeaderboards.Count(Predicate);
	public static int TotalPages => (int)Math.Ceiling(TotalEntries / (float)PageSize);
	private static int MaxPageIndex => Math.Max(0, TotalPages - 1);

	public static void Render()
	{
		Vector2 iconSize = new(16);

		ImGui.BeginChild("LeaderboardList");

		if (ImGui.ImageButton((IntPtr)Root.InternalResources.ReloadTexture.Handle, iconSize))
			LoadAll();

		ImGui.SameLine();
		if (ImGui.ImageButton((IntPtr)Root.InternalResources.ArrowStartTexture.Handle, iconSize))
			SetPageIndex(0);

		ImGui.SameLine();
		if (ImGui.ImageButton((IntPtr)Root.InternalResources.ArrowLeftTexture.Handle, iconSize))
			SetPageIndex(PageIndex - 1);

		ImGui.SameLine();
		if (ImGui.ImageButton((IntPtr)Root.InternalResources.ArrowRightTexture.Handle, iconSize))
			SetPageIndex(PageIndex + 1);

		ImGui.SameLine();
		if (ImGui.ImageButton((IntPtr)Root.InternalResources.ArrowEndTexture.Handle, iconSize))
			SetPageIndex(TotalPages - 1);

		ImGui.SameLine();
		ImGui.BeginChild("ComboCategory", new(200, 20));
		if (ImGui.Combo("Category", ref _categoryIndex, _categoryNames, _categoryNames.Length))
			UpdatePagedCustomLeaderboards();
		ImGui.EndChild();

		ImGui.SameLine();
		if (ImGui.Checkbox("Featured", ref _featuredOnly))
			UpdatePagedCustomLeaderboards();

		ImGui.SameLine();
		ImGui.BeginChild("InputSpawnset", new(150, 20));
		if (ImGui.InputText("Name", ref _spawnsetFilter, 32))
			UpdatePagedCustomLeaderboards();
		ImGui.EndChild();

		ImGui.SameLine();
		ImGui.BeginChild("InputAuthor", new(150, 20));
		if (ImGui.InputText("Author", ref _authorFilter, 32))
			UpdatePagedCustomLeaderboards();
		ImGui.EndChild();

		ImGui.EndChild();
	}

	public static void LoadAll()
	{
		IsLoading = true;
		AsyncHandler.Run(
			p =>
			{
				IsLoading = false;
				_customLeaderboards.Clear();

				if (p == null)
				{
					PageIndex = 0;
				}
				else
				{
					_customLeaderboards.AddRange(p);
					ClampPageIndex();
				}

				UpdatePagedCustomLeaderboards();
			},
			() => FetchCustomLeaderboards.HandleAsync(UserCache.Model.PlayerId));
	}

	private static void SetPageIndex(int pageIndex)
	{
		PageIndex = pageIndex;
		ClampPageIndex();
		UpdatePagedCustomLeaderboards();
	}

	private static void ClampPageIndex()
	{
		PageIndex = Math.Clamp(PageIndex, 0, Math.Max(0, MaxPageIndex));
	}

	public static void UpdatePagedCustomLeaderboards()
	{
		IEnumerable<GetCustomLeaderboardForOverview> sorted = Sorting switch
		{
			LeaderboardListSorting.Name => SortAscending ? _customLeaderboards.OrderBy(cl => cl.SpawnsetName.ToLower()) : _customLeaderboards.OrderByDescending(cl => cl.SpawnsetName.ToLower()),
			LeaderboardListSorting.Author => SortAscending ? _customLeaderboards.OrderBy(cl => cl.SpawnsetAuthorName.ToLower()) : _customLeaderboards.OrderByDescending(cl => cl.SpawnsetAuthorName.ToLower()),
			LeaderboardListSorting.Score => SortAscending ? _customLeaderboards.OrderBy(cl => cl.SelectedPlayerStats?.Time) : _customLeaderboards.OrderByDescending(cl => cl.SelectedPlayerStats?.Time),
			LeaderboardListSorting.NextDagger => SortAscending ? _customLeaderboards.OrderBy(GetNextDaggerSortingKey) : _customLeaderboards.OrderByDescending(GetNextDaggerSortingKey),
			LeaderboardListSorting.Rank => SortAscending ? _customLeaderboards.OrderBy(GetRankSortingKey) : _customLeaderboards.OrderByDescending(GetRankSortingKey),
			LeaderboardListSorting.Players => SortAscending ? _customLeaderboards.OrderBy(cl => cl.PlayerCount) : _customLeaderboards.OrderByDescending(cl => cl.PlayerCount),
			LeaderboardListSorting.WorldRecord => SortAscending ? _customLeaderboards.OrderBy(cl => cl.WorldRecord?.Time) : _customLeaderboards.OrderByDescending(cl => cl.WorldRecord?.Time),
			_ => throw new UnreachableException(),
		};

		// Clamp the page index before any filtering.
		ClampPageIndex();

		PagedCustomLeaderboards = sorted
			.Where(Predicate)
			.Skip(PageIndex * PageSize)
			.Take(PageSize)
			.ToList();

		static int GetRankSortingKey(GetCustomLeaderboardForOverview customLeaderboard)
		{
			return customLeaderboard.SelectedPlayerStats?.Rank ?? int.MaxValue;
		}

		static double GetNextDaggerSortingKey(GetCustomLeaderboardForOverview customLeaderboard)
		{
			if (customLeaderboard.Daggers == null || customLeaderboard.SelectedPlayerStats == null)
				return double.MinValue;

			return customLeaderboard.SelectedPlayerStats.NextDagger?.Time ?? double.MaxValue;
		}
	}

	private static bool Predicate(GetCustomLeaderboardForOverview cl)
	{
		return
			cl.Category == Category &&
			(string.IsNullOrEmpty(_spawnsetFilter) || cl.SpawnsetName.Contains(_spawnsetFilter, StringComparison.OrdinalIgnoreCase)) &&
			(string.IsNullOrEmpty(_authorFilter) || cl.SpawnsetAuthorName.Contains(_authorFilter, StringComparison.OrdinalIgnoreCase)) &&
			(!_featuredOnly || cl.Daggers != null);
	}
}
