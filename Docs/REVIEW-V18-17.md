# V18.17 – fortgesetzter Scan, vertikaler Fledermausschacht und schnellere Austritte

Stand: 6. September 2026. Nutzerauftrag nach Diagnose der vier Beobachtungen. Vor Beginn per ADB bestätigt: tatsächlich installiert war **0.18.15 / code 33**, letzte Aktualisierung 16:05:48. V18.16 war nur gebaut; fehlende Brücke/Sprünge im letzten Test waren daher keine Widerlegung dieser Implementierung. Das verdrehte Deckenbild und der harte Scan-Allokationsstopp waren dagegen echte bestehende Probleme.

**Lieferstatus: installiert am 6. September um 17:14:15, Version 0.18.17 / code 35.** 1.405 native CHECK-Meldungen (32 neue Expansion-Prüfungen), Release-Signatur sowie tatsächliche Geräteversion und APK-Hash bestätigt. Daten/Berechtigungen erhalten, nicht automatisch gestartet. Getragene Abnahme offen; [vollständiger Nachweis](../Verification/ScanExpansion/delivery-v18.17.txt).

## Implementierter Umfang

### 1. Speicherbegrenzte, weiterlaufende Raumerfassung

Vorher: maximal 256 Blöcke mit je 1,28 Metern Kantenlänge, dauerhaft GPU-resident. Danach keine neue Fläche, lediglich Aktualisierung vorhandener Blöcke. Nicht nur Möbel/Wände, sondern auch beobachteter Leerraum belegt Blöcke. Der Codebefund war eindeutig; ein erhaltenes Laufprotokoll, das die tatsächliche Auslastung beim beanstandeten Test beweist, lag nicht vor.

Jetzt getrennte Budgets: **bis 768 gespeicherte Blöcke, 128 aktiv residente GPU-Blöcke**. Beim GPU-Wechsel bleiben CPU-Messdaten und vorhandene Mesh-Collider erhalten. Reaktivierung stellt die gespeicherten Distanzen/Gewichte wieder her. Laufende Readbacks und auf Geometriearbeit wartende Daten werden nicht verdrängt; noch unbestätigte GPU-Felder dürfen konservativ zu unbekannt zurückfallen, damit sie die Residenz nicht dauerhaft blockieren.

Bei vollem CPU-Archiv darf nur der am weitesten entfernte, ungeschützte Block entfernt werden. Geschützt sind Blockzentren innerhalb neun Metern um den Kopf, vier Metern um aktive Gegner/Portale und drei Metern um den Schrein; laufende Readbacks/Geometrieaufgaben bleiben ebenfalls geschützt. Entfernte Collider werden deaktiviert, die Kartenrevision erhöht. **Entfernt bedeutet unbekannt, nicht frei.** Beim Wiederbetreten kann der Bereich neu entdeckt werden. Sind alle Blöcke geschützt, bleibt die ehrliche Kapazitätswarnung; kein stilles Löschen benutzter Wände.

GPU-/Worker-/Commit-Frequenzen aus V18.15 bleiben begrenzt. Initiale Vorbereitung → Scan → Schreinplatzierung bleibt getrennt. Neue Zustandsprotokolle enthalten GPU-Residenz, Recyclingzähler und tatsächliche Kapazitätsblockade. Die höhere CPU-Kapazität kann mehr Speicher und Collider bedeuten; daraus wird **keine bereits gemessene Quest-Performanceverbesserung** abgeleitet.

Unverändert: keine gespeicherte Meta-Raumgeometrie im Standardpfad; nur tatsächliche Tiefenbeobachtungen. Sechs Meter Messbereich und sieben Meter Integrationsnähe sowie konservative Unbekannt-/Freiraumregeln bleiben. Weitergehen ermöglicht weitere Erfassung; keine unendliche Reichweite vom Startpunkt, kein garantiert vollständiger Raum und kein hier neu bewiesenes Laser-Tag-Niveau. Fokusverlust/Relokalisierung setzt die Sitzung weiterhin konservativ zurück.

### 2. Eigene vertikale Decken-Kulisse

Original in Blender erzeugter **Fledermausschacht**: offener Mund bei Y=0, hohler aufweitender Felsschacht nach oben, unregelmäßige Schichten, abgebrochene Vorsprünge, herabhängende Felszähne, modellierte Nester/Kokons und Ketten sowie eine entfernte Glutöffnung in etwa 19 Metern Höhe. Keine seitlich gedrehte Schmiede oder Brücke.

Sowohl Kamera- als auch Gegnerabbildung verwenden dieselbe vertikale Portalbasis. Reales Oben bleibt auch in der entfernten Welt Oben. Der bestehende Stereo-Pro-Auge-/Off-Axis-Renderer und die Auflösung bleiben erhalten. Die Boden-Kulissenauswahl hat einen unabhängigen Zähler: Auch zwischen beliebig vielen Deckenportalen wechseln die nächsten Bodenportale zwischen Schmiede und Lavabrücke.

