﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure;

namespace AthenaFramework
{
    public class BeamExtension : DefModExtension
    {
        public float maxRange;
        public int textureChangeDelay;
        public int sizeTextureAmount = 1;
        public int textureFrameAmount = 1;
    }

    public struct BeamInfo : IExposable
    {
        public Thing beamStart;
        public Thing beamEnd;
        public BeamRenderer beam;

        public BeamInfo(Thing beamStart, Thing beamEnd, BeamRenderer beam)
        {
            this.beamStart = beamStart;
            this.beamEnd = beamEnd;
            this.beam = beam;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref beamStart, "beamStart");
            Scribe_References.Look(ref beamEnd, "beamEnd");
            Scribe_References.Look(ref beam, "beam");
        }
    }

    public struct StaticBeamInfo : IExposable
    {
        public Vector3 beamStart;
        public Vector3 beamEnd;
        public BeamRenderer beam;

        public StaticBeamInfo(Vector3 beamStart, Vector3 beamEnd, BeamRenderer beam)
        {
            this.beamStart = beamStart;
            this.beamEnd = beamEnd;
            this.beam = beam;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref beamStart, "beamStart");
            Scribe_Values.Look(ref beamEnd, "beamEnd");
            Scribe_References.Look(ref beam, "beam");
        }
    }
}