# V18.5 – A3: Feuer, Plasma, Aufladung und Einschlag

Stand: 5. September 2026. Vorgezogener [Qualitätssprint](IMPLEMENTATION-COMBAT-POLISH.md), nach Nutzerbestätigung von A1/A2 in V18.4.

## Sichtbare Änderungen

- **Feuer:** animiertes, unregelmäßiges Flammenvolumen mit heißem Kern und rot/orange ausklingender, frei im Raum liegender Spur.
- **Plasma:** eigener blauer Volumen-Atlas mit bewegten verzweigten Entladungen am Kopf; separat eingefärbter Dampf im Schweif, ohne ständig wiederholte Blitz-Glyphen.
- **Handladung:** dieselbe animierte Materialsprache, Wachstum bis zur Freisetzung; keine primitive glatte Kugel. Ladung gehört jetzt zum Gegnerobjekt und wird bei dessen Entfernung mit aufgeräumt.
- **Einschläge/Abfangen:** kurzer gerichteter Ausbruch, Ausglühen und Ende der Emission. Die alte Flugspur bleibt an ihren Weltpositionen und klingt aus, statt zum Einschlagspunkt zu springen. Kein generisches Einschussloch auf dem verschwindenden Projektil-Trigger.

Keine neue Audio-Abmischung, keine neuen Waffen-/Mündungsanimationen in diesem Paket. A4–A7 bleiben offen. Keine weiteren Änderungen an Scan, Portalen oder Gegnerrigs.

## Blender-Produktion und Quest-Budget

`BlenderSource/build_projectiles_v18_5.py` erzeugt zwei Original-Szenen (`InfernalFire.blend`, `IonPlasma.blend`) und backt jeweils 16 Frames. Feuer aus animiertem 3D-Dichte-/Emissionsfeld, Plasma zusätzlich aus bewegten dreidimensionalen Kurven. Keine externen Rasterassets und keine neue Fremdlizenz. Dies ist ein prozeduraler Volumen-Bake, keine physikalische Fluidsimulation.

- Zwei 1024×1024-RGBA-Atlanten, 4×4 Felder à 256 Pixel, transparenter Rand. ASTC 4×4 mit Mipmaps, keine behaltene CPU-Pixelkopie; etwa 2,7 MiB Texturspeicher einschließlich Mips, ohne Treiber-Overhead.
- Laufzeit: zwei ParticleSystem-Renderer pro fliegendem/einschlagendem Projektil; maximal **18 Kern- plus 28 Spurpartikel = 46**. Eine Handladung nutzt nur den Kern-Renderer. Drei gemeinsam benutzte Materialien, keine neue Materialinstanz pro Geschoss.
- Keine Echtzeit-Volumensimulation, keine Zusatzlichter, keine Kugel-/Splittermeshes, keine TrailRenderer-Bänder. Die sichtbaren Volumenbilder sind Billboard-Partikel, keine begehbare oder raymarched 3D-Flüssigkeit.
- Interpolation zwischen Animationsframes über Unitys UV2/AnimBlend-Vertrag; eigener premultiplizierter Shader mit Stereo-Initialisierung, Z-Test und Meta-Environment-Depth-Varianten. Transparentes RGB wird mit der Deckkraft ausgeblendet; Tiefenverdeckung dämpft Farbe und Alpha.
- Texturen und Materialvorbereitung in den bestehenden Start-Warmup aufgenommen, Manifest jetzt 13 statt 11 Assets. Endeffekte leben höchstens 0,85 simulierte Sekunden; Pause hält auch diese Frist an.

## Kollisionskorrektur im selben Projektilpfad

Vorher prüfte der Code zuerst den Spieler und erst danach Wände; außerdem nahm er den ersten unsortierten SphereCastAll-Treffer. Bei langen Frames konnte dadurch ein näherer Wandschutz übergangen werden.

Jetzt entscheidet der erste Kontakt entlang der gesamten Strecke: nächster rekonstruierter Raumtreffer (Kugelsweep), gegebenenfalls näherer Live-Tiefentreffer, dann der mathematisch bestimmte Eintritt in die Spieler-Trefferkugel. Wände gewinnen Gleichstände. Ein Start innerhalb bekannter Raumgeometrie beendet das Projektil. Physikabfragen berücksichtigen Raum-Mesh-Layer 28 statt beliebiger virtueller Grafik-Collider. Rekonstruktion und Meta-Tiefenabfrage selbst bleiben unverändert.

Geschwindigkeit (1,7 / 2,05 m/s), Schaden, Abschussradius, Lebensdauer von sieben Sekunden und nicht zielverfolgende Flugbahn unverändert. Ein verbrauchtes Geschoss deaktiviert seinen Schuss-Trigger sofort und kann weder nochmals schaden noch einen weiteren Treffer abfangen.

## Prüfung und Auslieferung

Native Regressionen einschließlich der V18.4-Gesichts-/Angriffstests sowie zusätzliche Asset-, Rendererbudget-, Lebenszeit-, Ladungsbesitz-, Kollisions- und GPU-Verdeckungsprüfungen. Finale Prüfsumme, APK-Status und Installation: [Buildbericht](BUILD-REPORT.md).

Vorschauen mit tatsächlichem Unity-Shader: `Verification/ProjectilePolish/projectile-flight-unity.png`, `projectile-impact-unity.png`, `projectile-occluded-unity.png`. Sie belegen Editor-Rendering, nicht die Wirkung im getragenen Headset oder gemessene Quest-Performance. Echte Stereo-/Live-Depth-Wirkung, Lesbarkeit im hellen Passthrough und gleichzeitige Projektile bleiben im Headset zu prüfen.

Reproduktion: Blender-Skript, danach `bash Tools/build-v18.5.sh`. Der Paketierer kann den ausdrücklich bekannten temporären V18.4-Gradle-Arbeitsordner als inkrementellen Compiler-Cache wiederverwenden. Nur dessen generierte Eingaben werden ersetzt; alte APKs und abgeschlossene Unity-Exporte bleiben erhalten. Fehlt dieser Ordner, wird ein neuer temporärer Buildordner angelegt.
