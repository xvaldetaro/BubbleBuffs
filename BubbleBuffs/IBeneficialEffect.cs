using BubbleBuffs.Extensions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Enums;
using Kingmaker.Blueprints;

namespace BubbleBuffs {

    public class UnitBuffData {
        public readonly UnitEntityData Unit;
        public readonly HashSet<Guid> Buffs;

        public UnitBuffData(UnitEntityData u) {
            Unit = u;
            Buffs = new(u.Buffs.RawFacts.Select(b => b.BGuid()));
        }
    }

    public class AbilityCombinedEffects {

        private HashSet<Guid> AppliedBuffs;
        private HashSet<Guid> AppliedPetBuffs;
        private HashSet<Guid> PrimaryWeaponEnchants;
        private HashSet<Guid> SecondaryWeaponEnchants;
        public PetType? PetType = null;
        public DurationRate? ShortestDurationRate { get; private set; }

        public IEnumerable<(string name, Guid guid)> All {
            get {
                IEnumerable<Guid> some = Enumerable.Empty<Guid>();
                if (AppliedBuffs != null)
                     some = some.Concat(AppliedBuffs);
                if (AppliedPetBuffs != null)
                     some = some.Concat(AppliedPetBuffs);
                if (PrimaryWeaponEnchants != null)
                     some = some.Concat(PrimaryWeaponEnchants);
                if (SecondaryWeaponEnchants != null)
                     some = some.Concat(SecondaryWeaponEnchants);

                return some.Select(x => (Resources.GetBlueprint<SimpleBlueprint>(new BlueprintGuid(x)).name, x));
            }
        }

        private void Add(ref HashSet<Guid> set, Guid fact) {
            if (set == null)
                set = new();
            set.Add(fact);
        }

        private void UpdateShortestDuration(DurationRate? rate) {
            if (rate == null) return;
            if (ShortestDurationRate == null || rate.Value < ShortestDurationRate.Value)
                ShortestDurationRate = rate;
        }

        public void AddPetBuff(Guid buff, PetType type, bool isLong, DurationRate? durationRate = null) {
            if (PetType != null && type != PetType) {
                Main.Error("Could not add pet buff with different pet type");
                return;
            }

            Add(ref AppliedPetBuffs, buff);
            PetType = type;
            IsLong |= isLong;
            UpdateShortestDuration(durationRate);
        }

        public void AddBuff(Guid buff, bool isLong, DurationRate? durationRate = null) {
            Add(ref AppliedBuffs, buff);
            IsLong |= isLong;
            UpdateShortestDuration(durationRate);
        }
        public void AddPrimaryWeaponEnchant(Guid buff, bool isLong, DurationRate? durationRate = null) {
            Add(ref PrimaryWeaponEnchants, buff);
            IsLong |= isLong;
            UpdateShortestDuration(durationRate);
        }
        public void AddSecondaryWeaponEnchnant(Guid buff, bool isLong, DurationRate? durationRate = null) {
            Add(ref SecondaryWeaponEnchants, buff);
            IsLong |= isLong;
            UpdateShortestDuration(durationRate);
        }

        public AbilityCombinedEffects(IEnumerable<IBeneficialEffect> beneficialEffects) {
            foreach (var effect in beneficialEffects.EmptyIfNull())
                effect.AppendTo(this);
            Empty = AppliedBuffs == null && AppliedPetBuffs == null && PrimaryWeaponEnchants == null && SecondaryWeaponEnchants == null;
        }


