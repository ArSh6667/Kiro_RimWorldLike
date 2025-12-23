using System;

namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// Perlin噪声生成器实现
    /// </summary>
    public class PerlinNoiseGenerator : INoiseGenerator
    {
        private readonly int[] _permutation;
        private readonly int[] _p;
        private Random _random;

        public PerlinNoiseGenerator(int seed = 0)
        {
            _random = new Random(seed);
            _permutation = new int[256];
            
            // 初始化排列表
            for (int i = 0; i < 256; i++)
            {
                _permutation[i] = i;
            }
            
            // 打乱排列表
            for (int i = 0; i < 256; i++)
            {
                int j = _random.Next(256);
                (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
            }
            
            // 复制排列表以避免边界检查
            _p = new int[512];
            for (int i = 0; i < 512; i++)
            {
                _p[i] = _permutation[i % 256];
            }
        }

        public void SetSeed(int seed)
        {
            _random = new Random(seed);
            
            // 重新初始化排列表
            for (int i = 0; i < 256; i++)
            {
                _permutation[i] = i;
            }
            
            for (int i = 0; i < 256; i++)
            {
                int j = _random.Next(256);
                (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
            }
            
            for (int i = 0; i < 512; i++)
            {
                _p[i] = _permutation[i % 256];
            }
        }
        public float[,] GenerateNoise(int width, int height, NoiseConfig config)
        {
            var noise = new float[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sampleX = (x + config.OffsetX) * config.Frequency;
                    float sampleY = (y + config.OffsetY) * config.Frequency;
                    
                    noise[x, y] = GenerateOctaveNoise(sampleX, sampleY, config);
                }
            }
            
            return noise;
        }

        public float GetNoiseValue(float x, float y)
        {
            return PerlinNoise(x, y);
        }

        private float GenerateOctaveNoise(float x, float y, NoiseConfig config)
        {
            float value = 0f;
            float amplitude = config.Amplitude;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < config.Octaves; i++)
            {
                value += PerlinNoise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                
                amplitude *= config.Persistence;
                frequency *= config.Lacunarity;
            }

            return value / maxValue; // 归一化到[0,1]
        }

        private float PerlinNoise(float x, float y)
        {
            // 找到单位立方体的坐标
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;

            // 找到相对坐标
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);

            // 计算淡化曲线
            float u = Fade(x);
            float v = Fade(y);

            // 哈希坐标
            int A = _p[X] + Y;
            int B = _p[X + 1] + Y;

            // 插值结果
            return Lerp(v, 
                Lerp(u, Grad(_p[A], x, y), Grad(_p[B], x - 1, y)),
                Lerp(u, Grad(_p[A + 1], x, y - 1), Grad(_p[B + 1], x - 1, y - 1)));
        }
        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}