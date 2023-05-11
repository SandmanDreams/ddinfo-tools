using DevilDaggersInfo.App.Engine.Content;
using DevilDaggersInfo.App.Engine.Parsers.Sound;
using DevilDaggersInfo.App.User.Settings;
using DevilDaggersInfo.Core.Asset;
using DevilDaggersInfo.Core.Mod;
using DevilDaggersInfo.Core.Mod.Extensions;
using DevilDaggersInfo.Core.Spawnset;

namespace DevilDaggersInfo.App;

public static class ContentManager
{
	private static ContentContainer? _content;

	public static ContentContainer Content
	{
		get => _content ?? throw new InvalidOperationException("Content is not initialized.");
		private set => _content = value;
	}

	public static void Initialize()
	{
		if (!Directory.Exists(UserSettings.Model.DevilDaggersInstallationDirectory))
			throw new InvalidGameInstallationException("Installation directory does not exist.");

		// TODO: Use correct Linux file name for executable.
#if WINDOWS
		string ddExe = Path.Combine(UserSettings.Model.DevilDaggersInstallationDirectory, "dd.exe");
		if (!File.Exists(ddExe))
			throw new InvalidGameInstallationException("Executable does not exist.");
#endif

		if (!File.Exists(UserSettings.DdSurvivalPath))
			throw new InvalidGameInstallationException("File 'dd/survival' does not exist.");

		if (!File.Exists(UserSettings.ResAudioPath))
			throw new InvalidGameInstallationException("File 'res/audio' does not exist.");

		if (!File.Exists(UserSettings.ResDdPath))
			throw new InvalidGameInstallationException("File 'res/dd' does not exist.");

		// TODO: Also verify survival hash.
		byte[] survivalBytes = File.ReadAllBytes(UserSettings.DdSurvivalPath);
		if (!SpawnsetBinary.TryParse(survivalBytes, out SpawnsetBinary? defaultSpawnset))
			throw new InvalidGameInstallationException("File 'dd/survival' could not be parsed.");

		if (Directory.Exists(UserSettings.ModsSurvivalPath))
			throw new InvalidGameInstallationException("There must not be a directory named 'survival' in the 'mods' directory. You must delete the directory, or mods will not work.");

		ModBinaryReadFilter ddReadFilter = ModBinaryReadFilter.Assets(
			new(AssetType.Texture, "iconmaskdagger"),
			new(AssetType.Mesh, "dagger"),
			new(AssetType.Texture, "daggersilver"),
			new(AssetType.Mesh, "boid4"),
			new(AssetType.Texture, "boid4"),
			new(AssetType.Mesh, "boid4jaw"),
			new(AssetType.Texture, "boid4jaw"),
			new(AssetType.Mesh, "tile"),
			new(AssetType.Texture, "tile"),
			new(AssetType.Mesh, "pillar"),
			new(AssetType.Texture, "pillar"),
			new(AssetType.Texture, "post_lut"),
			new(AssetType.Mesh, "hand4"),
			new(AssetType.Texture, "hand6"));

		ModBinaryReadFilter audioReadFilter = ModBinaryReadFilter.Assets(
			new(AssetType.Audio, "jump1"),
			new(AssetType.Audio, "jump2"),
			new(AssetType.Audio, "jump3"));

		ModBinary ddBinary;
		using (FileStream fs = new(UserSettings.ResDdPath, FileMode.Open))
			ddBinary = new(fs, ddReadFilter);

		ModBinary audioBinary;
		using (FileStream fs = new(UserSettings.ResAudioPath, FileMode.Open))
			audioBinary = new(fs, audioReadFilter);

		Content = new(
			DefaultSpawnset: defaultSpawnset,
			IconDaggerTexture: GetTexture(ddBinary, "iconmaskdagger"),
			DaggerMesh: GetMesh(ddBinary, "dagger"),
			DaggerSilverTexture: GetTexture(ddBinary, "daggersilver"),
			Skull4Mesh: GetMesh(ddBinary, "boid4"),
			Skull4Texture: GetTexture(ddBinary, "boid4"),
			Skull4JawMesh: GetMesh(ddBinary, "boid4jaw"),
			Skull4JawTexture: GetTexture(ddBinary, "boid4jaw"),
			TileMesh: GetMesh(ddBinary, "tile"),
			TileTexture: GetTexture(ddBinary, "tile"),
			PillarMesh: GetMesh(ddBinary, "pillar"),
			PillarTexture: GetTexture(ddBinary, "pillar"),
			SoundJump1: GetSound(audioBinary, "jump1"),
			SoundJump2: GetSound(audioBinary, "jump2"),
			SoundJump3: GetSound(audioBinary, "jump3"),
			PostLut: GetTexture(ddBinary, "post_lut"),
			Hand4Mesh: GetMesh(ddBinary, "hand4"),
			Hand4Texture: GetTexture(ddBinary, "hand6"));
	}

