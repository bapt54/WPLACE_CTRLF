# Scanner Pixel avec PuppeteerSharp pour wplace.live

Ce programme en C# utilise PuppeteerSharp pour scanner des pixels dans une zone spécifique via des requêtes HTTP dans un navigateur headless (ex: Brave).

---

## Fonctionnalités

- Scan une zone rectangulaire définie par des coordonnées X et Y.
- Supporte un délai entre chaque requête pour gérer la charge (`-delay`).
- Multi-threading avec un nombre max de requêtes simultanées (`-maxconcurrency`).
- Scan par pixel individuel ou regroupé par zones de couleur (`-all`).
- Arguments en ligne de commande pour personnaliser la plage, le navigateur utilisé, et la cible.
- Export automatique des résultats en fichier CSV.
- Gestion propre de l'arrêt (Ctrl+C) avec sauvegarde des données en cours.

---

## Arguments

| Argument          | Description                                                    | Obligatoire | Par défaut                      |
|-------------------|----------------------------------------------------------------|-------------|--------------------------------|
| `-navpath`        | Chemin complet vers l’exécutable du navigateur                 | Oui         | N/A                            |
| `-targetid`       | ID de l’utilisateur à rechercher (optionnel)                   | Non         | -1 (non utilisé)               |
| `-zonex`          | Coordonnée X de la zone                                         | Oui         | N/A                            |
| `-zoney`          | Coordonnée Y de la zone                                         | Oui         | N/A                            |
| `-xmin`           | Coordonnée X de départ pour le scan                             | Non         | 0                              |
| `-xmax`           | Coordonnée X de fin pour le scan                                | Non         | 999                            |
| `-ymin`           | Coordonnée Y de départ pour le scan                             | Non         | 0                              |
| `-ymax`           | Coordonnée Y de fin pour le scan                                | Non         | 999                            |
| `-delay`          | Délai en millisecondes entre chaque requête                    | Non         | 1100                           |
| `-maxconcurrency` | Nombre maximum de requêtes simultanées                         | Non         | 1                              |
| `-all`            | Scanner tous les pixels sans regroupement par zones de couleur | Non         | false (regroupement activé)    |

---

## Validation

- `-navpath`, `-zonex`, `-zoney` sont obligatoires.
- `-targetid` est optionnel, utilisé uniquement pour filtrer certains pixels dans le résultat.
- Les coordonnées X et Y doivent être entre 0 et 999.
- `-delay` doit être un entier positif.
- `-maxconcurrency` doit être un entier positif.

---

## Notes

- Le programme télécharge une image 1000x1000 pixels correspondant à la zone spécifiée (`-zonex` et `-zoney`).
- Par défaut, le scan regroupe les pixels contigus de même couleur en un seul point, pour réduire le nombre de requêtes.
- Avec l’option `-all`, le scan se fait pixel par pixel, sans regroupement.
- Les résultats sont automatiquement exportés dans un fichier CSV horodaté à la fin du scan ou lors d’un arrêt (Ctrl+C).
- Le délai (`-delay`) est ajusté dynamiquement selon la charge et les erreurs HTTP rencontrées.


Pour Trouver les zones, il faut aller dans les devtools du navigateur dans l'onglet network puis cliquer et voir la tramme émise , le target id est écris lors que lon clique sur un pixel posé par l'utilisateur :

![Exemple zones](docs/photo.png)

---

## Exemple d’utilisation

```bash
WPLACE_CTRLF.exe -navpath "C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe" -targetid 1339861 -zonex 1051 -zoney 737 -delay 1200 -maxconcurrency 5 -all
