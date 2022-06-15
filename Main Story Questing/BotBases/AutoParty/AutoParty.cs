using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Pathing.Service_Navigation;
using ff14bot.RemoteWindows;
using LlamaLibrary.Helpers;
using LlamaLibrary.Logging;
using LlamaLibrary.Memory;
using LlamaLibrary.RemoteWindows;
using TreeSharp;
using Action = System.Action;

namespace AutoParty
{
    public class AutoParty: BotBase
    {
        private static readonly LLogger Log = new LLogger("AutoParty", Colors.Pink);

        private Composite _root;

        private static bool ranOrderbot = false;

        public override string Name => "AutoParty";
        public override PulseFlags PulseFlags => PulseFlags.All;

        public override bool IsAutonomous => true;
        public override bool RequiresProfile => false;

        public override Composite Root => _root;

        public override bool WantButton { get; } = true;

        private static string FolderPath;
        
        private static Dictionary<ushort, string> ZoneMappings = new Dictionary<ushort, string>()
        {
            {101, "101.xml"},
            {140, "Profiles/ARR/Cape Westwind.xml"},
            {143, "Profiles/ARR/The Steps of Faith.xml"},
            {150, "Profiles/ARR/Keeper of the Lake.xml"},
            {157, "Profiles/ARR/Sastasha.xml"},
            {158, "Profiles/ARR/Brayfloxs Longstop.xml"},
            {159, "Profiles/ARR/The Wanderer's Palace.xml"},
            {160, "Profiles/ARR/Pharos Sirius.xml"},
            {161, "Profiles/ARR/The Copperbell Mines.xml"},
            {163, "Profiles/ARR/The Sunken Temple of Qarn.xml"},
            {164, "Profiles/ARR/Tam Tara Deepcroft.xml"},
            {166, "Profiles/ARR/Haukke Manor.xml"},
            {167, "Profiles/ARR/Amdapor Keep.xml"},
            {168, "Profiles/ARR/Stone Vigil.xml"},
            {169, "Profiles/ARR/The Thousand Maws of Toto-Rak.xml"},
            {170, "Profiles/ARR/Cutter's Cry.xml"},
            {171, "Profiles/ARR/Dzemael Darkhold.xml"},
            {172, "Profiles/ARR/Aurum Vale.xml"},
            {188, "Profiles/ARR/The Wanderer's Palace (Hard).xml"},
            {202, "Profiles/ARR/The Bowl of Embers.xml"},
            {206, "Profiles/ARR/The Navel.xml"},
            {207, "Profiles/ARR/Thornmarch (Hard).xml"},
            {208, "Profiles/ARR/The Howling Eye.xml"},
            {281, "Profiles/ARR/The Whorleater (Hard).xml"},
            {293, "Profiles/ARR/The Navel (Hard).xml"},
            {294, "Profiles/ARR/The Howling Eye (Hard).xml"},
            {292, "Profiles/ARR/The Bowl of Embers (Hard).xml"},
            {349, "Profiles/ARR/The Copperbell Mines (Hard).xml"},
            {350, "Profiles/ARR/Haukke Manor (Hard).xml"},
            {360, "Profiles/ARR/Halatali (Hard).xml"},
            {362, "Profiles/ARR/Brayfloxs Longstop (Hard).xml"},
            {363, "Profiles/ARR/The Lost City of Amdapor.xml"},
            {367, "Profiles/ARR/The Sunken Temple of Qarn (Hard).xml"},
            {371, "Profiles/ARR/Snowcloak.xml"},
            {373, "Profiles/ARR/Tam-Tara Deepcroft (Hard).xml"},
            {374, "Profiles/ARR/The Striking Tree (Hard).xml"},
            {377, "Profiles/ARR/Akh Afah Amphitheatre (Hard).xml"},
            {387, "Profiles/ARR/Sastasha (Hard).xml"},
            {426, "Profiles/ARR/The Chrysalis.xml"},          
            {1043, "Profiles/ARR/Castrum Meridianum.xml"},
            {1044, "Profiles/ARR/The Praetorium.xml"},
            {1048, "Profiles/ARR/The Porta Decumana.xml"},
            
            
            {416, "Profiles/Heavensward/The Great Gubal Library.xml"},
            {421, "Profiles/Heavensward/The Vault.xml"},
            {432, "Profiles/Heavensward/Thok ast Thok (Hard).xml"},
            {434, "Profiles/Heavensward/The Dusk Vigil.xml"},            
            {435, "Profiles/Heavensward/The Aery.xml"},
            {436, "Profiles/Heavensward/The Limitless Blue.xml"},
            {437, "Profiles/Heavensward/The Singularity Reactor.xml"},
            {438, "Profiles/Heavensward/The Aetherochemical Research Facility.xml"},
            {441, "Profiles/Heavensward/Sohm Al.xml"},
            {516, "Profiles/Heavensward/The Antitower.xml"},
            {559, "Profiles/Heavensward/The Final Steps of Faith.xml"},
            //{572, "Profiles/Heavensward/Xelphatol.xml"}, So many bad meshings
            //{615, "Profiles/Heavensward/Baelsar's Wall.xml"} Needs to be done
            
            //{623, "Profiles/Stormblood/Bardam's Mettle.xml"} Can't do because simon says game for boss 2.
            {626, "Profiles/Stormblood/Sirensong Sea.xml"},
            {660, "Profiles/Stormblood/Doma Castle.xml"},
            //{661, "Profiles/Stormblood/Castrum Abania.xml"} Need to do
            //{674, "Profiles/Stormblood/The Pool of Tribute.xml"}, Can't do this because Active Time Manuever 
            //{679, "Profiles/Stormblood/The Royal Menagerie.xml"}, For some reason we run toward the water and off the platform, also can't survive stack
            //{689, "Profiles/Stormblood/Ala Mhigo.xml"}, Profile made, but need better mechanics for second boss out of body 
            {731, "Profiles/Stormblood/The Drowned City of Skalla.xml"},
            {793, "Profiles/Stormblood/The Ghimlyt Dark.xml"}  


        };
        
        
        public AutoParty()
        {
            OffsetManager.Init();
            Log.Information("Want to set agents now...");
            OffsetManager.SetOffsetClassesAndAgents();
            
            //var foldername = "AutoParty";
            FolderPath = GeneralFunctions.SourceDirectory().FullName;
            
        }
        
