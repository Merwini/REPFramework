using System;
using RimWorld;
using Verse;

namespace rep.heframework
{
    public class Building_DoorNoNPC : Building_Door
    {
        //a door that can only be opened by player pawns, and only if they own the door. Intended for use in NPC bases, to keep them out of certain rooms e.g. ammo stockpiles
        public override bool PawnCanOpen(Pawn p)
        {
            return (base.Faction == Faction.OfPlayer && p.Faction == Faction.OfPlayer);
        }
    }
}

