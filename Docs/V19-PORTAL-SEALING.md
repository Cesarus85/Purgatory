# V19.0 — A10-Abschluss und Portalversiegelung

Status: implementiert, 1.737 native CHECK-Meldungen bestanden, als 0.19.0/code39 am 6. September 2026 um 20:48:28 datenerhaltend auf Quest 3 installiert. Nicht automatisch gestartet. [Lieferung und offene physische Abnahme](REVIEW-V19-0.md). Der restliche V19-Hauptblock bleibt geplant.

## Umfang

1. Dritter echter 3D-Schauplatz: gotische Kathedralen-Seitenhalle mit nahen Säulen, Rippengewölbe, Seitenkapellen und fernem Ritualfenster. Gleiche Höllenwelt wie Schmiede und Brücke. Der Deckenschacht bleibt aufrecht und unabhängig.
2. Drei originale Blender-Rahmen: geschmiedete Klammern, gebrochene Ketten, gotische Maßwerk-Rippen. Bestehende Öffnungs- und Raumprüfungen werden nicht vergrößert oder gelockert.
3. Erster V19-Spielblock: begrenzter Gegnernachschub je Riss, dann zwei bzw. drei beschießbare Siegel. Erst die zerstörten Siegel schließen den Riss. Kein endloser Spawn und keine automatische Wellenbelohnung.

## Spielregeln und Sicherheit

- Maximal ein aktiver Begegnungsriss; Bodengruppen bis zwei Gegner, Deckenrisse ein Gegner. Wellenquote und Raum-Crowd-Limit bleiben erhalten.
- Siegel erst nach abgeschlossenem Austritt und einer kurzen Angriffsphase aktiv. Pause friert Phase und Treffer ein.
- Siegel liegen innerhalb der Öffnung vor der Wand. Sicht und freier Raum werden geprüft; keine Ziele hinter realen Hindernissen. Unbekanntes Volumen bleibt gesperrt.
- Pro Öffnung einmalig zusätzliche Siegelmunition; bestehende Notladung bleibt als Schutz gegen Fehlschüsse. Kein Treffer zählt doppelt, kein Bonus beim Abbruch.
- Abgebrochener Austritt wiederholt nur die offene Gegnerquote. Tod/Neustart entfernt Riss und Siegel.
- Zielhinweis kompakt, keine breiten Textbänder. Waffe, Scanabschluss und Animationskorrekturen aus V18.20 bleiben erhalten.

## Nachweise

- Blender-Quellen, FBX, gebackene Texturen und Vorschaubilder im Projekt.
- Native Unity-Prüfungen: Modelle/Budgets, unterschiedliche Schauplätze und Rahmen, Zustandswechsel/Pause/Treffer/Abbruch, echte Spawn-Routine, bestehende Regressionen.
- ARM64-Release bauen, APK prüfen; bei angeschlossener Quest datenerhaltend installieren, nicht automatisch starten.
- Trage-Test bleibt offen: Lesbarkeit/Erreichbarkeit der Siegel, Rhythmus und Stereo-Tiefe sowie reale Quest-Framerate.

Nicht Bestandteil: restliche V19-Gegnerrollen, kompletter Encounter-Director, weitere Waffen oder ein neues Scansystem.
