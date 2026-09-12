# Quest Demon MR – Implementierungsplan

Status: 2026-09-04  
Zielgerät: Meta Quest 3  
Engine: Unity 6, OpenXR, Meta XR Core SDK, MR Utility Kit

## 1. Produktziel

Ein kurzes, unmittelbar verständliches Mixed-Reality-Spiel in einem gescannten realen Raum. Der Spieler bleibt körperlich am Platz, hält mit dem rechten Touch-Plus-Controller eine virtuelle Pistole und bekämpft eigenständig gestaltete Pixel-Dämonen. Gegner treten aus Wandportalen oder später aus zunächst verdeckten Raumpositionen hervor.

Der erste Meilenstein ist kein Store-Produkt, sondern ein belastbarer vertikaler Schnitt auf echter Quest 3.

## 2. Verbindlicher MVP-Schnitt

- ein lokaler Spieler und ein gescannter Raum
- Passthrough als Hintergrund
- eine Pistole, Hitscan, Mündungsblitz und Haptik
- ein originaler Gegner mit Lauf-, Treffer- und Todeszustand
- Portalauftritt, drei Wellen, Punkte und Spielergesundheit
- Mindestabstand für Spawns; keine künstliche Fortbewegung
- MRUK-Raumpositionen mit sicherer Fallback-Arena
- Android-APK für ARM64

Nicht im ersten MVP: Handtracking-Pistole, Nachladen, freie Raum-zu-Raum-Navigation, Multiplayer, Store-IAP, Cloud-Saves, echte Projektilballistik und kamerabasierte Objekterkennung.

## 3. Architektur

### Plattform

- OpenXR ist der XR-Provider.
- Meta XR Core liefert Quest-Rig und Passthrough.
- MRUK lädt den Scene-API-Raumscan und liefert räumliche Abfragen.
- Unity Physics behandelt virtuelle Treffer und Gegnerkollisionen.
- `UnityEngine.XR.InputDevices` liest Controllerpose, Trigger und Haptik ohne alte OVRInput-Abhängigkeit.

### Laufzeitmodule

| Modul | Verantwortung |
| --- | --- |
| `QuestDemonGame` | Boot, Kamerasuche, Raumladung, Wellen, HUD, Spawnregeln |
| `QuestGun` | Controllertracking, Trigger-Flanke, Hitscan, Mündungsblitz, Haptik |
| `DemonAgent` | geriggte Animation, räumlich begrenzte Zielverfolgung, Schaden, Tod, Punkte |
| `PixelArtFactory` | Laden des Produktionssprites und deterministischer Notfall-Fallback |
| `PortalVisual` | prozedurale Portalgeometrie und Animation |
| `BillboardToHead` | horizontale Ausrichtung des Sprites zum Kopf |
| `RoomSpatializer` | MRUK-Depth-Occluder, statische Raumkollider und gemeinsame Raumtests |
| `QuestDemonProjectBuilder` | Scene-Erzeugung, OpenXR-/Android-Konfiguration und APK-Build |

### Raumdaten

1. Beim Start wartet das Spiel kurz auf MRUK.
2. Ist ein Raum geladen, erzeugt ein unsichtbares MRUK-EffectMesh Tiefenwerte und statische Kollider für Wände, Boden, Decke und Möbel; Tür- und Fensteröffnungen werden ausgeschnitten.
3. Quest Environment Depth liefert zusätzlich aktuelle, sichtfeldabhängige Tiefenraycasts sowie Hard Occlusion für die Spielmaterialien. Es ist kein dauerhaftes semantisches 360-Grad-Live-Mesh.
4. Eine Scene-Wand ist nur dann bevorzugter Spawnort, wenn der aktuelle Tiefenfeed denselben Treffer bestätigt. Das Portal und der Gegner liegen immer auf der Kopfseite dieser Wand.
5. Kann in einem geladenen kleinen Raum keine sichere Position gefunden werden, wird der Spawn übersprungen statt außerhalb des Raums platziert.
6. Bei der Bewegung werden Raumgrenze, Möbelvolumen, Live-Hindernisse und Nachbargegner getestet. Stau löst einen zeitlich begrenzten seitlichen Recovery-Weg aus.
7. Ohne Scene-Daten bleibt ausschließlich für Entwicklungs-/Berechtigungsfehler ein begrenzter Halbkreis-Fallback aktiv.

