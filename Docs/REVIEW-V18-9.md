# V18.9 – A6b Schussdruck und erkennbare Kontakte

## Befund vor der Änderung

Die V18.8-Rückmeldung ist maßgeblich: weiter zu leise und der kräftige Waffenknall zu selten beziehungsweise nicht klar von Treffern unterscheidbar. Native Prüfungen sind keine Hörabnahme.

Offline-Analyse der fünf bisherigen Schüsse: Spitzen jeweils 0,82, aber erste 80 ms nur etwa **−19,1 bis −19,8 dBFS RMS**. Fleischkontakt-Clips liegen im gleichen Fenster bei **−12,9 bis −14,3 dBFS RMS**. Im Code beginnen Report und Kontakt im selben Fire-Aufruf. Das belegt sehr spitze, körperarme Schüsse und ein Maskierungsrisiko; es beweist nicht, welcher Anteil am getragenen Headset die Hauptursache war. Es wurde kein sporadisch fehlender Play-Aufruf allein aus diesen Messungen behauptet.

Die deterministische ungedämpfte Mono-Vergleichsfolge (Miss, Wand, Fleisch, Kill, sechs schnelle Schüsse) erreicht mit dem vorherigen Mix etwa 17,7 % maximale Limiter-Gainreduktion; der neue Mix bleibt dort unter der Limitergrenze. Das ist ein **Offline-Vergleich**, keine Messung des tatsächlichen Unity-/Quest-Ausgangs und kein Mehrgegner-Hardwareprofil.

## Umsetzung

- **Jeder Report erhält denselben verlässlichen echten Revolverknallbeginn**, dann Überblendung in fünf unterschiedliche aufgezeichnete Klangkörper/Ausklänge. Keine Laser-/Explosionslage. Moderate obere Mittenbetonung und Offline-Dynamikbearbeitung geben dem Knall Körper, statt nur die einzelne Spitze höher zu setzen.
- Erste 80 ms nun bei etwa **−12,58 dBFS RMS**: je nach Variante rund **+6,5 bis +7,3 dB**, nahezu angeglichen. Ganze Clips unterscheiden sich um weniger als 2 dB RMS; sample peaks bleiben unter 0,90. Alte Clips/APKs bleiben unverändert erhalten. [Reproduktion und Quellen](../ExternalSource/FirearmAudioV18/SOURCE-A6b.md).
- Treffer haben anders gefilterte Fleisch-/harte Oberflächen-/Kill-Anteile, niedrigere Spitzen und **38 ms akustische Verzögerung**. Der Knall kann zuerst wahrgenommen werden, dann der räumliche Aufprall. Trefferberechnung, Schaden, Wunden und Projektilinteraktion passieren weiterhin sofort; es wird keine Flugzeit simuliert.
- Report-Priorität 16 statt 64, drei feste Reportstimmen; ältere Ausklänge bleiben abgesenkt. Angriffswarnungen werden beim kurzen Absenken der Gegnerbegleitgeräusche ausdrücklich nicht geduckt. Kein allgemeines Abschalten von Warnungen oder pauschale Veränderung der Quest-Systemlautstärke.
- **Getrennte gespeicherte Waffen-/Trefferpegel** von 0–100 % in 10-%-Schritten. Standard Waffe 100 %, Treffer 70 %; Waffenregler umfasst die Mechanik. Einstellung am Pausenschrein mit Controllerstrahl/Trigger: WAFFE −/+, TREFFER −/+. Kein Munitionsverbrauch, kein versehentliches Starten durch diese Tasten. Das bisherige Schreinmodell bleibt bewusst bis A8 erhalten.
- **KLANGTEST / STOP** spielt in Pause beschriftet: Schuss ohne Treffer, Wandtreffer, Dämonentreffer, tödlicher Treffer und sechs schnelle Schüsse mit Grollen. Verwendet die produktiven Clips/Pegel, aber eigene begrenzte Vorschauquellen; wendet keinen Schaden an und erzeugt keine Gegner. Erneutes Drücken stoppt; Start/Fortsetzen, App-Unterbrechung, Reset/Rescan und Entfernung des Panels stoppen die Vorschau ebenfalls.
- Produktionsschuss und zugehöriger Kontakt teilen im Audit eine Kennung. Normales Spiel schreibt keine laufenden Audio-Logs. Klangtest und bestehende opt-in V17-Aufnahme protokollieren begrenzt Clip, Stimme, Zeit und Gain; letzter 256-Ereignisse-Puffer. Der Listener-Limiter zählt echte Audio-Callbacks und die minimale gemessene Gainstufe; null Callbacks bedeutet **keinen** bestätigten aktiven Ausgangspfad.

## Prüfung und Grenzen

**813 native Prüfungen bestanden**, 748 bestehende + 65 A6b-Prüfungen. Neue Sample-/Mixprüfungen, persistente und unabhängige Regler, Mute, Priorität, Warnungsschutz, produktiver Fire-Pfad (Miss/Raum/Fleisch/Kill/Schnellfeuer), Bedienung ohne Munitionsverbrauch sowie vollständige und abgebrochene Klangtestfolge. Native Unity-Vorschau der Bedienelemente angesehen und übergroße/überlappende Schrift vor dem Abschlusslauf korrigiert: [Audiobedienung](../Verification/ShotClarity/audio-controls.png). APK-Nachweis im [Buildbericht](BUILD-REPORT.md).

**Offene getragene Abnahme:** Bei unveränderter Quest-Systemlautstärke die fünf Klangtest-Abschnitte und eine echte Runde hören. Jeder Schuss soll kräftig und eindeutig sein; Kontakte und Kill sollen zuordenbar bleiben. Nach Möglichkeit Lautsprecher und Kopfhörer vergleichen. Die Soundgestaltung und die 38-ms-Trennung sind erst nach diesem Höreindruck abgenommen. Runtime-Callback-/Gainwerte lassen sich beim Test erfassen; derzeit keine neue Hardware-Ausgangsmessung oder garantierte Verzerrungsfreiheit der Lautsprecher behauptet.

A7 Bewegung, A8 Schrein-Art/Platzierung, A9 Pickups und A10 Portal-Schauplätze sind **nicht** Teil dieses Builds. Keine Änderung an Raumscan, Gegnernavigation, Schaden, Munition, Revolvergröße oder Portalgeometrie.
