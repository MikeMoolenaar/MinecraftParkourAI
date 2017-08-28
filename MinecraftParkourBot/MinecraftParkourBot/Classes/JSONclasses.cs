using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MinecraftBot.AI;

namespace MinecraftBot
{
    public class Entity
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double yaw { get; set; }
        public double pitch { get; set; }
        public string name { get; set; }
        public Position pos { get; set; }
        
    }
    public class Sight
    {
        public string hitType { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public string type { get; set; }
        public bool inRange { get; set; }
        public Position pos { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Stats
    {
        public int DistanceTravelled { get; set; }
        public int TimeAlive { get; set; }
        public int MobsKilled { get; set; }
        public int PlayersKilled { get; set; }
        public int DamageTaken { get; set; }
        public double Life { get; set; }
        public int Score { get; set; }
        public int Food { get; set; }
        public int XP { get; set; }
        public bool IsAlive { get; set; }
        public int Air { get; set; }
        public string Name { get; set; }
        public double XPos { get; set; }
        public double YPos { get; set; }
        public double ZPos { get; set; }
        public double Pitch { get; set; }
        public Helpers.Direction direction { get; set; }
        public double Yaw { get; set; }
        public Position pos { get; set; }
        public int WorldTime { get; set; }
        public int TotalTime { get; set; }
        public List<string> surounding { get; set; }
        public List<Entity> entities { get; set; }
        public Sight LineOfSight { get; set; }
        public List<string> Chat { get; set; }
        public List<InventoryItem> Inventory { get; set; }
    }
}

public class InventoryItem
{
    public int inventoryslot { get; set; }
    public string itemname { get; set; }

}

public class Definitions
{
    public List<string> monsters { get; set; }
}
