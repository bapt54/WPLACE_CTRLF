using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing.Text;
using System.Net;
using System.Net.Http;
using Spectre.Console;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PuppeteerSharp;
using static WPLACE_CTRLF.Program;
using System.Net.Configuration;
using System.Globalization;

namespace WPLACE_CTRLF
{
    public class Program
    {
        public class PixelInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public System.Drawing.Color Color { get; set; }
        }
        public class PaintedBy
        {
            public int id { get; set; }
            public string name { get; set; }
            public int allianceId { get; set; }
            public string allianceName { get; set; }
            public int equippedFlag { get; set; }
            public string discord { get; set; }
        }

        public class Region
        {
            public int id { get; set; }
            public int cityId { get; set; }
            public string name { get; set; }
            public int number { get; set; }
            public int countryId { get; set; }
        }

        public class PixelResponse
        {
            public PaintedBy paintedBy { get; set; }
            public Region region { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string link { get; set; }
            public int R { get; set; }
            public int V { get; set; }
            public int B { get; set; }
        }

        private static List<PixelResponse> _allPixels = new List<PixelResponse>();
        private static int zoneX = -1, zoneY = -1;
        static async Task Main(string[] args)
        {

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // empêche l’arrêt immédiat
                Console.WriteLine("\n--- Pixels enregistrés avant l'arrêt ---");

                foreach (var pixel in _allPixels)
                {
                    Console.WriteLine($"{pixel.X},{pixel.Y},{pixel.paintedBy?.id},{pixel.paintedBy?.name},{pixel.paintedBy?.allianceId},{pixel.paintedBy?.allianceName},{pixel.region?.id},{pixel.region?.name},{pixel.paintedBy?.discord},{pixel.R},{pixel.V},{pixel.B},{pixel.link}");
                }
                CSV();
                Environment.Exit(0);
            };
            int initdelai = 1100;
            int cptcgtdelai = 0;

