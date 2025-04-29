using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace rep.heframework
{
    public class HEF_Mod : Mod
    {
        HEF_Settings Settings;

        public HEF_Mod(ModContentPack content) : base(content)
        {
            this.Settings = GetSettings<HEF_Settings>();
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


            list.CheckboxLabeled("Friendly HE factions can expand (not actually implemented): ", ref HEF_Settings.friendlyHEFsCanExpand);
            list.CheckboxLabeled("Verbose debug logging: ", ref HEF_Settings.debugLogging);

            string buffer = HEF_Settings.earliestExpansionDays.ToString();
            list.TextFieldNumericLabeled("Earliest days that factions can expand", ref HEF_Settings.earliestExpansionDays, ref buffer);

            list.End();
        }
    }
}
