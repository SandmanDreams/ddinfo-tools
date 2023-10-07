using DevilDaggersInfo.Web.ApiSpec.App.CustomLeaderboards;

namespace DevilDaggersInfo.Tools.Networking.TaskHandlers;

public static class FetchCustomLeaderboards
{
	public static async Task<List<GetCustomLeaderboardForOverview>> HandleAsync(int selectedPlayerId)
	{
		return await AsyncHandler.Client.GetCustomLeaderboards(selectedPlayerId);
	}
}