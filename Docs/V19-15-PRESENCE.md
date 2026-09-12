# Purgatory V19.15 – Klangdruck, Nahkampf und lebendiger Himmel

## Ziel und Umsetzung

- Revolver und Shotgun: bestehende, lizenzierte Aufnahmen klanglich beibehalten,
  aber den Schusskörper offline verdichten. Die ersten 80 ms haben gegenüber
  V19.14 jeweils +4,5 dB RMS; auch die ersten 200 ms sind kräftiger. Spitzen
  bleiben unter 0,89, der bestehende stereo-verknüpfte Ausgangslimiter bleibt
  aktiv. Kein wirkungsloser AudioSource-Wert über 1, keine zusätzliche
  Audio-Latenz. Ausklingende Echos werden nicht entsprechend mit hochgezogen.
  Bestehende getrennte Waffen-/Trefferregler und Stummschaltung bleiben erhalten.
- Monster: +2,6 dB Quellpegel für Warnungen, Stimmen, Schritte und Flügelfoley.
  Räumliche Ortung, Entfernungsabfall, acht gemeinsame Stimmen und Prioritäten
  bleiben erhalten. Angriffswarnungen werden weiterhin nicht vom Schuss geduckt.
- Bodendämonen: 12 cm näher vor Angriffsbeginn, schwerer Dämon 14 cm. Zusätzlich
  Nahkontakt-Toleranz von 28 auf 16 cm reduziert: kein unsichtbares Verlängern
  des Arms, echtes Zurückweichen vermeidet den Kontakt. Trefferzeitpunkt,
  Schaden, Unterbrechbarkeit und Kollisionsprüfung bleiben unverändert.
  Die bereits nahen Fledermaus-Sturzangriffe werden nicht weiter ins Gesicht versetzt.
- Himmel: die bestehenden Blender-Wolken bleiben echte 3D-Geometrie mit
  getrennten Ansichten für beide Augen. Vier Wolkenbänke driften leicht,
  Silhouetten und Wolkenstruktur wallen langsam. Dazu 48 weiche Nebelfetzen in
  vier Tiefenschichten mit Wind, Größenänderung und leichter Rotation.
  Keine neuen Bitmaps, keine Raymarches, keine zusätzlichen Kameras.
  Ein vorberechneter Partikelpool, keine Objekterzeugung beim Beistand;
  sämtliche Bewegung folgt der Spielzeit und friert in Pause ein.

## Reproduzierbarkeit

Audiobake: `python3 ExternalSource/FirearmAudioV18/mix_presence.py`.
Quellen/Lizenzen unverändert: `SOURCE-A6b.md`, `SOURCE-Shotgun.md` im selben Ordner.
Alte WAVs bleiben erhalten. Messbericht: `Verification/Presence/audio-analysis.json`.

Tests: `QuestDemonMR.Editor.PresenceValidation.Validate`, danach vollständige
Shotgun-/Immersion-/Raum-/Gameplay-Regressionskette im Export.
Build: `bash Tools/build-v19.15.sh`.

## Auf der Quest prüfen

1. Zum ersten Klangtest Headset-Lautstärke zunächst etwas niedriger wählen.
   Einzel- und schnelle Revolverschüsse, danach Shotgun mit/ohne Treffer
   vergleichen. Knall muss deutlich präsenter bleiben, nicht verzerren.
   Auch Waffenregler reduzieren und auf 0 testen. Mechanik und Treffer sollen
   weiterhin unterscheidbar sein; Schritte/Flügel räumlich hörbar.
2. Einen Bodendämon herankommen lassen, Angriff beobachten, leicht zurückweichen.
   Etwas näherer Kontakt, aber keine zusätzlichen unsichtbaren Fernschläge.
3. Beim Beistand nach oben schauen: Wolkenränder wallen, Nebel zieht zwischen
   räumlichen Schichten. Leichte Kopfbewegung zeigt weiterhin Parallaxe.
   Pause/Fortsetzen sowie gleichzeitiges Dämonenportal auf Ruckler prüfen.

Offline-Lautheitsmessung ist kein gemessener Quest-Schalldruck. Native
Editorvorschauen und Build-Tests ersetzen weder Klang- noch Framerate-Abnahme
auf der getragenen Quest. Raumkartenformat und gespeicherte Benutzerdaten
bleiben unverändert; keine Deinstallation zum Aktualisieren erforderlich.

## Ergebnis vom 8. September 2026

- Finaler Gesamtlauf: `Verification/Presence/unity-qdmr-v1915-export.CIvLwu.log`.
  37 erfolgreiche Testsuiten, 3.341 protokollierte CHECKs einschließlich
  wiederholter/nested Regressionen, davon 117 neue Presence-Prüfungen.
- Beide realen Audio-Playback-Pfade verwenden nachweislich die neuen Aufnahmen;
  Android-PCM, Stummschaltung, konstante Stimmenanzahl und Ausgangslimiter geprüft.
  Alle drei Bodentypen greifen im normalen Update näher an; Distanzgrenzen,
  Einmal-Kontakt, Unterbrechung, Angriffswinkel und echte Scan-Wandblockade geprüft.
- Native Himmelbilder bei 0,7 und 1,6 Sekunden: 115.120 Pixel über der
  Differenzschwelle verändern sich bei unbewegter Kamera. Separater Vergleich
  mit ausgeschalteten Partikeln: 2.696 Pixel. Pause ist bildidentisch.
  Die unabhängige Stereo-/Parallaxenregression aus V19.14 besteht weiterhin.
  Vorschauen: `Verification/Presence/sky-*.png`.
- Erster Versuch gestoppt: Partikel waren weitgehend hinter den festen Wolken
  verdeckt; nach innen versetzt und erneut nativ geprüft. Erster Gesamtlauf
  stoppte an einem veralteten Audio-Pfadtest; dieser kontrolliert jetzt die
  tatsächlich verwendete neue Schussbank. Nur der finale Lauf ist freigegeben.
- ARM64/IL2CPP erfolgreich: 31 aktualisierte Schritte, 11 Sekunden; Gradle
  erfolgreich in 57 Sekunden. Wolken- und Nebelshader erfolgreich exportiert.
- APK: `Builds/Purgatory-v19.15-presence.apk`, 110.542.632 Bytes,
  Version 0.19.15 / Code 54 / Paket `de.stefanmaier.questdemonmr`.
  SHA-256: `bb03c0183c6c162bf774f298d46c96157e623d6c44daa9d4172b3954859e70c1`.
  ZIP-Integrität und APK-v2-Signatur geprüft, gleiches Zertifikat wie V19.14.
  Die vorherige APK ist per Prüfsumme unverändert. V19.15 wurde nicht installiert.
