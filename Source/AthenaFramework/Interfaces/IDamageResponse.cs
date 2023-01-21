﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AthenaFramework
{
    public interface IDamageResponse
    {
        public abstract void PreApplyDamage(ref DamageInfo dinfo, ref bool absorbed);

        public abstract void PostApplyDamage(ref DamageInfo dinfo, ref float totalDamageDealt);
    }
}
