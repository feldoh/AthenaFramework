﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection;
using AthenaFramework.Things;

namespace AthenaFramework
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony(id: "rimworld.smartkar.athenaframework.main");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(CompTurretGun), "TurretMat", MethodType.Getter)]
        public static class CompTurretGun_TurretMat_Fixer
        {
            static void Prefix(CompTurretGun __instance)
            {

                CompProperties_TurretGun props = __instance.props as CompProperties_TurretGun;

                if (__instance.turretMat != null)
                {
                    return;
                }

                if (!__instance.gun.def.HasModExtension<TurretGraphicOverride>())
                {
                    return;
                }

                __instance.turretMat = props.turretDef.graphicData.Graphic.MatSingle;
            }
        }

        [HarmonyPatch(typeof(CompTurretGun), "CanShoot", MethodType.Getter)]
        public static class CompTurretGun_CanShoot
        {
            static void Postfix(CompTurretGun __instance, ref bool __result)
            {
                if (!__instance.gun.def.HasModExtension<TurretRoofBlocked>())
                {
                    return;
                }

                Pawn pawn = __instance.parent as Pawn;

                if(pawn == null)
                {
                    return;
                }

                RoofDef roof = pawn.Position.GetRoof(pawn.MapHeld);

                if (roof != null)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(CompTurretGun), "PostDraw")]
        public static class CompTurretGun_PrePostDraw
        {
            static bool Prefix(CompTurretGun __instance)
            {
                if (!__instance.gun.def.HasModExtension<TurretGraphicOverride>())
                {
                    return true;
                }

                CompProperties_TurretGun props = __instance.props as CompProperties_TurretGun;

                if (__instance.turretMat == null)
                {
                    __instance.turretMat = props.turretDef.graphicData.Graphic.MatSingle;
                }

                Rot4 rotation = __instance.parent.Rotation;
                Vector3 vector = new Vector3(0f, 0.04054054f, 0f);
                Matrix4x4 matrix4x = default(Matrix4x4);
                Vector2 drawSize = props.turretDef.graphicData.drawSize;
                matrix4x.SetTRS(__instance.parent.DrawPos + vector, ((float)typeof(CompTurretGun).GetField("curRotation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)).ToQuat(), new Vector3(drawSize.x, 0, drawSize.y));
                Graphics.DrawMesh(MeshPool.plane10, matrix4x, __instance.turretMat, 0);
                return false;
            }
        }

        [HarmonyPatch(typeof(PawnGraphicSet), "ResolveAllGraphics")]
        public static class PawnGraphicSet_ResolveAllGraphics_Postfix
        {
            static void Postfix(PawnGraphicSet __instance)
            {
                if (!__instance.pawn.RaceProps.Humanlike)
                {
                    return;
                }

                bool graphicsSet = false;

                foreach (Comp_CustomApparelBody customBody in __instance.pawn.apparel.WornApparel.SelectMany((Apparel x) => x.AllComps).OfType<Comp_CustomApparelBody>())
                {
                    Graphic customBodyGraphic = customBody.getBodyGraphic;
                    if (customBodyGraphic != null)
                    {
                        __instance.nakedGraphic = customBodyGraphic;
                        graphicsSet = true;
                    }

                    Graphic customHeadGraphic = customBody.getHeadGraphic;
                    if (customHeadGraphic != null)
                    {
                        __instance.headGraphic = customHeadGraphic;
                        graphicsSet = true;
                    }
                }

                if (graphicsSet)
                {
                    __instance.CalculateHairMats();
                    __instance.ResolveApparelGraphics();
                    __instance.ResolveGeneGraphics();
                }
            }
        }

        // Damage patches

        [HarmonyPatch(typeof(DamageInfo), nameof(DamageInfo.Amount), MethodType.Getter)]
        public static class DamageInfo_AmountGetter
        {
            static void Postfix(DamageInfo __instance, ref float __result)
            {
                if (__instance.Instigator == null || !(__instance.Instigator is Pawn))
                {
                    return;
                }

                Pawn pawn = __instance.Instigator as Pawn;
                foreach (HediffComp_DamageAmplifier amplifier in pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().SelectMany((HediffWithComps x) => x.comps).OfType<HediffComp_DamageAmplifier>())
                {
                    __result *= amplifier.damageMultiplier;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
        public static class Pawn_PostPreApplyDamage
        {
            static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
            {
                foreach (HediffComp_Shield shield in __instance.health.hediffSet.hediffs.OfType<HediffWithComps>().SelectMany((HediffWithComps x) => x.comps).OfType<HediffComp_Shield>())
                {
                    shield.BlockDamage(ref dinfo, ref absorbed);

                    if (absorbed)
                    {
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StunHandler), "Notify_DamageApplied")]
        public static class ThingWithComps_PreApplyDamage
        {
            static bool Prefix(StunHandler __instance, ref DamageInfo dinfo)
            {
                if (!(__instance.parent is Pawn))
                {
                    return true;
                }

                Pawn pawn = __instance.parent as Pawn;

                foreach (HediffComp_Shield shield in pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().SelectMany((HediffWithComps x) => x.comps).OfType<HediffComp_Shield>())
                {
                    if (shield.BlockStun(ref dinfo))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Rendering patches

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.DrawAt))]
        public static class Pawn_PostDrawAt
        {
            static void Postfix(Pawn __instance, Vector3 drawLoc)
            {
                foreach (HediffComp_Renderable renderable in __instance.health.hediffSet.hediffs.OfType<HediffWithComps>().SelectMany((HediffWithComps x) => x.comps).OfType<HediffComp_Renderable>())
                {
                    renderable.DrawAt(drawLoc);
                }
            }
        }
    }
}
