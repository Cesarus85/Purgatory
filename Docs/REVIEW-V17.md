# V17 – Diagnose und reproduzierbarer Leistungstest

Stand: 5. September 2026. **V17.1 installiert; 160 native Unity-Regressionstests und fünf Auswertungstests bestanden.** Drei tatsächliche Quest-Benchmark-Läufe sind gesichert, einschließlich Vorher-/Nachher-Nachweis für Sparse-Wunden: 1705 auf 90 Vollhaut-Bakes bei unveränderter Zahl exakter Dreieckstests. Aktuelle APK-/Installationsnachweise: [BUILD-REPORT.md](BUILD-REPORT.md), Messbefunde: [V17-FIRST-DEVICE-PROFILE.md](V17-FIRST-DEVICE-PROFILE.md). Die vollständige Geräte-/Langzeitabnahme bleibt ein getrenntes Gate. V16 wurde vom Nutzer als abgeschlossen bestätigt.

## Ergebnis dieses Schritts

V17 ergänzt Diagnose- und Messinfrastruktur, ohne die abgenommene Portalauflösung, Waffenpose oder Raum-Navigation auf Verdacht zu verändern. Nach den zwei echten Messläufen wurde die häufige Vollhaut-Neuberechnung für Wunden als konkreter Kandidat ausgewählt: V17.1 führt nur die betroffenen Hautpunkte exakt nach. Knochenqualität inklusive Android-Zwei-Knochen-Gewichtung wird berücksichtigt; nicht unterstützte Rig-Funktionen behalten den bisherigen vollständigen Pfad. Exakte Schusstreffer, Wundgeometrie und Lebensdauer bleiben unverändert. Dass dies der insgesamt größte Engpass ist oder bereits ein bestimmter FPS-Gewinn entsteht, wird nicht behauptet.

### Raumdiagnose, standardmäßig ausgeschaltet

- Linken **X-Knopf eine Sekunde halten**: Diagnoseansicht und lokale Aufzeichnung ein/aus.
- Dünne Umrisse für vorhandene Scene-Anker: Wände, Boden/Decke und Möbelvolumen. Keine gefüllten Flächen über realen Hindernissen; höchstens 192 Linien. Global-Mesh-Bounding-Volumes werden nicht als vermeintlich massive Raumhindernisse angezeigt.
- Grün: ein Live-Strahl meldet `NoHit`; Rot: bestätigter Live-Treffer; Gelb: unbekannter/nicht auswertbarer Status. Scene-Umrisse sind cyan. `NoHit` ist keine Garantie für einen vollständig freien Raum.
- Echte Flugproben und Blockaden werden angezeigt. Während der Diagnose kommt ein einzelner Blickrichtungsstrahl alle 0,2 s hinzu. Im Belastungstest ist diese zusätzliche Visualisierung/Abfrage ausgeschaltet.
- Flugblockaden werden nach Höhenlimit, Scene-Geometrie oder bestätigter Live-Oberfläche protokolliert; Recovery, Spawns, Tod, Landung und Pause ebenfalls. Die Ausweichlogik selbst bleibt V16.
- Im Editor existieren entsprechende Play-Mode-Menüpunkte unter `Quest Demon MR/V17`.

### 60-Sekunden-Belastungstest

Vor Beginn einer Runde **X und Y am linken Controller gemeinsam zwei Sekunden halten**. Der Test startet nur mit geladenem Raum und vorhandenem Head-Rig, bei Welle 0 und ohne laufende Runde/Gegner. Eine bereits begonnene, pausierte Runde wird nicht gelöscht oder übernommen. Gegebenenfalls App neu starten, ohne eine Runde zu beginnen.