        internal bool IsPresent(UnitBuffData unitBuffData, HashSet<Guid> ignoreForOverwriteCheck) {
            if (AppliedPetBuffs != null) {
                var pet = unitBuffData.Unit.GetPet(PetType.Value);
                var existingBuffs = new HashSet<Guid>(pet.Buffs.RawFacts.Select(b => b.BGuid()));
                if (existingBuffs.Overlaps(AppliedPetBuffs.Except(ignoreForOverwriteCheck)))
                    return true;
            }

            if (AppliedBuffs != null) {
                if (unitBuffData.Buffs.Overlaps(AppliedBuffs.Except(ignoreForOverwriteCheck)))
                    return true;
            }

            if (PrimaryWeaponEnchants != null) {
                var important = PrimaryWeaponEnchants.Except(ignoreForOverwriteCheck).ToHashSet();
                foreach (var enchant in unitBuffData.Unit.Body.PrimaryHand.MaybeWeapon.Enchantments) {
                    if (important.Contains(enchant.BGuid()))
                        return true;
                }
            }
            if (SecondaryWeaponEnchants != null) {
                var important = SecondaryWeaponEnchants.Except(ignoreForOverwriteCheck).ToHashSet();
                foreach (var enchant in unitBuffData.Unit.Body.SecondaryHand.MaybeWeapon.Enchantments) {
                    if (important.Contains(enchant.BGuid()))
                        return true;
                }
            }

            return false;
        }

        public readonly bool Empty = true;
        public bool IsLong { get; private set; }
    }

    public interface IBeneficialEffect {
        public void AppendTo(AbilityCombinedEffects effect);
        public PetType? PetType { get; set; }
        public DurationRate? DurationRate { get; }
    }
    public class AreaBuffEffect : IBeneficialEffect {

        public readonly Guid Applied;
        public readonly bool IsLong;
        public DurationRate? DurationRate { get; private set; }

        public AreaBuffEffect(AbilityAreaEffectBuff action, bool isLong, DurationRate? durationRate = null) {
            Applied = action.Buff.AssetGuid.m_Guid;
            IsLong = isLong;
            DurationRate = durationRate;
        }

        public PetType? PetType { get; set; }

        public void AppendTo(AbilityCombinedEffects effect) {
            if (PetType != null)
                effect.AddPetBuff(Applied, PetType.Value, IsLong, DurationRate);
            else
                effect.AddBuff(Applied, IsLong, DurationRate);
        }
    }

    public class BuffEffect : IBeneficialEffect {

        public readonly Guid Applied = Guid.Empty;
        public readonly bool IsLong;
        public DurationRate? DurationRate { get; private set; }

        public BuffEffect(ContextActionApplyBuff action) {
            if (action.Buff == null) return;
            Applied = action.Buff.AssetGuid.m_Guid;
            IsLong = action.IsLong();
            DurationRate = action.DurationValue?.Rate;
        }

        public BuffEffect(Guid applied) {
            Applied = applied;
            IsLong = true;
            DurationRate = null;
        }

        public PetType? PetType { get; set; }

        public void AppendTo(AbilityCombinedEffects effect) {
            if (Applied != Guid.Empty) {
                if (PetType != null)
                    effect.AddPetBuff(Applied, PetType.Value, IsLong, DurationRate);
                else
                    effect.AddBuff(Applied, IsLong, DurationRate);
            }
        }
    }

    public class WornItemEnchantmentEffect : IBeneficialEffect {
        public readonly Guid Applied;
        public readonly bool PrimaryWeapon;
        public readonly bool SecondaryWeapon;
        public readonly bool IsLong;
        public DurationRate? DurationRate { get; private set; }

        public PetType? PetType { get; set; }

        public WornItemEnchantmentEffect(ContextActionEnchantWornItem action) {
            Applied = action.Enchantment.AssetGuid.m_Guid;
            if (action.Slot == Kingmaker.UI.GenericSlot.EquipSlotBase.SlotType.PrimaryHand)
                PrimaryWeapon = true;
            if (action.Slot == Kingmaker.UI.GenericSlot.EquipSlotBase.SlotType.SecondaryHand)
                SecondaryWeapon = true;

            IsLong = action.IsLong();
            DurationRate = action.DurationValue?.Rate;
        }
        public void AppendTo(AbilityCombinedEffects effect) {
            if (PrimaryWeapon)
                effect.AddPrimaryWeaponEnchant(Applied, IsLong, DurationRate);
            else if (SecondaryWeapon)
                effect.AddSecondaryWeaponEnchnant(Applied, IsLong, DurationRate);
        }
    }

}
