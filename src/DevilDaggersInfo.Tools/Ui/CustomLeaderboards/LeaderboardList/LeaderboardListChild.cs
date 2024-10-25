using DevilDaggersInfo.Tools.Networking;
using DevilDaggersInfo.Tools.Networking.TaskHandlers;
using DevilDaggersInfo.Tools.Ui.Popups;
using DevilDaggersInfo.Tools.User.Cache;
using DevilDaggersInfo.Web.ApiSpec.Tools.CustomLeaderboards;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace DevilDaggersInfo.Tools.Ui.CustomLeaderboards.LeaderboardList;

public static class LeaderboardListChild
{
	public const int PageSize = 20;

	private static readonly List<GetCustomLeaderboardAllowedCategory> _categories = [];
	private static string[] _categoryNames = [];

	private static int _categoryIndex;
	private static bool _featuredOnly;
	private static string _spawnsetFilter = string.Empty;
	private static string _authorFilter = string.Empty;

	private static readonly List<GetCustomLeaderboardForOverview> _customLeaderboards = [];

	private static CustomLeaderboardRankSorting RankSorting => _categories.Count > _categoryIndex ? _categories[_categoryIndex].RankSorting : CustomLeaderboardRankSorting.TimeDesc;
	private static SpawnsetGameMode GameMode => _categories.Count > _categoryIndex ? _categories[_categoryIndex].GameMode : SpawnsetGameMode.Survival;

	public static bool IsLoading { get; private set; }
	public static int PageIndex { get; private set; }
	public static List<GetCustomLeaderboardForOverview> PagedCustomLeaderboards { get; private set; } = [];
	public static LeaderboardListSorting Sorting { get; set; }
	public static bool SortAscending { get; set; }

	public static int TotalEntries => GetCount();
	public static int TotalPages => (int)Math.Ceiling(TotalEntries / (float)PageSize);
	private static int MaxPageIndex => Math.Max(0, TotalPages - 1);

	public static void Render()
	{
		Vector2 iconSize = new(16);

		if (ImGui.BeginChild("LeaderboardList"))
		{
			if (ImGuiImage.ImageButton("Reload", Root.InternalResources.ReloadTexture.Id, iconSize))
				LoadAll();

			ImGui.SameLine();
			if (ImGuiImage.ImageButton("Begin", Root.InternalResources.ArrowStartTexture.Id, iconSize))
				SetPageIndex(0);

			ImGui.SameLine();
			if (ImGuiImage.ImageButton("Previous", Root.InternalResources.ArrowLeftTexture.Id, iconSize))
				SetPageIndex(PageIndex - 1);

			ImGui.SameLine();
			if (ImGuiImage.ImageButton("Next", Root.InternalResources.ArrowRightTexture.Id, iconSize))
				SetPageIndex(PageIndex + 1);

			ImGui.SameLine();
			if (ImGuiImage.ImageButton("End", Root.InternalResources.ArrowEndTexture.Id, iconSize))
				SetPageIndex(TotalPages - 1);

			ImGui.SameLine();
			if (ImGui.BeginChild("ComboCategory", new Vector2(360, 20)))
			{
				if (_categoryNames.Length > 0 && ImGui.Combo("Category", ref _categoryIndex, _categoryNames, _categoryNames.Length, 20))
					UpdatePagedCustomLeaderboards();
			}

			ImGui.EndChild();

			ImGui.SameLine();
			if (ImGui.Checkbox("Featured", ref _featuredOnly))
				UpdatePagedCustomLeaderboards();

			// TODO: Tab doesn't work because of children.
			ImGui.SameLine();
			if (ImGui.BeginChild("InputSpawnset", new Vector2(150, 20)))
			{
				if (ImGui.InputText("Name", ref _spawnsetFilter, 32))
					UpdatePagedCustomLeaderboards();
			}

			ImGui.EndChild();

			ImGui.SameLine();
			if (ImGui.BeginChild("InputAuthor", new Vector2(150, 20)))
			{
				if (ImGui.InputText("Author", ref _authorFilter, 32))
					UpdatePagedCustomLeaderboards();
			}

			ImGui.EndChild();
		}

		ImGui.EndChild();
	}