| Zeit | Angeforderte Last |
| --- | --- |
| 0–10 s | Baseline, keine Testgegner/-portale |
| 10–20 s | Ein dauerhaft offenes Testportal |
| 20–30 s | Zwei Testportale |
| 30–45 s | Zusätzlich bis zu drei Gegner, entsprechend freier Raumfläche |
| 45–60 s | Zusätzlich kontrollierte Mesh-Treffer, Wunden, Schuss-/Treffer-Sound, Mündungsblitz und Tracer |

Seed `170917`, Portalvariantenfolge zurückgesetzt. Das ist ein reproduzierbarer Szenarioablauf, kein bitgenauer Replay: Kopfbewegung, Raumdaten, Frametiming und verfügbare Spawns beeinflussen das Ergebnis. Dieselbe sichere Kopfposition/Blickrichtung und derselbe Raum sind Voraussetzung für Vergleiche.

Die vorhandenen sicheren Spawnregeln bleiben aktiv. Nicht platzierbare Portale/Gegner werden übersprungen und als solche geloggt. Der Test fordert zwei Portale an, behauptet aber nicht, dass tatsächlich zwei sichtbar waren. CSV enthält die sichtbare Portalzahl, gerenderte Portalpixel und tatsächliche lebende Gegnerzahl.

Kein Spielerschaden, keine Punkte, keine verbrauchte Munition und keine Benchmark-Haptik. Angriffe und Feuerbälle dürfen visuell laufen; der normale Wellenablauf bleibt angehalten. Kein automatisches Bewegen der Kamera oder des Spielers. Im sicheren Bereich bleiben; nicht versuchen, durch reale Hindernisse beide Portale gleichzeitig ins Bild zu bekommen.

Abbruch: X+Y erneut zwei Sekunden halten oder auf das Pult schießen. App-Pause/Fokusverlust beendet den Test ebenfalls. Eigene Testobjekte, Projektile und kurzlebige Effekte werden aufgeräumt, Schussblitz/-Audio gestoppt; die App kehrt zum pausierten Startzustand zurück. Unity-Zufallszustand und Portalfolgenzähler werden wiederhergestellt. Normale Beendigung und geschlossene Messdateien sind auf Quest in zwei Läufen nachgewiesen; gezielte Abbruch-/Fokus-/Pause-Tests bleiben offen.

### Messung und Datenhaltung

- Framezeit aus Unitys `unscaledDeltaTime`, Durchschnitt, Fenster-P95 und Maximum; CPU aus `FrameTimingManager`, GPU aus dem aktiven `XRDisplaySubsystem`, sofern verfügbar.
- CPU-/GPU-Werte werden nicht addiert. Ungültige oder fehlende Werte bleiben leer/`n/v`, nicht 0 ms. CPU-Zeitstempel verhindern die Wiederverwendung desselben alten CPU-Samples.
- Bildwiederholrate aus XR, andernfalls explizit als Zielwert 72 Hz gekennzeichnet. Die Zählung von Budgetüberschreitungen ist keine Ersatzmessung des Compositors.
- Unity-Allocator-Speicher und verwalteter Heap, GC-Zyklen, instrumentierte Flug-/Diagnosestrahlen, Mesh-Bakes, geprüfte Dreiecke und Flugblockaden. Das sind nicht der gesamte Prozess-RAM und nicht alle Raumabfragen des Spiels. Ab `0.17.1` zählt GC nur `CollectionCount(0)`, um Generationen nicht mehrfach zu zählen; ältere `0.17.0`-GC-Spalten verwendeten deren Summe und sind nicht direkt vergleichbar.
- P95 ist die obere Grenze eines 0,25-ms-Histogrammbins innerhalb eines etwa einsekündigen Fensters. Kein erfundener globaler P95 aus gemittelten Perzentilen.
- Aufnahme schreibt ausschließlich lokal nach `Application.persistentDataPath/Diagnostics/v17-…/`: `metadata.txt`, `frames.csv`, `events.csv`. Einzigartige Sitzungsnamen, maximal 1200 Messzeilen und 2048 Ereignisse pro Aufnahme; keine alten Aufnahmen werden überschrieben oder automatisch gelöscht.
- Die neuen CSV-Dateien enthalten keine Raumkoordinaten oder Kamerabilder. Bereits bestehende Unity-Debuglogs können weiterhin räumliche Debugangaben enthalten. Kein Upload wird ergänzt.
- Diagnose-Overlay-Overhead ist in jeder Messzeile gekennzeichnet. Sampling-Statistik selbst allokiert nach Initialisierung keine verwalteten Objekte; Export/Textdarstellung können allokieren und sind als Messaufwand zu berücksichtigen.
- Temperatur, thermische Drosselung und vollständige Compositor-Metriken werden hier nicht vorgetäuscht; dafür bleiben echte Quest-Metriken und eine längere Sitzung erforderlich.

