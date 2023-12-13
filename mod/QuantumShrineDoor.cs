﻿using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class QuantumShrineDoor
{
    public static bool hasQuantumShrineCodes = false;

    private static InteractReceiver doorIR = null;
    private static NomaiGateway gatewayComponent = null;

    public static void SetHasQuantumShrineCodes(bool hasQuantumShrineCodes)
    {
        if (QuantumShrineDoor.hasQuantumShrineCodes != hasQuantumShrineCodes)
        {
            QuantumShrineDoor.hasQuantumShrineCodes = hasQuantumShrineCodes;            
            UpdateIRState();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuantumShrine), nameof(QuantumShrine.Start))]
    public static void QuantumShrine_Start_Prefix(QuantumShrine __instance)
    {
        Randomizer.Instance.ModHelper.Console.WriteLine($"QuantumShrine.Start deleting door orb and adding door prompt");

        var qs = Locator.GetQuantumMoon().transform.Find("Sector_QuantumMoon/QuantumShrine").GetComponent<QuantumShrine>();

        // deactivate the orb you'd use to open the door in the vanilla game
        var orbs = qs._childOrbs;
        var doorOrbPair = orbs[0]; // orbs[1] is the light switch inside, leave that active
        doorOrbPair.gameObject.SetActive(false);

        // the orb's light sources also have to be detached from the shrine component,
        // or else they'll interfere with quantum entanglement despite being invisible
        qs._lamps = qs._lamps.Where(l => l != doorOrbPair._glowLight && l != doorOrbPair._extraGlowLight).ToArray();

        // set up our "[X] Open Quantum Shrine" prompt by attaching
        // an InteractReceiver to a new child object of the gateway
        var shrineGatewayTransform = __instance.transform.Find("Prefab_NOM_Gateway");
        GameObject shrineDoorInteract = new GameObject("APRandomizer_ShrineDoorInteract");
        shrineDoorInteract.transform.SetParent(shrineGatewayTransform, false);
        var box = shrineDoorInteract.AddComponent<BoxCollider>();
        box.isTrigger = true; // We just want to detect the player, not make an invisible wall
        box.size = new Vector3(7, 10, 6);

        // store these references for UpdateIRState and OnPressInteract to use later
        doorIR = shrineDoorInteract.AddComponent<InteractReceiver>();
        gatewayComponent = shrineGatewayTransform.gameObject.GetComponent<NomaiGateway>();

        UpdateIRState();

        doorIR.OnPressInteract += () =>
        {
            if (!hasQuantumShrineCodes) return;
            if (gatewayComponent is null)
            {
                Randomizer.Instance.ModHelper.Console.WriteLine($"APRandomizer_ShrineDoorInteract OnPressInteract failed to locate NomaiGateway component on shrineGatewayTransform", OWML.Common.MessageType.Error);
                return;
            }

            Randomizer.Instance.ModHelper.Console.WriteLine($"APRandomizer_ShrineDoorInteract OnPressInteract {(gatewayComponent._open ? "closing" : "opening")} gatewayComponent");
            // Open/CloseGate()'s implementation never uses its slot argument,
            // but _openSlot/_closeSlot are what it would normally be set to.
            if (gatewayComponent._open)
                gatewayComponent.CloseGate(gatewayComponent._closeSlot);
            else
                gatewayComponent.OpenGate(gatewayComponent._openSlot);

            UpdateIRState();
        };
    }

    private static void UpdateIRState()
    {
        if (doorIR is null || gatewayComponent is null) return;

        if (hasQuantumShrineCodes)
        {
            doorIR.ChangePrompt(gatewayComponent._open ? "Close Quantum Shrine" : "Open Quantum Shrine");
            doorIR.SetKeyCommandVisible(true);
        }
        else
        {
            doorIR.ChangePrompt("Requires Quantum Shrine Codes");
            doorIR.SetKeyCommandVisible(false);
        }
    }

    // This was very helpful for debugging how this door code interfered with the entanglement code
    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.OnFlashlightOff))]
    public static void PlayerState_OnFlashlightOff_Prefix()
    {
        Randomizer.Instance.ModHelper.Console.WriteLine($"PlayerState.OnFlashlightOff");

        var qs = Locator.GetQuantumMoon().transform.Find("Sector_QuantumMoon/QuantumShrine").GetComponent<QuantumShrine>();

        Randomizer.Instance.ModHelper.Console.WriteLine($"lamps {qs._lamps.Length}");
        for (int i = 0; i < qs._lamps.Length; i++)
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"lamp {i} / {qs._lamps[i].name} / {qs._lamps[i].intensity}");
        }
        Randomizer.Instance.ModHelper.Console.WriteLine($"_isPlayerInside {qs._isPlayerInside}");
        Randomizer.Instance.ModHelper.Console.WriteLine($"_fadeFraction {qs._fadeFraction}");
        Randomizer.Instance.ModHelper.Console.WriteLine($"_isProbeInside {qs._isProbeInside}");
        Randomizer.Instance.ModHelper.Console.WriteLine($"PlayerState.IsFlashlightOn() {PlayerState.IsFlashlightOn()}");
        Randomizer.Instance.ModHelper.Console.WriteLine($"thruster light {Locator.GetThrusterLightTracker().GetLightRange()}");
    }*/
}