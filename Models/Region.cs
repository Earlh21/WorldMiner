using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WorldMiner.Models
{
    internal class Region
    {
        public Chunk?[,] Chunks { get; set; }
        public int X { get; set; }
        public int Z { get; set; }

        public Region(Chunk?[,] chunks, int x, int z)
        {
            Chunks = chunks;
            X = x;
            Z = z;
        }

        public static async Task<Region> LoadFromFileAsync(string path)
        {
            var data = await File.ReadAllBytesAsync(path);

            var chunks = new Chunk?[32, 32];

            string filename = Path.GetFileNameWithoutExtension(path);
            string[] parts = filename.Split('.');
            int region_x_offset = Convert.ToInt32(parts[1]);
            int region_z_offset = Convert.ToInt32(parts[2]);

            //32 chunks per region, 16 blocks per chunk
            var region = new Region(chunks, region_x_offset * 32 * 16, region_z_offset * 32 * 16);

            //Some region files don't even contain the full header for some reason
            if (data.Length < 8192)
            {
                return region;
            }

            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    chunks[x, z] = Chunk.LoadFromRegion(region, data, x, z, path);
                }
            }

            return region;
        }
    }
}
