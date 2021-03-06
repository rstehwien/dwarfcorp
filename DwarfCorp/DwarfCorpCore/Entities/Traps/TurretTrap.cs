﻿using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class TurretTrap : Body
    {
        private CreatureAI closestCreature;
        private Vector3 offset = Vector3.Zero;

        public TurretTrap()
        {
        }

        public TurretTrap(Vector3 position, Faction faction) :
            base("TurretTrap", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position),
                new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, true)
        {
            Allies = faction;
            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32);
            Weapon = new Attack("BowAttack", 5.0f, 1.0f, 5.0f, ContentPaths.Audio.trap, ContentPaths.Effects.pierce)
            {
                ProjectileType = "Arrow",
                Mode = Attack.AttackMode.Ranged,
                LaunchSpeed = 15
            };
            var deathParticles = new ParticleTrigger("puff", PlayState.ComponentManager, "DeathParticles", this,
                Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                SoundToPlay = ""
            };
            var health = new Health(PlayState.ComponentManager, "health", this, 50.0f, 0.0f, 50.0f);
            Sensor = new EnemySensor(PlayState.ComponentManager, "sensor", this, Matrix.Identity, new Vector3(8, 8, 8),
                Vector3.Zero)
            {
                Allies = faction
            };
            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
            BaseSprite = new Fixture(Vector3.Zero, spriteSheet, new Point(2, 7), this);
            BaseSprite.Sprite.OrientationType = Sprite.OrientMode.YAxis;
            TurretSprite = new Fixture(Vector3.Up*0.25f, spriteSheet, new Point(1, 7), this);
            TurretSprite.Sprite.OrientationType = Sprite.OrientMode.Fixed;
            SetTurretAngle(0.0f);
        }

        public Attack Weapon { get; set; }
        public Fixture BaseSprite { get; set; }
        public Fixture TurretSprite { get; set; }
        public Faction Allies { get; set; }
        public EnemySensor Sensor { get; set; }

        private void Sensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            closestCreature = null;
            float minDist = float.MaxValue;
            foreach (CreatureAI enemy in enemies)
            {
                float dist = (enemy.Position - Position).LengthSquared();

                if (dist < minDist)
                {
                    offset = enemy.Position - Position;
                    minDist = dist;
                    closestCreature = enemy;
                }
            }
        }

        public void SetTurretAngle(float radians)
        {
            Matrix turretTransform = Matrix.CreateRotationX(1.57f)*Matrix.CreateRotationY((radians));
            turretTransform.Translation = Vector3.Up*0.25f;
            TurretSprite.LocalTransform = turretTransform;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (closestCreature != null && !closestCreature.IsDead)
            {
                Weapon.RechargeTimer.Update(gameTime);

                SetTurretAngle((float) Math.Atan2(offset.X, offset.Z) + (float) Math.PI*0.5f);

                if (Weapon.RechargeTimer.HasTriggered)
                {
                    closestCreature.Kill(this);
                    Weapon.LaunchProjectile(Position + Vector3.Up*0.5f, closestCreature.Position,
                        closestCreature.Physics);
                    Weapon.PlayNoise(Position);
                }
            }
            base.Update(gameTime, chunks, camera);
        }
    }
}