Blender-Rezept `BlenderSource/build_shaft_v18_17.py`, Quelle `BlenderSource/BatShaftV18_17.blend`, Runtime-FBX/Atlas `Resources/Models/BatShaftV18_17*`. **25.826 Dreiecke, vier Asset-Materialslots, eigener 2048²-Atlas mit gebackener Verschattung, ASTC 6×6**. Sky, Rahmen und Effekte sind zusätzliche Renderarbeit und nicht in diesen vier Slots enthalten. Warmup-Manifest jetzt 26 Assets. Keine neuen Fremdassets oder Lizenzen.

### 3. Schnellere und flexiblere Ankunft

- Normaler Bodenauftritt **1,15 statt 2,2 Sekunden**, normaler Fledermausauftritt **0,95 statt 1,7 Sekunden**.
- Umschauen seltener (Welle/Index modulo sieben), insgesamt zwei statt 3,15 Sekunden; die Pose ist weiterhin wirklich animiert.
- Jeder zweite gewählte Bodenauftritt versucht die V18.16-Sprungvariante. Ein geeigneter schlanker Gegner darf sie auch aus einem kompakten Wandportal verwenden. Brutes bleiben ausgenommen.
- Drei geprüfte Landestrecken statt nur einer: 0,65 / 0,35 / 0,10 Meter über den bisherigen Austritt hinaus. Bei unzureichendem Abstand oder unbekannter/blockierter Kurve weiterhin normaler Austritt; Ablehnung wird protokolliert. Kein Erzwingen durch Möbel/Wände.
- Enthält die V18.16-Kampfsprünge und kopfüber Fledermaus-Bursts mit gefalteten Flügeln und Rumpfrolle. Keine Unverwundbarkeit während des Austritts, keine Waffenabschwächung oder Änderung der Audiopegel/Lebensversorgung.

## Prüfung und Lieferung

Reproduzierbar mit `Tools/build-v18.17.sh` / `Tools/package-v18.17.sh`; neue `ScanExpansionValidation` verkettet die gesamte vorherige native Regression einschließlich ArrivalVariantValidation. Finalen Build-/Signatur-/Installationsstand siehe [Buildbericht](BUILD-REPORT.md) und `Verification/ScanExpansion/delivery-v18.17.txt`.

Neue Prüfungen decken mehr als 256 Blöcke, beide Speicherbudgets, Rotation von GPU-Residenz, erhaltene CPU-Evidenz, echten GPU-Restore, geschützte Readbacks/Portalregionen, Recycling bei vollem Archiv, Unbekanntheit und Neuentdeckung, Reset und Residenz ohne bestätigte Samples ab. Zusätzlich eigener importierter vertikaler Schacht, Weltachsen einschließlich FBX-Basiskonvertierung, gemeinsames Kamera-/Akteurmapping und Boden-Auswahl trotz eingeschobener Deckenportale. Frühere Poseprüfungen wurden zeitlich auf den kürzeren Peek-Ablauf verschoben, nicht entfernt.

Art-Vorschauen: `Verification/ScanExpansion/shaft-blender.png` sowie native Aufnahmen `shaft-up.png` / `shaft-offset.png`; Arrival-Regression unter `ArrivalRegression/`. Editorbilder beweisen keine getragene Stereowirkung oder reale Sensor-Performance. Frühere fehlgeschlagene Testläufe bleiben als Logs erhalten; Testreflexion, alter Peek-Zeitpunkt und lokale statt transformierte FBX-Bounds wurden korrigiert.

## Getragener Quest-Test bleibt erforderlich

1. Versionsstand prüfen, mehrere Bodenportale für beide Schauplätze abwarten; Deckenportal direkt von unten und schräg ansehen.
2. Normalen Lauf, seltenes Umschauen, Sprung und Fledermaus-Burst beobachten: Tempo angemessen, keine falschen Treffer-/Rollposen?
3. Erst Startbereich, anschließend weitere Bereiche ansehen/erlaufen; Diagnosewerte müssen neue Blöcke/Revisionen zeigen. Bekannte reale Hindernisse müssen erhalten bleiben, unbekannte Stellen dürfen nicht zum freien Weg werden.
4. Bei längerer Erfassung CPU/GPU-Speicher, Framezeiten und thermische Last messen. Recycling bei längerer Ortsveränderung, Zurückgehen und viele gleichzeitig geschützte Bereiche am Gerät testen.

Kathedralen-Seitenhalle und ortsspezifische Bodenrahmen bleiben offene A10-Arbeit. Hintere Bodenportalverteilung und V17-Langzeittests nicht pauschal als erledigt behandeln.