Profiler-Marker: `QDMR.MeshBake`, `QDMR.ExactMeshRaycast`, `QDMR.WoundPatch`, `QDMR.SparseWoundSkinning`, `QDMR.FlightDepthProbe`, `QDMR.LiveGridSample`. Damit lässt sich nach einem Geräteprofil zwischen Haut-Baking, Dreieckstests, Wunden und Raumabfragen unterscheiden.

## Tatsächlich ausgeführte Prüfungen

1. Gesamte Runtime-C#-Assembly mit den neuen Dateien gegen die vorhandenen Unity-/Meta-Referenzen kompiliert: erfolgreich.
2. Editor-C#-Assembly einschließlich beider V17-Build-Einstiege kompiliert: erfolgreich.
3. **36 reine C#-Prüfungen bestanden:** fehlende/ungültige Messwerte, unabhängige CPU-/GPU-Samplezahlen, Mittelwerte, Histogramm/P95, lange Stalls, allokationsfreier Sampling-Hotpath unter .NET, alle Phasengrenzen, Startschutz und begrenzte/dateisichere CSV-Ausgabe.
4. **5 Python-Auswertungstests bestanden:** gewichtete Mittelwerte, fehlende GPU-Werte als `null`, korrekt bezeichnete Fenster-P95, Warnungen bei übersprungenen Inhalten, fehlenden Phasen, Overlay-Overhead und Ziel-Hz statt gemessenen Hz.
5. Alle **103 nativen Unity-Editor-Prüfungen bestanden**: 28 aus V15, 39 aus V16 und 36 aus V17. Marker `QDMR_V15_REGRESSION_OK`, `QDMR_V16_REGRESSION_OK`, `QDMR_V17_REGRESSION_OK` im neuen Log `work/unity-v17-build.log` des übergeordneten Workspace. Die neue Statistik-Allokationsprüfung lief damit auch unter Unity, nicht nur unter .NET.
6. Versionierte V16-Rückfall-APK unverändert: SHA-256 `059a9738ee1cf50ae8d0710212d8c3810ea312d51ff18a2f6a196e99c114eed6`. Die unversionierte Standard-APK ist ein Build-Ausgabeziel und wird durch V17 ersetzt.
7. Weitere 57 native Geometrie-/Allokations-/Fallback-Prüfungen für Sparse-Wunden bestanden; der integrierte V17.1-Lauf enthält insgesamt **160** bestandene Prüfungen. Produktionsclips, vier Posen, drei Knochen-Qualitätsstufen und nichtuniforme Skalierung gegen Unitys BakeMesh geprüft.
8. Zwei reale Quest-Sitzungen vollständig ausgelesen und analysiert: Development mit einem Portal/zwei Gegnern sowie Nicht-Development mit zwei sichtbaren Portalen/drei Gegnern. Im zweiten Lauf Kampf-Framezeit im Mittel 13,889 ms; ein kurzes VrApi-GPU-Fenster ist zusätzlich gesichert. Details und Grenzen im Geräteprofil, keine Langzeitfreigabe.

