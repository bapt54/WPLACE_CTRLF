using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PuppeteerSharp;

namespace WPLACE_CTRLF
{
    internal class Program
    {
        public class PaintedBy
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class PixelResponse
        {
            public PaintedBy paintedBy { get; set; }
        }

        static async Task Main(string[] args)
        {

            
            int startX = 0;
            int endX = 990;
            int startY = 0;
            int endY = 990;
            int step = 10;
            int maxConcurrency = 5;

            
            string browserPath = null;
            int targetId = -1;
            int zoneX = -1;
            int zoneY = -1;

            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-xmin":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int xMinVal))
                        {
                            startX = xMinVal;
                            i++;
                        }
                        break;
                    case "-xmax":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int xMaxVal))
                        {
                            endX = xMaxVal;
                            i++;
                        }
                        break;
                    case "-ymin":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int yMinVal))
                        {
                            startY = yMinVal;
                            i++;
                        }
                        break;
                    case "-ymax":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int yMaxVal))
                        {
                            endY = yMaxVal;
                            i++;
                        }
                        break;
                    case "-step":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int stepVal))
                        {
                            step = stepVal;
                            i++;
                        }
                        break;
                    case "-maxconcurrency":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int maxConc))
                        {
                            maxConcurrency = maxConc;
                            i++;
                        }
                        break;
                    case "-navpath":
                        if (i + 1 < args.Length)
                        {
                            browserPath = args[i + 1];
                            i++;
                        }
                        break;
                    case "-targetid":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int tid))
                        {
                            targetId = tid;
                            i++;
                        }
                        break;
                    case "-zonex":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int zx))
                        {
                            zoneX = zx;
                            i++;
                        }
                        break;
                    case "-zoney":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int zy))
                        {
                            zoneY = zy;
                            i++;
                        }
                        break;
                }
            }

            
            if (string.IsNullOrEmpty(browserPath))
            {
                Console.WriteLine("ERREUR : Le chemin du navigateur (-navpath) est obligatoire.");
                return;
            }
            if (endX >= 1000 || endX >= 1000)
            {
                Console.WriteLine("ERREUR : Les coordonées (-xmax -xmin) sont comprises entre 0 et 9999");
                return;
            }
            if (targetId < 0)
            {
                Console.WriteLine("ERREUR : L'ID cible (-targetid) est obligatoire et doit être un entier positif.");
                return ;
            }
            if (zoneX < 0 || zoneY < 0)
            {
                Console.WriteLine("ERREUR : Les coordonnées de la zone (-zonex et -zoney) sont obligatoires et doivent être positives.");
                return ;
            }

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = $@"{browserPath}"
            });

            var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = new List<Task>();

            int total = ((endX - startX) / step + 1) * ((endY - startY) / step + 1);
            int completed = 0;
            object lockObj = new object();

            var foundPoints = new ConcurrentBag<string>();

            for (int x = startX; x <= endX; x += step)
            {
                for (int y = startY; y <= endY; y += step)
                {
                    await semaphore.WaitAsync();

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var page = await browser.NewPageAsync();
                            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");

                            string url = $"https://backend.wplace.live/s0/pixel/{zoneX}/{zoneY}?x={x}&y={y}";
                            var response = await page.GoToAsync(url);

                            if (response != null && response.Ok)
                            {
                                string content = await response.TextAsync();

                                try
                                {
                                    var pixelData = JsonConvert.DeserializeObject<PixelResponse>(content);
                                    if (pixelData?.paintedBy?.id == targetId)
                                    {
                                        foundPoints.Add($"Point trouvé par {pixelData.paintedBy.name} à ({x},{y})");
                                    }
                                }
                                catch (JsonException)
                                {
                                    
                                }
                            }

                            await page.CloseAsync();
                        }
                        catch
                        {
                            
                        }
                        finally
                        {
                            semaphore.Release();

                            lock (lockObj)
                            {
                                completed++;

                                Console.Clear();

                                
                                foreach (var point in foundPoints)
                                    Console.WriteLine(point);

                                
                                double progress = (completed * 100.0) / total;
                                Console.WriteLine($"\nProgression : {completed} / {total} ({progress:0.00}%)");
                            }
                        }
                    });

                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);

            await browser.CloseAsync();

            Console.WriteLine("\nScan terminé !");
        }
    }
    }

