# V18.7 – A5 Revolver-/Trefferklang und kleinere Waffe

## Umfang

Der Nutzer beauftragt ausdrücklich A5 und zieht die zunächst für A6 notierte Größenkorrektur in A5 vor. Gegnerstimmen/Schritte/Fluggeräusche bleiben A6; keine Änderung an Raumscan, Portalen, Gegnerverhalten, Schaden, Munition oder Nachladedauer.

- **Revolver linear 10 % kleiner**, einheitliche Skalierung 0,9. Der definierte Griffpunkt `(0,-0,055,-0,060)` im Modell bleibt an derselben Stelle relativ zum Controller: `neue Position = alte Position + Griffpunkt × (1 − Skalierung)`. Kalibrierungswerte werden nicht geändert. Trommel, Hahn, Abzug und Laufsocket folgen gemeinsam. Der Laufursprung liegt entsprechend näher am Controller; kein bloßes Schrumpfen um die Laufspitze.
- **Fünf neue Schussvarianten aus echten Revolveraufnahmen**, statt der bisherigen Mischung aus Explosions-, Laser- und Punch-Klängen. Verwendet werden .38-Special-/Single-Six-Aufnahmen der CC0 Free Firearm Sound Library. Quellen und genaue Segmente: `ExternalSource/FirearmAudioV18/SOURCE.md`. Verschiedene Mikrofonpositionen können dasselbe Ereignis enthalten; keine Behauptung von fünf unabhängigen Aufnahmetakes.
- Mono-Downmix, enger Beginn, Hoch-/Tiefpass, moderate Tiefmittenbetonung, kurzer auslaufender Körper und begrenzte Spitzen. Keine Laser-/Sinusanteile im neuen Kampf-Audiopaket. Der fiktive Revolver bildet kein bestimmtes reales Waffenmodell akustisch exakt nach.
- Getrennte **Fleisch-, harte Oberflächen- und Kill-Klänge**, jeweils fünf Varianten. Kurze, gestaltete Kenney-CC0-Foley-Layer ohne synthetisches Klingeln. Harte Oberflächen sind weiterhin eine gemeinsame Kategorie, keine materialgenaue Sofa-/Glas-/Metallklassifikation.
- Sieben Mechanik-Kategorien mit je fünf Varianten: Hahnspannen, Trommelrastung, Öffnen, Auswerfen, Laden, Schließen und Leerklick. Gestaltete Metall-/Holz-Foley-Layer, nicht neue Echtaufnahmen einer Revolvermechanik. Öffnen startet mit der Nachladeaktion; Auswerfen bei 30 %, Laden bei 52 %, Schließen bei 98 % der bestehenden 1,25-s-Nachladebewegung. Trommelrastung 130 ms nach Schuss. Keine Magazin-/Pistolenreload-Synthese mehr am Revolver.
- Trigger-Hysterese verhindert dauernde Hahnklicks. Pause friert laufende Waffen-/Trefferstimmen und geplante Rastung ein. Reset/Abbruch stoppt laufende Waffenstimmen und verwirft geplante Mechanik-Cues. Audio nutzt keine verzögerten Hintergrundaufrufe, die nach einem Reset weiterlaufen.
- Stärkere Schusshaptik aus A4 bleibt erhalten; der schwächere Trefferimpuls überschreibt sie nicht mehr im selben Frame.

## Budget und Grenzen

55 Mono-PCM-Clips mit 44,1 kHz, insgesamt **1.142.400 WAV-Dateibytes** (nicht gleich Unity-RAM-Verbrauch). Android importiert sie als vorab dekodiertes PCM; keine laufende Schusssynthese oder Streaming-Dekodierung. Alte versionierte Audioassets bleiben für Regression/Rückfall erhalten und weiterhin in Resources, daher noch kein Paketgrößen-Cleanup.

Maximal drei Schuss-, zwei Mechanik- und acht Trefferstimmen. Wiederverwendete AudioSources statt unbegrenzter PlayOneShot-Überlagerungen; keine neue Soundquelle pro Schuss/Treffer. Der jüngste Schuss bleibt im Vordergrund, ältere Ausklänge werden abgesenkt. Treffer teilen ein festes Gesamt-Gainbudget anhand ihrer mit der Spielzeit pausierenden Laufzeiten. Waffen-/Treffer-Doppler ist deaktiviert; Treffer bleiben voll räumlich.

Clip-Peaks: Schuss 0,82, Treffer höchstens 0,72, Mechanik höchstens 0,60. Zusammen mit den festgelegten Voice-Gains ergibt sich ein konservatives Summenbudget von **0,9308** für diesen Kampf-Audioteil. Das ist **kein globaler Master-Limiter**: übrige Gegner-/Portal-/Systemklänge und die tatsächliche Lautstärke/Verzerrung der Quest-Lautsprecher sind damit nicht vermessen.

## Verifikation

**564 native Unity-Prüfungen bestanden**, 401 bestehende + 163 neue A5-Prüfungen: alle PCM-Imports, Daten/Spitzen/DC, unterschiedliche Varianten, Wiederholungsvermeidung, Größen-/Griff-/Laufposition, Voice-Limits, Gainbudget, Trigger-Hysterese, zeitlich passende Mechanik-Cues, Pause/Resume/Reset und produktive Schuss-/Reload-Anbindung aus dem A4-Regressionslauf.

Gemessene Daten und Audition: `Verification/AudioPolish/mix-analysis.json`, `mix.log`, `a5-audition.wav`. Die Audition enthält alten/neuen Schuss, fünf neue Varianten, Nachladefolge und die drei Trefferarten; sie ist eine Offline-Hörprobe, kein Quest-Mixmitschnitt. Unity-GPU-Vorschauen der kleineren Waffe wurden angesehen; historische A4-Bilder bleiben in `Verification/RevolverPolish/` erhalten.

Build/Installation separat im [Buildbericht](BUILD-REPORT.md). Offene Abnahme: Klangdruck/Wiederholungswirkung und Mischung auf Quest-Lautsprechern/Kopfhörern, tatsächliche Griffgröße und räumliche Ortung. Keine getragene Hörprüfung oder neue Performance-/Thermikmessung aus Native-Checks ableiten.
