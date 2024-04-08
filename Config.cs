using System.ComponentModel;
using Terraria.ModLoader.Config;
// ReSharper disable UnassignedField.Global
// ReSharper disable once ClassNeverInstantiated.Global

namespace PetHappiness;

public class Config : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	[Slider]
	[Range(0.65f, 1f)]
	[DefaultValue(0.95f)]
	public float priceMultiplier;

	[Slider]
	[Range(10, 100)]
	[DefaultValue(25)]
	public int detectionRange;
}