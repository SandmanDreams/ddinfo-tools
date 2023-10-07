using DevilDaggersInfo.Web.ApiSpec.App.CustomLeaderboards;

namespace DevilDaggersInfo.Tools.Networking.TaskHandlers;

public static class FetchCustomLeaderboardById
{
	public static async Task<GetCustomLeaderboard> HandleAsync(int customLeaderboardId)
	{
		return await AsyncHandler.Client.GetCustomLeaderboardById(customLeaderboardId);
	}
}