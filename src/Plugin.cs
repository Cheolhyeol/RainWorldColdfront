using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using MoreSlugcats;

namespace SlugTemplate
{
    [BepInPlugin(MOD_ID, "Slugcat Template", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "author.slugtemplate";

        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("slugtemplate/super_jump");
        public static readonly PlayerFeature<bool> ExplodeOnDeath = PlayerBool("slugtemplate/explode_on_death");
        public static readonly GameFeature<float> MeanLizards = GameFloat("slugtemplate/mean_lizards");


        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Player.Jump += Player_Jump;
            On.Player.Die += Player_Die;
            On.Lizard.ctor += Lizard_ctor;
            On.Player.ctor += Player_ctor;
        }

        private AbstractPhysicalObject.AbstractObjectType CustomCrafting(Player self)
        {
            if (self.slugcatStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                if (self.FoodInStomach > 0)
                {
                    var grasps = self.grasps;
                    for (int i = 0; i < grasps.Length; i++)
                    {
                        if (grasps[i] != null && grasps[i].grabbed is IPlayerEdible edible && edible.Edible)
                        {
                            return null;
                        }
                    }
                    if (grasps[0] != null && grasps[0].grabbed is Spear spear0 && !spear0.abstractSpear.electric)
                    {
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }
                    if (grasps[0] == null && grasps[1] != null && grasps[1].grabbed is Spear spear1 && !spear1.abstractSpear.explosive && self.objectInStomach == null)
                    {
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }
                }
            }

            return GourmandCombos.CraftingResults_ObjectData(self.grasps[0], self.grasps[1], true);
        }


        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }

        // Implement MeanLizards
        private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (MeanLizards.TryGet(world.game, out float meanness))
            {
                self.spawnDataEvil = Mathf.Min(self.spawnDataEvil, meanness);
            }
        }


        // Implement SuperJump
        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);

            if (SuperJump.TryGet(self, out var power))
            {
                self.jumpBoost *= 1f + power;
            }
        }

        // Implement ExlodeOnDeath
        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool wasDead = self.dead;

            orig(self);

            if (!wasDead && self.dead
                && ExplodeOnDeath.TryGet(self, out bool explode)
                && explode)
            {
                // Adapted from ScavengerBomb.Explode
                var room = self.room;
                var pos = self.mainBodyChunk.pos;
                var color = self.ShortCutColor();
                room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
                room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
                room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));

                room.ScreenMovement(pos, default, 1.3f);
                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
            }
        }

        // Add custom spawn tile

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.room is Room playerRoom
                && playerRoom.game.IsStorySession
                && playerRoom.game.GetStorySession.saveState is SaveState save
                && !save.GetTeleportationDone())
            {
                if (playerRoom.game.IsVoidStoryCampaign())
                {
                    InitializeTargetRoomID(playerRoom);
                }

                int currentRoomIndex = self.abstractCreature.pos.room;

                if (currentRoomIndex == NewSpawnPoint.room)
                {
                    save.SetTeleportationDone(true);
                    self.abstractCreature.pos = NewSpawnPoint;
                    Vector2 newPosition = self.room.MiddleOfTile(NewSpawnPoint.x, NewSpawnPoint.y);
                    Array.ForEach(self.bodyChunks, x => x.pos = newPosition);
                    self.standing = true;
                    self.animation = Player.AnimationIndex.StandUp;
                }
            }
        }

        private static int targetRoomID = -1;
        static WorldCoordinate NewSpawnPoint
        {
            get
            {
                if (targetRoomID == -1) throw new Exception("Target room ID is not initialized!");
                return new WorldCoordinate(targetRoomID, originalSpawnPoint.x, originalSpawnPoint.y, originalSpawnPoint.abstractNode);
            }
        }

        private static readonly WorldCoordinate originalSpawnPoint = new WorldCoordinate(-1, 47, 30, 0);

        static void InitializeTargetRoomID(Room room)
        {
            if (targetRoomID == -1)
            {
                AbstractRoom targetRoom = room.world.GetAbstractRoom("CML_VESSELSPAWN") ?? throw new Exception($"Room 'CML_VESSELSPAWN' does not exist.");
                targetRoomID = targetRoom.index;
            }
        }

        private const string teleportationDone = uniqueprefix + "TeleportationDone";

        public static bool GetTeleportationDone(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(teleportationDone, out bool done) && done;
        public static void SetTeleportationDone(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(teleportationDone, value);
    }
}