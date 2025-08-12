name: Scanner Pixel avec PuppeteerSharp pour wplace.live
description: >
  Ce programme en C# utilise PuppeteerSharp pour scanner les pixels d'une image issue
  du site wplace.live. Il fonctionne en ouvrant un navigateur headless (ex : Brave, Chrome)
  et en effectuant des requêtes HTTP pour identifier les informations des pixels.
  Les résultats sont exportés automatiquement en CSV.

features:
  - Scan d'une zone 1000x1000 pixels depuis une tuile (`zonex`, `zoney`)
  - Option pour regrouper les pixels de même couleur ou scanner pixel par pixel
  - Gestion du délai entre requêtes pour éviter le blocage
  - Multi-threading avec limitation du nombre de requêtes simultanées
  - Export automatique au format CSV avec coordonnées, couleurs et infos joueur
  - Affichage en temps réel de la progression avec barre Spectre.Console

arguments:
  - name: -navpath
    description: Chemin complet vers l’exécutable du navigateur (Brave, Chrome, etc.)
    required: true
    default: null
  - name: -zonex
    description: Coordonnée X de la tuile (zone)
    required: true
    default: null
  - name: -zoney
    description: Coordonnée Y de la tuile (zone)
    required: true
    default: null
  - name: -targetid
    description: ID de l’utilisateur à rechercher (optionnel, filtre les résultats)
    required: false
    default: -1
  - name: -delay
    description: Délai initial en millisecondes entre chaque requête
    required: false
    default: 1100
  - name: -xmin
    description: Coordonnée X minimale à scanner (non utilisée dans la version actuelle)
    required: false
    default: 0
  - name: -xmax
    description: Coordonnée X maximale à scanner (non utilisée dans la version actuelle)
    required: false
    default: 999
  - name: -ymin
    description: Coordonnée Y minimale à scanner (non utilisée dans la version actuelle)
    required: false
    default: 0
  - name: -ymax
    description: Coordonnée Y maximale à scanner (non utilisée dans la version actuelle)
    required: false
    default: 999
  - name: -maxconcurrency
    description: Nombre maximal de requêtes simultanées
    required: false
    default: 1
  - name: -all
    description: Désactive le regroupement des pixels par couleur et scanne pixel par pixel
    required: false
    default: false

usage:
  example: >
    WPLACE_CTRLF.exe -navpath "C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe"
    -targetid 1339861 -zonex 1051 -zoney 737 -delay 1200 -maxconcurrency 3 -all
  notes:
    - Les coordonnées `zonex` et `zoney` se trouvent via les DevTools du navigateur,
      onglet "Network", en inspectant les requêtes envoyées lors d'un clic sur un pixel.
    - `-targetid` permet de ne lister que les pixels posés par un joueur précis.
    - `-all` augmente fortement le temps de scan, mais permet une analyse détaillée pixel par pixel.

output:
  format: CSV
  filename: pixels_YYYYMMDD_HHMMSS.csv
  columns:
    - X
    - Y
    - PlayerID
    - PlayerName
    - AllianceID
    - AllianceName
    - RegionID
    - RegionName
    - Discord
    - R
    - V
    - B
  description: >
    Le fichier CSV est créé automatiquement à la fin du scan ou à l’arrêt du programme
    (Ctrl+C). Il contient toutes les informations collectées.

requirements:
  - .NET 6 ou supérieur
  - PuppeteerSharp
  - Newtonsoft.Json
  - Spectre.Console
  - Navigateur compatible (Brave, Chrome, Chromium)

screenshot:
  path: docs/photo.png
  description: Exemple de récupération des coordonnées `zonex` et `zoney` via DevTools

license: MIT
