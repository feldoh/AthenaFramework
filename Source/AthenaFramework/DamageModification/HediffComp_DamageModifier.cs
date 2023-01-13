﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace AthenaFramework
{
    public class HediffComp_DamageModifier : HediffComp
    {
        private HediffCompProperties_DamageModifier Props => props as HediffCompProperties_DamageModifier;

        public virtual float OutgoingDamageMultiplier
        {
            get
            {
                return Props.outgoingDamageMultiplier;
            }
        }

        public virtual (float, float) GetOutcomingDamageModifier(Thing target, ref List<string> excludedGlobal, Thing instigator, DamageInfo? dinfo, bool projectile = false)
        {
            float modifier = 1f;
            float offset = 0f;
            List<string> excluded = new List<string>();

            foreach (DamageModificator modGroup in Props.outgoingModifiers)
            {
                (float, float) result = modGroup.GetDamageModifiers(target, ref excluded, ref excludedGlobal, instigator, dinfo, projectile);
                modifier *= result.Item1;
                offset += result.Item2;
            }

            return (modifier, offset);
        }

        public virtual (float, float) GetIncomingDamageModifier(Thing target, ref List<string> excludedGlobal, Thing instigator, DamageInfo? dinfo, bool projectile = false)
        {
            float modifier = Props.incomingDamageMultiplier;
            float offset = 0f;
            List<string> excluded = new List<string>();

            foreach (DamageModificator modGroup in Props.incomingModifiers)
            {
                (float, float) result = modGroup.GetDamageModifiers(instigator, ref excluded, ref excludedGlobal, target, dinfo, projectile, true);
                modifier *= result.Item1;
                offset += result.Item2;
            }

            return (modifier, offset);
        }
    }

    public class HediffCompProperties_DamageModifier : HediffCompProperties
    {
        public HediffCompProperties_DamageModifier()
        {
            this.compClass = typeof(HediffComp_DamageModifier);
        }

        // List of possible modification effects that affect outgoing damage
        public List<DamageModificator> outgoingModifiers = new List<DamageModificator>();
        // List of possible modification effects that affect outgoing damage
        public List<DamageModificator> incomingModifiers = new List<DamageModificator>();
        // Passive outgoing damage modifier that's always applied
        public float outgoingDamageMultiplier = 1f;
        // Passive incoming damage modifier that's always applied
        public float incomingDamageMultiplier = 1f;
    }
}