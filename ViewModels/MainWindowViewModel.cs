using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ReactiveUI;
using WorldMiner.Models;
using System.Threading;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using WorldMiner.Views;

namespace WorldMiner.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private string world_path = "";
        private string output_path = "";
        
        private bool is_json_checked;
        private bool is_human_checked = true;
        
        private string? world_validation_message = null;
        private string? output_validation_message = null;

        private CancellationTokenSource? mining_cts = null;
        private bool save_cancelled;

        private bool is_mining;
        private bool is_cancelling;
        
        private WorldProgress world_progress = new(10);

        public string WorldPath
        {
            get => world_path;
            set => this.RaiseAndSetIfChanged(ref world_path, value);
        }

        public string OutputPath
        {
            get => output_path;
            set => this.RaiseAndSetIfChanged(ref output_path, value);
        }

        public string? WorldValidationMessage
        {
            get => world_validation_message;
            set => this.RaiseAndSetIfChanged(ref world_validation_message, value);
        }

        public string? OutputValidationMessage
        {
            get => output_validation_message;
            set => this.RaiseAndSetIfChanged(ref output_validation_message, value);
        }

        public bool IsMining
        {
            get => is_mining;
            private set => this.RaiseAndSetIfChanged(ref is_mining, value);
        }

        public bool IsJsonChecked
        {
            get => is_json_checked;
            set => this.RaiseAndSetIfChanged(ref is_json_checked, value);
        }

        public bool IsHumanChecked
        {
            get => is_human_checked;
            set => this.RaiseAndSetIfChanged(ref is_human_checked, value);
        }

        public WorldProgress WorldProgress
        {
            get => world_progress;
            set => this.RaiseAndSetIfChanged(ref world_progress, value);
        }

        public bool IsCancelling
        {
            get => is_cancelling;   
            set => this.RaiseAndSetIfChanged(ref is_cancelling, value);
        }

        public ReactiveCommand<Unit, Unit> Mine { get; }
        public ReactiveCommand<Unit, Unit> Stop { get; }
        public ReactiveCommand<Unit, Unit> Cancel { get; }
        public ReactiveCommand<Unit, Unit> BrowseWorldPath { get; }
        public ReactiveCommand<Unit, Unit> BrowseOutputPath { get; }

        public MainWindowViewModel()
        {
            Mine = ReactiveCommand.CreateFromTask(async () =>
            {
                WorldValidationMessage = null;
                OutputValidationMessage = null;
                
                if (!Directory.Exists(WorldPath))
                {
                    WorldValidationMessage = "World path doesn't exist.";
                    return;
                }

                if (!Directory.Exists(Path.Combine(WorldPath, "region")))
                {
                    WorldValidationMessage = "World path doesn't contain a region directory.";
                }

                if (!Directory.Exists(OutputPath))
                {
                    try
                    {
                        Directory.CreateDirectory(OutputPath);
                    }
                    catch (Exception)
                    {
                        OutputValidationMessage = "Output path is invalid.";
                    }
                }

                if (OutputValidationMessage != null || WorldValidationMessage != null)
                {
                    return;
                }
                
                //Sort region files by descending size so that progress accelerates during scraping
                var region_file_infos =
                    Directory.GetFiles(Path.Combine(world_path, "region"))
                        .Select(path => new FileInfo(path))
                        .Where(file => file.Extension == ".mca" || file.Extension == ".mcr");
                
                var region_files =
                    region_file_infos.OrderByDescending(file_info => file_info.Length)
                        .Select(file_info => file_info.FullName).ToList();
                
                IsMining = true;
                save_cancelled = false;
                WorldProgress = new(region_files.Count);
                mining_cts = new();

                var results = await MineWorldAsync(region_files, mining_cts.Token);

                if (!save_cancelled)
                {
                    SaveBooksSigns(results.Item1, results.Item2);
                }

                if(!mining_cts.IsCancellationRequested)
                {
                    WorldProgress.RegionsDone = WorldProgress.TotalRegions;
                }

                mining_cts = null;
                IsMining = false;
                IsCancelling = false;
            }, this.WhenAnyValue(vm => vm.IsMining, vm => vm.WorldPath, vm => vm.OutputPath).Select(x =>
            {
                if (String.IsNullOrWhiteSpace(x.Item2))
                {
                    return false;
                }
                
                if (String.IsNullOrWhiteSpace(x.Item3))
                {
                    return false;
                }

                return !x.Item1;
            }));

            Stop = ReactiveCommand.Create(() =>
            {
                mining_cts?.Cancel();
                IsCancelling = true;

                return Unit.Default;
            }, this.WhenAnyValue(vm => vm.IsMining, vm => vm.IsCancelling).Select(x => IsMining && !IsCancelling));

            Cancel = ReactiveCommand.Create(() =>
            {
                mining_cts?.Cancel();
                IsCancelling = true;
                
                save_cancelled = true;

                return Unit.Default;
            }, this.WhenAnyValue(vm => vm.IsMining, vm => vm.IsCancelling).Select(x => IsMining && !IsCancelling));

            BrowseWorldPath = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(MainWindow.Instance);

                if (result != null)
                {
                    WorldPath = result;
                }
            }, this.WhenAnyValue(vm => vm.IsMining).Select(x => !x));
            
            BrowseOutputPath = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(MainWindow.Instance);

                if (result != null)
                {
                    OutputPath = result;
                }
            }, this.WhenAnyValue(vm => vm.IsMining).Select(x => !x));
        }

        private void SaveBooksSigns(IEnumerable<Book> books, IEnumerable<Sign> signs)
        {
            var books_path = Path.Combine(output_path, "books.txt");
            var signs_path = Path.Combine(output_path, "signs.txt");
                    
            if (IsJsonChecked)
            {
                File.WriteAllText(books_path,
                    JsonConvert.SerializeObject(books, Formatting.Indented));
                File.WriteAllText(signs_path,
                    JsonConvert.SerializeObject(signs, Formatting.Indented));
            }
            else
            {
                var book_strings = books.Select(book => book.ToString());
                var sign_strings = signs.Select(sign => sign.ToString());
                        
                File.WriteAllText(books_path, String.Join(Environment.NewLine + Environment.NewLine, book_strings));
                File.WriteAllText(signs_path,
                    String.Join(Environment.NewLine + Environment.NewLine, sign_strings));
            }
        }
        
        private async Task<Tuple<IEnumerable<Book>, IEnumerable<Sign>>> MineWorldAsync(List<string> region_paths, CancellationToken token)
        {
            var threads = Environment.ProcessorCount;
            var splits = Split(region_paths, threads);
            var results = new Tuple<List<Book>, List<Sign>>[threads];

            await Task.WhenAll(splits.Select((split, i) => Task.Run(async () =>
            {
                results[i] = await MiningWorkerAsync(split, token);
            })));
            
            var books = results.SelectMany(x => x?.Item1 ?? new List<Book>());
            var signs = results.SelectMany(x => x?.Item2 ?? new List<Sign>());
            
            return new (books, signs);
        }
        
        private async Task<Tuple<List<Book>, List<Sign>>> MiningWorkerAsync(IEnumerable<string> region_files, CancellationToken? cancel_token)
        {
            var books = new List<Book>();
            var signs = new List<Sign>();

            int regions_done = 0;
            int chunks_done = 0;
            int books_found = 0;
            int signs_found = 0;

            foreach (var region_file in region_files)
            {
                if (cancel_token?.IsCancellationRequested ?? false)
                {
                    return new Tuple<List<Book>, List<Sign>>(books, signs);
                }

                var region = await Region.LoadFromFileAsync(region_file);

                foreach (var chunk in region.Chunks)
                {
                    if (chunk == null)
                    {
                        continue;
                    }

                    chunks_done++;

                    var f_books = chunk.FindBooks();
                    var f_signs = chunk.FindSigns();

                    //Tracking books/signs like this to avoid evaluating the Enumerables (no ToList or Count)
                    books_found -= books.Count;
                    signs_found -= signs.Count;
                    
                    books.AddRange(f_books);
                    signs.AddRange(f_signs);

                    books_found += books.Count;
                    signs_found += signs.Count;
                }

                regions_done++;

                //Updating progress after every region seems to stop UI updates with lots of tiny regions and, somehow, result in incorrect counts despite the lock
                if (chunks_done > 1000 || regions_done > 500)
                {
                    lock (WorldProgress)
                    {
                        WorldProgress.RegionsDone += regions_done;
                        WorldProgress.ChunksDone += chunks_done;
                        WorldProgress.BooksFound += books_found;
                        WorldProgress.SignsFound += signs_found;

                        regions_done = 0;
                        chunks_done = 0;
                        books_found = 0;
                        signs_found = 0;
                    }
                }
            }

            lock (WorldProgress)
            {
                WorldProgress.RegionsDone += regions_done;
                WorldProgress.ChunksDone += chunks_done;
                WorldProgress.BooksFound += books_found;
                WorldProgress.SignsFound += signs_found;
            }

            return new Tuple<List<Book>, List<Sign>>(books, signs);
        }
        
        private static IEnumerable<IEnumerable<T>> Split<T>(IEnumerable<T> list, int parts)
        {
            return list.Select((item, index) => new { index, item })
                .GroupBy(x => x.index % parts)
                .Select(x => x.Select(y => y.item));
        }
    }
}
