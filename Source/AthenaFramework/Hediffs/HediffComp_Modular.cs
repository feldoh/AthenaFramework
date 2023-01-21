﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace AthenaFramework
{
    public class HediffComp_Modular : HediffComp, IStageOverride
    {
        private HediffCompProperties_Modular Props => props as HediffCompProperties_Modular;

        public HediffStage cachedInput;
        public HediffStage cachedOutput;
        public bool resetCache = false; //Set to true after changing modules to force a recache

        public ThingOwner<ThingWithComps> moduleHolder;

        public virtual bool CanInstall(CompUseEffect_HediffModule comp)
        {
            for (int i = moduleHolder.Count - 1; i >= 0; i--)
            {
                ThingWithComps module = moduleHolder[i];
                CompUseEffect_HediffModule moduleComp = module.TryGetComp<CompUseEffect_HediffModule>();

                if(moduleComp.GetData.moduleIDs.Intersect(comp.GetData.moduleIDs).Count() > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public virtual void InstallModule(ThingWithComps thing)
        {
            CompUseEffect_HediffModule comp = thing.TryGetComp<CompUseEffect_HediffModule>();

            thing.DeSpawnOrDeselect();
            moduleHolder.TryAdd(thing, false);
            resetCache = true;

            if (comp.GetComps == null)
            {
                return;
            }

            if (parent.comps == null)
            {
                parent.comps = new List<HediffComp>();
            }

            for (int i = comp.GetComps.Count - 1; i >= 0; i--)
            {
                HediffComp hediffComp = null;
                try
                {
                    hediffComp = (HediffComp)Activator.CreateInstance(comp.GetComps[i].compClass);
                    hediffComp.props = comp.GetComps[i];
                    hediffComp.parent = parent;
                    parent.comps.Add(hediffComp);
                    comp.linkedComps.Add(hediffComp);
                }
                catch (Exception ex)
                {
                    Log.Error("Modular HediffComp could not instantiate or initialize a HediffComp: " + ex);
                    parent.comps.Remove(hediffComp);
                }
            }
        }

        public virtual void RemoveModule(ThingWithComps thing)
        {
            CompUseEffect_HediffModule comp = thing.TryGetComp<CompUseEffect_HediffModule>();
            moduleHolder.TryDrop(thing, Pawn.Position, Pawn.Map, ThingPlaceMode.Near, 1, out Thing placedThing);

            for (int i = comp.linkedComps.Count - 1; i >= 0; i--)
            {
                parent.comps.Remove(comp.linkedComps[i]);
            }
        }

        public virtual HediffStage GetStage(HediffStage stage)
        {
            if (stage == cachedInput && !resetCache)
            {
                return cachedOutput;
            }

            cachedInput = stage;
            resetCache = false;

            if (moduleHolder.Count + Props.defaultGroups.Count == 0)
            {
                cachedOutput = stage;
                return cachedOutput;
            }

            List<ModularHediffGroup> activeModules = new List<ModularHediffGroup>(Props.defaultGroups);

            for (int i = moduleHolder.Count - 1; i >= 0; i--)
            {
                ThingWithComps module = moduleHolder[i];
                CompUseEffect_HediffModule comp = module.TryGetComp<CompUseEffect_HediffModule>();

                for (int j = activeModules.Count - 1; j >= 0; j--)
                {
                    ModularHediffGroup activeModule = activeModules[i];

                    if (activeModule.moduleIDs.Intersect(comp.GetData.moduleIDs).Count() > 0)
                    {
                        activeModules.Remove(activeModule);
                    }
                }

                activeModules.Add(comp.GetData);
            }

            cachedOutput = new HediffStage();
            cachedOutput.CopyValues(stage);

            for (int i = activeModules.Count - 1; i >= 0; i--)
            {
                cachedOutput = activeModules[i].modifiedStages[parent.CurStageIndex].ModifyHediffStage(cachedOutput);
            }

            return cachedOutput;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look(ref moduleHolder, "moduleHolder");

            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                return;
            }

            if (moduleHolder == null)
            {
                moduleHolder = new ThingOwner<ThingWithComps>();
                return;
            }

            for (int i = moduleHolder.Count - 1; i >= 0; i--)
            {
                ThingWithComps module = moduleHolder[i];
                CompUseEffect_HediffModule comp = module.TryGetComp<CompUseEffect_HediffModule>();

                for (int j = comp.GetComps.Count - 1; j >= 0; j--)
                {
                    HediffComp hediffComp = null;
                    try
                    {
                        hediffComp = (HediffComp)Activator.CreateInstance(comp.GetComps[j].compClass);
                        hediffComp.props = comp.GetComps[j];
                        hediffComp.parent = parent;
                        parent.comps.Add(hediffComp);
                        comp.linkedComps.Add(hediffComp);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Modular HediffComp could not instantiate or initialize a HediffComp: " + ex);
                        parent.comps.Remove(hediffComp);
                    }
                }
            }
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            moduleHolder = new ThingOwner<ThingWithComps>();
        }
    }

    public class HediffCompProperties_Modular : HediffCompProperties
    {
        public HediffCompProperties_Modular()
        {
            this.compClass = typeof(HediffComp_Modular);
        }

        // IDs of modules that can be inserted into this hediff. A module can be inserted if it has at least one matching ID
        public List<string> moduleIDs = new List<string>();

        // List of groups that are active when none of the inserted modules have overlapping IDs
        public List<ModularHediffGroup> defaultGroups = new List<ModularHediffGroup>();
    }

    public class ModularHediffGroup
    {
        // List of module IDs. Modules with overlapping IDs conflict and can't be installed together
        // Can be installed into any hediff that has at least one ID from this field
        public List<string> moduleIDs = new List<string>();

        // Stats that this group modifies
        public List<StageOverlay> modifiedStages = new List<StageOverlay>();
    }
}
