# V18.12 – Bodenportale im hinteren Gang

Zusätzliche Nutzerkorrektur nach V18.11, vor A8. **0.18.12 / Code 30**, vollständig nativ geprüft, signaturgeprüft gebaut und am 6. September 2026 auf Quest 3 installiert. Geräteversion und APK-Hash bestätigt; App-Daten und Berechtigungen erhalten, nicht automatisch gestartet. Getragene Abnahme offen. A8 wird nicht vorgezogen.

## Befunde

- V18.11 suchte rundum, steuerte aber keine späteren Boden-Austritte gezielt hinter die aktuelle Blickrichtung. Die Wiederholungsbewertung allein garantiert keine Nutzung eines hinteren Gangs.
- Die kleinste bisherige Wandform brauchte rund 1,16 m zusammenhängende Breite. Die Suche endete 5,5 m vom Kopf entfernt. Beides kann eine schmale, weiter entfernte Stirnwand ausschließen.
- Der kleinere Rahmen wurde erst nach erfolgreicher Austrittsprüfung des großen Rahmens versucht. Ein zu enger großer Austritt verhinderte dadurch auch die Prüfung eines tatsächlich passenden kleineren Austritts.
- Das sind Codebefunde, keine nachträglich gemessene Diagnose des konkreten Kinder-Spielzeugs oder der Scanqualität im Nutzerraum. Der verfügbare Logcat-Ausschnitt enthielt keine passenden früheren Platzierungsereignisse.

## Umsetzung

- **Rückseiten-Präferenz ab Welle 3:** ungefähr jeder vierte reguläre Boden-Slot, ab Welle 4 jeder dritte, ab Welle 5 jeder zweite sucht zuerst hinter der aktuellen Blickrichtung. Die bestehende Decken-Slotfolge bleibt erhalten. Das ist eine Suchpräferenz, keine erzwungene Quote: Gibt es hinten keinen geprüften Anmarsch, sind andere sichere Plätze erlaubt. In Welle 1/2 bleibt die bisherige Rundum-Auswahl ohne zusätzliche Rückseiten-Präferenz bestehen.
- **Gezielter Gangsuchlauf:** 16 zusätzliche rückwärtige Strahlen, einschließlich gerader Mittellinie und naher Nachbarwinkel, ergänzen die 48 Rundum-Strahlen. Zwei Höhen bleiben erhalten; Suchreichweite 7,5 statt 5,5 m. Seitliche Flächenalternativen jetzt bei 15, 35 und 70 cm. Sie müssen weiterhin die erste tatsächlich gemessene Oberfläche sein.
- **Kompakter Bodenriss:** etwa 1,06 m äußere Breite, 81,2 cm innere Fensterbreite. Bestehender Rahmen und Renderer werden gemeinsam skaliert; kein neuer Blender-Modellentwurf in diesem Korrekturblock. Dieser Austritt erzeugt ausschließlich den schlanken AshStalker, keine zu breite Gegnerklasse. Dessen tatsächlich importierte Austrittsanimation wurde gegen die Öffnungsbreite geprüft.
- Große, schmale und kompakte Form werden unabhängig geprüft, auch wenn der Körperaustritt einer größeren Form scheitert. Kompakt verwendet den vorhandenen 27-cm-Bewegungsradius des schlanken Gegners und sitzt 6 cm näher an der Wand. Mindestabstand zum Spieler, bekannte freie Körperzone, echte Wandfläche und Navigation bleiben Pflicht.
- Höchstens 128 Flächenkandidaten und acht Anmarsch-Kandidatenprüfungen je Suche; zunächst maximal vier Rückseitenversuche, danach Ausweichsuche. Räumlich verteilte Kandidaten vor eng benachbarten Wiederholungen prüfen. Kompakte Routen bekommen zusätzlich die 27-cm-Begehbarkeitsprüfung an ihren Wegpunkten.
- Hintere Bodenportale geben **2 statt 1,25 Sekunden Vorlauf** vor dem Gegner-Austritt. Die vorhandene räumliche Portalquelle erhält dort eine größere Nahdistanz und höhere Priorität, um die Richtung vorher ankündigen zu können. Keine neue Schussmischung, keine zusätzliche Audioquelle.
- Revolvergröße, Griff, Munition, Kampfanimationen und Scan-Architektur bleiben unverändert. Keine Spielzeuge ausgeblendet, keine unbekannten Bereiche als frei behandelt, kein Teleport über Hindernisse.

## Nachweise und Grenzen

**1.074 CHECK-Meldungen im finalen nativen Validierungs-/Exportlauf**, darunter 116 neue Rückseiten-/Gangprüfungen und vier zusätzliche Prüfungen der neuen Portalform in der bisherigen Regressionskette. Synthetischer bekannter Tiefenraum mit echten Unity-Kollidern: breiter Hauptraum und 1,1 m breiter, 5 m langer hinterer Gang, zwei seitliche Spielzeughindernisse. Zwölf aufeinanderfolgende Rückseitenanforderungen des produktiven Selektors mit echter Navigation: zwölf erreichbar hinten, sechs tiefer im Gang. Eine separat erzwungene Stirnwand-Auswahl gelingt bei 6,5 m Wandentfernung mit der kompakten Form. Ein quer durch den Gang reichendes Hindernis führt in vier weiteren Versuchen zu gültigen Ausweichplätzen im Hauptraum. Nach Entfernen aller bekannten Freiraumdaten wird kein Spawn erlaubt.

Der erste kompakte Entwurf war zu schmal: 77 cm Fenster gegenüber 78,6 cm gemessener maximaler Austritts-Silhouette. Der Test schlug korrekt fehl; die Öffnung wurde auf 81,2 cm verbreitert und erneut geprüft. Fehlgeschlagener Lauf bleibt als `Verification/RearPortals/quick-1.log` erhalten; erfolgreicher isolierter Lauf `quick-2.log`, Verteilung `corridor-placements.csv`.

Die Suchpräferenz bezieht sich auf die Blickrichtung zum Suchzeitpunkt, nicht auf eine dauerhaft gespeicherte Raum-Vorderseite. Spieler können sich während des Vorlaufs drehen. Ein Gang ohne zusammenhängenden bekannten Boden, ein verdeckter Durchgang, echte Blockaden oder fehlende Wandfläche bleiben Ausschlussgründe. Das ist kein Nachweis eines Live-Scans über unbekannte Räume hinweg oder einer Gleichwertigkeit mit Laser Tag/Spatial Ops.

Offene getragene Abnahme: tatsächlicher hinterer Gang mit vorhandenem Spielzeug, räumliche Vorwarnung, Rahmen-/Körpersilhouette aus verschiedenen Blickwinkeln und Frametimes während der erweiterten Suche. Automatische Prüfungen ersetzen diese Abnahme nicht. Build-/Installationsnachweis in [BUILD-REPORT.md](BUILD-REPORT.md).
