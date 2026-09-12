# V18.1 – echter Startabbruch auf der Quest

## Nachgewiesene Ursache

Beim vom Nutzer gestarteten V18.0-Lauf am 5. September 2026, 17:50:36, brach `QuestDemonGame.Start` in `QuestGun.BuildMuzzleFlash` ab: `Material` erhielt einen fehlenden Shader. Sowohl die optionale Meta-Shader-Suche als auch `Unlit/Color` lieferten im Player keinen Shader. Das geschah **vor** `LiveRoomScanner.Create`. Der alte HUD-Text „Raum wird geladen“ blieb deshalb stehen; die Wartezeit war keine Messung der Scan-Geschwindigkeit. Originalausschnitt: `Verification/V18/startup-failure-0180.txt`.

## Korrekturen

- Eigener kleiner `WeaponUnlit`-Shader im Resources-Verzeichnis, ausdrücklich für Mündungsfeuer und Schuss-Leuchtspur geladen. Stereo-Instancing berücksichtigt; normale Tiefenprüfung gegen rekonstruierte Raumflächen bleibt aktiv. Keine Abhängigkeit mehr von zufälligen Shader-Referenzen eines MRUK-Prefabs.
- Live-Scanner startet vor der Waffenaufbereitung. HUD unterscheidet Scan-Start und Waffenaufbereitung. Ein abgefangener Initialisierungsfehler sperrt Spielbeginn und zeigt dauerhaft „STARTFEHLER“ statt endlosem Laden. Auch Fehler aus verschachtelten Vorlade-Coroutinen werden erfasst.
- Geräteprotokoll bestätigt Waffeninitialisierung einschließlich des tatsächlich geladenen Shaders. Scanstatus meldet alle zehn Sekunden Sensorverfügbarkeit, Abschnitte, Oberflächen, Boden und Startfreigabe. Das erlaubt eine Trennung zwischen Start-, Sensordaten- und Rekonstruktionsproblemen.
- Inhaltsprotokoll verwendet die tatsächliche Paketversion statt des alten festen V17.2-Labels.

## Prüfstand

Neun neue native Checks für explizite Shaderauflösung, Shader-Import, Farbeigenschaft sowie erfolgreichen und fehlschlagenden verschachtelten Startablauf. Dazu die bisherigen 220 nativen Prüfungen einschließlich synthetischer GPU-Tiefenverarbeitung. Die konkrete ursprüngliche Waffeninitialisierung muss zusätzlich im installierten Android-Player über `QDMR_GUN_READY` bestätigt werden; ein Editor-Test allein ist hier ausdrücklich unzureichend.

Version: 0.18.1 / versionCode 19. Separates Ziel: `Builds/QuestDemonMR-v18.1-livescan.apk`. Historische V18.0- und V17.2-APKs wurden nicht überschrieben. Alle 229 nativen Checks bestanden, Build erfolgreich und installiert. Echter Quest-Start bestätigt `QDMR_GUN_READY`, Tiefendaten und rekonstruierte Oberflächen. Nach etwa 30 Sekunden auch Boden erkannt; letzter erfasster Status nach etwa 40 Sekunden noch nicht spielbereit. Danach USB getrennt. Vollständige Spielfreigabe und vergleichbare Scanqualität bleiben offen; siehe Buildbericht und Geräteauszug.

Reproduktion: `bash Tools/build-v18.1.sh` im bereits eingerichteten Unity-Projekt. Kein erneuter Art-Import/Scene-Neuaufbau notwendig. Der Stand verwendet weiterhin eine eigene TSDF-Rekonstruktion aus Meta-Tiefendaten, nicht den Laser-Tag-Quellcode. Vergleichbare Raumqualität und unmittelbare Bedienbarkeit bleiben physisch zu prüfen.
