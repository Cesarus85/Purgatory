# V18.3 – keine leeren Erfolgswellen, Portal-Spawnkorrektur

## Nutzerbefund und Ursache

V18.2 zeigte abwechselnd fehlenden Portalplatz und erfolgreich abgeschlossene Wellen, ohne Gegner zu erzeugen. Im Quellcode bestätigt: `SpawnSequence` kehrte bei abgelehnter Platzierung zurück; `WaveLoop` zählte trotzdem den Quotenplatz weiter und belohnte anschließend eine leere Gegnerliste. Das war ein Spielablauffehler, keine erfolgreiche Welle.

Die neue Randprobe lag an Wandportalen zudem nur 2,7–2,8 cm über dem gemessenen Boden. Das passt schlecht zu einem 8-cm-Rekonstruktionsraster und unvollständigen/gerundeten Boden-Wand-Kanten. Ein echter Unity-Kollisionstest mit einer 10 cm hohen fehlenden Wandnaht reproduziert die Ablehnung beider V18.2-Wandgrößen an Probe 10. Das ist ein nachgewiesener reproduzierbarer Ablehnungsfall, **kein aus Geräte-Telemetrie belegter alleiniger Grund in diesem konkreten Nutzerraum**: V18.2 protokollierte diese Detailstufen noch nicht, alte Geräteprotokolle lagen nicht mehr vor.

## Korrekturen

- Fehlgeschlagene Platzierungen bleiben innerhalb desselben Spawn-Quotenplatzes. Nach zwei Sekunden laufender Spielzeit wird erneut gesucht. Pause hält die Wiederholung an. Keine Wellensteigerung/Bonusmunition für leere Versuche; die nächste Welle beginnt erst nach regulärer Erzeugung und Erledigung der Gegner.
- Präziser HUD-Text: „KEIN FREIER AUSTRITT – WAND UND BODEN ANSEHEN“, keine unbelegte Behauptung, der ganze Raum sei zu klein.
- Wandportale samt sichtbarem Rahmen/Öffnung um 12 cm angehoben. Damit liegen die Randproben bei knapp 15 cm über dem Boden. Nicht bloß die Tests nach innen verschoben: Geometrie und geprüfte Fläche bleiben deckungsgleich. Deckenposition/-größe unverändert.
- Nach zwei erfolglosen Suchen darf eine kürzlich verwendete Wandposition wieder gewählt werden. Abwechslung darf einen Raum mit nur einem brauchbaren Austritt nicht dauerhaft sperren. Boden-, Körper-, Spielerabstands-, Flächen- und Wegprüfung bleiben aktiv, belegte Austritte bleiben gesperrt. Bevorzugung anderer Orte im Bewertungsverfahren bleibt erhalten.
- Ein kompakter `QDMR_PLACEMENT`-Eintrag je Suche trennt Wandtreffer, freie Austritte, passende Rahmenflächen und verbundene Wege. Fehlende/nicht passende Randproben enthalten ihre Probenummer. `scan_not_ready` und wartende Wellen-/Quotenplätze werden ausdrücklich gemeldet.

Scanner, Assets, Shader, Portalgrößen und Renderauflösungen bleiben ansonsten V18.2. Keine Abschaltung räumlicher Sicherheitsprüfungen und kein Spawn in erfundenem/unbekanntem Freiraum.

## Prüfungen

269 native Unity-Checks: 252 bestehende plus 17 neue. Reale Physics.Raycast-Abfragen auf nachgebildeter Wandnaht reproduzieren V18.2 und prüfen die angehobene Form; fehlende, zu schmale und zurückliegende Wände bleiben abgelehnt. Der echte WaveLoop übergibt den ersten Slot an die echte SpawnSequence; zwanzig gescheiterte Wiederholungen beenden diese nicht und verändern weder Welle noch Munition. Pause/Resume und Wiederverwendung eines alten, aber nicht eines belegten Austritts geprüft. Die Wartezeiten werden im Editor-Test übersprungen; kein Echtzeit-Headsettest behauptet.

Erster Prüflauf stoppte wegen fehlender Awake-Ausführung im Editor-Testaufbau; Singleton im Test explizit initialisiert. Zweiter Lauf bestand 14 neue Tests, Paketierung für die zusätzliche Wiederverwendungs-Korrektur vor Lieferung gestoppt. Abschließender Lauf enthält alle 17 neuen Prüfungen. Nur finalen Build installieren.

Der abschließende Unity-Lauf bestand alle 269 Tests und erzeugte den nativen IL2CPP-Player, hing aber beim Gradle-Paketieren. Thread-/Dateiprüfung fand das Einlesen nummerierter Datenkopien im generierten Verzeichnis; 590 nummerierte Hashdateien mit vorhandener regulärer Datei wurden wiederherstellbar nach `Verification/GradleQuarantine/v183-data-1788628874400` verschoben. Manifest: `Verification/V18/v183-generated-quarantine.json`. Keine regulären Dateien gelöscht. Eine erneut aufgetauchte `network_sec_config 2.xml` blockierte auch den separaten Paketierungsanlauf. Die Herkunft der wiederauftauchenden Dateien ist nicht geklärt; keine Änderung der systemweiten Synchronisation.

Daher finale Paketierung aus einer kanonischen Kopie des bereits generierten Gradle-Projekts unter `/private/tmp/qdmr-v183-gradle.pWfKG7`, ohne nummerierte Konfliktdateien, mit zwei Workers, ohne parallele Projekte, offline. Die native `libil2cpp.so` ist per SHA256 identisch zum zuvor von Unity erzeugten Player. Der bestehende Postgenerate-Quarantäne-Guard berücksichtigt für kommende Builds zusätzlich den Android-Datenordner; diese einzeilige Editor-Erweiterung ist nicht Teil des ausgelieferten Runtime-Codes und wurde nicht in den vorangegangenen 269 Checks ausgeführt.

Version 0.18.3 / versionCode 21, `Builds/QuestDemonMR-v18.3-spawn-fix.apk`. Build-/Installationsnachweis im Buildbericht. Reproduktion: `bash Tools/build-v18.3.sh`; falls nur die Paketierung erneut betroffen ist, nach abgeschlossenem IL2CPP-Schritt `bash Tools/package-generated-v18.3.sh` (verweigert vorhandene Ziel-APKs). Die Bestätigung tatsächlicher Gegnerspawns im Nutzerraum bleibt ein separater Gerätetest.
