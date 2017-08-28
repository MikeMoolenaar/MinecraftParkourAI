 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Malmo;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace MinecraftBot.AI
{
    public class Malmo
    {
        #region Values
        public MissionSpec Mission;
        public AgentHost agentHost;
        public State worldState;
        public Observation observation;
        public Definitions definitions;

        public Task HandlingMonsters;
        public CancellationTokenSource HandlingMonstersToken;
        public Task FollowingPlayer;

        public APIAI APIAIchat;

        System.Timers.Timer tmChat;
        bool timeristicking = false;

        List<string> items;
        List<string> blocks;
        #endregion

        #region Start
        public Malmo(MissionSpec mission)
        {
            this.tmChat = new System.Timers.Timer(1000);
            this.tmChat.Elapsed += (obj, e) =>
            {
                this.timeristicking = false;
            };
            this.Mission = mission;

            this.items = File.ReadAllLines(@"C:\Malmo-0.21.0-Windows-64bit\Schemas\items.txt").ToList();
            this.blocks = File.ReadAllLines(@"C:\Malmo-0.21.0-Windows-64bit\Schemas\blocks.txt").ToList();

            APIAIchat = new APIAI("517829a83d714675898031e8d58fd58c");
        }

        public bool Start()
        {
            //Agent
            agentHost = new AgentHost();
            try
            {
                //definitions = JsonConvert.DeserializeObject<Definitions>(File.ReadAllText("Definitions.json"));
                agentHost.parse(new StringVector(Environment.GetCommandLineArgs()));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: {0}", ex.Message);
                Console.Error.WriteLine(agentHost.getUsage());
                Console.ReadKey();
                return false;
            }

            //Mission
            try
            {
                ClientPool client_pool = new ClientPool();
                client_pool.add(new ClientInfo("127.0.0.1", 10001));
                agentHost.startMission(this.Mission, client_pool, new MissionRecordSpec(),0, "");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error starting mission: {0}", ex.Message);
                Console.ReadKey();
                return false;
            }

            //Missie voorbereiden
            Console.Write("Waiting for the mission to start");
            do
            {
                Console.Write(".");
                Thread.Sleep(100);
                worldState = new State(agentHost.getWorldState());
                foreach (TimestampedString error in worldState.WorldState.errors) Console.Error.WriteLine("Error: {0}", error.text);
            }
            while (!worldState.WorldState.has_mission_begun);

            AddChatEvents();

            //Observation binnenhalen, sluit progamma als die er niet is
            Thread.Sleep(100);
            if (GetState().observation.Count == 0) { return false; }

            //Wachten totdat eerste obervation binnen is
            Observate().Start();
            while (GetOsevation() == null) { Thread.Sleep(50); }

            Console.WriteLine();
            Console.WriteLine("Mission started.");
            return true;
        }
        #endregion

        #region Chat
        public void AddChatEvents()
        {
            APIAIchat.OnNoEventsFired = (obj, playername) =>
            {
                Say(obj.Result.Fulfillment.Speech.Replace("{player}", playername));
            };

            APIAIchat.AddEvent("followstart", (obj, playername) =>
            {
                if (FollowingPlayer == null)
                {
                    HandlingMonstersToken = new CancellationTokenSource();
                    FollowingPlayer = Task.Run(() => FollowPlayer(playername), HandlingMonstersToken.Token);
                    Say("OK, I'll follow you");
                }
                else
                {
                    Say("I'm already following you");
                }
            });

            APIAIchat.AddEvent("followstop", (obj, playername) =>
            {
                if (FollowingPlayer != null)
                {
                    HandlingMonstersToken.Cancel();
                    FollowingPlayer = null;
                    Say("I'll stop following you");
                }
                else
                {
                    Say("I didn't follow you");
                }
            });

            APIAIchat.AddEvent("drop item", (obj, playername) =>
            {
                string itemname = obj.Result.Parameters["any"].ToString().Replace(" ", "_");
                if (!items.Contains(itemname) && !blocks.Contains(itemname))
                {
                    Say("That item doen't exist");
                    return;
                }
                int slot = GetItemSlot(itemname);
                if (slot != -1)
                {
                    WalkTo(GetPlayer(playername).pos, true, 6);
                    DropItem(slot);
                    Say("There you go");
                }
                else
                {
                    Say("I don't have that item");
                }
            });

            APIAIchat.AddEvent("report", (obj, playername) =>
            {
                string name = obj.Result.Parameters["any"].ToString().Replace(" ", "_");
                switch (name)
                {
                    case "health":
                    case "life":
                        var health = GetOsevation().stats.Life.ToString();
                        Say($"My health-level is {health}/20");
                        break;
                    case "food":
                        var food = GetOsevation().stats.Food;
                        Say($"My food-level is {food}/20");
                        break;
                    case "time":
                    case "worldtime":
                        var time = GetOsevation().stats.WorldTime.ToString();
                        Say($"The time is {time}");
                        break;
                    default:
                        Say("Sorry, I don't know what that is");
                        break;
                }
            });
        }
        public void HandleChat(Observation obv)
        {
            if (obv.stats.Chat == null || timeristicking) { return; }
            tmChat.Start();
            timeristicking = true;
            var chatmessage = obv.stats.Chat[0];
            if (!chatmessage.StartsWith("<Bot>") && chatmessage.Contains('>'))
            {
                string message = chatmessage.Split('>')[1].Trim().ToLower();
                //if (!message.StartsWith(@"\")) return; //Bericht moet met '\' starten

                var playername = chatmessage.Split('>')[0].Substring(1);

                APIAIchat.Interact(message, playername);
            }
        }
        #endregion

        #region Observation
        public State GetState(bool refresh = true)
        {
            if (refresh)
            {
                WorldState curState = agentHost.peekWorldState();
                if (curState.number_of_observations_since_last_state > 0)
                {
                    worldState.observation = curState.observations;
                }
                worldState.WorldState = curState;
            }
            return worldState;

        }
        public Observation GetOsevation(bool refresh = false)
        {
            if(refresh)
            {
                observation = new Observation(GetState(true), 10);
               
                /*
                if (HandlingMonsters == null || HandlingMonsters.IsCompleted)
                {
                    new Task(() => KillEntities()).Start();
                }*/

                //HandleChat(observation);
            }
            return observation;
        }

        public Task Observate(int interval = 35)
        {
            return new Task(() =>
            {
                while (true)
                {
                    observation = GetOsevation(true);
                    Thread.Sleep(interval);
                }
            });
        }
        #endregion

        #region Movements
        public void Jump()
        {
            agentHost.sendCommand("jump 1");
            Thread.Sleep(10);
            agentHost.sendCommand("jump 0");
        }

        public void WalkTo(Position pos, bool turnhead = true, int offset = 4)
        {
            Observation obsevation = GetOsevation(true);
            Position walktoposition = Helpers.PosToAbsolute(pos, obsevation.stats.pos);

            //Hoofd draaien naar positie
            if (turnhead)
            {
                TurnHead(walktoposition, Convert.ToInt32(obsevation.stats.Yaw));
            }

            while (GetOsevation().stats.pos.GetDistance(pos) > offset && moveto)
            {
                agentHost.sendCommand("move 1");
                var suroundings = Helpers.GetFirstThreeBlocks(GetOsevation());
                //Debugger.Break();

                if (suroundings[0].Blockname != "air" && suroundings[1].Blockname == "air" && suroundings[2].Blockname == "air")
                {
                    Jump();
                }
                else if (suroundings[0].Blockname != "air" && suroundings[1].Blockname != "air" && suroundings[2].Blockname == "air")
                {
                    agentHost.sendCommand("attack 1");
                    while (Helpers.GetFirstThreeBlocks(GetOsevation())[1].Blockname != "air")
                    {
                        Thread.Sleep(30);
                    }
                    agentHost.sendCommand("attack 0");

                    Jump();
                }
                else if (suroundings[0].Blockname != "air" && suroundings[1].Blockname != "air" && suroundings[2].Blockname != "air")
                {
                    agentHost.sendCommand("attack 1");
                    while (Helpers.GetFirstThreeBlocks(GetOsevation())[1].Blockname != "air")
                    {
                        Thread.Sleep(30);
                    }
                    agentHost.sendCommand("attack 0");

                    PitchHead(-30);
                    agentHost.sendCommand("attack 1");
                    while (Helpers.GetFirstThreeBlocks(GetOsevation())[2].Blockname != "air")
                    {
                        Thread.Sleep(30);
                    }
                    agentHost.sendCommand("attack 0");
                    PitchHead(0);
                    Jump();
                }
            }

            //Agent is bij positie
            agentHost.sendCommand("move 0");
        }

        public void TurnHead(Position pos, double currentyaw, int speed = 5)
        {
            //Hoek berekenen op bais van X en Z
            TurnHead(pos.X, pos.Z, currentyaw, speed);
        }

        public void TurnHead(double x, double z, double currentyaw, int speed = 5)
        {
            double degrees = Math.Atan2(x, z) * (180 / Math.PI) * -1;
            
            if (degrees == -180 && currentyaw > 0)
            {
                degrees = 180;
            }
            else if (degrees == 180 && currentyaw < 0)
            {
                degrees = -180;
            }

            TurnHead(degrees, currentyaw, speed);
        }

        public void TurnHead(double degrees, double dcurrentyaw, int speed = 5)
        {
            double difference = Math.Abs(dcurrentyaw - degrees);

            //Correctie, om de bot zo efficient te draaien (Als dit niet gebreurt, kan de bot teveel draaien)
            if (difference > 180) { degrees -= 360; }

            //Verschil moet 3 of groter zijn
            if (difference < 3) { return; }

            int currentyaw = Convert.ToInt32(dcurrentyaw);
            if (degrees > currentyaw)
            {
                for (int h = currentyaw; h < degrees; h += 1)
                {
                    agentHost.sendCommand("setYaw " + h.ToString());
                    Thread.Sleep(speed);
                }
            }
            else
            {
                for (int h = currentyaw; h > degrees; h -= 1)
                {
                    agentHost.sendCommand("setYaw " + h.ToString());
                    Thread.Sleep(speed);
                }
            }
        }

        public void PitchHead(int degrees, int currentpitch = 1000, int speed = 5)
        {
            if(currentpitch == 1000)
            {
                currentpitch = Convert.ToInt32(GetOsevation().stats.Pitch);
            }

            if (degrees > currentpitch)
            {
                for (int h = currentpitch-=2; h < degrees; h += 1)
                {
                    agentHost.sendCommand("setPitch " + h.ToString());
                    Thread.Sleep(speed);
                }
            }
            else
            {
                for (int h = currentpitch+=2; h > degrees; h -= 1)
                {
                    agentHost.sendCommand("setPitch " + h.ToString());
                    Thread.Sleep(speed);
                }
            }
        }
        #endregion

        #region Methods
        public void Hotbar(int slot)
        {
            agentHost.sendCommand($"hotbar.{slot.ToString()} 1");
            agentHost.sendCommand($"hotbar.{slot.ToString()} 0");
        }

        public void Say(string message)
        {
            agentHost.sendCommand($"chat {message}");
        }

        public int GetItemSlot(string itemname)
        {
            Observation obv = GetOsevation();
            List<InventoryItem> lsMatchinghItems = obv.stats.Inventory
                .Where(x => x.itemname == itemname.ToLower())
                .ToList();

            return lsMatchinghItems.Count == 0 ? -1 : lsMatchinghItems.First().inventoryslot + 1;
        }
        public void DropItem(int slot)
        {
            Hotbar(slot);
            Thread.Sleep(200);
            agentHost.sendCommand("discardCurrentItem");
        }

        public void CutTree()
        {
            //Boom omhakken
            PitchHead(50);

            //Eerste 2 logs omhakken, kijkt naard e richting van de boom
            agentHost.sendCommand("attack 1");
            var blockpos = Helpers.DirectionToZX(GetOsevation().stats.direction);
            while (GetOsevation().lsPotisions.
                Where(x => x.Z == blockpos.Item1 && x.X == blockpos.Item2 && (x.Y == 0 || x.Y == 1))
                .Select(x => x.Blockname)
                .Contains("log"))
            {
                Thread.Sleep(30);
            }
            agentHost.sendCommand("attack 0");
            //Onder de rest van de blokken staan en hoofd naar boven richten
            agentHost.sendCommand("move 1");
            Thread.Sleep(250);
            agentHost.sendCommand("move 0");
            PitchHead(-120);

            //De rest van de boom omhakken
            agentHost.sendCommand("attack 1");
            while (GetOsevation().lsPotisions.
                Where(x => x.Z == 0 && x.X == 0)
                .Select(x => x.Blockname)
                .Contains("log"))
            {
                Thread.Sleep(30);
            }
            Thread.Sleep(50);
            agentHost.sendCommand("attack 0");

            PitchHead(0);
        }

        public bool moveto = true;
        Entity GetPlayer(string playername) => GetOsevation().stats.entities
                    .Where(x => x.name == playername)
                    .FirstOrDefault();
        public async void FollowPlayer(string playername)
        {
            //Code opschonen?
            //Alle chat berichten door API.AI laten gaan
            //Bouwen op basis van schematic bestand (NodeJS code gebruiken)
            Task tskFollowPlayer = null;
            while(true)
            {
                if(HandlingMonstersToken.IsCancellationRequested) { break; }

                Entity player = GetPlayer(playername);
                if (player == null) { return; }
                
                if (tskFollowPlayer != null)
                {
                    moveto = false;
                    while (!tskFollowPlayer.IsCompleted) { Thread.Sleep(10); }
                }
                moveto = true;
                await Task.Run(() => WalkTo(player.pos));
            }
        }

        public List<Entity> lsEntities() => GetOsevation().stats.entities
            .Skip(1)
            .Where(x => definitions.monsters.Contains(x.name))
            .ToList();
        public void KillEntities()
        {
            if (lsEntities().Count == 0) { return; }
            Hotbar(2);
            foreach (Entity fountentity in lsEntities())
            {
                Entity entity() => lsEntities()
                    .Where(x => x.name == fountentity.name)
                    .FirstOrDefault();

                while (entity() != null)
                {
                    void TurnHeadToEntity()
                    {
                        var obv = GetOsevation(true);
                        var ent = entity();
                        if (ent != null)
                        {
                            TurnHead(Convert.ToInt32(entity().x - obv.stats.XPos), Convert.ToInt32(entity().z - obv.stats.ZPos), GetOsevation().stats.Yaw);
                        }
                    };
                    void MoveCloseToEntity()
                    {
                        var ent = entity();
                        if (ent != null)
                        {
                            agentHost.sendCommand("move 1");
                            while (!GetOsevation().stats.pos.IsNearPosition(ent.pos, 3)) { Thread.Sleep(20); }
                            agentHost.sendCommand("move 0");
                        }
                    };

                    do
                    {
                        TurnHeadToEntity();
                        MoveCloseToEntity();
                        agentHost.sendCommand("attack 1");
                        Thread.Sleep(30);
                    }
                    while (entity() != null);
                    agentHost.sendCommand("move 0");
                    agentHost.sendCommand("attack 0");
                }
            }
        }
        #endregion

    }
    
}
