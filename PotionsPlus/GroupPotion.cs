using System.Linq;
using Groups;
using UnityEngine;

namespace PotionsPlus;

public class GroupPotion : SE_Stats
{
    // Keep as public static to match original - but this should really be per-instance
    // If you want proper per-instance tracking, make it private and remove Stop() override
    public static bool effectApplied = false;
    public HitData.DamageType damageType;
    public int range;

    public override void Setup(Character character)
    {
        base.Setup(character);

        // Safety check: only proceed if character is local player and Groups API is loaded
        if (character != Player.m_localPlayer || effectApplied || !API.IsLoaded())
        {
            return;
        }

        try
        {
            var groupPlayers = API.GroupPlayers();
            if (groupPlayers == null)
            {
                return;
            }

            // PlayerReference is a struct, so no null check needed on the reference itself
            foreach (PlayerReference groupPlayer in groupPlayers.Where(p => p != PlayerReference.fromPlayer(Player.m_localPlayer)))
            {
                // Get the player's position from the active Player instances
                // This is the correct way - ZNet.PlayerInfo doesn't have m_position
                Player targetPlayer = Player.s_players?.FirstOrDefault(p => p != null && p.GetPlayerID() == groupPlayer.peerId);

                if (targetPlayer == null)
                {
                    // Player not in range or not loaded, skip
                    continue;
                }

                Vector3 groupPlayerPos = targetPlayer.transform.position;

                // Check if in range (range 0 = unlimited)
                if (range == 0 || global::Utils.DistanceXZ(character.transform.position, groupPlayerPos) < range)
                {
                    if (ZRoutedRpc.instance != null)
                    {
                        ZRoutedRpc.instance.InvokeRoutedRPC(groupPlayer.peerId, "PotionsPlus Potion Activated", name);
                    }
                }
            }

            effectApplied = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PotionsPlus] Error in GroupPotion.Setup: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
    {
        if ((int)damageType == 0)
        {
            return;
        }

        float chopDamage = hitData.m_damage.m_chop;
        float pickaxeDamage = hitData.m_damage.m_pickaxe;
        float totalDamage = hitData.GetTotalDamage() - pickaxeDamage - chopDamage;
        hitData.m_damage = new HitData.DamageTypes { m_chop = chopDamage, m_pickaxe = pickaxeDamage };

        switch (damageType)
        {
            case HitData.DamageType.Fire:
                {
                    hitData.m_damage.m_fire = totalDamage;
                    break;
                }
            case HitData.DamageType.Frost:
                {
                    hitData.m_damage.m_frost = totalDamage;
                    break;
                }
            case HitData.DamageType.Lightning:
                {
                    hitData.m_damage.m_lightning = totalDamage;
                    break;
                }
            case HitData.DamageType.Poison:
                {
                    hitData.m_damage.m_poison = totalDamage;
                    break;
                }
            case HitData.DamageType.Spirit:
                {
                    hitData.m_damage.m_spirit = totalDamage;
                    break;
                }
        }
    }
}