Reproduktion im Projekt: `bash Tools/verify-v17.sh`. Dieses Hilfsskript nutzt die vorhandenen Compiler-Referenzdateien aus dem letzten Unity-Import; es ist kein Ersatz für einen neuen Unity-Import, IL2CPP-Build oder ein natives Spiel. Ausgaben liegen unter `Verification/V17/`. Die `AnalyzerFixture`-Daten sind ausdrücklich synthetische Testdaten, keine Quest-Messungen.

## Behobene Systemblockade

Der erste, frühere Versuch war durch die damalige Systemfreigabe blockiert:

- ADB: `could not install smartsocket listener: Operation not permitted`.
- Unity Package Manager: `listen EPERM … /tmp/Unity-Upm-1511.sock`; zusätzlich schreibgeschützte Datenbankzugriffe. Der Unity-Versuch endete mit Exitcode 1, bevor die Regressionstests liefen. Log: übergeordnetes `work/unity-v17-baseline.log`.

Nach Wiederherstellung des Systemzugriffs laufen Unity Package Manager und ADB regulär. Quest 3 `2G0YC5ZG9609PY` wurde verbunden erkannt. Die oben aufgeführten nativen Tests sind jetzt erneut bestanden. Die alte Beschränkung wurde nicht umgangen und ist kein aktueller Blocker mehr.

Der Builder und die beim neuen Import serialisierten ProjectSettings verwenden nun Version `0.17.1` / versionCode `17`. Die beiden Baseline-APKs verwenden `0.17.0`. Der APK-Nachweis wird separat anhand des tatsächlichen Build-Artefakts geführt.

## Build- und Geräteprotokoll

1. `QuestDemonMR.Editor.FollowupV17Validation.ValidateAndBuild` aufrufen: Projekt vorbereiten, alte und neue Checks ausführen, Development-APK bauen und als `Builds/QuestDemonMR-debug-v17.apk` sichern.
2. `QuestDemonMR.Editor.FollowupV17Validation.ValidateAndBuildMeasurement`: gleiche Tests, zusätzlich nicht-development APK `Builds/QuestDemonMR-measure-v17.apk`. Beide verwenden dasselbe Paket; nur eine Variante ist gleichzeitig installiert. Der Messbuild ist keine Store-Freigabe und bleibt lokal signiert.
3. Signatur, Version und Installation als Update prüfen. Keine App-Daten löschen.
4. Auf Quest zuerst Abbruch, App-Pause und Startzustand testen; dann drei gleiche Benchmark-Läufe im selben Raum. Zweite Raumgröße ergänzen. Nicht vergleichbare Läufe mit übersprungenen Inhalten getrennt halten.
5. Messverzeichnis vom Gerät holen und `python3 Tools/analyze_v17.py /pfad/zur/sitzung` aufrufen. Das Skript gibt einen JSON-Bericht auf stdout aus und verändert keine Messdateien. Warnungen markieren fehlende Phasen, zu selten zwei sichtbare Portale, fehlende Treffer-Effekte und nicht verfügbare Hardwarewerte; kein automatisches „Quest bestanden“.
6. Mit tatsächlichen CPU-/GPU-Daten den größten Engpass auswählen und vor/nach derselben Szene messen. Erst dann Portal-Qualitätsstufen, Pooling oder Treffertest-Optimierung freigeben.
7. 20-Minuten-Lauf mit externen Quest-Metriken für stabile 72 Hz, Frametimes und Wärme. Danach V17 abschließen und zu V18 wechseln.

**Offenes Geräte-Gate:** Gezielte Abbruch-/Fokus-/Pause-Tests, vollständigeres CPU-/GPU-/Compositor-Profil, weitere Raumgröße und Langzeittest. Erzeugungsspitzen bleiben ein eigener Optimierungskandidat. Der Sparse-Vorher-/Nachher-Nachweis ist inzwischen erbracht; APK-/Installationsstatus steht im Buildbericht. V17 ist noch nicht als vollständig abgeschlossen markiert.