Der Depth-Feed ist für dynamische visuelle Verdeckung vorgesehen, nicht als verlässliche Physik- oder Navigationsquelle. Bewegte reale Möbel bleiben daher ein eigenes Sicherheitsrisiko.

## 4. Umsetzungsphasen und Gates

### Phase A – Buildbares Fundament

- Unity-Projekt und feste Paketversionen
- OpenXR für Android
- Meta Quest Feature Group
- Vulkan, ARM64, IL2CPP
- automatisierte Szenenerstellung

Gate A: Unity importiert ohne Compilerfehler und erzeugt eine signierte Debug-APK.

### Phase B – Vertikaler Kampfschnitt

- Quest-Rig und Passthrough
- Pistole am rechten Controller
- Trigger, Hitscan und Haptik
- Portal, Gegner, Schaden, Tod und Punkte
- drei Wellen

Gate B: Auf der getragenen Quest lässt sich ein Gegner zuverlässig mit dem physischen Trigger treffen und töten.

### Phase C – Raumverständnis

- MRUK-Raumscan laden
- freie Spawnpositionen statt fester Koordinaten
- Wand-/Türportalplatzierung
- statische Kollisionen und Verdeckung
- verständlicher Fallback bei fehlendem Space Setup

Gate C: Tests in mindestens zwei unterschiedlich großen gescannten Räumen; keine Spawns in Möbelvolumen oder näher als der Sicherheitsabstand.

### Phase D – Gegner „um die Ecke“

- Kandidaten außerhalb des aktuellen Sichtkegels
- Wand- und Möbelkanten als Peek-Wegpunkte
- Laufzeit-NavMesh nur, wenn die einfacheren Wegpunkte nicht genügen
- Abbruch/Repositionierung bei blockiertem Weg

Gate D: zehn wiederholte Spawns in zwei Räumen ohne Wanddurchdringung oder Annäherung innerhalb 0,8 m.

### Phase E – Politur

- weitere geriggte Gegnertypen und verfeinerte Bewegungsübergänge
- räumliches Audio, bessere Haptik und Trefferfeedback
- Depth-Occlusion-kompatibler Alpha-Clip-Shader
- Portalshader, Schwierigkeitskurve, Pause/Resume
- Performance- und Thermikprofil

Gate E: stabile Bildrate, 30-Minuten-Lauf, Home/Resume, fünf Kaltstarts und verständliche Berechtigungsfehler.

## 5. Testmatrix

| Prüfung | Editor | APK | getragene Quest 3 |
| --- | ---: | ---: | ---: |
| C#-Kompilierung | ja | ja | – |
| Fallback-Gameplay | ja | ja | ja |
| Trigger und Controllerpose | teilweise | – | erforderlich |
| Haptik | nein | – | erforderlich |
| Passthrough | Simulation begrenzt | – | erforderlich |
| MRUK-Raumscan | JSON/Simulation | – | erforderlich |
| reale Verdeckung | nein | – | erforderlich |
| Sicherheitsabstände | synthetisch | – | erforderlich |
| Home/Resume und Thermik | nein | – | erforderlich |

Build-Erfolg ist ausdrücklich kein Nachweis für korrekte physische Quest-Interaktion.

## 6. Asset- und Lizenzregeln

- Keine Doom-WADs, Originalsprites, Sounds, Namen oder Logos.
- Gegner, Waffen und Effekte erhalten eigenständige Silhouetten, Palette und Benennung.
- Jede externe Ressource braucht Herkunft und Lizenznotiz.
- Der MVP verwendet ein eigenständig generiertes Produktionssprite mit hartem Alpha-Cutout. Eine prozedurale Laufzeitgrafik bleibt als technischer Notfall-Fallback erhalten.

## 7. Nächste konkrete Tickets

- `QDMR-001`: Paketimport und Compilerfehlerfreiheit
- `QDMR-002`: automatische Main-Szene und Android-Profil
- `QDMR-003`: Quest-APK bauen und Signatur prüfen
- `QDMR-004`: Quest verbinden, installieren und Startlog sichern
- `QDMR-005`: getragener Trigger-/Haptik-/Treffertest
- `QDMR-006`: echten MRUK-Raumspawn und Occlusion auf der getragenen Quest validieren
- `QDMR-007`: Waffenpose und vier Gegneranimationen im Headset validieren
- `QDMR-008`: echte Kanten-/Tür-Wegpunkte über das lokale Ausweichen hinaus ergänzen
