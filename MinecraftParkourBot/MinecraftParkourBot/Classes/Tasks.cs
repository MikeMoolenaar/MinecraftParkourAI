using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinecraftBot.AI;

namespace MinecraftBot.Classes
{
    class Tasks
    {
        Malmo malmoMission = null;
        public void CutTrees()
        {
            //Naar boom lopen
            Observation obsevation = malmoMission.GetOsevation();
            Position psClosesttree = obsevation.GetLocationOfBlock("log");
            Console.WriteLine("Found tree at " + psClosesttree.ToString());
            malmoMission.WalkTo(psClosesttree);

            //Omdraaien naar boom
            obsevation = malmoMission.GetOsevation();
            malmoMission.TurnHead(obsevation.GetLocationOfBlock("log"), obsevation.stats.Yaw);

            Console.WriteLine("Cutting tree...");
            malmoMission.CutTree();
            Console.WriteLine("Finished cutting tree");
        }

        public void WalkToGoldBlock()
        {
            Observation obsevation = malmoMission.GetOsevation();
            malmoMission.WalkTo(obsevation.GetLocationOfBlock("gold_block"));
        }
    }
}
