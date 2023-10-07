using DevilDaggersInfo.Web.ApiSpec.App.CustomLeaderboards;

namespace DevilDaggersInfo.Tools.Networking.TaskHandlers;

public static class FetchCustomEntryReplayById
{
	public static async Task<GetCustomEntryReplayBuffer> HandleAsync(int customEntryId)
	{
		return await AsyncHandler.Client.GetCustomEntryReplayBufferById(customEntryId);
	}
}