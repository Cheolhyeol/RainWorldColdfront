using BepInEx;
using MoreSlugcats;
using Noise;
using RWCustom;
using SlugBase.SaveData;
using System;
using UnityEngine;

namespace cheolsVesselSlugcat
{
    [BepInPlugin(MOD_ID, "The Vessel", "0.1.0")]
    class CheolVesselPlugin : BaseUnityPlugin
    {
        private const string MOD_ID = "cheol.thevessel";

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources); // load atlases

            On.Player.Die += explodeDeath; // singularity bomb explosion on death
            On.Player.GrabUpdate += vesselGrabUpdate; // enable crafting without IL hooks
            On.Player.SpitUpCraftedObject += vesselCraftResults; // the results of crafting
            Spawn.Hook();
            On.PlayerGraphics.Update += craftingGraphicsAnimation;
        }

        private void craftingGraphicsAnimation(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            if (self?.player?.slugcatStats?.name != null &&
                self.player.slugcatStats.name.value == "thevessel")
            {
                if (self.player.GetSeolVessel().craftingCounter > 15 && self.player.GetSeolVessel().craftingCounter % 10 == 0)
                {
                    self.blink = Math.Max(self.blink, UnityEngine.Random.Range(-5, 8));
                }
                else if (self.player.GetSeolVessel().craftingCounter > 0)
                {
                    if (self.player.GetSeolVessel().craftingCounter > 30)
                    {
                        self.blink = 5;
                    }
                    float num12 = Mathf.InverseLerp(0f, 110f, (float)self.player.GetSeolVessel().craftingCounter);
                    float num13 = (float)self.player.GetSeolVessel().craftingCounter / Mathf.Lerp(30f, 15f, num12);
                    if (self.player.standing)
                    {
                        self.drawPositions[0, 0].y += Mathf.Sin(num13 * 3.1415927f * 2f) * num12 * 2f;
                        self.drawPositions[1, 0].y += -Mathf.Sin((num13 + 0.2f) * 3.1415927f * 2f) * num12 * 3f;
                    }
                    else
                    {
                        self.drawPositions[0, 0].y += Mathf.Sin(num13 * 3.1415927f * 2f) * num12 * 3f;
                        self.drawPositions[0, 0].x += Mathf.Cos(num13 * 3.1415927f * 2f) * num12 * 1f;
                        self.drawPositions[1, 0].y += Mathf.Sin((num13 + 0.2f) * 3.1415927f * 2f) * num12 * 2f;
                        self.drawPositions[1, 0].x += -Mathf.Cos(num13 * 3.1415927f * 2f) * num12 * 3f;
                    }
                }
            }
            orig(self);
        }

        private void vesselCraftResults(On.Player.orig_SpitUpCraftedObject orig, Player self)
        {
            if (self?.slugcatStats?.name != null &&
                self.slugcatStats.name.value == "thevessel")
            {
                self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
                for (int i = 0; i < self.grasps.Length; i++)
                {
                    if (self.grasps[i] != null)
                    {
                        AbstractPhysicalObject abstractPhysicalObject = self.grasps[i].grabbed.abstractPhysicalObject;
                        if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear &&
                            !(abstractPhysicalObject as AbstractSpear).electric)
                        {
                            if ((abstractPhysicalObject as AbstractSpear).explosive)
                            {
                                Vector2 vector = Vector2.Lerp(self.firstChunk.pos, self.firstChunk.lastPos, 0.35f);
                                self.room.AddObject(new SootMark(self.room, vector, 80f, true));
                                self.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7,
                                    new Color(1f, 0.4f, 0.3f) // < can be custom
                                    ));
                                self.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                                self.room.AddObject(new ExplosionSpikes(self.room, vector, 14, 30f, 9f, 7f, 170f,
                                    new Color(1f, 0.4f, 0.3f) // can be custom
                                    ));
                                self.room.AddObject(new ShockWave(vector, 330f, 0.045f, 5, false));

                                self.Stun(200);
                                return;
                            }

                            self.ReleaseGrasp(i);
                            abstractPhysicalObject.realizedObject.RemoveFromRoom();
                            self.room.abstractRoom.RemoveEntity(abstractPhysicalObject);
                            self.SubtractFood(1);
                            AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null,
                                self.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                            self.room.abstractRoom.AddEntity(abstractSpear);
                            abstractSpear.RealizeInRoom();
                            if (self.FreeHand() != -1)
                            {
                                self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                            }
                            return;
                        }
                    }
                }
            }
            else
            {
                orig(self);
            }
        }

        private void vesselGrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self?.slugcatStats?.name != null &&
                self.slugcatStats.name.value == "thevessel")
            {
                bool flag = ((self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw) ||
                (ModManager.MMF && self.input[0].x == 0 && self.input[0].y == 1 && !self.input[0].jmp && !self.input[0].thrw &&
                (self.bodyMode != Player.BodyModeIndex.ClimbingOnBeam || self.animation == Player.AnimationIndex.BeamTip ||
                self.animation == Player.AnimationIndex.StandOnBeam))) &&
                (self.mainBodyChunk.submersion < 0.5f);
                if (flag && self.input[0].pckp && CustomCrafting(self) != null)
                {
                    self.GetSeolVessel().isCrafting = true;
                    self.GetSeolVessel().craftingCounter++;

                    if (self.GetSeolVessel().craftingCounter > 105)
                    {
                        self.SpitUpCraftedObject();
                        self.GetSeolVessel().craftingCounter = 0;
                    }
                }
            }

        }

        private AbstractPhysicalObject.AbstractObjectType CustomCrafting(Player self)
        {
            if (self?.slugcatStats?.name != null && self.FoodInStomach > 0 &&
                self.slugcatStats.name.value == "thevessel")
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

            return null;
        }


        private void LoadResources(RainWorld rainWorld)
        {
            // sprites could be loaded via this in the future
        }

        private void explodeDeath(On.Player.orig_Die orig, Player self)
        {
            bool alreadyDead = self.dead; // makes sure the explosion happens only once

            orig(self);

            if (self?.slugcatStats?.name != null &&
                !alreadyDead && self.dead &&
                self.slugcatStats.name.value == "thevessel")
            {
                // explodes into a singularity bomb
                #region Singularity Explosion
                Vector2 vector = Vector2.Lerp(self.firstChunk.pos, self.firstChunk.lastPos, 0.35f);
                self.room.AddObject(new SingularityBomb.SparkFlash(self.firstChunk.pos, 300f, new Color(0f, 0f, 1f)));
                self.room.AddObject(new Explosion(self.room, self, vector, 7, 450f, 6.2f, 10f, 280f, 0.25f, self, 0.3f, 160f, 1f));
                self.room.AddObject(new Explosion(self.room, self, vector, 7, 2000f, 4f, 0f, 400f, 0.25f, self, 0.3f, 200f, 1f));
                self.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7,
                    new Color(1f, 0.4f, 0.3f)
                    ));
                self.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                self.room.AddObject(new Explosion.ExplosionLight(vector, 2000f, 2f, 60,
                    new Color(1f, 0.4f, 0.3f)
                    ));
                // new Color(1f, 0.4f, 0.3f) can be replaced with any colour you want this to have
                self.room.AddObject(new ShockWave(vector, 350f, 0.485f, 300, true));
                self.room.AddObject(new ShockWave(vector, 2000f, 0.185f, 180, false));
                for (int i = 0; i < 25; i++)
                {
                    Vector2 vector2 = Custom.RNV();
                    if (self.room.GetTile(vector + vector2 * 20f).Solid)
                    {
                        if (!self.room.GetTile(vector - vector2 * 20f).Solid)
                        {
                            vector2 *= -1f;
                        }
                        else
                        {
                            vector2 = Custom.RNV();
                        }
                    }
                    for (int j = 0; j < 3; j++)
                    {
                        self.room.AddObject(new Spark(vector + vector2 * Mathf.Lerp(30f, 60f,
                            UnityEngine.Random.value), vector2 * Mathf.Lerp(7f, 38f, UnityEngine.Random.value) +
                            Custom.RNV() * 20f * UnityEngine.Random.value, Color.Lerp(new Color(1f, 0.4f, 0.3f),
                            // this is usually self.explodeColor. i set it to the default blue, but the colour just before this
                            // could be set to anything you want
                            new Color(1f, 1f, 1f),
                            UnityEngine.Random.value), null, 11, 28));
                    }
                    self.room.AddObject(new Explosion.FlashingSmoke(vector + vector2 * 40f *
                        UnityEngine.Random.value, vector2 * Mathf.Lerp(4f, 20f, Mathf.Pow(UnityEngine.Random.value, 2f)),
                        1f + 0.05f * UnityEngine.Random.value, new Color(1f, 1f, 1f), new Color(1f, 0.4f, 0.3f),
                        // self.explodeColor again
                        UnityEngine.Random.Range(3, 11)));
                }
                for (int k = 0; k < 6; k++)
                {
                    self.room.AddObject(new SingularityBomb.BombFragment(vector, Custom.DegToVec(((float)k +
                        UnityEngine.Random.value) / 6f * 360f) * Mathf.Lerp(18f, 38f, UnityEngine.Random.value)));
                }
                self.room.ScreenMovement(new Vector2?(vector), default(Vector2), 0.9f);
                self.room.PlaySound(SoundID.Bomb_Explode, self.firstChunk);
                self.room.InGameNoise(new InGameNoise(vector, 9000f, self, 1f));
                for (int m = 0; m < self.room.physicalObjects.Length; m++)
                {
                    for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                    {
                        if (self.room.physicalObjects[m][n].abstractPhysicalObject.rippleLayer ==
                            self.abstractPhysicalObject.rippleLayer ||
                            self.room.physicalObjects[m][n].abstractPhysicalObject.rippleBothSides ||
                            self.abstractPhysicalObject.rippleBothSides)
                        {
                            if (self.room.physicalObjects[m][n] is Creature &&
                                Custom.Dist(self.room.physicalObjects[m][n].firstChunk.pos, self.firstChunk.pos) < 350f)
                            {
                                if (self != null)
                                {
                                    (self.room.physicalObjects[m][n] as Creature).killTag = self.abstractCreature;
                                }
                                (self.room.physicalObjects[m][n] as Creature).Die();
                            }
                            if (self.room.physicalObjects[m][n] is ElectricSpear)
                            {
                                if ((self.room.physicalObjects[m][n] as ElectricSpear).abstractSpear.electricCharge == 0)
                                {
                                    (self.room.physicalObjects[m][n] as ElectricSpear).Recharge();
                                }
                                else
                                {
                                    (self.room.physicalObjects[m][n] as ElectricSpear).ExplosiveShortCircuit();
                                }
                            }
                        }
                    }
                }

                // for causing creatures to be afraid of the explosion
                var scareObj = new FirecrackerPlant.ScareObject(self.firstChunk.pos, self.abstractPhysicalObject.rippleLayer);
                scareObj.fearRange = 8000f;
                scareObj.fearScavs = true;
                self.room.AddObject(scareObj);
                scareObj.lifeTime = -600;
                scareObj.fearRange = 12000f;
                self.room.InGameNoise(new InGameNoise(self.firstChunk.pos, 12000f, self, 1f));
                // end explosion fear
                #endregion
            }
        }


        // add custom tile spawn
    }


    public static class Spawn
    {
        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
        }
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.room is Room playerRoom
                && playerRoom.game.IsStorySession
                && playerRoom.game.GetStorySession.saveState is SaveState save
                && !save.GetTeleportationDone())
            {

                InitializeTargetRoomID(playerRoom);

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

        const string uniqueprefix = "vesselmod";

        private const string teleportationDone = uniqueprefix + "TeleportationDone";

        public static bool GetTeleportationDone(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(teleportationDone, out bool done) && done;
        public static void SetTeleportationDone(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(teleportationDone, value);
    }
}