        public override void Start()
        {
            Navigator.PlayerMover = new SlideMover();
            Navigator.NavigationProvider = new ServiceNavigationProvider();
            _root = new ActionRunCoroutine(r => DutyRun());
        }


        public override void Stop()
        {
            _root = null;
            (Navigator.NavigationProvider as IDisposable)?.Dispose();
            Navigator.NavigationProvider = null;
        }

        private async Task<bool> DutyRun()
        {
            if (PluginManager.Plugins.Any(p => p.Plugin.Name == "SideStep" && p.Enabled))
            {
                PluginManager.Plugins.First(p => p.Plugin.Name == "SideStep" && p.Enabled).Enabled = false;
            }

            if (!PartyManager.IsInParty)
            {
                Log.Information("Waiting on party invite...");
                TreeRoot.StatusText = $"Waiting on party invite";
                await Coroutine.Wait(-1, () => NotificationParty.Instance.IsOpen);
                if (NotificationParty.Instance.IsOpen)
                {
                    if (await PartyYesNo.Instance.WaitTillWindowOpen(-1))
                    {
                        Log.Information("Party invite open");
                        Log.Information(PartyYesNo.Instance.NameLine);

                        SelectYesno.Yes();
                        //await Coroutine.Wait(10000, () => !SelectYesno.IsOpen || PartyManager.IsInParty);
                        await Coroutine.Wait(10000, () => PartyManager.IsInParty);
                    }
                }
            }

            while (PartyManager.IsInParty && DutyManager.QueueState != QueueState.InDungeon)
            {
                Log.Information("Waiting on Dungeon Queue pop...");
                TreeRoot.StatusText = $"Waiting on Dungeon Queue.";
                while (DutyManager.QueueState != QueueState.None ||
                       DutyManager.QueueState != QueueState.InDungeon || CommonBehaviors.IsLoading || !PartyManager.IsInParty)
                {
                    if (DutyManager.QueueState == QueueState.CommenceAvailable)
                    {
                        Log.Information("Waiting for queue pop.");
                        TreeRoot.StatusText = $"Waiting on Dungeon Queue.";
                        await Coroutine.Wait(
                            -1,
                            () => (DutyManager.QueueState == QueueState.JoiningInstance ||
                                   DutyManager.QueueState == QueueState.None));
                    }

                    if (DutyManager.QueueState == QueueState.JoiningInstance)
                    {
                        Random rnd = new Random();
                        int waitTime = rnd.Next (1000, 10000);
                        
                        Log.Information($"Dungeon popped, commencing in {waitTime/1000} seconds.");
                        await Coroutine.Sleep(waitTime);
                        DutyManager.Commence();
                        await Coroutine.Wait(
                            -1,
                            () => (DutyManager.QueueState == QueueState.LoadingContent ||
                                   DutyManager.QueueState == QueueState.CommenceAvailable));
                    }

                    if (DutyManager.QueueState == QueueState.LoadingContent)
                    {
                        Log.Information("Waiting for everyone to accept queue.");
                        await Coroutine.Wait(-1,
                            () => (CommonBehaviors.IsLoading ||
                                   DutyManager.QueueState == QueueState.CommenceAvailable));
                        await Coroutine.Sleep(1000);
                    }

                    if (CommonBehaviors.IsLoading)
                    {
                        break;
                    }
                    
                    if (!PartyManager.IsInParty)
                    {
                        break;
                    }

                    await Coroutine.Sleep(500);
                }
                
                if (!PartyManager.IsInParty)
                {
                    break;
                }

                await Coroutine.Sleep(500);
                if (CommonBehaviors.IsLoading)
                {
                    await Coroutine.Wait(-1, () => !CommonBehaviors.IsLoading);
                }

                if (QuestLogManager.InCutscene)
                {
                    TreeRoot.StatusText = "InCutscene";
                    if (ff14bot.RemoteAgents.AgentCutScene.Instance != null)
                    {
                        ff14bot.RemoteAgents.AgentCutScene.Instance.PromptSkip();
                        await Coroutine.Wait(250, () => SelectString.IsOpen);
                        if (SelectString.IsOpen)
                        {
                            SelectString.ClickSlot(0);
                        }
                    }
                }

                Log.Information("Should be in duty");

                var director = (ff14bot.Directors.InstanceContentDirector) DirectorManager.ActiveDirector;
                if (director != null)
                {
                    Log.Information($"{director.TimeLeftInDungeon}");
                    await Coroutine.Wait(-1,
                        () => director.TimeLeftInDungeon.Seconds < 60 &&
                              director.TimeLeftInDungeon.Seconds > 0);
                }
                else
                {
                    Log.Error("Director is null");
                }

                Log.Information($"Should be ready");
            }

            if (DutyManager.QueueState == QueueState.InDungeon)
            {
                Log.Information($"In zone {WorldManager.CurrentZoneName}");
                TreeRoot.StatusText = $"Loading profile for {WorldManager.CurrentZoneName}.";

                //Put code here Either switch based on zone id or dungeon id

                if (ZoneMappings.ContainsKey(WorldManager.ZoneId))
                {
                    if (PluginManager.Plugins.Any(p => p.Plugin.Name == "SideStep" && !p.Enabled))
                    {
                        PluginManager.Plugins.First(p => p.Plugin.Name == "SideStep" && !p.Enabled).Enabled = true;
                    }
                    
                    await RunProfile(ZoneMappings[WorldManager.ZoneId]);
                }
                else
                {
                    Log.Information($"Don't have profile for zone {WorldManager.ZoneId}");
                }
                

            }




            return false;
        }
        
        public static async Task RunProfile(string profileName)
        {
            var absolutePath = Path.GetFullPath(Path.Combine(FolderPath, profileName));

            if (!File.Exists(absolutePath))
            {
                Log.Information($"You done fucked up and {absolutePath} doesn't exist");
                return;
            }
            
            await OrderbotHelper.CallOrderbot(absolutePath);
        }
        
    }
}