# Purgatory V19.14 – Himmel, Pumpgriff, Streubild und Portal-Austritt

## Anlass und Befund

V19.13 verwendete drei flache Wolkenscheiben. Der Pumpgriff maß die freie Hand,
löste sich bei seitlichem Abstand und koppelte die Waffe nicht an die Stützhand.
Die Shotgun berechnete 13 gestreute Trefferrays, zeichnete aber ausschließlich
die Revolver-Mittelspur. PortalTraversal prüfte die komplette Scan-Freiraum-
Klassifizierung während des Sprungs erneut und konnte bei wechselnder Scan-
Konfidenz einfrieren/zurückziehen. Jeder tödliche Treffer während eines Austritts
nutzte außerdem dieselbe rückwärts gerichtete Ausblendung – auch im Raum.

Das sind reproduzierbare Codepfade, keine abschließende Diagnose einer
bestimmten Aufnahme auf der Quest. Eine reale Hardware-Abnahme bleibt nötig.

## Umsetzung

1. Originale Blender-Wolkenwelt (52.728 exportierte Dreiecke, sechs Meshes): verschmolzene,
   modellierte Wolkenbänke und ferne Sonne. Zwei 768²-Renderziele, getrennte
   augenabhängige Off-Axis-Projektionen; kopfpositionsabhängige echte Parallaxe.
   Kamera-/Weltkosten nur während des sichtbaren, 3,4 Sekunden langen Beistands.
   Keine volumetrischen Raymarches, Echtzeit-Schatten oder Postprocessing.
2. Frische Griptaste am Pumphebel rastet virtuell ein. Die Waffe richtet sich
   unmittelbar zur Stützhand aus; keine Änderung der XR-Tracking-Anker. Pumpweg
   ist auf 7,5 cm begrenzt und wird aus Handabstandsänderung ermittelt. Drehungen
   lösen keine unbeabsichtigte Pumpbewegung aus. Loslassen, Pause, Handwechsel
   oder Trackingverlust lösen den Griff. Keine physische Controller-Arretierung.
3. Alle 13 Pellet-Bahnen werden kurz sichtbar und enden am tatsächlichen Treffer.
   Begrenzte verteilte Oberflächeneffekte; Schaden und Ton bleiben pro Gegner
   aggregiert. Siegel/andere Ziele können auch durch Randpellets getroffen werden,
   höchstens einmal pro Ziel und Salve. Schrein- und Menüfunktionen reagieren nur
   auf gezieltes Anvisieren, nicht auf streuende Randpellets. Unverändert fünf Patronen, Pumpen vor jedem
   Schuss, 5,2° halber Streuwinkel und Entfernungsabfall statt garantierter Tötung.
4. Bereits zugelassene Austrittsbahnen reagieren auf reale Scan-Mesh-Hindernisse,
   nicht auf vorübergehend fehlende Scan-Konfidenz. Nichttödliche Treffer frieren
   den Sprung nicht mehr ein. Sichtbare Gegner suchen einen kurzen, kollisions-
   geprüften Lande-/Ausweichweg; Rückzug nur solange der Körper noch im Portal
   ist. Getötete Gegner im Raum gehen in normale Leichenphysik über. Verlorene
   Portale dürfen keinen bereits sichtbaren Gegner löschen.

## Verifikation und Abnahme

Automatische Prüfungen: ImmersionValidation, ShotgunValidation und vollständige
bisherige Regressionen einschließlich Knie-/Fledermauskurven, Raumkarten,
Wurfsternen, Handrollen, Performance-Strukturen und Blut-/Splatter-Pools.
Native Unity-Vorschauen: `Verification/Immersion/heaven-*.png`.

Noch auf der getragenen Quest zu prüfen:

- Beim Beistand nach oben schauen und den Kopf leicht seitlich bewegen: nahe und
  ferne Wolken müssen sich unterschiedlich verschieben. Kein Kopfzwang.
- Freie Hand an den Pumphebel, Grip halten, zurück/vor pumpen; beide Hände drehen,
  danach loslassen. Rechts-/Linkshand testen; kein nachlaufender Hauptgriff.
- Auf eine freie Wand in 2–3 m Abstand schießen: mehrere Streuspuren/Trefferorte.
  Ein naher, zentral getroffener Gegner erhält mehrere Pellets; Randtreffer weniger.
- Mehrere Austritte beobachten, einen springenden Gegner nicht tödlich treffen,
  einen im Raum während des Sprungs töten. Kein Rückwärts-Ausblenden sichtbarer
  Gegner. Fledermaus-Austritte und Spieler nahe der Landestelle mitprüfen.
- Auch Beistand plus offenes Dämonenportal auf Framerate prüfen. Editorbilder
  und Build-Erfolg belegen weder wahrgenommenes Quest-3D noch Quest-Performance.

Build: `bash Tools/build-v19.14.sh`; eigener APK-Dateiname, Vorgänger bleibt erhalten.
Raumformat, gespeicherte Profile und Benutzerdaten werden nicht migriert/gelöscht.

### Durchlauf vom 8. September 2026

Der finale Gesamtlauf `unity-qdmr-v1914-export.HHj0Oh.log` bestand 36 Testsuiten
mit 3.224 protokollierten CHECK-Prüfungen sowie den separaten 1.200 exakten
BVH-Vergleichsrays. Davon entfallen 51 neue Prüfungen auf Immersion/Griff/Entry.
Im nativen 768²-Stereobild unterscheiden sich 79.456 Pixel über der verwendeten
Schwelle; ein seitlicher Kopfversatz verändert weitere Bildbereiche. Das belegt
die unterschiedlichen geometrischen Ansichten, ersetzt aber keine VR-Abnahme.

Vorherige Versuche werden nicht als Freigabe gewertet: ein falscher Test zur
Sichtbarkeit am FOV-Rand, ein Shader-Bezeichner und ein Deckenportal-Grenzfall
wurden korrigiert. Der vorletzte Export wurde für den ergänzten Menüschutz
kontrolliert beendet. Nur die finale APK wird zur Installation angeboten.

### APK fertig

- `Builds/Purgatory-v19.14-heaven-spread-entry.apk`, 110.013.710 Bytes.
- Version 0.19.14 / Code 53, Paket `de.stefanmaier.questdemonmr`.
- SHA-256: `4c879206113f14c5edb4efcb2243449054f80bd569886c1971fe7156ad75768c`.
- ARM64-Native-Build erfolgreich (2:59 Minuten); beide neuen Himmelshader mit
  jeweils sechs Vulkanvarianten exportiert, keine C#-/Shaderfehler im finalen Log.
- ZIP-Integrität, Paketmetadaten und APK-v2-Signatur geprüft. Gleiches
  Entwicklungszertifikat wie V19.13; normales Update ohne Deinstallation möglich.
- Bisherige V19.13-APK unverändert. Neue APK noch nicht durch den Agenten auf
  der Quest installiert; Installationsfreigabe wurde separat angeboten.
- Vollständige Lieferdaten: `Verification/Immersion/delivery.json`.
