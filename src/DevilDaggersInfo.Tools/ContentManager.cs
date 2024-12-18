using DevilDaggersInfo.Core.Asset;
using DevilDaggersInfo.Core.Mod;
using DevilDaggersInfo.Core.Mod.Extensions;
using DevilDaggersInfo.Core.Spawnset;
using DevilDaggersInfo.Tools.Engine.Content;
using DevilDaggersInfo.Tools.Engine.Content.Parsers.Sound;
using DevilDaggersInfo.Tools.User.Settings;
using System.Security.Cryptography;

namespace DevilDaggersInfo.Tools;

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

#if WINDOWS
		string ddExe = Path.Combine(UserSettings.Model.DevilDaggersInstallationDirectory, "dd.exe");
#elif LINUX
		string ddExe = Path.Combine(UserSettings.Model.DevilDaggersInstallationDirectory, "devildaggers");
#endif
		if (!File.Exists(ddExe))
			throw new InvalidGameInstallationException("Executable does not exist.");

		if (!File.Exists(UserSettings.DdSurvivalPath))
			throw new InvalidGameInstallationException("File 'dd/survival' does not exist.");

		if (!File.Exists(UserSettings.ResAudioPath))
			throw new InvalidGameInstallationException("File 'res/audio' does not exist.");

		if (!File.Exists(UserSettings.ResDdPath))
			throw new InvalidGameInstallationException("File 'res/dd' does not exist.");

		byte[] survivalBytes = File.ReadAllBytes(UserSettings.DdSurvivalPath);
		if (!SpawnsetBinary.TryParse(survivalBytes, out SpawnsetBinary? defaultSpawnset))
			throw new InvalidGameInstallationException("File 'dd/survival' could not be parsed.");

		byte[] survivalHash = MD5.HashData(survivalBytes);
		if (!SpawnsetBinary.V3Hash.SequenceEqual(survivalHash))
			throw new InvalidGameInstallationException("File 'dd/survival' is invalid. Make sure you have not modified the file. Validate your game files and try again.");

		if (Directory.Exists(UserSettings.ModsSurvivalPath))
			throw new InvalidGameInstallationException("There must not be a directory named 'survival' in the 'mods' directory. You must delete the directory, or mods will not work.");

		ModBinaryReadFilter ddReadFilter = ModBinaryReadFilter.Assets(
			new AssetKey(AssetType.Texture, "iconmaskcrosshair"),
			new AssetKey(AssetType.Texture, "iconmaskdagger"),
			new AssetKey(AssetType.Texture, "iconmaskgem"),
			new AssetKey(AssetType.Texture, "iconmaskhoming"),
			new AssetKey(AssetType.Texture, "iconmaskskull"),
			new AssetKey(AssetType.Texture, "iconmaskstopwatch"),
			new AssetKey(AssetType.Mesh, "dagger"),
			new AssetKey(AssetType.Texture, "daggersilver"),
			new AssetKey(AssetType.Mesh, "boid4"),
			new AssetKey(AssetType.Texture, "boid4"),
			new AssetKey(AssetType.Mesh, "boid4jaw"),
			new AssetKey(AssetType.Texture, "boid4jaw"),
			new AssetKey(AssetType.Mesh, "tile"),
			new AssetKey(AssetType.Texture, "tile"),
			new AssetKey(AssetType.Mesh, "pillar"),
			new AssetKey(AssetType.Texture, "pillar"),
			new AssetKey(AssetType.Texture, "post_lut"),
			new AssetKey(AssetType.Mesh, "hand4"),
			new AssetKey(AssetType.Texture, "hand6"));

		ModBinaryReadFilter audioReadFilter = ModBinaryReadFilter.Assets(
			new AssetKey(AssetType.Audio, "jump1"),
			new AssetKey(AssetType.Audio, "jump2"),
			new AssetKey(AssetType.Audio, "jump3"));

		ModBinary ddBinary;
		using (FileStream fs = new(UserSettings.ResDdPath, FileMode.Open, FileAccess.Read))
			ddBinary = new ModBinary(fs, ddReadFilter);

		ModBinary audioBinary;
		using (FileStream fs = new(UserSettings.ResAudioPath, FileMode.Open, FileAccess.Read))
			audioBinary = new ModBinary(fs, audioReadFilter);

		Content = new ContentContainer(
			DefaultSpawnset: defaultSpawnset,
			IconMaskCrosshairTexture: GetTexture(ddBinary, "iconmaskcrosshair", true),
			IconMaskDaggerTexture: GetTexture(ddBinary, "iconmaskdagger", true),
			IconMaskGemTexture: GetTexture(ddBinary, "iconmaskgem", true),
			IconMaskHomingTexture: GetTexture(ddBinary, "iconmaskhoming", true),
			IconMaskSkullTexture: GetTexture(ddBinary, "iconmaskskull", true),
			IconMaskStopwatchTexture: GetTexture(ddBinary, "iconmaskstopwatch", true),
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
		if (!ddBinary.AssetMap.TryGetValue(new AssetKey(AssetType.Mesh, meshName), out AssetData? meshData))
			throw new InvalidGameInstallationException($"Required mesh '{meshName}' from 'res/dd' was not found.");

		return ToEngineMesh(meshName, meshData.Buffer);
	}

	private static TextureContent GetTexture(ModBinary ddBinary, string textureName, bool flipVertically = false)
	{
		if (!ddBinary.AssetMap.TryGetValue(new AssetKey(AssetType.Texture, textureName), out AssetData? textureData))
			throw new InvalidGameInstallationException($"Required texture '{textureName}' from 'res/dd' was not found.");

		return ToEngineTexture(textureName, textureData.Buffer, flipVertically);
	}

	private static SoundContent GetSound(ModBinary audioBinary, string soundName)
	{
		if (!audioBinary.AssetMap.TryGetValue(new AssetKey(AssetType.Audio, soundName), out AssetData? audioData))
			throw new InvalidGameInstallationException($"Required audio '{soundName}' from 'res/audio' was not found.");

		try
		{
			SoundData waveData = WaveParser.Parse(audioData.Buffer);
			return new SoundContent(waveData.Channels, waveData.SampleRate, waveData.BitsPerSample, waveData.Data.Length, waveData.Data);
		}
		catch (WaveParseException ex)
		{
			Root.Log.Warning(ex, $"Could not parse .wav file {soundName}.");
			throw new InvalidGameInstallationException($"Required audio '{soundName}' from 'res/audio' was not in a valid format. Make sure your game files are not corrupted.");
		}
	}

	private static MeshContent ToEngineMesh(string meshName, byte[] ddMeshBuffer)
	{
		const int headerSize = 10;

		if (ddMeshBuffer.Length < headerSize)
			throw new InvalidGameInstallationException($"Invalid data for mesh '{meshName}'. Length was {ddMeshBuffer.Length} but should be at least {headerSize}. Make sure your game files are not corrupted.");

		using MemoryStream ms = new(ddMeshBuffer);
		using BinaryReader br = new(ms);

		int indexCount = br.ReadInt32();
		int vertexCount = br.ReadInt32();
		_ = br.ReadUInt16();

		const int vertexSize = sizeof(float) * 8;
		int minimumSize = indexCount * sizeof(uint) + vertexCount * vertexSize + headerSize;
		if (ddMeshBuffer.Length < minimumSize)
			throw new InvalidGameInstallationException($"Invalid data for mesh '{meshName}'. Not enough data for complete mesh ({ddMeshBuffer.Length:N0} / {minimumSize:N0}). Make sure your game files are not corrupted.");

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
			engineVertices[i] = new Vertex(ddVertex.Position, ddVertex.TexCoord, ddVertex.Normal);
		}

		return new MeshContent(engineVertices, indices);
	}

	private static TextureContent ToEngineTexture(string textureName, byte[] ddTextureBuffer, bool flipVertically)
	{
		const ushort expectedHeader = 16401;
		const int headerSize = 11;

		if (ddTextureBuffer.Length < headerSize)
			throw new InvalidGameInstallationException($"Invalid data for texture '{textureName}'. Length was {ddTextureBuffer.Length} but should be at least {headerSize}. Make sure your game files are not corrupted.");

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

		byte[] pixelData = new byte[width * height * 4];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				pixelData[(y * width + x) * 4 + 0] = br.ReadByte();
				pixelData[(y * width + x) * 4 + 1] = br.ReadByte();
				pixelData[(y * width + x) * 4 + 2] = br.ReadByte();
				pixelData[(y * width + x) * 4 + 3] = br.ReadByte();
			}
		}

		if (flipVertically)
		{
			for (int y = 0; y < height / 2; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte r = pixelData[(y * width + x) * 4 + 0];
					byte g = pixelData[(y * width + x) * 4 + 1];
					byte b = pixelData[(y * width + x) * 4 + 2];
					byte a = pixelData[(y * width + x) * 4 + 3];

					pixelData[(y * width + x) * 4 + 0] = pixelData[((height - y - 1) * width + x) * 4 + 0];
					pixelData[(y * width + x) * 4 + 1] = pixelData[((height - y - 1) * width + x) * 4 + 1];
					pixelData[(y * width + x) * 4 + 2] = pixelData[((height - y - 1) * width + x) * 4 + 2];
					pixelData[(y * width + x) * 4 + 3] = pixelData[((height - y - 1) * width + x) * 4 + 3];

					pixelData[((height - y - 1) * width + x) * 4 + 0] = r;
					pixelData[((height - y - 1) * width + x) * 4 + 1] = g;
					pixelData[((height - y - 1) * width + x) * 4 + 2] = b;
					pixelData[((height - y - 1) * width + x) * 4 + 3] = a;
				}
			}
		}

		return new TextureContent(width, height, pixelData);
	}
}
