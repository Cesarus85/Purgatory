# V18.4 – erster Block des vorgezogenen Kampf-Qualitätssprints

Stand: 5. September 2026. Umfang: **A1 + A2** aus dem [Zusatzplan](IMPLEMENTATION-COMBAT-POLISH.md). A3–A7 sind damit nicht erledigt.

## Befund und Umsetzung

### A1: Gesicht und Bodendämon

Das Blender-Rig enthielt falsche automatische Gewichte auf voneinander getrennten Gesichtsschalen. Bei den 1640 Zahnvertices lagen die summierten Gewichte überwiegend auf `Neck`, aber auch auf beiden Oberarmen und `Chest`; die 268 Augenvertices waren ebenfalls teilweise an die Arme gebunden. Das erklärt das beobachtete Auseinanderziehen bei großen Armbewegungen.

- 2828 Augen-/Zahn-/Mundvertices fest an `Head` gebunden; angrenzende Schädelhaut konsistent gebunden, mit kurzer Übergangszone zum Hals. Insgesamt 4361 Gewichtebereiche/Vertices überarbeitet. Keine erneute automatische Heat-Bindung.
- Nahkampf neu in Blender: kurze asymmetrische Ausholphase, diagonaler rechter Klauenhieb, Nachschwingen und Erholung. Armziele im Modellraum gelöst statt ungeprüfter Eulerwinkel auf spiegelbildlichen Knochen. Füße und Root bleiben stehen; Oberkörperneigung am Kontakt neun Grad statt starkem Zurück-/Vorwerfen.
- Auch die Feuerball-Wurfgeste neu: Hände führen die Ladung vor dem Körper und stoßen sie aus, ohne den Oberkörper zu schleudern. **Projektilgrafik und Klang selbst noch unverändert.**
- Kontaktframe 84 von 71–100 passt zu etwa 0,421 s bei 0,94 s Gesamtdauer. Wurfkontakt 312 von 291–330 passt zu etwa 0,582 s bei 1,08 s Gesamtdauer. `CombatTiming.cs` hält die gemeinsamen Zeiten fest.

### A2: Fledermaus

- Vorhandenes externes CC0-Fledermausmodell weiterverwendet; kein aus Primitiven zusammengesetzter Ersatz. Herkunft bleibt `ExternalSource/VampireBatCC0` und dessen bisherige Lizenzdokumentation.
- Neues deformierendes `PelvicCurl`-Gelenk für den bisher starren Hinterkörper. Übertragung nur vorhandener Rumpfgewichte, Flügel-/Beinbindung erhalten.
- Neuer 31-Frame-Angriff: Ansetzen, Körper aufrichten, Hinterkörper einrollen, beide Krallen vorführen, Flügel als Bremse, kontrolliert wieder strecken. U-Pose hält über den Kontaktbereich Frames 18–21.
- Zusatzgelenk in Fly/Idle/Death explizit neutral animiert, damit der FBX-Export keine eingerollte Restpose in andere Takes übernimmt.
- Trefferphase 19/31 bei 1,05 s Angriffsdauer. Nicht mit einem Ganzkörper-Pitch als Ersatz für echte Deformation umgesetzt.

### Gemeinsamer Kontaktvertrag

- Ein neuer Angriff startet seinen Einmalclip bei Zeit null; wiederholte Update-Aufrufe starten ihn nicht neu.
- Trefferreaktion bricht Nahkampf, Wurf und Sturzangriff einschließlich ausstehendem Kontakt ab. Das verhindert unsichtbaren Schaden während eines anderen Clips.
- Blockierter Flugangriff verbraucht den Kontakt ohne Schaden und geht in die Erholung.
- Scan, Spawn-Footprint, Hindernistests, Portale, Waffe, Projektil-VFX und Audio unverändert, soweit nicht oben explizit genannt.

## Assets und Reproduktion

- `BlenderSource/build_combat_v18_4.py`, ausgeführt mit Blender **5.2.0 LTS**.
- Neue Quellen `RiftStalkerV18_4.blend`, `InfernalBatV18_4.blend`; alte `.blend`-Dateien erhalten.
- Bestehende Produktions-FBX-Pfade `EmberfiendAnimatedV12.fbx` und `InfernalBatAnimatedV13.fbx` absichtlich stabil gehalten (GUIDs, Cache und Materialverträge). Der Dateiname bezeichnet nicht mehr den aktuellen Animationsstand. Vorherige FBXs unter `Verification/CombatPolish/baseline-*.fbx`, außerhalb von Unity-Resources.
- Keine zusätzliche Textur, kein zusätzliches Mesh/Drawcall durch die Animation; ein zusätzliches Bat-Gelenk. Animationskompression für diese Imports ausgeschaltet, um die geprüften Kontakt- und Kopfkurven zu erhalten. Zusätzlicher Speicher-/Laufzeitaufwand noch nicht am getragenen Headset gemessen.
- `bash Tools/build-v18.4.sh`: native Tests, expliziter Unity-Gradle-Export in einen frischen temporären Ordner, danach isolierte Release-Paketierung. Bestehende versionierte APK wird nicht überschrieben. Export und Packaging bleiben für Diagnose erhalten.

## Prüfungen und offene Abnahme

- Blender: alle 365 Quellframes, maximaler Kopfraumfehler der Gesichtsschalen unter 0,000001 Modell-Einheiten.
- Native Unity: 269 bisherige + 32 neue Prüfungen = **301 bestanden**. Alle elf importierten Bodenclips über ihre gesamte Länge gebacken; maximale lokale Gesichtsabweichung etwa 0,0000011. Exakte Haut-Treffer auf beiden Kontaktposen weiterhin möglich.
- Bat-Tests: voller neuer Take, echte Bauchrotation über 80 Grad, beide Hinterkrallen mehr als 40 cm nach vorn bei Spielskalierung; Position vor/unter dem Kopf; neutraler Rücklauf und keine Pose-Lecks nach Fly/Idle/Death.
- Blender- und Unity-Produktionsmaterial-Vorschauen visuell geprüft: `Verification/CombatPolish/demon-84.png`, `bat-side-19.png`, `unity-demon-contact.png`, `unity-bat-contact.png`.
- Headset-Abnahme noch offen: kompakter Klauenhieb, Gesicht auch beim Feuerballwurf, U-Pose von vorne/seitlich, Flucht aus Angriff nach Treffer, blockierter Anflug. Keine neue Performance-/Thermikfreigabe und kein selbst durchgeführter getragener Spieltest behauptet.

Build-/Installationsstatus wird im [Buildbericht](BUILD-REPORT.md) separat nachgewiesen.
