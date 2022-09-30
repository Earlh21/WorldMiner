using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zlib;
using fNbt;

namespace WorldMiner.Models
{
    internal class Chunk
    {
        public Region Region { get; set; }
        public NbtFile NbtData { get; set; }

        public Chunk(Region region, NbtFile nbt_data)
        {
            Region = region;
            NbtData = nbt_data;
        }

        public static Chunk? LoadFromRegion(Region region, byte[] region_data, int x_offset, int z_offset, string path)
        {
            int header_location = (x_offset % 32 + (z_offset % 32) * 32) * 4;
            int data_location = IntFromBytes(region_data, header_location, 3) << 12;

            //Un-generated chunks are listed with location 0 in header
            if (data_location == 0)
            {
                return null;
            }

            if (data_location > region_data.Length - 5)
            {
                return null;
            }

            NbtFile nbt = new();

            try
            {
                using var stream = new MemoryStream(region_data);
                stream.Seek(data_location + 5, SeekOrigin.Begin);
                nbt.LoadFromStream(stream, NbtCompression.ZLib);
            }
            catch (Exception ex)
            {
                return null;
            }

            return new Chunk(region, nbt);
        }

        public IEnumerable<Sign> FindSigns()
        {
            return GetBlockEntities()
                       ?.Where(entity => entity["id"].StringValue is "minecraft:sign" or "Sign")
                       .Select(ReadSign)
                   ?? Enumerable.Empty<Sign>();
        }

        private static Sign ReadSign(NbtCompound entity)
        {
            int x = entity["x"].IntValue;
            int y = entity["y"].IntValue;
            int z = entity["z"].IntValue;

            var lines = Enumerable.Range(1, 4).Select(i => entity[$"Text{i}"].StringValue).ToArray();

            return new Sign(lines, x, y, z);
        }

        public IEnumerable<Book> FindBooks()
        {
            return GetBlockEntities()
                       ?.Where(entity => entity["id"].StringValue is "minecraft:chest" or "minecraft:trapped_chest"
                           or "minecraft:shulker_box" or "Chest" or "TrappedChest" or "ShulkerBox")
                       .Where(container => container.Get<NbtList>("Items") != null)
                       .SelectMany(container => 
                           FindBooks(
                               container.Get<NbtList>("Items"),
                               container["x"].IntValue,
                               container["y"].IntValue,
                               container["z"].IntValue)
                       )
                   ?? Enumerable.Empty<Book>();
        }

        //This helper function exists to check shulker boxes recursively, they're not tile entities when inside another container
        private static IEnumerable<Book> FindBooks(NbtList inventory, int x, int y, int z)
        {
            foreach (var item in inventory.ToArray<NbtCompound>())
            {
                var id = item["id"]?.StringValue;

                if (id is "minecraft:written_book" or "minecraft:writable_book" or "386" or "387")
                {
                    var book = ReadBook(item, x, y, z);

                    if (book != null)
                    {
                        yield return (Book)book;
                    }
                }

                if (id?.EndsWith("shulker_box") ?? false)
                {
                    var shulker_items = 
                        item.Get<NbtCompound>("tag")
                            ?.Get<NbtCompound>("BlockEntityTag")
                            ?.Get<NbtList>("Items");

                    if (shulker_items == null)
                    {
                        continue;
                    }
                    
                    foreach (var book in FindBooks(shulker_items, x, y, z))
                    {
                        yield return book;
                    }
                }
            }
        }

        private static Book? ReadBook(NbtCompound book, int x, int y, int z)
        {
            var tag = book.Get<NbtCompound>("tag");

            if (tag == null)
            {
                return null;
            }

            //Some books have no pages tag
            var pages = tag.Get<NbtList>("pages")?.Select(page => page.StringValue).ToArray() ?? Array.Empty<string>();
            var title = "null";
            var author = "null";

            //Only completed books (i.e. not book/quill) have a title/author
            if (book["id"]?.StringValue is "minecraft:written_book" or "387")
            {
                title = tag["title"].StringValue;
                author = tag["author"].StringValue;
            }

            return new Book(title, author, pages, x, y, z);
        }

        private static int IntFromBytes(byte[] data, int start, int length)
        {
            int val = 0;

            for (int i = start; i < start + length; i++)
            {
                val <<= 8;
                val |= data[i];
            }

            return val;
        }

        private NbtCompound[]? GetBlockEntities()
        {
            var root = (NbtCompound)NbtData.RootTag;

            return root.Get<NbtCompound>("Level")?.Get<NbtList>("TileEntities")?.ToArray<NbtCompound>()
                   ?? root.Get<NbtList>("block_entities")?.ToArray<NbtCompound>();
        }
    }
}