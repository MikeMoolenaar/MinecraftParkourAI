using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Malmo;
using MinecraftBot;
using MinecraftBot.AI;

namespace MinecraftParkourBot
{
    class Program
    {
        static void Main(string[] args)
        {
            XMLmissionspec xmlMission = new XMLmissionspec("missionspec.xml");
            xmlMission.ChangeSetting("FileWorldGenerator", "forceReset", false);
            xmlMission.ChangeSetting("AgentSection", "mode", "Creative");

            MissionSpec mission = new MissionSpec(xmlMission.ToString(), false);
            mission.observeGrid(-10, -10, -10, 10, 10, 10, "surounding");
            mission.allowAllInventoryCommands();

            //Start mission
            Malmo malmoMission = new Malmo(mission);
            if (!malmoMission.Start()) { return; }

            //Laat het progamma open zoalang de missie is
            while (malmoMission.GetState(false).WorldState.is_mission_running) ;
            {
                
            }

            Console.WriteLine("Mission has stopped.");
            Console.ReadKey();
        }
    }
}
