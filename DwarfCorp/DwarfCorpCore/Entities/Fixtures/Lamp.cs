﻿// Lamp.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Lamp : Body
    {
        public Lamp()
        {
        }

        public Lamp(Vector3 position) :
            base(
            "Lamp", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);

            var frames = new List<Point>
            {
                new Point(0, 1),
                new Point(2, 1),
                new Point(1, 1),
                new Point(2, 1)
            };
            var lampAnimation = new Animation(GameState.Game.GraphicsDevice,
                new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "Lamp", 32, 32, frames, true,
                Color.White, 3.0f, 1f, 1.0f, false);

            var sprite = new Sprite(PlayState.ComponentManager, "sprite", this, Matrix.Identity, spriteSheet, false)
            {
                LightsWithVoxels = false,
                OrientationType = Sprite.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            Tags.Add("Lamp");


            var voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }


            new LightEmitter("light", this, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 8)
            {
                HasMoved = true
            };
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}