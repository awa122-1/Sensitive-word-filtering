using System;
using HarmonyLib;
using UnityEngine;

namespace AmongUsFilterMod
{
    public class RuntimeOutfit
    {
        public string Name;
        public byte ColorId;
        public bool IsConfigured = false;
    }

    public static class OutfitConfigStorage
    {
        // 使用原生数组代替 Dictionary，彻底避开 Il2Cppmscorlib.dll 的 List/Dictionary 泛型编译拦截
        public static readonly RuntimeOutfit[] Slots = new RuntimeOutfit[]
        {
            new RuntimeOutfit (), // 索引 0 空出，或者当 Slot 0
            new RuntimeOutfit { Name = "芙酱_awa_", ColorId = 13, IsConfigured = true }, // Slot 1
            new RuntimeOutfit (), // Slot 2
            new RuntimeOutfit ()  // Slot 3
        };

        public static void CaptureToSlot(int slot)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
            var player = PlayerControl.LocalPlayer;

            Slots[slot].Name = player.Data.PlayerName;
            
            try 
            {
                // 使用 dynamic 避开 ColorId 的编译期强类型绑定
                dynamic dynamicData = player.Data;
                Slots[slot].ColorId = (byte)dynamicData.ColorId;
            } 
            catch 
            {
                Slots[slot].ColorId = 0; 
            }
            
            Slots[slot].IsConfigured = true;
        }

        public static void ApplySlot(int slot)
        {
            if (PlayerControl.LocalPlayer == null || !Slots[slot].IsConfigured) return;
            var player = PlayerControl.LocalPlayer;
            var target = Slots[slot];

            if (!string.IsNullOrEmpty(target.Name)) player.RpcSetName(target.Name);
            
            try
            {
                player.RpcSetColor(target.ColorId);
            }
            catch {}
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class OutfitShortcutTriggerPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (PlayerControl.LocalPlayer == null) return;

            // 显式指定 UnityEngine.Input 和 UnityEngine.KeyCode，强行通过编译
            // F5/F6/F7 直接换装
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F5)) OutfitConfigStorage.ApplySlot(1);
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F6)) OutfitConfigStorage.ApplySlot(2);
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F7)) OutfitConfigStorage.ApplySlot(3);

            // Alt + F5/F6/F7 记住当前外观
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftAlt) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightAlt))
            {
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F5)) OutfitConfigStorage.CaptureToSlot(1);
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F6)) OutfitConfigStorage.CaptureToSlot(2);
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F7)) OutfitConfigStorage.CaptureToSlot(3);
            }
        }
    }
}