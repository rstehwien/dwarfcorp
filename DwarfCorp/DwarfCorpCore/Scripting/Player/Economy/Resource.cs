﻿// Resource.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     A resource is a kind of item that can be bought or sold, and can be used
    ///     to build things.
    /// </summary>
    public class Resource
    {
        public enum ResourceTags
        {
            Edible,
            Material,
            HardMaterial,
            Precious,
            Flammable,
            SelfIlluminating,
            Wood,
            Metal,
            Stone,
            Fuel,
            Magical,
            Soil,
            Grain,
            Fungus,
            None,
            AnimalProduct,
            Meat,
            Gem,
            Craft,
            Encrustable,
            Alcohol,
            Brewable,
            Bakeable,
            RawFood,
            PreparedFood,
            Plantable,
            AboveGroundPlant,
            BelowGroundPlant,
            Bone
        }

        private string shortName;

        public Resource()
        {
            PlantToGenerate = "";
        }

        public Resource(Resource other)
        {
            Type = other.Type;
            MoneyValue = other.MoneyValue;
            Description = new string(other.Description.ToCharArray());
            Image = other.Image;
            Tint = other.Tint;
            Tags = new List<ResourceTags>();
            Tags.AddRange(other.Tags);
            FoodContent = other.FoodContent;
            ShortName = other.ShortName;
            PlantToGenerate = other.PlantToGenerate;
        }

        public Resource(ResourceLibrary.ResourceType type, float money, string description, NamedImageFrame image,
            Color tint, params ResourceTags[] tags)
        {
            Type = type;
            MoneyValue = money;
            Description = description;
            Image = image;
            Tint = tint;
            Tags = new List<ResourceTags>();
            Tags.AddRange(tags);
            FoodContent = 0;
        }

        public ResourceLibrary.ResourceType Type { get; set; }

        public string ResourceName
        {
            get { return Type; }
        }

        public float MoneyValue { get; set; }
        public string Description { get; set; }
        public NamedImageFrame Image { get; set; }
        public List<ResourceTags> Tags { get; set; }
        public float FoodContent { get; set; }

        public bool SelfIlluminating
        {
            get { return Tags.Contains(ResourceTags.SelfIlluminating); }
        }

        public bool IsFlammable
        {
            get { return Tags.Contains(ResourceTags.Flammable); }
        }

        public string ShortName
        {
            get
            {
                if (shortName == null) return ResourceName;
                return shortName;
            }
            set { shortName = value; }
        }

        public string PlantToGenerate { get; set; }

        public Color Tint { get; set; }

        public string GetTagDescription(string delimiter)
        {
            string s = "";

            for (int i = 0; i < Tags.Count; i++)
            {
                string tag = Tags[i].ToString();
                s += tag;

                if (i < Tags.Count - 1)
                {
                    s += delimiter;
                }
            }

            return s;
        }
    }
}