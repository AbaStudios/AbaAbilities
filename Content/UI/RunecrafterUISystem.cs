using System.Collections.Generic;
using AbaAbilities.Content.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace AbaAbilities.Content.UI;

public class RunecrafterUISystem : ModSystem
{
    internal static RunecrafterDialogueUI DialogueUI;
    internal static UserInterface Interface;
    private static bool _visible;
    private static GameTime _lastGameTime;

    public override void Load() {
        if (Main.dedServ)
            return;

        DialogueUI = new RunecrafterDialogueUI();
        DialogueUI.Activate();

        Interface = new UserInterface();

        DialogueUI.OnCloseClicked += Close;
        DialogueUI.OnEnchantClicked += OnEnchant;
        DialogueUI.OnUnlockInfoClicked += OnUnlockInfo;
        DialogueUI.OnAskClicked += OnAskClicked;

        On_Player.SetTalkNPC += DetectRunecrafterTalk;
    }

    private static void DetectRunecrafterTalk(On_Player.orig_SetTalkNPC orig, Player self, int npc, bool fromNet) {
        orig(self, npc, fromNet);
        if (npc >= 0 && Main.npc.IndexInRange(npc) && Main.npc[npc].type == ModContent.NPCType<Runecrafter>()) {
            Open();
        }
    }

    public override void Unload() {
        DialogueUI = null;
        Interface = null;
    }

    public static void Open() {
        _visible = true;
        Interface?.SetState(DialogueUI);
        Main.playerInventory = false;
        Main.npcChatText = "";
        
        if (Main.npc.IndexInRange(Main.LocalPlayer.talkNPC)) {
            string dialogue = Main.npc[Main.LocalPlayer.talkNPC].GetChat();
            DialogueUI.SetDialogueText(dialogue);
        }
    }

    public static void Close() {
        _visible = false;
        Interface?.SetState(null);
        Main.LocalPlayer.SetTalkNPC(-1);
    }

    public static bool IsOpen => _visible;

    private static void OnEnchant() {
        Close();
    }

    private static void OnUnlockInfo() {
    }

    private static void OnAskClicked() {
    }

    public override void UpdateUI(GameTime gameTime) {
        _lastGameTime = gameTime;

        if (!_visible)
            return;

        Interface?.Update(gameTime);

        if (DialogueUI?.IsFullyClosed == true)
            Close();

        if (Main.LocalPlayer.controlInv) {
            DialogueUI?.RequestClose();
            Main.LocalPlayer.releaseInventory = false;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
        int mouseIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (mouseIndex != -1) {
            layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                "AbaAbilities: Runecrafter Dialogue",
                () => {
                    if (_visible && _lastGameTime != null)
                        Interface?.Draw(Main.spriteBatch, _lastGameTime);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
        // If our dialogue is open, disable drawing of the vanilla NPC/sign dialog layer to avoid conflicts
        int npcDialogIndex = layers.FindIndex(l => l.Name == "Vanilla: NPC / Sign Dialog");
        if (npcDialogIndex != -1)
        {
            layers[npcDialogIndex].Active = !IsOpen;
        }
    }
}
