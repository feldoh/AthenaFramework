﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AthenaFramework
{
    public interface IStageOverride
    {
        public abstract HediffStage GetStage(HediffStage stage);
    }
}
