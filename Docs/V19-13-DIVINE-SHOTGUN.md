# V19.13 — Göttliche Rettung / Pump-Shotgun

## Umsetzung

1. Nutzer-Modell in Downloads/Shotgun prüfen, in Blender achsen- und griffgerecht exportieren. Beweglicher Pumpgriff, gebackene Materialien, Quest-Texturimport; Originale bleiben unverändert.
2. Einmal je Welle bei 1–49 Leben und mindestens drei lebenden, vollständig ausgetretenen Gegnern aktivieren. Kein Auslösen in Einrichtung, Pause oder Benchmark. Weiß-blauer Himmelsriss mit goldenen Strahlen; kein Vollbildblitz.
3. Fünf Bonuspatronen, zunächst ungeladen. Freie Hand: Grip nahe Vorderschaft halten, zurückziehen und vorschieben. Handrollen, Pause, Trackingverlust, Wurfsterne und Neustart berücksichtigen.
4. Streuschuss mit echten Sicht-/Geometrieprüfungen und aggregiertem Schaden pro Gegner. Zentrierte Treffer aus kurzer Distanz tödlich, Randtreffer schwächer; keine pauschalen Kills durch Wände. Revolver kehrt nach fünf abgegebenen Schüssen mit unverändertem Munitionsstand zurück.
5. Eigene Pump-, Schuss-, Treffer- und Segensklänge. Bestehende Lautstärkeeinstellungen beachten, Stimmen begrenzen. Mündungsfeuer/Rauch und kompakte Zustandsanzeige.
6. Modell-/Mechanik-/Handrollen-/Kampf-/Lebenszyklus-Regression, Unity-Vorschauen, Android-Build. Echte beidhändige Ergonomie und Soundbalance bleiben Quest-Abnahme.

## Ergänzung des Nutzers: Todes-Splatter

Nach Abschluss der Shotgun-Funktionsprüfung: gerichtete, ballistisch fallende Bluttropfen beim Tod von Dämonen und Fledermäusen. Getroffene reale Flächen erhalten unregelmäßige dunkle Blutflecken mit feuchten Glanzrändern. Keine schwebenden Ersatzflecken, keine Decals auf Gegner-Kapseln. Rückstände bleiben einige Zeit und trocknen/verblassen; feste Obergrenzen für Tropfen und Flächen, pausierbare Laufzeit, Bereinigung bei Raumwechsel/Neustart. Wände, Boden, Möbel und unbekannte Geometrie werden separat geprüft.

## Geplanter Headset-Test

- Mit drei Gegnern im Raum unter 50 Leben: Segen erscheint, Waffe wechselt. Bei genau zwei Gegnern oder 50 Leben nicht.
- Beide Händigkeiten: Grip am Holz-Vorderschaft, etwa 8 cm zurück und wieder vor; keine ausgelösten Sterne. Abzug ohne Pumpen verbraucht nichts.
- Fünf Schüsse einschließlich Fehlschüssen verbrauchen fünf Patronen; danach Revolver und Sterne wieder verfügbar. Nur eine Rettung je Welle.
- Pause/Trackingverlust während des Pumpens, Handwechsel in Pause, Tod und Neustart: keine Phantomladung oder verlorene Inventare.
- Naher zentrierter Treffer, Streifschuss und Gegner hinter realer Wand prüfen. Bewegungsfreiheit und Schussrichtung beider Hände prüfen.

## Ergebnis

Implementiert:

