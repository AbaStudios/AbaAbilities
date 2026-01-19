using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AbaAbilities.Content.NPCs;

[AutoloadHead]
public class Runecrafter : ModNPC
{
    public override string Texture => "AbaAbilities/Assets/Textures/NPC/Runecrafter_Default";

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Wizard];
        NPCID.Sets.ActsLikeTownNPC[Type] = true;
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Wizard);
        NPC.townNPC = true;
        NPC.friendly = true;
        AIType = NPCID.Wizard;
        AnimationType = NPCID.Wizard;
    }

    public override bool CanTownNPCSpawn(int numTownNPCs) => true;

    public override string GetChat()
    {
        return "Hello! I'm the Runecrafter. I can help you with abilities.";
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = "Shop";
        button2 = "Close";
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
    }
}