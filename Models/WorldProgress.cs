using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace WorldMiner.Models
{
    internal class WorldProgress : ReactiveObject
    {
        private int total_regions;
        private int regions_done;
        private int chunks_done;
        private int books_found;
        private int signs_found;

        public int TotalRegions
        {
            get => total_regions;
            set => this.RaiseAndSetIfChanged(ref total_regions, value);
        }

        public int RegionsDone
        {
            get => regions_done;
            set => this.RaiseAndSetIfChanged(ref regions_done, value);
        }

        public int ChunksDone
        {
            get => chunks_done;
            set => this.RaiseAndSetIfChanged(ref chunks_done, value);
        }

        public int BooksFound
        {
            get => books_found;
            set => this.RaiseAndSetIfChanged(ref books_found, value);
        }

        public int SignsFound
        {
            get => signs_found;
            set => this.RaiseAndSetIfChanged(ref signs_found, value);
        }


        public WorldProgress(int total_regions)
        {
            TotalRegions = total_regions;
        }
    }
}
