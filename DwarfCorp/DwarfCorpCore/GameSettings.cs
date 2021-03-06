﻿// GameSettings.cs
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
using System.IO;

namespace DwarfCorp
{
    public class GameSettings
    {
        public static Settings Default { get; set; }

        public static void Reset()
        {
            Default = new Settings();
        }

        public static void Save()
        {
            Save(ContentPaths.settings);
        }

        public static void Load()
        {
            Load(ContentPaths.settings);
        }

        public static void Save(string file)
        {
            try
            {
                FileUtils.SaveBasicJson(Default, file);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Failed to save settings: {0}", exception);
                if (exception.InnerException != null)
                {
                    Console.Error.WriteLine("Inner exception: {0}", exception.InnerException);
                }
            }
        }

        public static void Load(string file)
        {
            try
            {
                Default = FileUtils.LoadJson<Settings>(file, false);
            }
            catch (FileNotFoundException fileLoad)
            {
                Console.Error.WriteLine("Settings file does not exist. Using default settings.");
                Default = new Settings();
                Save();
            }
            catch (Exception otherException)
            {
                Console.Error.WriteLine("Failed to load settings file {0} : {1}", file, otherException);
                if (otherException.InnerException != null)
                {
                    Console.Error.WriteLine("Inner exception: {0}", otherException.InnerException);
                }
                Default = new Settings();
                Save();
            }
        }

        public class Settings
        {
            public bool AmbientOcclusion = true;
            public int AntiAliasing = 16;
            public bool CalculateRamps = true;
            public bool CalculateSunlight = true;
            public float CameraScrollSpeed = 10.0f;
            public float CameraZoomSpeed = 0.5f;
            public float ChunkDrawDistance = 100;
            public float ChunkGenerateDistance = 80.0f;
            public float ChunkGenerateTime = 0.5f;
            public int ChunkHeight = 48;
            public float ChunkRebuildTime = 0.5f;
            public float ChunkUnloadDistance = 250.0f;
            public int ChunkWidth = 24;
            public bool CursorLightEnabled = true;
            public bool DisplayIntro = true;
            public bool DrawChunksReflected = true;
            public bool DrawDebugData = false;
            public bool DrawEntityReflected = true;
            public bool DrawSkyReflected = true;
            public bool EnableAIDebugger = false;
            public bool EnableEdgeScroll = false;
            public bool EnableGlow = true;
            public bool EntityLighting = true;
            public bool FogofWar = true;
            public bool Fullscreen = false;
            public bool GrassMotes = true;
            public bool InvertZoom = false;
            public float MasterVolume = 1.0f;
            public int MaxChunks = 1000;
            public float MusicVolume = 0.2f;
            public int NumMotes = 1;
            public bool ParticlePhysics = true;
            public int ResolutionX = 1280;
            public int ResolutionY = 720;
            public bool SelfIlluminationEnabled = true;
            public float SoundEffectVolume = 1.0f;
            public bool UseDynamicShadows = false;
            public bool UseLightmaps = false;
            public float VertexCullDistance = 80;
            public float VisibilityUpdateTime = 0.1f;
            public float WorldScale = 2.0f;
        }
    }
}