using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;


namespace rep.heframework
{
    class GenStep_ExclusionArea : GenStep
    {
        public override int SeedPart => 000000000;

        public CellRect structureRect;

        public override void Generate(Map map, GenStepParams parms)
        {
            throw new NotImplementedException();
        }


    }
}
