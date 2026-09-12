# V18.2 – kompakte, neu modellierte Portale

**Gerätenachtrag:** Der Nutzer meldet abgelehnte Spawns und fälschlich abgeschlossene leere Wellen. V18.2 ist daher kein funktionell abgenommener Stand. Korrekturen und reproduzierte Fehlerfälle: [V18.3](REVIEW-V18-3.md).

## Umfang

Der Nutzer bestätigt V18.1 als spielbar, beschreibt Laser Tags Raumerfassung aber weiterhin als unmittelbarer und über Raumwechsel hinweg nutzbar. **Die Scan-Architektur und deren Startfreigabe bleiben auf Wunsch in diesem Schritt unverändert.** Kein Laser-Tag-Gleichstand behauptet.

### Grafik

Neues eigenständig in Blender erzeugtes Modell `ObsidianRiftV18`: zusammenhängende reliefartige Obsidianhülle mit gefalteten Graten, feiner Mineralstruktur und verzweigten glühenden Spalten. Keine aufeinandergesteckten Kugeln/Kegel, großen Schädelhörner, Ketten oder seitlich herausragenden Wurzeln des bisherigen V12-Modells. 22.460 Dreiecke, drei Render-Batches. Oberflächenfarbe und Tangenten-Normalmap in Blender gebacken, je 1024 × 512, Android ASTC 6×6 mit Mipmaps.

Quellmodell und reproduzierbares Skript liegen in `BlenderSource/ObsidianRiftV18.blend` und `BlenderSource/build_rift_v18_2.py`. Blender-Render und zusätzlich ein nativer Unity-Render mit den tatsächlich verwendeten FBX-/Tiefenmaterialien unter `Verification/V18/portal-v18.2-{blender,unity}.png`. Diese zeigen den Rahmen, nicht einen im Headset abgenommenen vollständigen Portalblick.

Der Höllenblick bleibt die vorhandene, separat pro Auge gerenderte 3D-Landschaft. In diesem Schritt keine neue Hintergrundwelt modelliert. Öffnung an die innere Blender-Kante angepasst; schmaler, unregelmäßig glimmender Rand statt flächiger Leuchtring. Partikel sitzen am Öffnungsrand statt am Portalursprung am Boden. Deutlich weniger und kürzere Risslinien, begrenzter Rauch und hierarchisch mitskalierte Partikel statt großer Ausbruchsarme.

### Gemeinsamer Größenvertrag

| Form | Maximale Rahmenfläche (Breite × Höhe bzw. Deckenlänge) | Öffnung |
| --- | --- | --- |
| Wand | 1,365 × 1,899 m | 1,05 × 1,654 m |
| Schmale Wand | 1,165 × 1,818 m | 0,896 × 1,584 m |
| Decke | 1,092 × 0,990 m | 0,840 × 0,862 m |

`PortalShape` steuert Rahmen, Öffnungsanimation und die geprüfte Wand-/Deckenfläche gemeinsam. Deckenwurzel berücksichtigt die reduzierte Größe, damit die Öffnung am gemessenen Deckenpunkt bleibt. Deckenportale sind keine flach gelegten großen Wandtore mehr.

Wandflächen prüfen zunächst Normalgröße, dann die schmale Variante. 12 Randproben plus Mittelpunkt folgen der ovalen Rahmenfläche statt den Ecken eines Rechtecks. Das ist eine begrenzte Mehrpunktprüfung, kein Beweis einer lückenlosen Oberfläche zwischen allen Proben. Die früheren Proben deckten überdies nicht die viel weiter ausladende alte Dekoration ab; neue Prüfung und neuer Rahmen passen nun zusammen.

Live-Wandaustritt 48 statt 62 cm vor der Wand; Mindestabstand des Gegners zum Spieler 1,45 statt 1,75 m. Körperfreiheit, erreichbarer Boden, Abstand zu vorhandenen Gegnern und verbundener Weg zum Spieler bleiben geprüft. Enge Stellen werden **nicht** freigegeben, wenn tatsächlich kein Gegner hindurchpasst. Große Brutes werden an schmalen Portalen durch Ash Stalker ersetzt; keine Monster-Skalierung als versteckter Platzierungsersatz. Die Decken-Ausgangs-/Flugstreckenprüfung bleibt erhalten.

### Ressourcen

Frame drei statt vier Material-Batches, allerdings detailreicheres Mesh; keine pauschale Beschleunigungsbehauptung. Die kleineren Öffnungen verwenden pro Auge 896×1240 für Wand bzw. 768×768 für Decke statt bisher überall 1024×1240. Das sind 12,5 % bzw. ca. 53,5 % weniger Render-Zielpixel, kein gemessener Gesamt-FPS-Gewinn. Beide Augen und asymmetrische Projektion bleiben bestehen. Vorlademanifest um die zwei Texturen auf elf Assets erweitert.

## Validierung / Lieferung

252 native Unity-Prüfungen bestanden: die vorherigen 229, zwei zusätzliche Texturauflösungen und 21 Portalprüfungen. Enthalten sind tatsächliche FBX-Metermaße, Geometrie-/Batchgrenze, Footprint-/Skalierungsvertrag, Deckenmittelpunkt, ovaler Probenrand, enger Wandstreifen, Ressourcen und Shaderkompilierung. Blender- und Unity-Rahmenansicht visuell geprüft. Kein getragener V18.2-Raum-/Gameplaytest ersetzt.

Separates Paket: `Builds/QuestDemonMR-v18.2-portals.apk`, Version 0.18.2 / versionCode 20. Finales Build-/Installationsresultat steht im Buildbericht. Historische V18.1-/V18.0-/V17.2-Pakete bleiben erhalten.

Reproduktion: `bash Tools/build-v18.2.sh`. Engstellen, Austrittsanimationen und neue Deckenmaße müssen anschließend im tatsächlichen Raum beurteilt werden; die langen Benchmarkserien bleiben zurückgestellt.
