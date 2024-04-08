using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace PetHappiness;

public class PetHappiness : Mod
{
	public override void Load()
	{
		IL_ShopHelper.ProcessMood += AddPetHappinessModifier;
		base.Load();
	}

	private static void AddPetHappinessModifier(ILContext il)
	{
		var c = new ILCursor(il);

		/*
			C#:
				if (flag && npcsWithinHouse <= 2 && npcsWithinVillage < 4) {
					AddHappinessReportText("LoveSpace");
					this._currentPriceAdjustment *= 0.95f;
				}
				
				<-- here
			[+]	if (GetNearbyPets(npc) != 0) {
			[+]		_currentPriceAdjustment *= 0.95f;
			[+]	}
		 
		 
			IL:
				IL_014b: ldarg.0
				IL_014c: ldstr "LoveSpace"
				IL_0151: ldnull
				IL_0152: ldc.i4.0
				IL_0153: call instance void Terraria.GameContent.ShopHelper::AddHappinessReportText(string, object, int32)
				IL_0158: ldarg.0
				IL_0159: ldarg.0
				IL_015a: ldfld float32 Terraria.GameContent.ShopHelper::_currentPriceAdjustment
				IL_015f: ldc.r4 0.95
				IL_0164: mul
				IL_0165: stfld float32 Terraria.GameContent.ShopHelper::_currentPriceAdjustment
				
				<-- here
				
			[+]			  ldarg.2
			[+]			  call int PetHappiness::GetNearbyPets(NPC)
			[+]			  ldc.i4.0
			[+]			  beq LABEL

			[+]			  ldarg.0
			[+]			  ldarg.0
			[+]			  ldfld float32 Terraria.GameContent.ShopHelper::_currentPriceAdjustment
			[+]			  call float PetHappiness::GetPriceMultiplier()
			[+]			  mul
			[+]			  stfld float32 Terraria.GameContent.ShopHelper::_currentPriceAdjustment

			[+]	IL_LABEL:
		*/

		FieldReference currentPriceAdjustment = null;
		if (!c.TryGotoNext(MoveType.After, 
			    i => i.MatchLdarg0(),
			    i => i.MatchLdstr("LoveSpace"),
			    i => i.MatchLdnull(),
			    i => i.MatchLdcI4(0),
			    i => i.MatchCall<ShopHelper>("AddHappinessReportText"),
			    i => i.MatchLdarg0(),
			    i => i.MatchLdarg0(),
			    i => i.MatchLdfld(out currentPriceAdjustment),
			    i => i.MatchLdcR4(0.95f),
			    i => i.MatchMul(),
			    i => i.MatchStfld<ShopHelper>("_currentPriceAdjustment")
		)) {
			throw new Exception("[PetHappiness] AddPetHappinessModifier IL-Edit failed. Please contact NotLe0n.");
		}

		c.Index++;
		
		var label = c.DefineLabel();
		// if (GetNearbyPets(npc) != 0) {
		c.EmitLdarg2();
		c.EmitCall(typeof(PetHappiness).GetMethod("GetNearbyPets", BindingFlags.NonPublic | BindingFlags.Static)!);
		c.EmitLdcI4(0);
		c.EmitBeq(label);

		// this._currentPriceAdjustment = this.currentPriceAdjustment * GetPriceMultiplier();
		c.EmitLdarg0();
		c.EmitLdarg0();
		c.EmitLdfld(currentPriceAdjustment);
		c.EmitCall(typeof(PetHappiness).GetMethod("GetPriceMultiplier", BindingFlags.NonPublic | BindingFlags.Static)!);
		c.EmitMul();
		c.EmitStfld(currentPriceAdjustment);

		// }
		c.MarkLabel(label);
	}

	// ReSharper disable UnusedMember.Local
	private static int GetNearbyPets(NPC npc)
	{
		int numPets = 0;
		var npcHomePos = new Vector2(npc.homeTileX, npc.homeTileY);
		if (npc.homeless) {
			npcHomePos = npc.Center / 16f;
		}

		for (int i = 0; i < 200; i++) {
			if (i == npc.whoAmI) {
				continue;
			}

			NPC pet = Main.npc[i];
			if (!pet.active || !NPCID.Sets.IsTownPet[pet.type]) {
				continue;
			}

			var petHomePos = new Vector2(pet.homeTileX, pet.homeTileY);
			if (pet.homeless) {
				petHomePos = pet.Center / 16f;
			}
				
			if (Vector2.Distance(npcHomePos, petHomePos) <= ModContent.GetInstance<Config>().detectionRange) {
				numPets++;
			}
		}

		return numPets;
	}

	private static float GetPriceMultiplier()
	{
		return ModContent.GetInstance<Config>().priceMultiplier;
	}
}