            int xmin = 0, xmax = 999, ymin = 0, ymax = 999, maxConcurrency = 1;
            string browserPath = null;
            int targetId = -1;
            bool all = false;


            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-delay": int.TryParse(args[++i], out initdelai); break;
                    case "-xmin": int.TryParse(args[++i], out xmin); break;
                    case "-xmax": int.TryParse(args[++i], out xmax); break;
                    case "-ymin": int.TryParse(args[++i], out ymin); break;
                    case "-ymax": int.TryParse(args[++i], out ymax); break;                    
                    case "-maxconcurrency": int.TryParse(args[++i], out maxConcurrency); break;
                    case "-navpath": browserPath = args[++i]; break;
                    case "-targetid": int.TryParse(args[++i], out targetId); break;
                    case "-zonex": int.TryParse(args[++i], out zoneX); break;
                    case "-zoney": int.TryParse(args[++i], out zoneY); break;
                    case "-all": all = true; break;
                }
            }

           
            if (string.IsNullOrEmpty(browserPath)) { Console.WriteLine("ERREUR : -navpath est obligatoire."); return; }           
            if (zoneX < 0 || zoneY < 0) { Console.WriteLine("ERREUR : -zonex et -zoney sont obligatoires."); return; }


            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = $@"{browserPath}"
            });

            var semaphore = new SemaphoreSlim(maxConcurrency);

            var PixelnfoList = new List<PixelInfo>();

            int total;
            int completed = 0;
            object lockObj = new object();

            var foundPoints = new ConcurrentBag<string>();

            Console.WriteLine($"Téléchargemnt de l'image ...");

            var pixels = GetNonEmptyPixels(DownloadImageAsync($"https://backend.wplace.live/files/s0/tiles/{zoneX}/{zoneY}.png", browser).GetAwaiter().GetResult(), includeColor: true,grouped: !all);

            foreach (var p in pixels)
            {
                if (p.X<=xmax&&p.Y<=ymax&& p.X >= xmin && p.Y >= ymin )
                {
                    PixelnfoList.Add(p);
                }
                
            }
            Console.WriteLine($"Total : {PixelnfoList.Count} Pixels");
            total = PixelnfoList.Count;
            Console.WriteLine($"Scan des pixels ... "+initdelai);
            
            bool erreur = false;
            string msgerreur = "";

            // Déclaration d'une variable pour la tâche de progression
            ProgressTask progressTask = null;

            AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn()
                )
                .Start(ctx =>
                {
                    progressTask = ctx.AddTask("[green]Scan en cours[/]", maxValue: total);

                    while (PixelnfoList.Count > 0)
                    {
                        var PixelnfoListCopie = PixelnfoList.ToList();
                        foreach (PixelInfo Pixel in PixelnfoListCopie)
                        {
                            semaphore.Wait();

                            Task.Delay(initdelai).Wait();

                            Task.Run(async () =>
                            {
                                try
                                {
                                    var page = await browser.NewPageAsync();
                                    await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");

                                    var response = await page.GoToAsync($"https://backend.wplace.live/s0/pixel/{zoneX}/{zoneY}?x={Pixel.X}&y={Pixel.Y}");

                                    if (response != null && response.Ok)
                                    {
                                        cptcgtdelai++;
                                        if (cptcgtdelai >= 10 && initdelai > 1100)
                                        {
                                            cptcgtdelai = 0;
                                            initdelai -= 100;
                                        }

                                        PixelnfoList.Remove(Pixel);

                                        string content = await response.TextAsync();

                                        try
                                        {
                                            PixelResponse pixelData = JsonConvert.DeserializeObject<PixelResponse>(content);

                                            pixelData.X=Pixel.X;
                                            pixelData.Y=Pixel.Y;

                                            pixelData.R = Pixel.Color.R;
                                            pixelData.V = Pixel.Color.G;
                                            pixelData.B = Pixel.Color.B;

                                            pixelData.link = xytolatlong(zoneX,zoneY, Pixel.X, Pixel.Y);

                                            _allPixels.Add(pixelData);

                                            completed++;
                                            if (pixelData?.paintedBy?.id == targetId)
                                            {
                                                foundPoints.Add($"Pixel trouvé de {pixelData.paintedBy.name} à ({Pixel})");
                                            }
                                        }
                                        catch (JsonException) { }
                                    }
                                    else
                                    {
                                        erreur = true;
                                        msgerreur = response.Status.ToString();
                                        initdelai = initdelai+100;
                                    }

                                    await page.CloseAsync();
                                }
                                catch (Exception) { }
                                finally
                                {
                                    semaphore.Release();

                                    lock (lockObj)
                                    {
                                        
                                        progressTask.Value = completed;

                                        
                                        if (erreur)
                                        {
                                            erreur = false;
                                            progressTask.Description = $"[red]{msgerreur}[/]";
                                            AnsiConsole.MarkupLine($"\n[red]{msgerreur}[/]");
                                        }
                                    }
                                }
                            });
                        }
                    }
                });

            await browser.CloseAsync();



            CSV();


            Console.WriteLine("\nScan terminé !");
        }

        public static void CSV()
        {
            try
            {
                string csvPath = $"{zoneX}_{zoneY}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                using (var writer = new StreamWriter(csvPath))
                {
                    writer.WriteLine("X,Y,PlayerID,PlayerName,AllianceID,AllianceName,RegionID,RegionName,Discord,R,V,B");
                    foreach (var p in _allPixels)
                    { 
                        writer.WriteLine($"{p.X},{p.Y},{p.paintedBy?.id},{p.paintedBy?.name},{p.paintedBy?.allianceId},{p.paintedBy?.allianceName},{p.region?.id},{p.region?.name},{p.paintedBy?.discord},{p.R},{p.V},{p.B},{p.link}");
                    }
                }
                Console.WriteLine($"\n[CSV] Export terminé : {csvPath}");
            }
            catch (Exception)
            {

                Console.WriteLine("Export CSV échoué");
            }
        }


        public static List<PixelInfo> GetNonEmptyPixels(Bitmap bmp, bool includeColor = true, bool grouped = false)
        {
            var pixels = new List<PixelInfo>();

            if (bmp.Width != 1000 || bmp.Height != 1000)
                throw new ArgumentException("L'image doit être exactement 1000x1000 pixels.");

            bool[,] visited = new bool[bmp.Width, bmp.Height];

            
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (visited[x, y]) continue;

                    var c = bmp.GetPixel(x, y);
                    bool isEmpty = c.A == 0 || (c.R == 255 && c.G == 255 && c.B == 255);
                    if (isEmpty)
                    {
                        visited[x, y] = true;
                        continue;
                    }

                    if (!grouped)
                    {
                        pixels.Add(new PixelInfo
                        {
                            X = x,
                            Y = y,
                            Color = includeColor ? c : System.Drawing.Color.Empty
                        });
                        visited[x, y] = true;
                    }
                    else
                    {
                        
                        var queue = new Queue<(int, int)>();
                        queue.Enqueue((x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            var (cx, cy) = queue.Dequeue();

                            for (int dir = 0; dir < dx.Length; dir++)
                            {
                                int nx = cx + dx[dir];
                                int ny = cy + dy[dir];

                                if (nx >= 0 && nx < bmp.Width && ny >= 0 && ny < bmp.Height && !visited[nx, ny])
                                {
                                    var nc = bmp.GetPixel(nx, ny);
                                    bool neighborEmpty = nc.A == 0 || (nc.R == 255 && nc.G == 255 && nc.B == 255);
                                    if (!neighborEmpty && nc.ToArgb() == c.ToArgb())
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue((nx, ny));
                                    }
                                }
                            }
                        }

                        
                        pixels.Add(new PixelInfo
                        {
                            X = x,
                            Y = y,
                            Color = includeColor ? c : System.Drawing.Color.Empty
                        });
                    }
                }
            }

            return pixels;
        }
        public static async Task<Bitmap> DownloadImageAsync(string url, IBrowser browser)
        {
            var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");

            var response = await page.GoToAsync(url);
            if (response != null && response.Ok)
            {
                var bytes = await response.BufferAsync();
                await page.CloseAsync();
                return new Bitmap(new System.IO.MemoryStream(bytes));
            }
            else
            {
                await page.CloseAsync();
                throw new Exception($"Impossible de télécharger l'image, statut HTTP: {response?.Status}");
            }
        }
        public static string xytolatlong(int xTile, int yTile, int px, int py, int tileSize = 1000)
        {
            const int tilesPerAxis = 2048;
            double mapSize = (double)tilesPerAxis * tileSize; 

           
            double globalX = (double)xTile * tileSize + px;
            double globalY = (double)yTile * tileSize + py;

            
            double xNorm = Math.Min(1.0, Math.Max(0.0, globalX / mapSize));
            double yNorm = Math.Min(1.0, Math.Max(0.0, globalY / mapSize));

            
            double lon = xNorm * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * yNorm)));
            double lat = latRad * 180.0 / Math.PI;

            return $"https://wplace.live/?lat={lat.ToString(CultureInfo.InvariantCulture)}&lng={lon.ToString(CultureInfo.InvariantCulture)}&zoom=22";
        }

    } 
    

}

