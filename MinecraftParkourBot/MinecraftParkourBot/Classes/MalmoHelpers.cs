using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Malmo;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MinecraftBot.AI
{
    public class State
    {
        public WorldState WorldState;
        public TimestampedStringVector observation;
        public State(WorldState worldstate)
        {
            this.WorldState = worldstate;
            this.observation = worldstate.observations;
        }
    }

    public class Observation
    {
        public List<Position> lsPotisions = new List<Position>();
        public Stats stats;
        public int Gridsize;
        public Observation(State worldState, int gridsize)
        {
            string json = worldState.observation[0].text;
            this.stats = JsonConvert.DeserializeObject<Stats>(json);

            this.stats.Inventory = new List<InventoryItem>();
            var data = (JObject)JsonConvert.DeserializeObject(json);
            var list = data.Properties().Where(x => x.Name.Contains("InventorySlot_"));
            foreach (List<JProperty> obj in list.GroupBy(x => x.Name.Split('_')[1]).Select(x => x.ToList()).ToList())
            {
                this.stats.Inventory.Add(new InventoryItem()
                {
                    inventoryslot = Convert.ToInt32(obj[0].Name.Split('_')[1]),
                    itemname = obj[1].First.ToString()
                });
            }

            this.stats.direction = Helpers.GetDirection(Convert.ToInt32(this.stats.Yaw));
            this.stats.pos = new Position(this.stats.XPos, this.stats.YPos, this.stats.ZPos, null);
            if (this.stats.LineOfSight != null)
            {
                this.stats.LineOfSight.pos = new Position(this.stats.LineOfSight.x, this.stats.LineOfSight.y, this.stats.LineOfSight.z, null);
            }

            foreach (var entity in this.stats.entities)
            {
                entity.pos = new Position(entity.x, entity.y, entity.z, null);
            }

            this.Gridsize = gridsize;

            /*-- Posities omzetten naar relatieve coordinaten --*/
            int value = (this.Gridsize * 44) + 1;
            int pervalue = this.stats.surounding.Count / value;

            int xpos = this.Gridsize * -1;
            int zpos = this.Gridsize * -1;
            
            for (int x = 0; x < value; x++)
            {
                double res = (double)x / pervalue;
                if (x != 0)
                {
                    if ((res % 1) == 0)
                    {
                        xpos = this.Gridsize * -1;
                        zpos++;
                    }
                    else
                    {
                        xpos++;
                    }
                }

                int ypos = 0;
                for (int y = x; y < this.stats.surounding.Count; y += value)
                {
                    this.lsPotisions.Add(new Position(xpos, ypos++ - this.Gridsize, zpos, this.stats.surounding[y]));
                }
            }

            //Posities sorteren, zodat de blocken die het dichstbij de bot
            //staat eerst komen
            this.lsPotisions = this.lsPotisions
                .OrderBy(n => n.Distance)
                .ToList();
        }

        public Position GetLocationOfBlock(string blockname)
        {
            var list = this.lsPotisions
                .Where(x => x.Blockname == blockname);
            Console.WriteLine("Found {0} at: {1}", blockname, list.First().ToString());
            return list.First();
        }
    }

    public class Position
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Helpers.Direction Direction { get; set; }
        public string Blockname { get; set; }
        public double Distance { get; set; }
        public Helpers.Direction StraightDirection { get; set; }

        public Position(double x, double y, double z, string blockname)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Blockname = blockname;
            this.Distance = Math.Abs(0 - this.X) + Math.Abs(0 - this.Z) + Math.Abs(0 - this.Y);
            this.Direction = Helpers.GetDirectionByXZ(x, z);
            this.StraightDirection = Helpers.GetDirectionByXZ(x, z, true);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetLocString()
        {
            dynamic pos = new System.Dynamic.ExpandoObject();
            pos.X = this.X;
            pos.Y = this.Y;
            pos.Z = this.Z;
            return JsonConvert.SerializeObject(pos);
        }

        public bool IsNearPosition(Position pos, double offset = 0)
        {
            return
                pos.X <= this.X + offset && pos.X >= this.X - offset
                &&
                pos.Y <= this.Y + offset && pos.Y >= this.Y - offset
                &&
                pos.Z <= this.Z + offset && pos.Z >= this.Z - offset;
        }

        public double GetDistance(Position pos)
        {
            return Math.Abs(this.X - pos.X) + Math.Abs(this.Y - pos.Y) + Math.Abs(this.Z - pos.Z);
        }
    }

    public static class Helpers
    {
        public enum Direction { North, East, South, West, Undefined }
        public static Direction GetDirection(int dergrees, int range = 45)
        {
            int north = 0;
            int east = 90;
            int west = -90;
            var south = new Tuple<int, int>(180, -180);
            if (dergrees >= north - range && dergrees <= north + range)
            {
                return Direction.North;
            }
            else if (dergrees >= east - range && dergrees <= east + range)
            {
                return Direction.East;
            }
            else if (dergrees >= west - range && dergrees <= west + range)
            {
                return Direction.West;
            }
            else if ((dergrees >= south.Item1 - range && dergrees <= south.Item1 + range) ||
                (dergrees >= south.Item2 - range && dergrees <= south.Item2 + range))
            {
                return Direction.South;
            }
            else
            {
                return Direction.Undefined;
            }
        }

        public static Direction GetDirectionByXZ(double x, double z, bool straight = false)
        {
            int degrees = Convert.ToInt32(Math.Atan2(x, z) * (180 / Math.PI)) * -1;
            return straight ? GetDirection(degrees) : GetDirection(degrees, 0);
        }

        public static Tuple<int, int> DirectionToZX(Direction direction)
        {
            switch (direction)
            {
                case Direction.East: return new Tuple<int, int>(0, -1);
                case Direction.North: return new Tuple<int, int>(1, 0);
                case Direction.South: return new Tuple<int, int>(-1, 0);
                case Direction.West: return new Tuple<int, int>(0, 1);
            }
            return new Tuple<int, int>(0, 0);
        }

        public static List<Position> GetFirstThreeBlocks(Observation obv)
        {
            var loc = Helpers.DirectionToZX(obv.stats.direction);
            var list = obv.lsPotisions
                    .Where(x => x.Z == loc.Item1 && x.X == loc.Item2 && (x.Y >= 0 && x.Y <= 2))
                    .ToList();
            return list;
        }

        public static Position PosToAbsolute(Position currentpos, Position pos)
        {
            return new Position(currentpos.X - pos.X, currentpos.Y - pos.Y, currentpos.Z - pos.Z, null);
        }
    }
}