	public static void LoadAll()
	{
		AsyncHandler.Run(
			getCategoriesResult =>
			{
				getCategoriesResult.Match(
					onSuccess: getCategories =>
					{
						_categories.Clear();
						_categories.AddRange(getCategories);
						_categoryNames = getCategories.Select(ToDisplayString).ToArray();

						static string ToDisplayString(GetCustomLeaderboardAllowedCategory allowedCategory)
						{
							string gameModeString = allowedCategory.GameMode switch
							{
								SpawnsetGameMode.Survival => "Survival",
								SpawnsetGameMode.TimeAttack => "Time Attack",
								SpawnsetGameMode.Race => "Race",
								_ => throw new UnreachableException(),
							};

							string rankSortingString = allowedCategory.RankSorting switch
							{
								CustomLeaderboardRankSorting.TimeDesc => "Highest Time",
								CustomLeaderboardRankSorting.TimeAsc => "Lowest Time",
								CustomLeaderboardRankSorting.GemsCollectedDesc => "Most Gems",
								CustomLeaderboardRankSorting.GemsCollectedAsc => "Least Gems",
								CustomLeaderboardRankSorting.GemsDespawnedDesc => "Most Gems Despawned",
								CustomLeaderboardRankSorting.GemsDespawnedAsc => "Least Gems Despawned",
								CustomLeaderboardRankSorting.GemsEatenDesc => "Most Gems Eaten",
								CustomLeaderboardRankSorting.GemsEatenAsc => "Least Gems Eaten",
								CustomLeaderboardRankSorting.EnemiesKilledDesc => "Most Kills",
								CustomLeaderboardRankSorting.EnemiesKilledAsc => "Least Kills",
								CustomLeaderboardRankSorting.EnemiesAliveDesc => "Most Enemies Alive",
								CustomLeaderboardRankSorting.EnemiesAliveAsc => "Least Enemies Alive",
								CustomLeaderboardRankSorting.HomingStoredDesc => "Most Homing",
								CustomLeaderboardRankSorting.HomingStoredAsc => "Least Homing",
								CustomLeaderboardRankSorting.HomingEatenDesc => "Most Homing Eaten",
								CustomLeaderboardRankSorting.HomingEatenAsc	=> "Least Homing Eaten",
								_ => allowedCategory.RankSorting.ToString(), // Fallback for when more sorting options are added.
							};

							return $"{gameModeString}: {rankSortingString}";
						}
					},
					onError: apiError =>
					{
						PopupManager.ShowError("Failed to fetch allowed categories.", apiError);
						Root.Log.Error(apiError.Exception, "Failed to fetch allowed categories.");
					});
			},
			FetchAllowedCategories.HandleAsync);

		IsLoading = true;
		AsyncHandler.Run(
			getLeaderboardsResult =>
			{
				IsLoading = false;
				_customLeaderboards.Clear();

				getLeaderboardsResult.Match(
					onSuccess: getLeaderboards =>
					{
						_customLeaderboards.AddRange(getLeaderboards);
						ClampPageIndex();
					},
					onError: apiError =>
					{
						PageIndex = 0;
						PopupManager.ShowError("Failed to fetch custom leaderboards.", apiError);
						Root.Log.Error(apiError.Exception, "Failed to fetch custom leaderboards.");
					});

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
			LeaderboardListSorting.Score => SortAscending ? _customLeaderboards.OrderBy(cl => cl.SelectedPlayerStats?.HighscoreValue) : _customLeaderboards.OrderByDescending(cl => cl.SelectedPlayerStats?.HighscoreValue),
			LeaderboardListSorting.NextDagger => SortAscending ? _customLeaderboards.OrderBy(GetNextDaggerSortingKey) : _customLeaderboards.OrderByDescending(GetNextDaggerSortingKey),
			LeaderboardListSorting.Rank => SortAscending ? _customLeaderboards.OrderBy(GetRankSortingKey) : _customLeaderboards.OrderByDescending(GetRankSortingKey),
			LeaderboardListSorting.Players => SortAscending ? _customLeaderboards.OrderBy(cl => cl.PlayerCount) : _customLeaderboards.OrderByDescending(cl => cl.PlayerCount),
			LeaderboardListSorting.WorldRecord => SortAscending ? _customLeaderboards.OrderBy(cl => cl.WorldRecord?.WorldRecordValue) : _customLeaderboards.OrderByDescending(cl => cl.WorldRecord?.WorldRecordValue),
			LeaderboardListSorting.DateCreated => SortAscending ? _customLeaderboards.OrderBy(cl => cl.DateCreated) : _customLeaderboards.OrderByDescending(cl => cl.DateCreated),
			LeaderboardListSorting.DateLastPlayed => SortAscending ? _customLeaderboards.OrderBy(cl => cl.DateLastPlayed ?? DateTime.UtcNow) : _customLeaderboards.OrderByDescending(cl => cl.DateLastPlayed ?? DateTime.UtcNow),
			_ => throw new UnreachableException(),
		};

		// Clamp the page index before doing any filtering.
		ClampPageIndex();

		PagedCustomLeaderboards = sorted
			.Where(ApplyOverviewFilter)
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

			return customLeaderboard.SelectedPlayerStats.NextDagger?.DaggerValue ?? double.MaxValue;
		}
	}

	private static int GetCount()
	{
		int count = 0;
		for (int i = 0; i < _customLeaderboards.Count; i++)
		{
			if (ApplyOverviewFilter(_customLeaderboards[i]))
				count++;
		}

		return count;
	}

	private static bool ApplyOverviewFilter(GetCustomLeaderboardForOverview cl)
	{
		return
			cl.RankSorting == RankSorting &&
			cl.SpawnsetGameMode == GameMode &&
			(string.IsNullOrEmpty(_spawnsetFilter) || cl.SpawnsetName.Contains(_spawnsetFilter, StringComparison.OrdinalIgnoreCase)) && // TODO: Trim filters?
			(string.IsNullOrEmpty(_authorFilter) || cl.SpawnsetAuthorName.Contains(_authorFilter, StringComparison.OrdinalIgnoreCase)) &&
			(!_featuredOnly || cl.Daggers != null);
	}
}
