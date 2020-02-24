﻿using RimWorld;
using SimpleSidearms.utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleSidearms.rimworld
{
    [StaticConstructorOnStartup]
    public class GoldfishModule : IExposable
    {
        public enum PrimaryWeaponMode
        {
            Ranged,
            Melee, 
            BySkill,
            ByGenerated
        }

        private List<ThingStuffPairExposable> rememberedWeapons = new List<ThingStuffPairExposable>();
        internal List<ThingStuffPair> RememberedWeapons { get
            {
                if (rememberedWeapons == null)
                    generateRememberedWeaponsFromEquipped();
                List<ThingStuffPair> fakery = new List<ThingStuffPair>();
                foreach (var wep in rememberedWeapons)
                    fakery.Add(wep.Val);
                return fakery;
            } }

        private bool forcedUnarmedEx = false;
        private ThingStuffPairExposable? forcedWeaponEx = null;
        private bool forcedUnarmedWhileDraftedEx = false;
        private ThingStuffPairExposable? forcedWeaponWhileDraftedEx = null;

        private bool preferredUnarmedEx = false;
        private ThingStuffPairExposable? defaultRangedWeaponEx = null;
        private ThingStuffPairExposable? preferredMeleeWeaponEx = null;

        public bool ForcedUnarmed
        {
            get
            {
                return forcedUnarmedEx;
            }
            set
            {
                if (value == true)
                    ForcedWeapon = null;
                forcedUnarmedEx = value;
            }
        }
        public ThingStuffPair? ForcedWeapon
        {
            get
            {
                if (forcedWeaponEx == null)
                    return null;
                return forcedWeaponEx.Value.Val;
            }
            set
            {
                if (value == null)
                    forcedWeaponEx = null;
                else
                    forcedWeaponEx = new ThingStuffPairExposable(value.Value);
            }
        }

        public bool ForcedUnarmedWhileDrafted
        {
            get
            {
                return forcedUnarmedWhileDraftedEx;
            }
            set
            {
                if (value == true)
                    ForcedWeaponWhileDrafted = null;
                forcedUnarmedWhileDraftedEx = value;
            }
        }
        public ThingStuffPair? ForcedWeaponWhileDrafted
        {
            get
            {
                if (forcedWeaponWhileDraftedEx != null)
                    return forcedWeaponWhileDraftedEx.Value.Val;
                else
                    return null;
            }
            set
            {
                if (value == null)
                    forcedWeaponWhileDraftedEx = null;
                else
                    forcedWeaponWhileDraftedEx = new ThingStuffPairExposable(value.Value);
            }
        }


        public bool PreferredUnarmed
        {
            get
            {
                return preferredUnarmedEx;
            }
            private set
            {
                if (value == true)
                    PreferredMeleeWeapon = null;
                preferredUnarmedEx = value;
            }
        }
        public ThingStuffPair? DefaultRangedWeapon {
            get
            {
                if (defaultRangedWeaponEx == null)
                    return null;
                return defaultRangedWeaponEx.Value.Val;
            }
            private set
            {
                if (value == null)
                    defaultRangedWeaponEx = null;
                else
                    defaultRangedWeaponEx = new ThingStuffPairExposable(value.Value);
            }
        }
        public ThingStuffPair? PreferredMeleeWeapon
        {
            get
            {
                if (preferredMeleeWeaponEx == null)
                    return null;
                return preferredMeleeWeaponEx.Value.Val;
            }
            private set
            {
                if (value == null)
                    preferredMeleeWeaponEx = null;
                else
                    preferredMeleeWeaponEx = new ThingStuffPairExposable(value.Value);
            }
        }

        public bool IsCurrentWeaponForced(bool alsoCountPreferredOrDefault)
        {
            if (Owner == null || Owner.Dead || Owner.equipment == null)
                return false;
            ThingStuffPair? currentWeaponN = Owner.equipment.Primary?.toThingStuffPair();
            if (currentWeaponN == null)
            {
                if (Owner.Drafted && ForcedUnarmedWhileDrafted)
                    return true;
                else if (ForcedUnarmed)
                    return true;
                else if (alsoCountPreferredOrDefault && PreferredUnarmed)
                    return true;
                else
                    return false;
            }
            else
            {
                ThingStuffPair currentWeapon = currentWeaponN.Value;
                if (Owner.Drafted && ForcedWeaponWhileDrafted == currentWeapon)
                    return true;
                else if (ForcedWeapon == currentWeapon)
                    return true;
                else if (alsoCountPreferredOrDefault)
                {
                    if (currentWeapon.thing.IsMeleeWeapon && PreferredMeleeWeapon == currentWeapon)
                        return true;
                    else if (currentWeapon.thing.IsRangedWeapon && DefaultRangedWeapon == currentWeapon)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public PrimaryWeaponMode primaryWeaponMode = PrimaryWeaponMode.BySkill;

        private Pawn _owner;
        public Pawn Owner { get { return _owner; } set { _owner = value; } }

        public GoldfishModule() : this(null, false) { }

        public GoldfishModule(Pawn owner) : this(owner, false) { }

        public GoldfishModule(Pawn owner, bool fillExisting)
        {
            this.rememberedWeapons = new List<ThingStuffPairExposable>();
            this.Owner = owner;
            if (fillExisting)
            {
                generateRememberedWeaponsFromEquipped();
            }
            if (owner != null) //null owner should only come up when loading from savegames
            {
                if (owner.IsColonist)
                    primaryWeaponMode = SimpleSidearms.ColonistDefaultWeaponMode.Value;
                else
                    primaryWeaponMode = SimpleSidearms.NPCDefaultWeaponMode.Value;

                if (primaryWeaponMode == PrimaryWeaponMode.ByGenerated)
                {
                    if (Owner == null || Owner.equipment == null || owner.equipment.Primary == null)
                        primaryWeaponMode = PrimaryWeaponMode.BySkill;
                    else if (owner.equipment.Primary.def.IsRangedWeapon)
                        primaryWeaponMode = PrimaryWeaponMode.Ranged;
                    else if (owner.equipment.Primary.def.IsMeleeWeapon)
                        primaryWeaponMode = PrimaryWeaponMode.Melee;
                    else
                        primaryWeaponMode = PrimaryWeaponMode.BySkill;
                }
            }
        }
                          
        private void generateRememberedWeaponsFromEquipped()
        {
            this.rememberedWeapons = new List<ThingStuffPairExposable>();
            IEnumerable<ThingWithComps> carriedWeapons = Owner.getCarriedWeapons();
            foreach (ThingWithComps weapon in carriedWeapons)
            {
                ThingStuffPairExposable pair = new ThingStuffPairExposable(new ThingStuffPair(weapon.def, weapon.Stuff));
                rememberedWeapons.Add(pair);
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref _owner, "owner");

            Scribe_Collections.Look<ThingStuffPairExposable>(ref rememberedWeapons, "rememberedWeapons", LookMode.Deep);

            Scribe_Deep.Look<ThingStuffPairExposable?>(ref forcedWeaponEx, "forcedWeapon");
            Scribe_Values.Look<bool>(ref forcedUnarmedEx, "forcedUnarmed");
            Scribe_Deep.Look<ThingStuffPairExposable?>(ref forcedWeaponWhileDraftedEx, "forcedWeaponWhileDrafted");
            Scribe_Values.Look<bool>(ref forcedUnarmedWhileDraftedEx, "forcedUnarmedWhileDrafted");

            Scribe_Values.Look<bool>(ref preferredUnarmedEx, "preferredUnarmed");
            Scribe_Deep.Look<ThingStuffPairExposable?>(ref defaultRangedWeaponEx, "prefferedRangedWeapon");
            Scribe_Deep.Look<ThingStuffPairExposable?>(ref preferredMeleeWeaponEx, "prefferedMeleeWeapon");
            Scribe_Values.Look<PrimaryWeaponMode>(ref primaryWeaponMode, "primaryWeaponMode");
        }

        public static GoldfishModule GetGoldfishForPawn(Pawn pawn, bool fillExistingIfCreating = false)
        {
            if (pawn == null)
                return null;
            if (SimpleSidearms.CEOverride)
                return null;
            if (SimpleSidearms.saveData == null)
                return null;
            var pawnId = pawn.thingIDNumber;
            GoldfishModule memory;
            if (!SimpleSidearms.saveData.memories.TryGetValue(pawnId, out memory))
            {
                memory = new GoldfishModule(pawn, fillExistingIfCreating);
                SimpleSidearms.saveData.memories.Add(pawnId, memory);
            }
            else
            {
                memory.NullChecks(pawn);
            }
            return memory;
        }

        internal void SetUnarmedAsForced(bool drafted)
        {
            if (drafted)
            {
                ForcedUnarmedWhileDrafted = true;
                ForcedWeaponWhileDrafted = null;
            }
            else
            {
                ForcedUnarmed = true;
                ForcedWeapon = null;
            }
        }

        internal void SetWeaponAsForced(ThingStuffPair weapon, bool drafted)
        {
            if (drafted)
            {
                ForcedUnarmedWhileDrafted = false;
                ForcedWeaponWhileDrafted = weapon;
            }
            else
            {
                ForcedUnarmed = false;
                ForcedWeapon = weapon;
            }
        }


        internal void UnsetUnarmedAsForced(bool drafted)
        {
            if (drafted)
            {
                ForcedUnarmedWhileDrafted = false;
                ForcedWeaponWhileDrafted = null;
            }
            else
            {
                ForcedUnarmed = false;
                ForcedWeapon = null;
            }
        }

        internal void UnsetForcedWeapon(bool drafted)
        {
            if(drafted)
            {
                ForcedUnarmedWhileDrafted = false;
                ForcedWeaponWhileDrafted = null;
            }
            else
            {
                ForcedUnarmed = false;
                ForcedWeapon = null;
            }
        }

        internal void SetRangedWeaponTypeAsDefault(ThingStuffPair rangedWeapon)
        {
            this.DefaultRangedWeapon = rangedWeapon;
            if (this.ForcedWeapon != null && this.ForcedWeapon != rangedWeapon && this.ForcedWeapon.Value.thing.IsRangedWeapon)
                UnsetForcedWeapon(false);
        }
        internal void SetMeleeWeaponTypeAsPreferred(ThingStuffPair meleeWeapon)
        {
            this.preferredUnarmedEx = false;
            this.PreferredMeleeWeapon = meleeWeapon;
            if (this.ForcedWeapon != null && this.ForcedWeapon != meleeWeapon && this.ForcedWeapon.Value.thing.IsMeleeWeapon)
                UnsetForcedWeapon(false);
            if (ForcedUnarmed)
                UnsetUnarmedAsForced(false);
        }
        internal void SetUnarmedAsPreferredMelee()
        {
            PreferredUnarmed = true;
            PreferredMeleeWeapon = null;
            if (this.ForcedWeapon != null && this.ForcedWeapon.Value.thing.IsMeleeWeapon)
                UnsetForcedWeapon(false);
        }

        internal void UnsetRangedWeaponDefault()
        {
            DefaultRangedWeapon = null;
        }
        internal void UnsetMeleeWeaponPreference()
        {
            PreferredMeleeWeapon = null;
            PreferredUnarmed = false;
        }

        /*
        internal void UnsetWeaponAsPreferred(Thing weapon)
        {
            this.UnsetWeaponAsPreferred(weapon.toThingStuffPair());
        }
        internal void UnsetWeaponAsPreferred(ThingStuffPair weapon)
        {
            if (weapon.thing.IsRangedWeapon && this.DefaultRangedWeapon == weapon)
                this.DefaultRangedWeapon = null;
            else if (weapon.thing.IsMeleeWeapon && this.PreferredMeleeWeapon == weapon)
                this.PreferredMeleeWeapon = null;
        }
        internal void SetWeaponAsPreferred(Thing weapon)
        {
            this.SetWeaponAsPreferred(weapon.toThingStuffPair());
        }
        internal void SetWeaponAsPreferred(ThingStuffPair weapon)
        {
            if (weapon.thing.IsRangedWeapon)
                this.DefaultRangedWeapon = weapon;
            else if (weapon.thing.IsMeleeWeapon)
            {
                this.preferredUnarmedEx = false;
                this.PreferredMeleeWeapon = weapon;
            }
        }*/

        internal void InformOfUndraft()
        {
            ForcedWeaponWhileDrafted = null;
            ForcedUnarmedWhileDrafted = false;
        }

        internal void InformOfAddedPrimary(Thing weapon)
        {
            InformOfAddedSidearm(weapon);
            if (weapon.def.IsRangedWeapon)
                SetRangedWeaponTypeAsDefault(weapon.toThingStuffPair());
            else
                SetMeleeWeaponTypeAsPreferred(weapon.toThingStuffPair());
        }
        internal void InformOfAddedSidearm(Thing weapon)
        {
            rememberedWeapons.Add(weapon.toThingStuffPair().toExposable());
        }

        internal void InformOfDroppedSidearm(Thing weapon, bool intentional)
        {
            if (intentional)
                ForgetSidearmMemory(weapon.toThingStuffPair());
        }

        internal void ForgetSidearmMemory(ThingStuffPair weaponMemory)
        {
            if (rememberedWeapons.Contains(weaponMemory.toExposable()))
                rememberedWeapons.Remove(weaponMemory.toExposable());

            if (!rememberedWeapons.Contains(weaponMemory.toExposable())) //only remove if this was the last instance
            {
                if (weaponMemory == PreferredMeleeWeapon)
                    PreferredMeleeWeapon = null;
                if (weaponMemory == DefaultRangedWeapon)
                    PreferredMeleeWeapon = null;
            }
        }



        private bool nullchecked = false;
        private void NullChecks(Pawn owner)
        {
            if (nullchecked)
                return;
            if(Owner == null)
            {
                Log.Warning("goldfish module didnt know what pawn it belongs to!");
                this.Owner = owner;
            }
            if(rememberedWeapons == null)
            {
                Log.Warning("Remembered weapons list of " + this.Owner.LabelCap + " was missing, regenerating...");
                generateRememberedWeaponsFromEquipped();
            }
            for (int i = rememberedWeapons.Count() - 1; i >= 0; i--)
            {
                try
                {
                    var pair = rememberedWeapons[i];
                    var disposed = pair.Val;
                }
                catch (Exception ex)
                {
                    Log.Warning("A memorised weapon of " + this.Owner.LabelCap + " had a missing def or malformed data, removing...");
                    rememberedWeapons.RemoveAt(i);
                }
            }
            if (PreferredMeleeWeapon != null)
            {
                try
                {
                    var disposed = PreferredMeleeWeapon.Value;
                }
                catch (Exception ex)
                {
                    Log.Warning("Melee weapon preference of " + this.Owner.LabelCap + " had a missing def or malformed data, removing...");
                    PreferredMeleeWeapon = null;
                }
            }
            if (DefaultRangedWeapon != null)
            {
                try
                {
                    var disposed = DefaultRangedWeapon.Value;
                }
                catch (Exception ex)
                {
                    Log.Warning("Ranged weapon preference of " + this.Owner.LabelCap + " had a missing def or malformed data, removing...");
                    PreferredMeleeWeapon = null;
                }
            }
            if (ForcedWeapon != null)
            {
                try
                {
                    var disposed = ForcedWeapon.Value;
                }
                catch (Exception ex)
                {
                    Log.Warning("Forced weapon of " + this.Owner.LabelCap + " had a missing def or malformed data, removing...");
                    PreferredMeleeWeapon = null;
                }
            }
            if (ForcedWeaponWhileDrafted != null)
            {
                try
                {
                    var disposed = ForcedWeaponWhileDrafted.Value;
                }
                catch (Exception ex)
                {
                    Log.Warning("Forced drafted weapon of " + this.Owner.LabelCap + " had a missing def or malformed data, removing...");
                    PreferredMeleeWeapon = null;
                }
            }
            nullchecked = true;
        }
    }
}
