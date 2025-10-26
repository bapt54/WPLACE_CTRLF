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
            public int tlX { get; set; }
            public int tlY { get; set; }
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
            public int tlX { get; set; }
            public int tlY { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string link { get; set; }
            public int R { get; set; }
            public int V { get; set; }
            public int B { get; set; }
        }

        private static List<PixelResponse> _allPixels = new List<PixelResponse>();
        private static int zoneX = -1, zoneY = -1,pixelX=-1,pixelY=-1;
        static async Task Main(string[] args)
        {

            
            int initdelai = 200;
            int cptcgtdelai = 0;

            int xmin = 0, xmax = 999, ymin = 0, ymax = 999, maxConcurrency = 1,echantillon=100,topplayerCount=5,topallianceCount=3;
            string browserPath = null,imagepath=null;
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
                    case "-imgpath": imagepath = args[++i]; break;
                    case "-pixelx": int.TryParse(args[++i], out pixelX); break;
                    case "-pixely": int.TryParse(args[++i], out pixelY); break;
                    case "-coords": var parts = args[++i].Split(',').Select(short.Parse).ToArray(); zoneX = parts[0]; zoneY = parts[1]; pixelX = parts[2]; pixelY = parts[3]; break;
                    case "-echantillon": int.TryParse(args[++i], out echantillon); break;
                    case "-topplayer": int.TryParse(args[++i], out topplayerCount); break;
                    case "-topalliance": int.TryParse(args[++i], out topallianceCount); break;

                }
            }

            int delai=initdelai;
            if (string.IsNullOrEmpty(browserPath)) { Console.WriteLine("ERREUR : -navpath est obligatoire."); return; }           
            if (zoneX < 0 || zoneY < 0) { Console.WriteLine("ERREUR : -zonex et -zoney sont obligatoires."); return; }
            if ((imagepath != null && pixelX < 0)|| (imagepath != null && pixelY < 0)) { Console.WriteLine("ERREUR : -pixelx et -pixely sont obligatoires."); return; }


            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n--- Pixels enregistrés avant l'arrêt ---");
                var PixelnfoListCopie = _allPixels.ToList();

                CSV();


                Console.WriteLine();
                Console.WriteLine(GetTopSummary(PixelnfoListCopie, topplayerCount, topallianceCount));
                Environment.Exit(0);
            };




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
            List<PixelInfo> pixels = null;
            if (imagepath == null)
            {
                

                Console.WriteLine($"Téléchargemnt de l'image ...");

                pixels = GetNonEmptyPixels(DownloadImageAsync($"https://backend.wplace.live/files/s0/tiles/{zoneX}/{zoneY}.png", browser).GetAwaiter().GetResult(), includeColor: true, grouped: !all);
            }
            else
            {
                pixels = ExtractPixels(imagepath, zoneX, zoneY, pixelX, pixelY,echantillon);
            }
            

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

                                    var response = await page.GoToAsync($"https://backend.wplace.live/s0/pixel/{Pixel.tlX}/{Pixel.tlY}?x={Pixel.X}&y={Pixel.Y}");

                                    if (response != null && response.Ok)
                                    {
                                        cptcgtdelai++;
                                        if (cptcgtdelai >= 10 && initdelai > delai)
                                        {
                                            cptcgtdelai = 0;
                                            initdelai -= 100;
                                        }

                                        PixelnfoList.Remove(Pixel);

                                        string content = await response.TextAsync();

                                        try
                                        {
                                            PixelResponse pixelData = JsonConvert.DeserializeObject<PixelResponse>(content);

                                            pixelData.tlX = Pixel.tlX;
                                            pixelData.tlY = Pixel.tlY;
                                            pixelData.X=Pixel.X;
                                            pixelData.Y=Pixel.Y;

                                            pixelData.R = Pixel.Color.R;
                                            pixelData.V = Pixel.Color.G;
                                            pixelData.B = Pixel.Color.B;

                                            pixelData.link = xytolatlong(Pixel.tlX,Pixel.tlY, Pixel.X, Pixel.Y);

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

            Console.WriteLine();
            Console.WriteLine(GetTopSummary(_allPixels));

            Console.WriteLine("\nScan terminé !");
        }

        public static void CSV()
        {
            try
            {
                string csvPath = $"{zoneX}_{zoneY}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                using (var writer = new StreamWriter(csvPath))
                {
                    writer.WriteLine("tlX,tlY,X,Y,PlayerID,PlayerName,AllianceID,AllianceName,RegionID,RegionName,Discord,R,V,B,link");
                    foreach (var p in _allPixels)
                    { 
                        writer.WriteLine($"{p.X},{p.Y},{p.X},{p.Y},{p.paintedBy?.id},{p.paintedBy?.name},{p.paintedBy?.allianceId},{p.paintedBy?.allianceName},{p.region?.id},{p.region?.name},{p.paintedBy?.discord},{p.R},{p.V},{p.B},{p.link}");
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
                            tlY = zoneY,
                            tlX = zoneX,
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
        public static List<PixelInfo> ExtractPixels(string pngPath, int startTileX, int startTileY, int startPxX, int startPxY, double percentage = 100.0)
        {
            var allPixels = new List<PixelInfo>();

            using (var bmp = new Bitmap(pngPath))
            {
                int width = bmp.Width;
                int height = bmp.Height;

                for (int py = 0; py < height; py++)
                {
                    for (int px = 0; px < width; px++)
                    {
                        System.Drawing.Color c = bmp.GetPixel(px, py);

                        if (c.A == 0) continue;

                        int globalX = startPxX + px;
                        int globalY = startPxY + py;

                        int tileX = startTileX + (globalX / 1000);
                        int tileY = startTileY + (globalY / 1000);

                        int localX = globalX % 1000;
                        int localY = globalY % 1000;

                        allPixels.Add(new PixelInfo
                        {
                            tlX = tileX,
                            tlY = tileY,
                            X = localX,
                            Y = localY
                        });
                    }
                }
            }

            if (percentage < 100.0 && percentage > 0.0 && allPixels.Count > 0)
            {
                int takeCount = (int)Math.Ceiling(allPixels.Count * (percentage / 100.0));
                var sampled = new List<PixelInfo>();

                double step = (double)allPixels.Count / takeCount;
                for (int i = 0; i < takeCount; i++)
                {
                    int index = (int)Math.Floor(i * step);
                    sampled.Add(allPixels[index]);
                }

                return sampled;
            }

            return allPixels;
        }
        public static string GetTopSummary(List<PixelResponse> pixels, int topPlayersCount = 5, int topAlliancesCount = 3)
        {
            if (pixels == null || pixels.Count == 0)
                return "Aucun pixel.";

            int total = pixels.Count;

            var topPlayers = pixels
                .Where(p => p.paintedBy != null)
                .GroupBy(p => p.paintedBy.name)
                .Select(g => new { Player = g.Key, Count = g.Count(), Percent = (g.Count() * 100.0) / total })
                .OrderByDescending(x => x.Count)
                .Take(topPlayersCount)
                .ToList();

            var topAlliances = pixels
                .Where(p => p.paintedBy != null)
                .GroupBy(p => string.IsNullOrEmpty(p.paintedBy.allianceName) ? "Sans alliance" : p.paintedBy.allianceName)
                .Select(g => new { Alliance = g.Key, Count = g.Count(), Percent = (g.Count() * 100.0) / total })
                .OrderByDescending(x => x.Count)
                .Take(topAlliancesCount)
                .ToList();

            string playersPart = "Joueurs: " + string.Join(", ", topPlayers.Select(p => $"{p.Player} {p.Percent:0.0}%"));
            string alliancesPart = "Alliances: " + string.Join(", ", topAlliances.Select(a => $"{a.Alliance} {a.Percent:0.0}%"));

            return $"{playersPart} | {alliancesPart}";
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