	private static MeshContent GetMesh(ModBinary ddBinary, string meshName)
	{
		if (!ddBinary.AssetMap.TryGetValue(new(AssetType.Mesh, meshName), out AssetData? meshData))
			throw new InvalidGameInstallationException($"Required mesh '{meshName}' from 'res/dd' was not found.");

		return ToEngineMesh(meshData.Buffer);
	}

	private static TextureContent GetTexture(ModBinary ddBinary, string textureName)
	{
		if (!ddBinary.AssetMap.TryGetValue(new(AssetType.Texture, textureName), out AssetData? textureData))
			throw new InvalidGameInstallationException($"Required texture '{textureName}' from 'res/dd' was not found.");

		return ToEngineTexture(textureName, textureData.Buffer);
	}

	private static SoundContent GetSound(ModBinary audioBinary, string soundName)
	{
		if (!audioBinary.AssetMap.TryGetValue(new(AssetType.Audio, soundName), out AssetData? audioData))
			throw new InvalidGameInstallationException($"Required audio '{soundName}' from 'res/audio' was not found.");

		SoundData waveData = WaveParser.Parse(audioData.Buffer);
		return new(waveData.Channels, waveData.SampleRate, waveData.BitsPerSample, waveData.Data.Length, waveData.Data);
	}

	private static MeshContent ToEngineMesh(byte[] ddMeshBuffer)
	{
		using MemoryStream ms = new(ddMeshBuffer);
		using BinaryReader br = new(ms);

		int indexCount = br.ReadInt32();
		int vertexCount = br.ReadInt32();
		_ = br.ReadUInt16();

		DevilDaggersInfo.Core.Mod.Structs.Vertex[] ddVertices = new DevilDaggersInfo.Core.Mod.Structs.Vertex[vertexCount];
		for (int i = 0; i < ddVertices.Length; i++)
			ddVertices[i] = br.ReadVertex();

		uint[] indices = new uint[indexCount];
		for (int i = 0; i < indices.Length; i++)
			indices[i] = br.ReadUInt32();

		Vertex[] engineVertices = new Vertex[vertexCount];
		for (int i = 0; i < ddVertices.Length; i++)
		{
			DevilDaggersInfo.Core.Mod.Structs.Vertex ddVertex = ddVertices[i];
			engineVertices[i] = new(ddVertex.Position, ddVertex.TexCoord, ddVertex.Normal);
		}

		return new(engineVertices, indices);
	}

	private static TextureContent ToEngineTexture(string textureName, byte[] ddTextureBuffer)
	{
		const ushort expectedHeader = 16401;
		const int headerSize = 11;
		using MemoryStream ms = new(ddTextureBuffer);
		using BinaryReader br = new(ms);
		ushort header = br.ReadUInt16();
		if (header != expectedHeader)
			throw new InvalidGameInstallationException($"Invalid header for texture '{textureName}'. Should be {expectedHeader} but got {header}. Make sure your game files are not corrupted.");

		int width = br.ReadInt32();
		int height = br.ReadInt32();
		if (width < 0 || height < 0)
			throw new InvalidGameInstallationException($"Dimensions for texture '{textureName}' cannot be negative ({width}x{height}). Make sure your game files are not corrupted.");

		_ = br.ReadByte(); // Mipmap count

		int minimumSize = width * height * 4 + headerSize;
		if (ddTextureBuffer.Length < minimumSize)
			throw new InvalidGameInstallationException($"Invalid data for texture '{textureName}'. Not enough pixel data for complete texture ({ddTextureBuffer.Length:N0} / {minimumSize:N0}). Make sure your game files are not corrupted.");

		return new(width, height, br.ReadBytes(width * height * 4));
	}
}