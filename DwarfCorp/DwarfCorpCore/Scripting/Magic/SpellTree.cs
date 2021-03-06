﻿// SpellTree.cs
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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class SpellTree
    {
        public SpellTree()
        {
            RootSpells = new List<Node>();
            Mana = 100.0f;
            MaxMana = 100.0f;
        }

        public List<Node> RootSpells { get; set; }
        public float Mana { get; set; }
        public float MaxMana { get; set; }


        public bool CanCast(Spell spell)
        {
            return spell.ManaCost <= Mana;
        }

        public void Recharge(float amount)
        {
            Mana = Math.Min(Mana + amount, MaxMana);
        }

        public void UseMagic(float amount)
        {
            Mana = Math.Max(Mana - amount, 0.0f);
        }

        public List<Spell> GetKnownSpells()
        {
            var toReturn = new List<Spell>();
            foreach (Node node in RootSpells)
            {
                node.GetKnownSpellsRecursive(toReturn);
            }

            return toReturn;
        }

        [JsonObject(IsReference = true)]
        public class Node
        {
            public Node()
            {
                Spell = null;
                Children = new List<Node>();
                ResearchTime = 0.0f;
                ResearchProgress = 0.0f;
            }

            public Spell Spell { get; set; }
            public float ResearchTime { get; set; }
            public float ResearchProgress { get; set; }
            public List<Node> Children { get; set; }
            public Node Parent { get; set; }

            public bool IsResearched
            {
                get { return ResearchProgress >= ResearchTime; }
            }


            public void GetKnownSpellsRecursive(List<Spell> spells)
            {
                if (ResearchProgress >= ResearchTime)
                {
                    spells.Add(Spell);
                }

                foreach (Node node in Children)
                {
                    node.GetKnownSpellsRecursive(spells);
                }
            }

            public void SetupParentsRecursive()
            {
                foreach (Node node in Children)
                {
                    node.Parent = this;
                    node.SetupParentsRecursive();
                }
            }
        }
    }
}