- Vorbereitete Blender-Shotgun mit 25.375 Dreiecken in zwei Meshes, gebackenen 1K-PBR-Karten, angepasstem Griffursprung und separater Pumpe. Modelllänge ca. 92 cm, Pumpgriff ca. 39 cm vor Controller, mechanischer Hub 7,5 cm. Originale im Download-Ordner nicht verändert.
- Weiß-blaue, räumlich gestaffelte Wolkenöffnung mit sieben goldenen Lichtschächten. Waffe wechselt nach 1,15 Sekunden; Effekt blendet bis 3,4 Sekunden aus. Keine zusätzliche Portalkamera, kein Vollbildblitz. Kein Schutzschild oder heimliches Aufheilen.
- Strenger Auslöser: laufendes Spiel, Welle > 0, 1–49 Leben, mindestens drei lebende vollständig eingetretene Gegner. Einmal pro Welle. Pause/Benchmark/Einrichtung lösen nicht aus.
- Ungeladene Fünf-Patronen-Bonuswaffe. Grip in 16 cm Nähe zum Vorderschaft, zurück bis mindestens 88% Hub, vor bis höchstens 14%; durchgehend gehaltener Grip funktioniert auch nach dem nächsten Schuss. Trackingverlust und Pause verwerfen unvollständige Pumpzüge, erhalten aber eine bereits geladene Patrone. Handwechsel bleibt möglich.
- 13 geometrisch geprüfte Streukugeln in einem 5,2°-Kegel. Bis 2,4 m je 0,82 Schaden, abfallend auf 0,3 bei 7 m; maximale Reichweite 12 m. Normale Gegner sterben bei vier guten Nah-Pellets, große Brutes bei sieben. Kein pauschaler Ein-Schuss-Kill bei einem Randtreffer. Auch die Strecke zwischen Hand und Mündung wird auf blockierende Wände geprüft.
- Ein Schaden-/Wund-/Treffersoundereignis pro getroffenem Gegner und Salve, nicht dreizehn parallele Treffer. Fünf abgegebene Schüsse inklusive Fehlschüssen; 0,48 Sekunden danach zurück zum Revolver mit dessen unverändertem Magazin/Reserve. Wurfsterne werden vorübergehend verstaut, ein gehaltener Stern zurückgegeben und der laufende Regenerationstimer beibehalten.
- Eigene 12-Gauge-Schussvarianten, zwei Pump-Phasen, tiefere Einschläge und Segensklang. Feste sechs zusätzliche Audiostimmen; bestehende Waffen-/Treffer-Lautstärken und Ausgangslimiter bleiben erhalten. Herkunft: `ExternalSource/FirearmAudioV18/SOURCE-Shotgun.md`.
- Todes-Blut für beide Gegnerfamilien und alle Waffen: 10–22 gerichtete räumliche Tropfen, Schwerkraft und echte Oberflächenkollision. Unregelmäßige, dunkelrote Flecken mit Satellitentropfen, Wandläufen und abtrocknendem Glanz. Bis 48 Sekunden Spielzeit sichtbar, sanftes Ausblenden, Pause friert ein. Flächen werden an allen vier Ecken geprüft und an Kanten verkleinert. Neue/ersetzte Scan-Geometrie wird nachgeprüft; fehlende Unterlagen entfernen die Spuren. Keine Spuren werden im Raumprofil gespeichert.
- Höchstens 96 Tropfen und 40 Flecken; zwei instanzierte Zeichenbatches auf unterstützter Hardware, keine zusätzlichen Physik-Collider oder pro Tod neu angelegten Partikelsysteme. Raum-Einrichtung und Run-Neustart räumen Rückstände auf.
- Exakte Trefferprüfung mit einem pro Mesh geteilten Dreiecksbaum und pro animierter Pose neu berechneten Grenzen. 1.200 Vergleichsstrahlen inklusive Deformation, Misses, Reichweitenlimit und nicht normierter lokaler Strahlen: gleiche Treffer wie Brute Force, 6.551 statt 960.000 geprüfte Dreiecke. Das ist ein CPU-Arbeitsnachweis, keine gemessene Quest-Framerate.

Focused-03: 138 Shotgun-Prüfschritte und 13 Splatter-Prüfschritte erfolgreich; vollständiger Lauf zusätzlich 14 Splatter-Prüfschritte einschließlich verschobener Möbel erfolgreich. Die alte Revolver-Audioprüfung wurde auf explizit fünf Revolver- plus sechs neue Shotgun-Stimmen angepasst. Vollständige Regression bis einschließlich Handrollen erfolgreich (`unity-qdmr-v1913-export.WOwRNb.log`): 3.172 geloggte Prüfschritte plus 1.200 BVH-Vergleichsstrahlen. Android-Export und Paketierung erfolgreich (Gradle 6m 1s, 117 Tasks). Bestehende SDK-/Editorwarnungen bleiben; keine C#- oder Shader-Kompilierfehler. Noch keine physische Quest-Abnahme.

### Geprüfte Lieferung

- `Builds/Purgatory-v19.13-divine-shotgun.apk` — 109.183.440 Bytes.
- Paket `de.stefanmaier.questdemonmr`, Version `0.19.13`, Code `52`.
- APK SHA-256: `0ccfb3af626267fafbe39f5e53c3ad4a3b7cb33c38028a22ea558feab05a89e2`.
- ARM64 `libil2cpp.so` SHA-256 im APK und im neuen nativen Build identisch: `042f33e7a8226b8cd105a1129788954525c432f498e9486d12f0db8a239ecbeb`.
- `aapt dump badging`, `unzip -tq`, `apksigner verify --verbose --print-certs` erfolgreich. Wie vorher Entwicklungszertifikat, SHA-256 `3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2`; als Update installierbar, keine Deinstallation/Datenlöschung nötig.
- Fertiger Unity-Export: `/private/tmp/qdmr-v1913-export.WOwRNb`; Paketierungsstage `/private/tmp/qdmr-v184-package.SE7n42` nun auf Code 52.
- Letzte ADB-Abfrage: kein Gerät verbunden. Nicht installiert. Alle V19.12-Änderungen sind enthalten; das bisherige V19.12-APK bleibt daneben erhalten.

Mobile Shaderprüfung: Im tatsächlichen Vulkan-Export von `QuestDemonMR/BloodAftermath` bleiben alle 18 Varianten nach Built-in- und Scriptable-Stripping erhalten (sechs eindeutige Programme). Daher keine pauschale Vergrößerung aller Projektshader nötig. Hintergrund zur Prüfung: Unity erläutert das [GPU-Instancing und Variant-Stripping](https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing.html); der [offizielle Editor-Quellcode](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorGraphicsSettings.cs) definiert `StripUnused=0`. Entscheidend ist hier das gemessene Exportprotokoll, nicht allein der Editor-Vorschaurender.

Zusätzlich auf der Quest prüfen: Blutspritzer beim Töten von Bodendämonen und fliegenden Fledermäusen, Ablagerung auf Sofa/Boden/Wand, Pause und Abklingen nach ca. 48 Sekunden sowie Performance bei mehreren schnellen Kills. Die V19.12-Prüfpunkte (Scan, Schrein, Monsterbewegung, Portalsanduhr, Controllerlatenz) bleiben Teil der Geräteabnahme.
