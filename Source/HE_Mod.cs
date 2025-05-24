using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace rep.heframework
{
    public class HE_Mod : Mod
    {
        HE_Settings Settings;

        public HE_Mod(ModContentPack content) : base(content)
        {
            this.Settings = GetSettings<HE_Settings>();
        }

        public override string SettingsCategory()
        {
            return "Hostility Enhanced";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            Text.Font = GameFont.Medium;
            list.Begin(inRect);


            list.CheckboxLabeled("Friendly HE factions can expand (not actually implemented): ", ref HE_Settings.friendlyHEFsCanExpand);
            list.CheckboxLabeled("Verbose debug logging: ", ref HE_Settings.debugLogging);

            string buffer = HE_Settings.earliestExpansionDays.ToString();
            list.TextFieldNumericLabeled("Earliest days that factions can expand", ref HE_Settings.earliestExpansionDays, ref buffer);

            list.End();
        }
    }
}
