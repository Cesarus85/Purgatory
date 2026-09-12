# V19.1 — Kampfrhythmus und lebendige Gegner

Auftrag: bestätigten Vorschlag nach dem V19.0-Test umsetzen.

Umsetzung und native Prüfung abgeschlossen: 1.936 CHECK-Meldungen, davon 355 neue RhythmLife-Prüfungen. [Ergebnis und Abnahmegrenzen](REVIEW-V19-1.md); verbindlicher Paket-/Gerätestatus im [Buildbericht](BUILD-REPORT.md). Die Punkte unten beschreiben den umgesetzten Umfang; getragene Rhythmus-, Animations- und Performanceabnahme bleibt offen.

1. Siegel optional, nur an ausgewählten Nachschub-Rissen und schon während des Angriffs. Zerstörung verhindert noch nicht begonnenen Nachschub; ignorierte Ziele verfallen. Kein Wellen-Gate, keine zusätzliche Gegnerquote. Maximal zwei Risse gleichzeitig und bestehendes Crowd-Limit.
2. Abgebrochene Austritte wiederholen nur unbesetzte Quoten. Pause, Tod und Neustart beenden/frieren auch parallel laufende Begegnungen. Echte Raumprüfung bleibt erhalten; ein ungeeigneter Siegelplatz deaktiviert nur das Extra, nicht das ganze Portal.
3. Dämon: kleine Blick-/Kopfbewegung, Atmung und Gewichtsverlagerung, Blender-Kiefer-/Lidrig für Lauern, Knurren und Schmerz. Augen/Zähne korrekt mitführen. Keine großflächigen Blendshapes, die den vorhandenen sparsamen Treffer-Skinning-Pfad abschalten.
4. Fledermaus: Kopfstabilisierung, kurze Gleitabschnitte, asymmetrische Flügelfaltung beim Wenden und verzögert nachführende Beine/Ohren; Angriffsgeste/Krallen aus vorhandenem Rig erhalten und mit Kieferreaktion ergänzen.
5. Präsentationsschicht nach Basisanimation, ohne kumulative Posefehler; während Portalübertritt und Tod hat die bestehende Spezialanimation Vorrang. Keine zusätzliche Root-Bewegung durch unbekanntes Volumen.
6. Native Tests: beide Siegelentscheidungen, automatisches Ende, parallele Quoten/Crowd/Pause/Reset, Blender-Rig und deformierte Trefferflächen, Animationsübergänge und bestehende Scan-/Knie-/Dive-Regression. Release bauen und bei angeschlossener Quest datenerhaltend installieren; nicht automatisch starten. Getragene Qualitäts-/Performanceabnahme bleibt offen.
