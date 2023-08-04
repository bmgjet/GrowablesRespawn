using UnityEngine;
using System.Collections.Generic;
using CompanionServer.Handlers;
using ProtoBuf;

namespace Oxide.Plugins
{
    [Info("GrowablesRespawn", "bmgjet", "1.0.0")]
    [Description("Respawn the plant collectables placed in rust edit.")]

    public class GrowablesRespawn : RustPlugin
    {
        // --------- User Edit ---------
        //Respawn delay
        int Delay = 15; //Mins
        //Area to scan
        float Area = 0.01f; //Radius (Smaller = can have closer together but higher chance of spawning duplicates)
        //Debug
        byte Debug = 0; // 0 = No not console output, 1 = On Respawn console output, 2 = all debug output
        // --------- No Edit ---------
        /*
        *LIST of prefabs supported
        *
        assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/corn/corn-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/potato/potato-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/pumpkin/pumpkin-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/berry-yellow/berry-yellow-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/berry-white/berry-white-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/berry-red/berry-red-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/berry-green/berry-green-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/berry-blue/berry-blue-collectable.prefab
        assets/bundled/prefabs/autospawn/collectable/berry-black/berry-black-collectable.prefab
        */
        //Checker Timer
        Timer Checker;
        //List of collectables
        List<PrefabData> Collectables = new List<PrefabData>();
        //IDs of collectables
        uint[] collectableIds = { 3006540952, 3019211920, 726972295, 2251957318, 3056106441, 1989241797, 1378329388, 3306182606, 2764599810, 3408978181 };

        void OnServerInitialized()
        {
            //Read all prefabs in map file
            //Add to list if prefab is in collectableIds
            int collectablesFound = 0;
            foreach (PrefabData pd in World.Serialization.world.prefabs)
            {
                if (collectableIds.Contains(pd.id))
                {
                    Collectables.Add(pd);
                    collectablesFound++;
                }
            }
            Puts("Server found " + collectablesFound.ToString() + " respawnable collectables in the map file.");
            //Check collectables are spawned
            Check();
            //Set up timer to keep checking
            Rechecker();
        }

        void Check()
        {
            if (Debug > 1) Puts("Checking respawnable collectables");
            //Hold list of those needing respawn so all areas can be scanned first with out overlap worrys.
            List<PrefabData> NeedsRespawn = new List<PrefabData>();
            //Check each collectable in the list
            foreach (PrefabData collect in Collectables)
            {
                //Scan area it should be
                CollectibleEntity plant = FindCollectables(collect.position, Area);
                //None found there so spawn
                if (plant == null)
                {
                    NeedsRespawn.Add(collect);
                }
                else
                {
                    if (plant.isSpawned)
                    {
                        //Do nothing
                        if (Debug > 1) Puts("Still Alive at " + collect.position.x.ToString("#.##") + " " + collect.position.y.ToString("#.##") + " " + collect.position.z.ToString("#.##"));
                    }
                    else
                    {
                        NeedsRespawn.Add(collect);
                    }
                }
            }
            foreach (PrefabData respawn in NeedsRespawn)
            {
                Respawn(respawn);
            }
            if (Debug > 0) Puts("Respawned " + NeedsRespawn.Count.ToString() + " collectables");
        }

        void Respawn(PrefabData pd)
        {
            if (Debug > 1) Puts("Respawning at" + pd.position.x.ToString("#.##") + " " + pd.position.y.ToString("#.##") + " " + pd.position.z.ToString("#.##"));
            //Create new Collectable
            CollectibleEntity replacement = GameManager.server.CreateEntity(StringPool.Get(pd.id), pd.position, pd.rotation) as CollectibleEntity;
            if (replacement == null) return;
            replacement.Spawn();
            replacement.transform.position = pd.position;
            replacement.transform.rotation = pd.rotation;
            replacement.SendNetworkUpdateImmediate(true);
        }

        void Rechecker()
        {
            //Timer to recheck if plants still exsist
            Checker = timer.Every(Delay * 60, () =>
             {
                 Check();
             }
            );
        }

        CollectibleEntity FindCollectables(Vector3 pos, float radius)
        {
            //Casts a sphere at given position and find all collectables
            var hits = Physics.SphereCastAll(pos, radius, Vector3.one);
            var x = new List<CollectibleEntity>();
            foreach (var hit in hits)
            {
                var entity = hit.GetEntity()?.GetComponent<CollectibleEntity>();
                if (entity && !x.Contains(entity) && entity.PrefabName.Contains("collectable"))
                    x.Add(entity);
            }
            if (x.Count == 0) return null;
            return x[0];
        }

        [ConsoleCommand("collectables.respawn")]
        void Reload(ConsoleSystem.Arg arg)
        {
            //Manually trigger recheck
            if (arg.IsAdmin)
            {
                Check();
                if (Checker != null)
                {
                    Checker.Destroy();
                }
                Rechecker();
            }
        }
    }
}