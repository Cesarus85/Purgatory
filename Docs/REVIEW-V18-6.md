# V18.6 – Ashwarden-Revolver / Qualitätssprint A4

## Auftrag und Ergebnis

Der Nutzer bestätigt A3 und ersetzt die bisherige Pistole ausdrücklich durch einen urigen, nach dem Schuss rauchenden Revolver. A4 wird entsprechend erweitert; A5 muss den Revolverklang und seine Mechanik berücksichtigen. Raumscan, Portale und Gegner bleiben unverändert.

- Originaler Blender-Entwurf **Ashwarden**: achteckiger Lauf mit offener Krone und vertieftem Innenlauf, sechskammerige kannelierte Trommel, geschwungener Abzugsbügel, dunkle Holzgriffschalen, Messingintarsien, Schrauben und kleine Ziergravuren. Keine neuen externen Assets oder Käufe.
- Reproduzierbare Quelle: `BlenderSource/build_revolver_v18_6.py`, gespeicherte Szene `AshwardenRevolverV18_6.blend`. Patina/Holzmaserung werden in Blender auf einen gemeinsamen 2048²-Albedoatlas gebacken; Android ASTC 6×6 mit Mips. Kein fotorealistischer Scan, sondern ein originaler stilisierter Spielgegenstand.
- Fünf mechanische Meshgruppen, **22.264 importierte Dreiecke** (Blender vor Importbereinigung: 23.016), fünf gemeinsam genutzte PBR-Materialtypen. Meshgruppen sind nicht gleich Drawcalls: Material-Submeshes bleiben getrennt.
- Trommel dreht pro Schuss 60°, Hahn spannt beim Triggerziehen und fällt bei Schussabgabe, Abzug kehrt zurück. Beim Nachladen schwenkt die Trommel samt sichtbarem Kran aus und wieder ein. Kein Pistolenverschluss, kein herausfallendes Magazin. Noch keine einzeln animierten Patronen oder manuelles Patronenladen.
- **6 geladene Patronen + 54 Reserve = weiterhin 60 Startschüsse**. Einzelschussschaden bleibt 1; Mindestabstand zwischen Schüssen 0,22 s, Trommelnachladen 1,25 s (automatisch zusätzlich bisherige 0,3 s Wartezeit). Teilnachladen überträgt nur fehlende Patronen; vorhandene Pickups/Wellenbelohnungen und automatisches Nachladen bleiben erhalten. Häufigeres Nachladen ist eine bewusste Folge der sichtbaren Sechsertrommel, kein neuer Munitionsverlust.
- Controllerkalibrierung bleibt gespeichert. Neuer Modellversatz relativ zum Waffenanker `(0, +0,015, -0,006)` m; Griffnähe und reale Handhaltung müssen am getragenen Headset beurteilt werden.
- Schuss und Rauch starten am importierten Laufsocket. Kurzer, sich verjüngender Feuerstoß, dezenter Trommelspalt-Gasstoß und **0,6–1,25 s auslaufender Rauch**. Nach kurzen Serien raucht der Lauf etwas länger nach. Partikel bleiben in Weltkoordinaten zurück, statt an der bewegten Hand zu kleben.
- Drei wiederverwendete Partikelsysteme mit zusammen maximal **64 Partikeln**, geteilte Materialien, keine pro Schuss erzeugten Effektobjekte/Materialien oder dynamischen Lichter. Eine wiederverwendete, dünne 22-ms-Schussspur ersetzt die neu erzeugte breite Linie. Normale Treffer-Effekte außerhalb der Waffe sind nicht Teil dieser Pooling-Aussage.
- Shader berücksichtigen Stereo und Meta-Tiefenverdeckung. Keine künstliche Kopf-/Kamerabewegung. Mechanik, Rauch und Nachladen pausieren; Reset leert die Effekte und schließt die Trommel. Nachladen per Taste startet nicht mehr im Pausenmenü.

## Prüfung und Korrekturen

**401 native Unity-Prüfungen bestanden:** 363 bisherige, zwei zusätzliche Warmup-Assets und 36 neue Revolverprüfungen. Enthalten sind importierte Geometrie/Laufachse, Trommelzentrierung und vollständige Umdrehung, Hahnkontakt, Pause/Reset, echte `Fire`-Anbindung, Teilnachladen und knappe Reserve, Renderer-/Partikellimits, endliche Lebensdauer, Shaderimport sowie fünf GPU-Vorschauen.

In der ersten Vorschau wurden eine falsche FBX-Achsenoption und eine doppelte Elterntransformation der Trommel erkannt. Import/Blender-Hierarchie wurden korrigiert. Flächennormalen und Trommelkanten wurden bereinigt. Ein rückwärts über den Lauf gestreckter Blitz wurde durch einen kurzen, vor der Laufkrone platzierten Feuerstoß ersetzt. Erst die korrigierte Fassung geht in den Build.

Vorschauen in `Verification/RevolverPolish/`: `revolver-three-quarter.png`, `revolver-player-view.png`, `revolver-reload.png`, `revolver-shot.png`, `revolver-smoke.png`. Diese wurden tatsächlich angesehen; sie ersetzen keine Stereo-/Passthrough-Abnahme auf der Quest. Build-/Installationsnachweis im [Buildbericht](BUILD-REPORT.md).

## A5 verbindlich angepasst

Der bisherige Schuss-/Nachladeklang ist **in A4 noch Übergang**. A5 ersetzt ihn durch passende Revolvervarianten mit trockenem Knall, kurzem tiefem Körper und Hahn-/Trommel-/Ladegeräuschen an den sichtbaren Phasen. Trefferklänge werden separat nach Fleisch, harter Oberfläche und Kill unterschieden. Dabei Lautheit begrenzen, Wiederholungen vermeiden und Shot-/Hit-Haptik so abstimmen, dass der Trefferimpuls den Hauptschuss nicht abschwächt. Keine Klangverbesserung bereits aus dem neuen Modell ableiten.

Offen: subjektive Wirkung, Controllergriff, Rauch vor hellem Passthrough, echtes Stereo/Live-Depth, Nachladetempo unter Gegnerdruck und neue Performance-/Thermikmessungen. Keine neue Hauptroadmap-Freigabe aus Editor oder APK-Installation ableiten.
