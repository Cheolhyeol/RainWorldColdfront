using RWCustom;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static cheolsVesselSlugcat.vesselCWT;

namespace cheolsVesselSlugcat
{
    public static class vesselCWT
    {
        public class vesselCat
        {
            public int craftingCounter;
            public bool isCrafting;

            public vesselCat()
            {
                // not necessary to put anything in here. this is for initializing variables
                // that should NOT start as default values
            }
        }

        private static readonly ConditionalWeakTable<Player, vesselCat> vessel = new();
        public static vesselCat GetSeolVessel(this Player player) => vessel.GetValue(player, _ => new());
    }
}
