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
    }
}