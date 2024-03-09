using System.Runtime.InteropServices;

namespace DevilDaggersInfo.Tools.GameMemory.Enemies.Data;

#pragma warning disable SA1134
[StructLayout(LayoutKind.Explicit, Size = MemoryConstants.PedeSize)]
public record struct Pede
{
	[FieldOffset(6984)] public PedeSegments Segments;
}
#pragma warning restore SA1134