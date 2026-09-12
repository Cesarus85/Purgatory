# Purgatory — Implementierungskonzept V19 bis V24

Stand: 8. September 2026, ergänzt um Nutzerplot, Gegnervielfalt, Schnellneustart und späteren Vier-Spieler-Koop. Ausgangsbasis: ausgelieferte V19.18 (0.19.18 / Code 57). Der Dateiname bleibt für bestehende Links erhalten.

Dieses Dokument ist der maßgebliche Umsetzungsplan für die Hauptblöcke. Die ältere `ROADMAP.md` bleibt als Historie und ursprüngliche Zielbeschreibung erhalten. Die Versionsüberschriften bezeichnen Meilensteine, keine verbindlichen APK-Nummern oder Termine. Der ursprüngliche Planungsauftrag änderte keinen Spielcode und löste weder Installation noch Assetkauf aus; nachfolgende ausdrücklich beauftragte Umsetzungen sind hier separat vermerkt.

**Umsetzungsstand 8. September 2026:** V19 inklusive Schnellneustart, Raumverteilung und Kampfregie implementiert; Startkorrektur V19.20 auf Quest bestätigt, V19-D-Spiel-/Leistungsabnahme noch offen. V20-B/C/E implementiert und mit nativen Tests als **V20.0/code60** gebaut, einschließlich Nutzer-Katana aus Blender und sichtbarer deformierender Schnittspuren. V20-A hat native Vergleichsansichten, die gemeinsame Stereo-/Ensemble-/Controller-/Leistungsabnahme aus V19-D und V20-A/D bleibt wegen ausgeschalteter Quest offen. [V20-Lieferbericht](V20-DELIVERY.md), [kombinierte Testfolge](V19-V20-TESTPLAN.md). V21–V24 bleiben geplant, einschließlich insgesamt vier Spielern im späteren Koop.

## 1. Ziel und bewahrter Bestand

Nachtrag 12. September 2026, V20.6: V20.5-Erkennung/Wunden vom Nutzer grundsätzlich bestätigt, tödliche leichte Kontakte beanstandet. Freigegeben und implementiert: abgestufter Schaden plus kleine Streifwunden bei unveränderter verlässlicher Schlagfolge, langsame Stichklassifizierung über verschiedene Bildraten korrigiert; nahe Portalöffnung und sicherer Gegner-Austritt getrennt, kurze seitliche Gehwege mit Spieler-/Möbelschutz und begrenztem Warten. **0.20.6/code66 gebaut und vollständig lokal verifiziert**, 1.740 neue native Prüfungen plus Vorgängerregressionen, keine Installation, Quest-Abnahme offen. [Umsetzung und Testfolge](V20.6-CONTACT-AND-CLOSE-PORTALS.md). Keine Vorziehung von V21–V24.

Nachtrag 9. September 2026, V20.5: Erneuter Nutzertest beanstandet ausgelassene Hiebe und kaum sichtbare Schnitte. Freigegebener Zuverlässigkeitsblock implementiert: echte Gegenhiebe ohne alte Gegnersperre, nachgeprüfter kurzer Schlaganlauf, Stillstand-/Zitterschutz, korrigierte Normalisierung sehr kleiner Hautdreiecke und sichtbare frische Wunden bei Dämonen/Fledermäusen. Grundschaden und Katana-Auslöser unverändert. 1.138 neue native Prüfungen und vollständige V20.4-Regressionskette bestanden; **0.20.5/code65 gebaut und vollständig lokal verifiziert**, keine Installation, getragene Quest-Abnahme offen. [Umsetzung und Testfolge](V20.5-KATANA-RELIABILITY.md). V21–V24 bleiben unverändert geplant.

Nachtrag 9. September 2026, V20.4: Nutzertest bestätigt Stiche, beanstandet abgeschwächte Hiebe/fehlende Wundlesbarkeit, verspätetes Schwungrauschen und unverständlichen Siegel-Nachschub. Umgesetzt: gleichwertige bewusste Hiebe/Stiche bei erhaltenem Berührungsschutz, robuste Schnittwunden, Schwungton aus und sichtbares Warten/Verlagern des fehlenden Nachschubs. **0.20.4/code64 gebaut und lokal verifiziert**, 3.001 neue parametrisierte Prüfschritte plus bisherige Regressionen; nicht installiert, Quest-Abnahme offen. [Umsetzung und Testfolge](V20.4-KATANA-SEAL-POLISH.md). Keine Vorziehung von V21–V24.

Nachtrag 9. September 2026, V20.3: Klingenhaltung im Nutzertest akzeptiert. Umgesetzt sind robustere wackelige Stiche, bewegungsabhängiger Kontaktschaden statt leichter Sofort-Kills, ein Feuer-/Plasma-Parierklang und mittige durchgehende Fledermaus-Austritte. **0.20.3/code63 lokal gebaut und vollständig verifiziert, nicht installiert.** [Korrekturblock und Testfolge](V20.3-KATANA-CONTACT-FIXES.md). Kein geänderter Katana-Auslöser, keine Vorziehung von V21–V24. Getragene Quest-Abnahme offen.

Nachtrag 9. September 2026: Nutzer meldet Verbesserungen mit V20.1. Der freigegebene Korrekturblock **V20.2** richtet die Klingenfläche aus, ersetzt den glockenartigen Parierklang, begrenzt das Schwungrauschen, trennt Spitzenstiche von Schnitten und macht die Fernkämpfer aktiver. Native Prüfungen bestanden; APK-Lieferstatus und Testfolge stehen im [V20.2-Bericht](V20.2-KATANA-COMBAT-POLISH.md). Rückwärtige Portalverteilung bleibt bis zum Test in einem freieren Raum unverändert. Keine Vorziehung von V21–V24.

Nachtrag V20-Nutzertest: Schnellneustart, Katana-Schaden, Feuerballparade und Rückkehr zum Revolver bestätigt; keine Leistungsbeanstandung, aber weiterhin kein gemessener Langzeitnachweis. V20.1 korrigiert Griff-/Treffergeometrie, Lesbarkeit tödlicher Schnitte und kreisende Boden-Gegner und ergänzt rückwärtige Portaldiagnostik. [Korrekturblock und Testfolge](V20.1-CORRECTIONS.md). Hauptblöcke V21–V24 werden dadurch nicht vorgezogen.

Eine intensive, verständliche MR-Rissjagd im eigenen Zimmer: abwechslungsreiche Kämpfe, materiell glaubhafte Dämonenwelt, zwei besondere Perk-Waffen und ein inszeniertes Finale. Große körperliche Ausweichbewegungen sind keine Spielvoraussetzung.

### Erzählerischer Kern — die letzte Bewährungsprobe

Der Spieler verkörpert einen bei Gott in Ungnade gefallenen Sünder. Er lebt noch, weiß aber, dass ihn nach dem Tod das Fegefeuer und mit hoher Wahrscheinlichkeit die Hölle erwarten. Ihm steht eine letzte Bewährungsprobe zu: Gott öffnet für diesen Kampf die Schleusen zur Hölle. Besteht der Sünder sämtliche Wellen, ist sein späterer Zugang zum Himmel gesichert. Scheitert er, lautet das Urteil Hölle. Dies ist die eigene Mythologie des Spiels, keine Simulation kirchlicher Lehre.

Gott ist dabei zugleich Richter und barmherziger Helfer: In aussichtslos wirkenden Situationen erhält der Spieler vorübergehend göttliche Waffenhilfe. Die Shotgun und das Katana sind deshalb **Gnadenwaffen**, keine beliebigen Ausrüstungsdrops. Der Revolver und die Wurfsterne bleiben seine bekannten Werkzeuge. Der Torwächter verkörpert den letzten Widerstand der Hölle, nicht einen Gegner aus dem Himmel.

Konsequenzen für Gestaltung und Mechanik:

- Der echte Raum ist der Ort der Prüfung, nicht schon das Fegefeuer. Portale lassen die zugelassene Höllenmacht herein; die einzelnen Gegner und ihr Nachschub bleiben feindlich. Optionale Siegel unterbrechen ihren Nachschub und verweigern nicht Gottes Prüfung.
- Start am Schrein: kurze überspringbare Einleitung und ausdrückliches „PRÜFUNG ANNEHMEN“. Arbeitstext: „Dein Urteil steht bevor. Bestehe die Prüfung, und die Pforten des Himmels werden dir offenstehen.“ Keine langen Textwände oder Ansprache während notwendiger Raumkalibrierung.
- Fortschrittsanzeige „Prüfung · Welle 7/20“ und knappe Aktwechsel statt zusätzlichem Moral-Punktestand. Gute Treffer bedeuten spielerische Leistung, keine automatische moralische Bewertung.
- Sieg sichert die Erlösung **für das spätere Ableben**; der Spieler stirbt im Sieg nicht automatisch und muss nicht körperlich durch ein Portal laufen. Niederlage zeigt das Höllenurteil als kurze kontrollierte Inszenierung, anschließend Wiederholen/Beenden.
- 20–25 Wellen sind der gewünschte Zielkorridor, noch keine final getestete Länge. Startauslegung: 20 Wellen in fünf Akten zu vier Wellen; 25 Wellen als konfigurierbare Vergleichsfassung mit fünf Wellen je Akt. Nicht einfach 25-mal dieselbe Gegnerfolge verlängern.
- Die frühere verbindlich wirkende 8–12-Minuten-Vorgabe entfällt für die Hauptprüfung. Dauer erst messen und mit dem Nutzer abstimmen; freiwillige Aktpausen und ein sicherer Unterbrechungsspeicher gehören zur längeren Prüfung. Ein späterer Kurzmodus kann weiterhin 8–12 Minuten anstreben.

Diese erzählerische Richtung stammt vom Nutzer. Aktnamen, Intro-/Urteilstexte, konkretes Timing und Koop-Regeln unten sind vorgeschlagene Ausgestaltung.

Vorhanden und weiterzuverwenden: Raumscan/-profile mit Zwei-Punkt-Kalibrierung, Schrein und Handrollen, Revolver, göttliche Fünf-Schuss-Pump-Shotgun, Wurfsterne, Lebens-/Munitionsrelikte, optionale Nachschubsiegel, Boden-/Fluggegner, abschießbare Feuerbälle, echte animierte Trefferflächen, Blutspuren, mehrere räumliche Portal-Schauplätze, Portalflackern sowie der Animations- und Rauchwechsel-Polish aus V19.18. Diese Funktionen werden nicht nochmals als Neuentwicklung eingeplant.

Nutzerfeedback zur letzten Version ist positiv. Das ersetzt keine aktuelle Langzeit-/Thermikmessung oder Abnahme sämtlicher Kombinationen. Historische offene Hinweise sind mit aktuellem Verhalten abzugleichen, nicht pauschal erneut als Fehler zu behandeln.

### Verbindliche Leitplanken

- Siegel bleiben optionale Eingriffe während des Kampfes. Keine Pflichtpause bis zum Zerschießen, kein erneutes V19.0-Siegelgate.
- Reale Hindernisse und unbekannte Bereiche werden nicht zur Verbesserung der Spawnquote ignoriert. Ein Raum darf zu eng für eine bestimmte Begegnung sein.
- Kein Kamerawackeln, keine erzwungene Fortbewegung, kein Abschalten der Systemsicherheitsgrenzen. Effekte dürfen reale Hindernisse nicht dauerhaft verdecken.
- Handrollen gelten überall; keine fest auf rechts verdrahtete Perk-/Menüsteuerung. Trackingverlust, Pause, Tod, Raumwechsel und Neustart gehören zu jedem Paket.
- Bestehende Spielstände, Raumprofile, Audio-/Handeinstellungen und ältere APKs erhalten. Neue Konfigurationsfelder bekommen rückwärtskompatible Standardwerte.
- Quest-Leistungsziel bleibt stabile 72 Hz, circa 13,9 ms Framebudget. Neue visuelle Qualität und mehr Gegner sind nur innerhalb gemessener Reserven zulässig.

## 2. Reihenfolge und lieferbare Pakete

| Reihenfolge | Paket | Ergebnis | Abhängigkeit |
|---|---|---|---|
| 0 | V19-0 Schnellneustart | Nach Spielende direkt erneut spielen, gültigen Raum/Schrein behalten | V19.18 |
| 1 | V19-A Raumverteilung | Bodenportale nutzen geeignete enge hintere Raumteile nachvollziehbar | V19.18 |
| 2 | V19-B Kampfregisseur | Raumabhängiger Kampfdruck und faire Angriffskombinationen | V19-A |
| 3 | V19-C Rollen und Präzision | Klarere Gegnerrollen, Schwachstellen, Abfangbonus und konsistentes Feedback | V19-B |
| 4 | V19-D Integration | Balance-/Raum-/Leistungsnachweis für den Gesamtblock | V19-A bis C |
| 5 | V20-A Qualitätsreferenz | Gemeinsamer Grafik-/Audio-Qualitätsschnitt des vorhandenen Ensembles | V19-D |
| 6 | V20-B Perk-Grundlage | Gemeinsame, getestete Verwaltung von Shotgun und Katana | V19-D |
| 7 | V20-C Katana | Spielbarer Nahkampf-Perk mit eigenem Modell, Klang und Effekten | V20-A/B |
| 8 | V20-D Gesamtprüfung | Katana, Shotgun, Sterne und Szenen im Headset abgestimmt | V20-C |
| 8a | V20-E Gegnerausbau I | Varianten und erster neuer Gegnertyp als Qualitätsreferenz | V19-C, V20-D |
| 9 | V21-A Rauminszenierung | Oberflächenreaktionen, räumliche Auftritte und dynamische Klangschichten | V20-D |
| 10 | V21-B Torwächter | Ein vollständiger, raumsicher skalierter Bosskampf | V19-B/C, V21-A |
| 10a | V21-C Gegnerausbau II | Zwei weitere unterschiedliche Gegnertypen und gemischte Begegnungen | V20-E, V21-B |
| 11 | V22-A Letzte Prüfung | Fünf Akte, 20–25 Wellen, Urteil und Unterbrechungsspeicher | V21-B/C |
| 12 | V22-B Entscheidungen | Sechs Upgrades, Auswertung, Schwierigkeit und lokale Highscores | V22-A |
| 13 | V22-C Endlosmodus | Eigene Eskalation und sauberer Abschluss/Neustart | V22-B |
| 14 | V23-A/B Alpha | Onboarding, Komfort, Lebenszyklus und reproduzierbares Testpaket | V22-C |
| 15 | V24-A Koop-Prototyp | Topologie-/Raumkonzept wählen und mit zwei Geräten belegen | V23; vorbereitete Zustandsgrenzen ab V19 |
| 16 | V24-B/C Koop | Ausbau auf bis zu vier Spieler insgesamt, Balance und Geräteabnahme | V24-A |

Jedes Paket erhält einen kurzen Änderungsbericht, automatisierte Prüfungen und eine eigene rückverfolgbare APK, sobald es im Headset sinnvoll beurteilbar ist. Keine riesige Sammellieferung bis V23. Spätere Pakete dürfen vorab entworfen werden; technisch abhängige Arbeit beginnt erst nach bestandenem Gate.

## 3. V19 — Raumverteilung und Kampfentscheidungen

**Umsetzungsstand V19.19:** Schnellneustart, raumlokale Kandidatensuche/Feinnavigation, Kampfregisseur mit Angriffsfreigaben und bestehende Gegnerrollen mit Oberflächen-Schwachstelle/Abfangbonus sind implementiert. Gesamttest und Paketlieferung werden in [V19.19 Umsetzung und Abnahme](V19-19-COMPLETION.md) dokumentiert. **V19-D ist kein abgeschlossener Hardware-Nachweis:** getragene Raum-/Balanceprüfungen und aktuelle Langzeit-/Thermikmessungen bleiben ausdrücklich als Gate zum breiten V20-Ausbau stehen.

### V19-0: Schnellneustart ohne erneute Raumeinrichtung

**Neuer Nutzerbefund, vor den anderen V19-Paketen zu bearbeiten:** Nach Spielende/Tod landet der Spieler wieder in der Raumwahl und muss die Zwei-Punkt-Ausrichtung wiederholen. Ein bloßer Rundenneustart darf keine neue Raum-Sitzung erzwingen. Ursache im tatsächlichen Ende-/Start-/Setup-Zustandspfad untersuchen; nicht durch ungeprüftes Setzen eines „Raum bereit“-Flags umgehen.

- Am vorhandenen Schrein bzw. einer kompakten Ergebnisansicht: primär **ERNEUT SPIELEN**, daneben **RAUM ÄNDERN** und **ZUM MENÜ**. Ein Klick auf „Erneut spielen“ genügt; danach kurzer klarer Startvorlauf. Auch das Hauptmenü bietet bei gültigem aktuellem Raum „Neue Prüfung“ ohne Raumwahl an.
- `RunSession` und `RoomSession` als Verantwortlichkeiten trennen: Rundenreset entfernt Gegner, Portale, Geschosse, Blut/Effekte, Angriffserlaubnisse, ausstehende Coroutinen und Perk-/Wellen-/Ergebniszustand; setzt Spielerressourcen und Sternvorrat auf die dokumentierten Startwerte. Er erhält Raumgeometrie, gültige Kalibrierung/Anker, bestätigte Schreinpose sowie Hand-/Audio-/Komforteinstellungen.
- Kein automatischer Aufruf von Scannerstart, Profilwahl oder Zwei-Punkt-Kalibrierung durch Tod, Sieg, „Erneut spielen“ oder Rückkehr aus der Ergebnisansicht. Kein Reload der gesamten Szene, wenn dadurch die gültige Raum-Sitzung zerstört würde.
- Kurze nicht-interaktive Gültigkeitsprüfung des bestehenden Tracking-/Raumzustands vor Freigabe. Bei intaktem Zustand keine zweite Bestätigung. Ein kurzzeitig fehlender Feed ist nicht automatisch Anlass zum Löschen des Scans; auf Wiederherstellung warten und Abbrechen ermöglichen.
- Bei wirklich ungültiger Ausrichtung, Raumwechsel oder verlorener Referenz kontrolliert pausieren und konkret erklären, was fehlt. Nur die erforderliche Wiederherstellung anbieten. Der Wunsch nach Schnellstart erlaubt keine Kämpfe auf verschobener Geometrie.
- „Raum ändern“ startet die bewusste Auswahl/Neuerfassung. Wiederholtes Spielende überschreibt keine Raumdateien und löscht keine Kalibrierung. Neuer App-Prozess und Fortsetzen nach App-Neustart bleiben vom In-App-Schnellneustart getrennt: gespeicherte Daten müssen dort weiterhin räumlich verifiziert werden.
- Eingaben vor Neustart neutralisieren; der Klick auf „Erneut spielen“ darf nicht zugleich schießen/werfen. Gnadenwaffen und Sternregeneration haben keine alten Timer/gesperrten Hände aus dem vorigen Spiel.

Abnahme: zehnmal Tod → Erneut spielen und mehrmals Sieg → Neue Prüfung in derselben laufenden App, auch nach Handwechsel und aktivem Perk beim Tod. Null automatisch gestartete Scans/Kalibrierungen bei gültigem Raum, gleichbleibende Raum-/Schreinpose, keine alten Gegner/Angriffe, keine anwachsenden Ressourcen-/Coroutinebestände. „Raum ändern“, echter Trackingverlust und App-Kaltstart als getrennte Gegenfälle. Getragener Test muss ohne erneutes Setzen der zwei Punkte direkt in die nächste Runde führen.

### V19-A: Bodenportale in engen hinteren Raumteilen

**Problem:** Hintere Bodenportale sind schon ab Welle 3 vorgesehen. `SpawnDistribution` enthält hintere Suchstrahlen, 7,5 m Suchreichweite und kleinere Rahmen-/Gegnerzuordnung. Nur die Zufallswahrscheinlichkeit zu erhöhen, genügt deshalb nicht. Zu unterscheiden sind fehlende Kandidaten, ein zu breiter Rahmen, ein unpassender Austritt und ein tatsächlich nicht begehbarer Weg.

Umsetzung:

1. Pro Kandidat begrenzt protokollieren: Raumsektor, Oberflächenrevision, Rahmenvariante, Gegner, Austrittsvariante, Ablehnungsgrund und Bewertung. Gründe mindestens: unbekannte Fläche, Rahmen passt nicht, Körpervolumen blockiert, Schwelle/Landung blockiert, kein Anschlussweg, Spieler-/Gegnerabstand, nur schlechtere Verteilungswertung. Diagnose im normalen HUD aus.
2. Kandidaten zusätzlich von erfassten Wandabschnitten und begehbaren Bodenregionen ableiten, nicht ausschließlich vom momentanen Kopfstandort aus. So können Nischen und abgewinkelte Gangenden gefunden werden. Begrenzten Kandidatenspeicher nur bei relevanten Raumrevisionen erneuern; Abfragen über Frames verteilen.
3. Getrennte Tests für sichtbaren Rahmen, echte Öffnung, vollständige animierte Körperhülle, Schwellenbewegung und Anschlussweg. Lokale Spielzeuge dürfen nicht einen ganzen Wandabschnitt pauschal verwerfen; eine tatsächlich belegte Austritts-/Laufbahn bleibt gesperrt.
4. Pro Ort passende Kombinationen versuchen: normaler/bodennaher kompakter Rahmen, schlanker Dämon, Schritt statt Sprung oder umgekehrt, sofern die jeweilige vollständige Bewegungsbahn frei ist. Kein unplausibles Schrumpfen des Monsters und kein Spawnen durch Möbel.
5. Navigation auf Konsistenz untersuchen: `RoomNavigator` nutzt derzeit 0,32-m-Zellen und feste Freiraumradien. Gegnerabhängige Durchgangshüllen und begrenzte lokale Feinprüfung dürfen konservative Rasterablehnungen auflösen, aber keine gemessenen Hindernisse entfernen. Eintrittsweg und Verfolgung müssen dieselben Freiraumregeln verwenden.
6. Raumfeste Sektoren verhindern, dass bloßes Kopfdrehen die Nutzungshistorie umsortiert. Auswahl bevorzugt unterrepräsentierte erreichbare Sektoren; die aktuelle Blickrichtung steuert die rückwärtige Vorwarnung. Ab Welle 3 zunehmend hintere Bodenorte einsetzen, wenn tatsächlich geeignet. Zwei Sekunden hörbaren Vorlauf für rückwärtige Austritte erhalten.
7. Vor Öffnung und vor Eintritt erneut prüfen. Ungültiger Kandidat wird verworfen und begrenzt ersetzt. Ohne sicheren hinteren Ort vorne/seitlich ausweichen; kein erzwungener Spawn und kein endloser Suchzustand. Keine vorzeitige Meldung „Welle geschafft“, wenn noch keine Begegnung zustande kam.

Abnahme:

- Synthetische Räume: großer Hauptraum mit engem hinterem Gang, L-Grundriss, Spielzeug außerhalb/im Austrittsweg, Sofa vor Wand, unbekannter hinterer Bereich. Positive UND negative Fälle.
- Bei mindestens zwei nachweislich geeigneten Bodenregionen zeigt eine deterministische Serie von 30 Bodenplatzierungen ab Welle 3 Nutzung beider Regionen; mindestens ein hinterer Austritt innerhalb von sechs berechtigten Bodenversuchen. Das gilt nur bei unverändert freien Testbedingungen, nicht als unsichere Laufzeitquote.
- Mindestens 30 Bodenannäherungen und 30 Fledermausanflüge je Referenzraum. Kein Teleportieren/Wanddurchdringen; Blockaden über zwei Sekunden führen zu protokollierter Recovery oder kontrolliertem Abbruch.
- Getragener Test im betroffenen hinteren Raumteil. Ablehnungen müssen anhand der Diagnose erklärbar sein. Scanqualität, Speichern/Laden und Such-Frametimes dürfen nicht schlechter werden.

### V19-B: Kampfregisseur

Heute steuert `QuestDemonGame.WaveLoop` vorwiegend Stückzahlen und Intervalle; Crowd-Limit und maximal zwei aktive Portale bestehen bereits. Das ist die Integrationsbasis, nicht ein Anlass zum vollständigen Spielumbau.

- Neuer kleiner `EncounterDirector` als testbarer Zustandskern: Druck aufbauen → Angriffsspitze → kurze Entlastung. Entlastung reduziert zunächst neue Angriffe/Nachschub; sie erzwingt keinen leeren Raum und kein Warten auf Siegel.
- Begegnungen erhalten Akt-/Wellenprofile für die spätere Bewährungsprobe. Die fünf Akte verändern Rollen, Kombinationen und Inszenierung; kein bloßer endloser Lebenspunkteanstieg. V19 liefert die Schnittstelle, V22 die vollständige 20–25-Wellen-Dramaturgie.
- Eingaben: erreichbare Raumsektoren, aktive/ankommende Gegner, laufende Angriffsvorbereitungen und Projektile, Ressourcenlage sowie Wellenfortschritt. Ausgaben: nächste Rollen-/Portalwahl, Spawnzeitfenster und begrenzte Angriffsfreigaben.
- Separater `AttackCoordinator` vergibt kurzlebige Freigaben für Nahschlag, Sturz und Wurf. Freigaben werden bei Tod, Abbruch, Pause/Trackingwechsel korrekt zurückgenommen. Bereits fliegende Projektile zählen weiter zum Druck.
- Erstes Tuning: in kleinen Räumen höchstens ein unmittelbar drohender Nah-/Sturzkontakt, dazu nur zeitlich versetzt ein klar angekündigter Fernangriff. Größere Räume dürfen zwei koordinierte Angriffe tragen; nie unangekündigte Kontaktspitzen von entgegengesetzten Seiten. Exakte Fenster werden anhand der vorhandenen `CombatTiming`-Kontaktzeiten bestimmt.
- Gegner ohne Angriffsfreigabe bewegen sich weiter sinnvoll, suchen Sicht/Abstand oder drohen; sie stehen nicht in einer Warteschlange. Raumfreigabe und Angriffserlaubnis bleiben getrennt.
- Gegen Selbstheilungsschleifen: keine verdeckte Änderung von Lebenspunkten oder Trefferwirkung. Schwierigkeit ergibt sich aus nachvollziehbaren Rollen, Tempo und Kombinationen. Bestehende Notmunition bleibt.
- Deterministische Seeds und kompakte Entscheidungsereignisse ermöglichen A/B-Vergleiche mit dem bisherigen Wellenverhalten. Alter Ablauf bleibt während Entwicklung als Diagnose-Rückfall verfügbar.

Abnahme: reproduzierbare Angriffstimeline mit korrekten Freigaben; keine Freigabe-Leaks, Starvation, Wellenblockaden oder nach Pause aufgestauten Sofortangriffe. Fünf vollständige Testwellen je kleiner/großer Referenzfläche; vom Nutzer empfundene Intensität mindestens erhalten. Kein Ausweichen in Richtung Möbel erforderlich.

### V19-C: Gegnerrollen, Schwachstellen und Abfangen

- Rollen zunächst mit bestehenden Archetypen/Assets schärfen: schlanker Nahkämpfer bindet; Werfer bevorzugt freien Abstand; Fledermaus kündigt ihren Stoß an; schwerer Gegner erhält eine lesbare, zeitweise exponierte Schwachstelle. Keine neue Monsterfamilie in diesem Paket.
- Rollenparameter getrennt von Animations-/Renderingcode konfigurieren. Der Regisseur wählt zulässige Kombinationen, die lokale KI sucht den tatsächlichen sicheren Weg.
- Schwachstellen anhand der animierten Oberfläche/zugeordneten Dreiecke prüfen, nicht anhand einer großen unsichtbaren Kugel vor dem Körper. Gegenprüfungen bei Blickbewegung, Zunge, Trefferpose und Portalübertritt. Kein Schießen durch eine davorliegende Körperfläche oder Wand.
- Abschießbare Feuerbälle bleiben erhalten. Neu ist ein klar signalisiertes Präzisionsfenster vor dem Einschlag: genau ein erfolgreicher Abfangbonus pro Projektil, zunächst Punkte und eine kleine begrenzte Munitionsbelohnung. Keine Bonusfarm durch mehrfach registrierte Pellets/Sternkontakte.
- Material-/Trefferkategorien vereinheitlichen: Fleisch, Panzerung, Schwachstelle, Abfangen, Kill. Pro tatsächlichem Ereignis abgestimmter Klang, kurze sichtbare Reaktion und Haptik; vorhandene Lautstärken/Limiter respektieren.
- Lebens- und Munitionsversorgung separat prüfen; volle Ressourcen verbrauchen kein Relikt. Shotgun-Hilfe nicht versehentlich durch einen dauerhaft zu niedrigen Gegnerdeckel unerreichbar machen.

Abnahme: geometrisch korrekte Treffer und Belohnungen, keine Mehrfachzählung bei Shotgun/Streuschuss; erkennbare Unterschiede auch ohne Farberkennung. Drei Testpersonen können gefährliches Ziel, Schwachstelle und optionale Siegel ohne mündliche Erklärung unterscheiden.

### V19-D: Integration und Leistungsbasis

- Aktuelles Quest-Profil statt Übertragung alter V17-Zahlen: CPU/GPU, P95/P99-Frametimes, verpasste Frames, Speicher/GC und thermischer Verlauf. Lastfälle 0/1/2 Portale, mehrere Gegner, Blut, Wurfsterne, Shotgun/Himmel und Scan/Schrein getrennt.
- Zwei unterschiedlich eingerichtete Räume, drei Spieltests und mindestens ein 20-Minuten-Lauf. 72-Hz-Ziel mit Reserve; Durchschnitts-FPS allein genügen nicht.
- Erst gemessene Engpässe bearbeiten: Kandidaten-/Wegabfragen verteilen, Daten wiederverwenden, unnötige Skin-Bakes vermeiden, Effekte begrenzen. Keine pauschale Auflösungsreduzierung vor Ursachenprüfung.
- Gate zum breiten V20-Ausbau: keine reproduzierbare Fortschrittsblockade, stabile Raum-/Hand-/Waffenbasis und dokumentiertes verfügbares Grafikbudget. Ein kleiner Art-Prototyp darf davor entstehen, nicht die komplette Assetproduktion.

## 4. V20 — Qualitätsabschluss und Katana-Perk

### V20-A: Bestehendes Ensemble zusammenführen

Vergleichbare Headset-Szenen mit gleichem Abstand/Licht: Revolver und Shotgun, Dämon/Fledermaus, Schrein/Relikte sowie Schmiede, Brücke, Kathedrale und vertikaler Fledermausschacht. Schärfe, Materialmaßstab, Kontrast, Animationen, Stereo-Parallaxe und Tonbalance beurteilen. Kein erneuter Ersatz bereits gelungener Assets.

Gezielte Korrekturen: UV-/Normal-/AO-Details, saubere Übergänge, gegebenenfalls gebackene Portalbeleuchtung und klar getrennte Nah-/Mittel-/Fernebenen. Portalrenderauflösung nach gemessener Wirkung und Last abstimmen. Alternative Portaltechnik nur bei belegtem Problem, nicht als spekulativer Pipelinewechsel.

### V20-B: Gemeinsame Perk-Waffenverwaltung

`DivineShotgunState` ist heute eine eigenständige Fünf-Schuss-Zustandsmaschine. Vor dem Katana eine schmale `PerkWeaponController`-Schicht ergänzen; keine zweite unabhängige Logik darf gleichzeitig die Führungshand übernehmen.

- Zustände: Revolver → Ankunft → aktive Perk-Waffe → Abgang → Revolver. Shotgun/Katana besitzen eigene interne Mechanik; nur eine Waffe verursacht zur selben Zeit Schaden.
- Shotgun-Regel erhalten: laufendes Spiel, 1–49 Leben, mindestens drei lebende vollständig eingetretene Gegner, einmal je Welle, fünf tatsächlich abgegebene Schüsse. Bestehenden Pumpgriff, Himmel und Rauchabgang erhalten.
- Eine schmale Gnadenhilfe-Auswahl bewertet nur nachvollziehbare Spielsituationen und verfügbare sichere Waffenaktionen. Kein Zufalls-Katana allein als Siegprämie: beide Perk-Waffen stehen erzählerisch für Hilfe in Bedrängnis. Bestehende Shotgun-Bedingungen bleiben zunächst unverändert; wenn diese erfüllt sind, hat sie weiterhin Vorrang. Für 20–25 Wellen Hilfshäufigkeit separat messen, bevor bestehende Grenzen verändert werden.
- Revolvermagazin/-reserve, Wurfsternvorrat und dessen Regeneration über alle Wechsel erhalten. Keine heimliche Gratisnachladung durch Perkwechsel.
- Shotgun hat bei gleichzeitigem Anspruch Vorrang. Eine aktive Katana-Aktion wird kontrolliert beendet, spätestens nach einem begrenzten Übergangsfenster von initial 0,6 Sekunden; anschließend beginnt die Rettungswaffe. Katana-Restladung kann in genau einem vorgemerkten Slot erhalten bleiben. Keine sofortige Schadensaktivierung mitten im Waffenwechsel.
- Katana wird nach Shotgun-Ende nicht überraschend automatisch erneut eingesetzt: verfügbarer Rest wird sichtbar angeboten. Kein Stapeln mehrerer vorgemerkter Katana-Ladungen. Wann ein Perk als verbraucht gilt, entscheidet sein tatsächlicher Aktivierungsübergang.
- Pause friert Perkzeit; Trackingverlust beendet aktive Schlag-/Pumpgesten und sperrt Treffer bis zur erneuten stabilen Freigabe. Handwechsel nur über den vorhandenen sicheren Menüablauf. Tod/Rundenreset löschen Bonuszustände, nicht gespeicherte Räume.

Abnahme vor Katana-Art: bestehende Shotgun-Prüfungen unverändert in ihrer Bedeutung, plus Wechselmatrix für beide Hände, Sterne, Pause, Tod, gleichzeitigen Bonusanspruch und wiederholten Neustart.

### V20-C: Katana — Bedienung, Kampf und Gestaltung

**Festgelegt:** zusätzliche zeitlich begrenzte Perk-Waffe in der Führungshand; kein Ersatz der Shotgun und keine dauerhaft parallel gehaltene zweite Hauptwaffe. Stil: gealterter Stahl, dunkler Griff, sakrale Gravuren und kontrollierte weiß-goldene Energie, passend zum göttlichen Gegenpol der Dämonenwelt.

**Erste Balanceannahmen, nach Test anpassbar:** Der frühere Vorschlag „drei Siegel = Katana“ wird durch die Plot-Ergänzung ersetzt. Katana-Hilfe wird angeboten, wenn Leben unter 50 liegen, mindestens zwei aktive Bedrohungen bestehen, darunter ein sicher erreichbarer Nahkämpfer, und die Shotgun-Rettung dieser Welle bereits verbraucht ist oder ihr Drei-Gegner-Auslöser nicht erfüllt werden kann. Keine Hilfe im Setup, bei Tod oder ohne geeigneten sicheren Nahkampfbereich. Initial höchstens eine Katana-Hilfe je Welle und 20 Sekunden Abstand zwischen beendeten Gnadenwaffen; keine gleichzeitige Aktivierung. Diese Schwellen müssen der Regisseur und die tatsächliche Waffenreichweite gemeinsam prüfen, nicht allein ein Gegnerzähler.

Der angebotene Segen wird aus sicherer Position mit der vorhandenen Relikt-Fernaufnahme angenommen; er zwingt keinen unerwarteten Waffenwechsel auf. Initial bis zu acht schadenswirksame Hiebe oder 25 Sekunden aktive Kampfzeit, je nachdem, was zuerst erreicht ist. Fehlschläge kosten keine Hiebladung, aber Zeit. Hilfe ist endlich und garantiert keinen Sieg. Tests prüfen insbesondere, dass absichtliches Niedrighalten der Gesundheit keine praktisch unbegrenzte Waffenschleife eröffnet. Diese Werte sind Konzeptvorschläge, keine finalen Spielregeln. Ein Diagnoseknopf aktiviert den Perk für Tests. V22-Upgrades dürfen Gnadenwaffen verbessern, ihre erzählerische Notlagenfunktion aber nicht durch einen obligatorischen Siegel-Grind ersetzen.

Bedienung und Ergonomie:

- Einhändig in der gewählten Waffenhand. Eine kurze natürliche Schnittbewegung genügt; kein maximales Tempo, weites Ausholen oder Ausfallschritt notwendig. Schaden steigt nicht unbegrenzt mit Schwunggeschwindigkeit.
- Zum Start nur Schneiden und kontrolliertes Projektil-Parieren, keine Stichmechanik und kein Zwang zum Nahblock gegen Monster. Gegner müssen sicher in Reichweite kommen, nicht der Spieler hinter Möbeln herlaufen.
- Freie Hand behält die Wurfsterne. Keine neue Zwei-Grip-Kombination, die Sterne oder Shotgun-Pumpe auslöst. Zweihändige Katana-Stütze ist nicht Teil des ersten Schnitts.
- Waffenpose/positive Skalierung für beide Hände prüfen. Virtuelle Klinge initial kompakt, etwa 65–75 cm ab Parierstange; finale Größe nach getragenem Reichweiten-/Komforttest.
- Optionaler Komfortmodus kann später einen kurzen, tastengestützten Schnitt bei kleiner Handbewegung anbieten; nicht als versteckter Auto-Kill im Basismodus.

Treffertechnik:

1. Controllerpose und virtuelle Klingensegmente zeitlich erfassen; langsames Halten im Gegner zählt nicht als Angriff. Ein begrenzter Bewegungsweg mit Hysterese startet und beendet einen Schlag. Schwellen anhand kleiner echter Handbewegungen kalibrieren.
2. Kontinuierlich überstrichene Klinge zwischen alten/neuen Posen mit begrenzten adaptiven Zwischenschritten prüfen, nicht nur einen Ray von der Klingenspitze pro Frame. Schnelle Drehung, niedrige Bildrate und Tracking-Sprünge separat behandeln; Tracking-Sprung verursacht nie einen Treffer.
3. Grobe Kandidatensuche, danach exakter Kontakt an `CombatSurface`/Dreiecksindex. Aufwand nur bei aktivem Schlag und nahen Kandidaten; keine Vollhaut-Bakes aller Gegner in jedem Frame. Reale Hindernisse vor dem Ziel haben Vorrang.
4. Schlag-ID verhindert wiederholten Schaden durch Überlappung. Initial maximal zwei verschiedene Gegner pro Hieb, eine Ladung pro Hieb mit Monsterschaden. Parieren zählt nicht als Monsterhieb; Anti-Farming-Cooldown und einmaliger Projektilabschluss.
5. Nahkämpfer sollen durch einen sauberen Hieb zuverlässig sterben können; schwere Gegner/der spätere Boss verwenden explizite Schadens-/Schwachstellenregeln. Keine pauschalen Kills durch Wände, Portalrahmen oder noch verdeckte Körperteile.
6. Reale Möbel können nicht physisch geblockt werden. Bei erkannter zu naher Oberfläche Hinweis/Haptik und virtuelle Trefferunterdrückung; keine Behauptung eines zuverlässigen Kollisionsschutzes. Sichere Umgebung und verfügbare Systemgrenzen bleiben erforderlich.

Art und Audio:

- Eigenes Blender-Modell mit korrektem Griffursprung, Schneide/Rücken/Parierstange, sauberem UV-Layout und gebackenen Metall-/Griffdetails. Ausgangsbudget höchstens etwa 20.000 Dreiecke, zwei bis drei Materialslots und maximal 2K-Texturen; endgültig am Gerät messen.
- Kurzer, geschwindigkeitsabhängiger Klingenschweif nur während aktiver Schnitte; keine dauerhafte Leuchtstange. Kontaktgerichtete Schnitt-/Funken-/Bluteffekte auf der echten Oberfläche, bestehende Blutbudgets wiederverwenden. Keine Körperzerteilung im ersten Paket.
- Ankunft/Abgang als begrenzte Licht-/Ascheformung entlang der Klinge, verwandt mit der Shotgun-Inszenierung, aber ohne jedes Mal einen weiteren großen Himmel zu öffnen.
- Eigene kurze Schwung-, Fleisch-, Panzerungs- und Parierklänge sowie differenzierte Haptik. Bestehende Soundbudgets/Lautstärkegruppen verwenden.
- Externe Modelle/Animationen nur nach dokumentierter Herkunft, Lizenz, Änderungsrechten und Importprobe. Kostenpflichtige Käufe brauchen separate Freigabe; kein Assetkauf durch diesen Plan.

### V20-D: Abnahme

- Links/rechts: 20 kontrollierte Nahschläge und zehn Paraden, ohne weites Ausholen. Halten/Zittern/Tracking-Recovery verursacht keinen Schaden; scharfe Kurven und schnelle Bewegungen tunneln nicht durch Ziel oder reale Wand.
- 30/72/90-Hz-Simulationsfälle und unterschiedliche Pose-Abtastraten, doppelte Kontakte, zwei Ziele, verdecktes Ziel, Portalübertritt, Pause mitten im Schlag, Shotgun-Priorität und Erschöpfung der Ladung.
- Kein Verlust von Revolvermunition oder Wurfsternen. Nach Perkende keine unsichtbare schädigende Klinge. Keine Änderung der existierenden Shotgun-Rettungsbedingungen.
- Gesamten Qualitätsausschnitt und kombinierte Effektlast im getragenen Headset abnehmen; V19-Leistungsbudget erhalten. Katana ist eine Wahlmöglichkeit, nie Voraussetzung für einen kleinen Raum oder einen Pflichtkampf.

### V20-E / V21-C: Bestiarium — Varianten und neue Gegnertypen

**Zusätzlicher Nutzerauftrag:** Mehr passende Gegnervariationen UND neue Typen. Die vorhandenen `Emberfiend`, `AshStalker`, `CinderBrute` und `RiftBat` bleiben erhalten. Kein bloßes Umfärben oder Hochskalieren als angeblich neuer Gegner. Neue Namen und Designs unten sind Konzeptvorschläge.

| Gegner | Eigenständige Silhouette / Präsentation | Kampfrolle und lesbare Gegenmaßnahme | Paket |
|---|---|---|---|
| Bestehende Dämonenvarianten | Unterschiedliche Horn-/Narben-/Glutdetails, Körperhaltung, Gang-/Angriffsakzente, Stimmen | Verhalten der Grundrolle bleibt erkennbar; keine verborgenen Trefferpunkt-Lotterien | V20-E |
| Fledermausvarianten | Zerrissene Flügel, andere Kopf-/Ohrenkontur, Flugrhythmus und Rufe | Sichtbar unterscheidbarer Sturzjäger bzw. ausdauernderer Kurvenflieger; keine neue Wanddurchflugfähigkeit | V20-E |
| **Kettenbüßer** | Gebückter gepanzerter Dämon mit gebrochenen Fesseln und sichtbarer Brustglut; nicht nur größerer CinderBrute | Langsamer Druckmacher mit frontalem Schutz. Vorbereiteter Schlag legt eine frontseitig erreichbare Schwachstelle frei; Präzision statt Umrunden realer Möbel | V20-E, erster neuer Qualitätsprototyp |
| **Höllenhund** | Niedrige, ausgemergelte vierbeinige Kreatur, eigener Gang/Rig, glimmender Rachen | Kurzer deutlich angekündigter Anlauf über eine geprüfte freie Bahn. Stoppen durch Treffer/Parade; kein Anspringen des realen Gesichts, kein Duckzwang | V21-C |
| **Aschenrufer** | Verhüllter, schmaler Ritualdämon mit bewegtem Kiefer, Aschesaum und klarer Kanalpose | Unterstützt genau einen vorhandenen Verbündeten mit zeitlich begrenztem, sichtbarem Schutz. Kanal unterbrechen oder Ziel priorisieren; kein unendliches Beschwören/Heilen | V21-C |
| **Torwächter** | Eigenständiger großer Portalboss | Finale mit angekündigten Angriffen und Schwachstellen, siehe V21-B | V21-B |

- Produktionsziel: drei neue reguläre Gegnertypen plus Torwächter, zusätzlich zunächst zwei erkennbare Varianten je bestehender Modellfamilie. Nicht alle gleichzeitig bauen: pro Typ Konzept/Silhouette → Blender-Rig-/Bewegungsprobe → spielbarer Gegner → Headset-Qualitäts-/Leistungstest → Detailausbau.
- Externe Assets dürfen helfen, wenn Herkunft/Lizenz/Rig und Stil passen; keine ungeprüften Downloads oder kostenpflichtigen Käufe durch diesen Plan. Gerade beim Höllenhund eigener Vierbein-Gang statt humanoidem Clip auf falschem Skelett.
- Jeder neue Typ braucht Ankunft, Fortbewegung/Kurve/Bremsen, Bedrohung, Angriff, Treffer, Abbruch und Tod; eigene Stimmen/Schritte und passende deformierte Trefferflächen. Varianten teilen Ressourcen, behalten aber glaubhafte Unterschiede. Kein Rückfall zu primitiven Platzhalterkörpern in der ausgelieferten Qualitätsfassung.
- Aschenrufer-Schutz erhöht nicht heimlich die Basis-Lebenspunkte, ist nicht stapelbar und endet bei Unterbrechung/Tod. Die verbleibende Situation muss auch mit Notmunition lösbar sein. Ketten/Asche sind begrenzte Darstellung, keine unkontrollierte Physikkette pro Gegner.
- Regisseur erhält klare Raum-/Rollenanforderungen. Ungeeigneter Typ wird durch eine zulässige Begegnung ersetzt, nicht in den Raum gezwängt. Initial höchstens ein neuer Spezialist je Gruppe; später getestete Kombinationen statt zufälliger Überlagerung aller Fähigkeiten.
- V22-Einführung: Akt I bekannte Grundrollen, Akt II erste Varianten/Kettenbüßer, Akt III Höllenhund, Akt IV Aschenrufer und abgestimmte Kombinationen, Akt V bekannte Mechaniken zugespitzt plus Boss. Beim ersten Auftreten eines Typs Platz/Audio für sein Signal lassen, ohne die ganze Runde anzuhalten.
- Abnahme je Typ: sichere Platzierung/Ankunft in zwei Raumgrößen, mindestens 20 Annäherungs-/Angriff-/Todzyklen, Treffer bei allen Posen und mit Revolver/Shotgun/Sternen/Katana, Pause/Reset, kein untreffbares Schutz-Endlosspiel. Erst nach Einzelabnahme gemischte Lastprüfung. Neue Assets erhöhen nicht automatisch das erlaubte Crowd-Limit.

## 5. V21 — MR-Inszenierung und Torwächter

### V21-A: Die Dämonenwelt greift in den Raum hinein

- Oberflächengebundene Risse, Asche, begrenzte virtuelle Glutauflagen und Kontaktwolken bei Landungen; aktuelle Raumgeometrie beachten und veraltete Auflagen entfernen.
- Wandportal: hörbarer Vorlauf, aufbrechende Tiefe, bewegte Details und räumliche Schließreaktion. Deckenportal: bestehenden senkrechten Fledermausaustritt inszenieren, nicht durch die alte gedrehte Bodenlandschaft ersetzen.
- Mehrschichtiges räumliches Ambiente und Musik mit Zuständen Ruhe/Druck/Spitze/Finale; Angriffswarnungen und Waffenklang bleiben verständlich.
- Zwei klar erkennbare Klang-/Bildsprachen: Asche, Glut, Ketten und tiefe Stimmen für die zugelassene Höllenmacht; weiß-blaues Licht, Gold und kurze sakrale Klangmotive für Gnade und Erlösung. Eine Stimme ist optional und nur an ruhigen Übergängen vorgesehen, mit Untertiteln und Überspringen.
- Vorhandenes Flackern/Himmel/Blut wiederverwenden und gemeinsame Effektobergrenzen festlegen. Reale Sichtbarkeit ist wichtiger als maximale Dunkelheit. Eine virtuelle Glutauflage ist keine echte Beleuchtung des Passthrough-Sofas.

### V21-B: Torwächter als erster Boss

- Ein großer Gegner verbleibt überwiegend hinter einem Portal. Kopf, Hände und ausgewählte Angriffe reichen durch die Öffnung. Eigene Blender-Qualitätsreferenz und klare Telegraphen vor Detailproduktion.
- Drei kurze Phasen: Bedrohung mit einzelnen Angriffen → wechselnde exponierte Schwachstellen → kurze finale Eskalation. Verwundbarkeit entsteht nach lesbaren Aktionen, nicht durch langes unbeschäftigtes Warten.
- Boss und normale Gegner teilen das Angriffsbudget aus V19. In kleinen Räumen keine zusätzliche Nahkämpfer-/Fledermausüberlastung während Bosskontakt.
- Platzprüfung verlangt passende Öffnung und sichere Sicht-/Angriffsachsen. Bei fehlendem Großportal kompakte Variante mit gleicher Gewinnmöglichkeit; falls auch diese unmöglich ist, nachvollziehbarer Ersatzabschluss mit normalen Rissen, kein blockiertes Finale.
- Revolver und vorhandene Versorgung reichen zum Sieg. Shotgun/Katana sind Vorteile, keine Voraussetzungen; Nahkampf reicht nicht durch die Portalwelt auf entfernte Schwachstellen.
- Der Torwächter besetzt die letzte Welle der Prüfung, nicht eine zusätzliche unangekündigte Welle 21/26. Sieg löst das Himmelsversprechen, Niederlage das Höllenurteil aus. Beide Enden sind virtuelle Rauminszenierungen mit sicherer Rückkehr zum Menü, keine erzwungenen Voll-VR-Wechsel.

Gate: kompletter Bossablauf in zwei Raumgrößen, keine Stereo-/Verdeckungsfehler oder untreffbaren Pflichtziele; Gewinnen, Sterben, Pause und Neustart in jeder Phase. Neue Art bleibt innerhalb der gemessenen Lastgrenzen.

## 6. V22 — Runde, Entscheidungen und Wiederspielwert

### V22-A: Die letzte Prüfung — fünf Akte

- `RunDirector` oberhalb des V19-Regisseurs: Vorbereitung/Prüfung annehmen → fünf Akte mit insgesamt zunächst 20, optional 25 Wellen → Urteil → Auswertung. Die letzte Welle enthält den Torwächter/zulässigen Ersatz. Wellenzahl und Aktprofile datengetrieben, keine an mehreren Stellen fest codierte 20/25.
- Kurze Zwischenphasen mit ausdrücklichem „Weiter“ und optionaler Auswahl; keine Rückkehr zum blockierenden Siegelprinzip innerhalb eines laufenden Kampfes. Der bestehende Wellenmodus bleibt während Integration als Rückfall erhalten.
- Endliche Gegner-/Nachschubquoten, klare Sieg-/Niederlagebedingungen und kontrollierte Behandlung fehlender Portalplätze. Keine scheinbar gewonnene leere Welle als Ersatz für nicht stattgefundene Kämpfe.
- Vorläufige Dramaturgie bei 20 Wellen: I **Das Urteil** (1–4, Regeln und erste Bedrohungen), II **Die Versuchung** (5–8, Werfer/Siegel und Zielwahl), III **Die Bedrängnis** (9–12, abgestimmte Boden-/Luftangriffe), IV **Der Abgrund** (13–16, stärkere Rollen-/Schwachstellenkombinationen), V **Die letzte Pforte** (17–20, zugespitzte Prüfung und Torwächter). Bei 25 Wellen werden fünf Wellen je Akt konfiguriert. Aktnamen sind Arbeitstitel.
- Schon vor V22 eingeführte Mechaniken bleiben verfügbar; Aktprofile bestimmen Häufigkeit und Einführungshinweise, keine künstlichen Waffenverluste. „Gegner-Massen“ entstehen durch Staffelung und kurze Nachschubfolgen innerhalb der Raum-/Leistungsbudgets, nicht durch unbeschränkt viele gleichzeitige Gegner.
- Keine starre Minutenvorgabe für die Hauptprüfung: fünf vollständige 20-Wellen-Durchläufe zuerst vermessen, dann 25-Wellen-Vergleich. Kriterien sind Spannung, Wiederholung, Komfort und Hilfshäufigkeit. Erweiterung auf 25 nur, wenn sie mehr als Laufzeit bringt.
- Lokaler Unterbrechungsspeicher an abgeschlossenen Wellen-/Aktgrenzen: Seed, Fortschritt, Inventare, Upgrades und verbrauchte Hilfen versioniert/atomar sichern, aber keine live platzierten Gegner, Projektile oder ungeprüften Weltkoordinaten wiederbeleben. Rückkehr über Raum laden/kalibrieren und gültige neue Platzierungen. Bei Abbruch mitten in einer Welle am letzten abgeschlossenen Stand fortsetzen; eine reguläre Niederlage beendet die Prüfung und entwertet diesen Fortsetzungsstand. Kein unbeabsichtigtes Rücksetzen der Perk-Verbrauchsgrenzen beim Laden.
- Finale Meldungen als Arbeitstexte: „Deine Prüfung ist bestanden. Der Himmel wird dich empfangen.“ beziehungsweise „Deine Prüfung ist gescheitert. Die Hölle erhebt Anspruch auf deine Seele.“ Keine Bildschirmverdeckung während noch spielrelevanter Angriffe; Urteil erst nach eindeutigem Zustandsabschluss.

### V22-B: Upgrades und Ergebnis

- Nach einem abgeschlossenen Abschnitt eine von drei Angeboten auswählen, insgesamt zunächst sechs Upgradetypen: Präzision/Schwachstelle, Abfangbelohnung, kurzzeitiger Schutz, Revolver-Nachladehilfe, Wurfstern-Erholung und Perk-Waffenbonus. Exakte Wirkungen/Kombinationsgrenzen separat als Daten konfigurieren und testen.
- Angebot, erworbene Wirkung und Restlaufzeit kompakt anzeigen. Keine Belohnung hinter Möbeln; bekannte Relikt-Fernaufnahme verwenden. Bestehende Lebens-/Munitionsrelikte nicht nochmals ersetzen.
- Verbesserungen als Segnungen zwischen Akten inszenieren; das ist von der akuten Gnadenwaffenhilfe aus V20 zu unterscheiden. Katana-/Shotgun-Verbesserungen austarieren, ohne die Rettungs-Shotgun heimlich auszutauschen. Keine stapelbaren Perks mit unbegrenzter Munition oder unendlichen Bonusketten.
- Ergebnis: Trefferquote, abgefangene Angriffe, verhinderter Nachschub, abgeschlossene Risse, Zeit und Schwierigkeitsstufe. Trefferquote für Revolverschüsse, Shotgun-Salven und Katana-Hiebe getrennt sinnvoll definieren; nicht 13 Pellets als 13 Fehlschüsse werten.
- Lokale Highscores versioniert nach Modus/Schwierigkeit; Diagnose-/Cheatläufe ausgeschlossen. Drei verständliche Schwierigkeitsprofile; keine heimliche Schadensanpassung. Kosmetische Freischaltungen optional, faire Runde ohne Grind.

### V22-C: Endlosmodus

Eigener Startpunkt, zunehmende Varianten statt unbegrenzt wachsender Gegnerzahl, Raum-/Angriffsbudgets bleiben. Geordneter Ausstieg mit Ergebnis und sauberem Neustart. Kein automatischer Übergang von abgeschlossener Rissjagd in Endlos.

Gate: fünf vollständige 20-Wellen-Prüfungen plus Vergleich der 25-Wellen-Auslegung und Endlos-Langzeittest; alle Upgrades einzeln und relevante Kombinationen, Notmunition, Perks, Pause und Reset. Sieg/Niederlage und Unterbrechen/Fortsetzen getrennt prüfen, auch nach Neustart an anderer Position. Kein dominanter Pflichtpfad und keine Fortschrittsblockade. Ergebnis-/Highscore-/Fortsetzungsdateien sind gegen alte oder beschädigte Konfiguration robust.

## 7. V23 — Komfort und spielbare Alpha

### V23-A: Einrichtung und Einstellungen

- Kurzes überspringbares Onboarding: Raum laden/neu scannen, vorhandene Kalibrierung verständlich führen, Schrein setzen, Hand wählen, Waffenpose justieren, Testschuss, Sterne/Pumpen/Katana freiwillig erproben.
- Ein Hauptschritt und ein kurzer Hinweis gleichzeitig; keine übereinanderliegenden Texttafeln. Bestehendes Raum-Menü und kompakte Textgestaltung erhalten.
- Erzählerisches Intro vom praktischen Setup trennen: erst sichere Einrichtung, dann freiwillige/überspringbare Einleitung. „Prüfung fortsetzen“ nur bei gültigem Unterbrechungsstand anzeigen; Story, verbleibende Wellen und Siegziel müssen ohne längere Erklärung verständlich sein.
- Vorhandene Waffen-/Trefferregler ergänzen: übrige Audio-Gruppen, Haptik, Effektdichte, Kontrast/Horrorintensität und geprüfte Grafikstufen. Lesbarkeit nicht allein über Farbe; gedrosseltes Flackern bis „aus“ möglich.
- Einhand-/Bewegungskomfort explizit behandeln: keine Pflicht zur Pumpe/Katana-Nahbewegung im Basisspiel; optionale Hilfen im Tutorial erklären und getrennt konfigurieren.

### V23-B: Stabilität und Übergabe

- Fünf Kaltstarts mit variierter Startpose, Raum laden/speichern, wiederholtes Home/Resume, Controller-/Trackingverlust, fehlende Berechtigungen und 30-Minuten-Sitzung in zwei Raumgrößen.
- Versionierte Einstellungen/Ergebnisdaten, abgesicherte Migration, Bereinigung von Effekten und verworfenen Aktionen. Raumdaten bleiben lokal; keine neue Cloud-/Kamerabildübertragung.
- Große Klassen nur entlang getesteter Grenzen weiter aufteilen: Raumabfrage, Begegnungs-/Rundendirektor, Waffenzustand, Darstellung und UI. Kein pauschales Refactoring parallel zu unbekannten Gameplayfehlern.
- Tutorial, Credits/Lizenzen, verständliche Fehlerwege, dokumentierte APK-/Buildversion, Prüfsumme und reproduzierbare Buildskripte. Entwicklungs-Signierung offen ausweisen; Store-Signierung/Veröffentlichung sind separate Aufträge.

Gate: nachvollziehbares Alpha-Testpaket ohne bekannte kritische Raum-, Fortschritts-, Eingabe- oder Datenverlustfehler. Sicht-/Hörqualität und echte Geräte-Frametimes getrennt von Editor-/Buildprüfungen dokumentieren.

## 8. Gemeinsame Architektur- und Testregeln

| Bestehender Bereich | Geplante Ergänzung | Grenze |
|---|---|---|
| `SpawnDistribution`, `PortalSurfaceFit`, `PortalTraversal`, `RoomNavigator`, Live-Raumabfragen | Raumsektoren, Kandidatenbewertung, erklärte Ablehnungen, gegnergerechte Wegprüfung | Keine zweite unabhängige Raumwahrheit |
| `QuestDemonGame.WaveLoop`, `PortalEncounter` | `EncounterDirector`, `AttackCoordinator`, später `RunDirector` | Quoten und Kampfzeit bleiben eindeutig besessen |
| `DemonAgent`, `CombatTiming`, `CombatSurface`, `DemonFireball` | Rollenparameter, oberflächengebundene Schwachstellen, Abfangereignisse | Animation, sichtbarer Kontakt und Schaden müssen übereinstimmen |
| `QuestGun`, `DivineShotgunState`, `HandRoles`, Sternvorrat | `PerkWeaponController`, `KatanaState`, begrenzte Klingen-Sweep-Abfrage | Nur ein aktiver Hauptwaffen-/Schadensbesitzer |
| Portal-, Himmel-, Blut- und Audioeffekte | Gemeinsame Lastgrenzen, Boss-/Katana-Varianten | Keine unbegrenzten Quellen, Decals oder Partikel |
| Raum-Menü, Schrein, lokale Einstellungen | Onboarding, Upgrade-/Ergebnisansichten, Migration | Ein UI-Kontext besitzt Eingaben; keine überlagerten Bestätigungen |

Neue Klassennamen sind Architekturvorschläge. Bei Umsetzung zunächst bestehende Grenzen prüfen, nicht allein für den Plan zusätzliche Abstraktionen anlegen.

**Geringe Vorbereitung für späteren Koop ab V19:** stabile IDs für Spieler/Begegnungen/Schadensereignisse, expliziter Besitzer von Quoten und Belohnungen, Darstellung getrennt von Zustandsentscheidungen sowie injizierbare Kampfzeit/Seeds. Nicht überall ungeprüft genau einen globalen Spieler als Ziel voraussetzen. Keine Netzwerkbibliothek oder Backendanbindung vor V24; vorhandene Einzelspielerfunktion darf durch diese Vorbereitung nicht unnötig umgebaut werden.

Jede Lieferung enthält:

1. Zustands-/Geometrie-/Lebenszyklustests mit deterministischen Fällen einschließlich negativer Fälle.
2. Bestehende vollständige Regression, speziell Scan/Laden, Portalübertritt, genaue Trefferflächen, Handrollen, Sterne, Shotgun und Pause.
3. Bei Artänderungen native Unity-Ansichten und gegebenenfalls reproduzierbare Blender-Quellen; keine Abnahme nur anhand eines Renderbilds.
4. APK-Metadaten, ARM64-Payload, Signatur und SHA-256; bisherige APK erhalten. Installation nur auf ausdrücklichen Auftrag und eindeutig identifiziertes Gerät, ohne Datenlöschung.
5. Kurze konkrete Headset-Testliste. Automatisch geprüft, auf Gerät installiert und tatsächlich getragen geprüft werden getrennt ausgewiesen.

## 9. V24 — Multiplayer / gemeinsame Bewährungsprobe

**Vom Nutzer ausdrücklich korrigiert:** Multiplayer mit **maximal vier Personen insgesamt**, also dem Spieler plus bis zu drei Mitspielern. Geplante Richtung ist kooperativ; kein PvP-Auftrag. Eigener späterer Meilenstein nach der stabilen Einzelspieler-Alpha, nicht mehr lediglich eine ausgeschlossene Idee. Vier Spieler sind das Ausbauziel, keine bereits belegte Leistungs-/Netzwerkzusage.

### V24-A: Modusentscheidung und Zwei-Geräte-Prototyp

- Zuerst festlegen: gemeinsame physische Spielfläche oder Online-Koop in getrennten Räumen. Beide Varianten benötigen unterschiedliche Raum-/Trefferregeln und sind nicht automatisch mit einer einzigen Koordinatentransformation lösbar. **Empfehlung für den ersten Prototyp: getrennte Räume**, weil vier gleichzeitig bewegte Personen im Wohnzimmer hohe Platzanforderungen haben. Die endgültige Wahl bleibt vor V24 offen; diese Empfehlung ist keine stillschweigende Festlegung durch den Nutzer.
- Getrennte Räume: gemeinsamer Prüfungs-/Wellenzustand, aber lokal gültige Portalplätze und Hindernisse. Zunächst explizit zugewiesene lokale Gegner und gemeinsame Ziele/Hilfen; kein behaupteter gemeinsamer physischer Gegnerpfad durch vier unterschiedliche Sofas. Ob und wie Spieler einander durch Portale sehen oder fremde Gegner treffen können, wird im Prototyp festgelegt und getestet, bevor dies zugesagt wird.
- Gemeinsamer Raum: geteilte Kalibrierung/Anker und fortlaufende Abgleichprüfung, räumliche Darstellung aller Mitspieler, individuelle Sicherheitsflächen und Ausschluss realer Personen aus Spawn-/Schlagbahnen. Kein Start, wenn Fläche/Tracking für die Gruppengröße ungeeignet sind; keine Umgehung von Grenzen. Katana zunächst in diesem Modus sperrbar, bis sichere Spielregeln und Abstände nachgewiesen sind.
- Für den gewählten Modus eine autoritative Zustandsquelle für Wellen, Lebenspunkte, Inventare, Treffer und Gnadenhilfe; Client-Anforderungen nicht blind als endgültige Kills übernehmen. Geometrievalidierung muss zur Raumzuordnung passen. Konkrete Netzwerk-/Relay-/Hostingtechnik erst dann anhand aktueller offizieller Dokumentation, Kosten, Plattformzugang und Prototypmessungen auswählen.
- Mit zwei tatsächlichen Headsets belegen: Lobby/Beitritt, Start, ein Portal, Schaden/Kill genau einmal, Wellenabschluss, Hilfe, Pause-/Trackingverlust, Verbindungsabbruch. Vor diesem Nachweis kein Ausbau aller Inhalte auf vier Spieler.

### V24-B: Gemeinsame Prüfung für zwei bis vier Spieler

- Narrativ mehrere Verurteilte in einer gemeinsamen Bewährungsprobe. Vorschlag: kurzzeitig niedergegangene Spieler können durch eine begrenzte, aus sicherer Position mögliche Hilfe zurückkehren; erst ein endgültiges Teamversagen führt zum gemeinsamen Höllenurteil. Diese Regel wird als Multiplayer-Variante erklärt und vor Umsetzung abgestimmt, nicht mit dem endgültigen Einzelspieler-Scheitern vermischt.
- Teamgröße und verfügbare lokale Fläche bestimmen Rollen/Quoten/Angriffsbudgets. Nicht schlicht Gegner-Lebenspunkte oder aktive Gegnerzahl vervierfachen. Gnadenhilfe berücksichtigt einzelne Bedrängnis und Teamdruck; keine vier gleichzeitigen großen Himmelseffekte im gemeinsamen Raum.
- Kein Friendly Fire im ersten Prototyp; das ist nur eine Spielregel, kein Schutz gegen reale Controllerkontakte. Ressourcenbesitz, Aufnahme-/Rettungskonflikte und Belohnungsverteilung explizit und gegen Doppelgewährung testen.
- Lobby mit verständlichem Bereitschaftsstatus; später Beitritt/Rückkehr an sicheren Wellenübergängen. Hostverlust zunächst sauber abbrechen/fortsetzbaren Stand anbieten, falls möglich; nahtlose Hostmigration ist eine separate Entscheidung, keine MVP-Zusage.
- Lokale Unterbrechung darf die anderen Spieler nicht unbemerkt weiter in unfaire Angriffe schicken. Team-Pause und kurzzeitiger Schutz für Tracking-/Verbindungsverlust begrenzen, Wiederholung nicht als unendliche Unverwundbarkeit zulassen.
- Sprachchat optional mit Mute/Push-to-talk und klarer Einwilligung; keine notwendige Voraussetzung für Spielverständnis. Kamera-/Raumnetzdaten standardmäßig nicht übertragen; notwendige geteilte Anker/abgeleitete Daten im gewählten Modus ausdrücklich erklären.

### V24-C: Abnahme

Gestuft zwei → drei → vier echte Clients; zuerst kurze Begegnung, dann vollständige Prüfung. Latenz, Paketverlust, Wiederverbindung, doppelte/verspätete Ereignisse, gleichzeitige Treffer/Perks, Hostverlust und Pause prüfen. Kein divergierender Wellenstand, doppelte Belohnung oder dauerhaft blockierter Teilnehmer. Mindestens ein 30-Minuten-Test mit vier Headsets und protokollierter Leistung je Gerät; im gemeinsamen Raum zusätzlich Drift-/Abstands-/Katana-Sicherheitsprüfung. Ohne passende Geräte/Fläche kann Simulation vorbereiten, aber nicht die Vier-Spieler-Abnahme ersetzen.

## 10. Bewusst außerhalb dieses Durchlaufs

Automatisches Mehrraum-Spiel wie im Laser-Tag-Vergleich, begehbare Portale/Voll-VR-Wechsel, Handtracking-Waffen, universelle Kletter-KI, Körperzerteilung, riesige Horden, Cloud-Saves und Store-Veröffentlichung. Diese Themen sind nicht gestrichen, brauchen aber eigene Machbarkeits-/Sicherheits-/Produktentscheidungen. Koop ist jetzt ausdrücklich in V24 aufgenommen. Kein Engine-/Render-Pipeline-Wechsel ohne belegten Nutzen.

## 11. Konkreter erster Umsetzungsauftrag

**V19-0 zuerst:** direkten Rundenneustart bei weiter gültigem Raum ermöglichen. Anschließend **V19-A:** hintere Bodenportalverteilung diagnostizieren, Kandidaten-/Austritts-/Wegprüfung gezielt verbessern und eine gesonderte Test-APK liefern. Danach V19-B Kampfregisseur, dann V19-C und Gesamtprüfung. Damit baut der Regisseur auf tatsächlich nutzbaren Raumregionen auf, statt das alte Platzierungsproblem nur anders zu verteilen.

Entscheidungen ohne Blockade des Starts: Katana-Auslöser, Ladungen/Laufzeit und finale Größe werden zunächst mit den oben markierten Startwerten prototypisiert und nach dem ersten getragenen Katana-Test abgestimmt. Ein kostenpflichtiges Asset oder eine wesentliche Erweiterung über diesen Umfang hinaus wird vorher separat vorgelegt.

Plotintegration erfolgt schrittweise: V19 Schnellneustart und Akt-/Druckschnittstellen, V20 Gnadenwaffen und erster Gegnerausbau, V21 weiteres Bestiarium/Bild-/Klangsprache und Urteil, V22 vollständige Prüfung, V23 verständliches Onboarding. Multiplayer-Vorbereitung bleibt klein; die Modusentscheidung und eigentliche Vernetzung für maximal vier Spieler warten bis V24. 20 versus 25 Wellen wird vor der finalen V22-Balance anhand der Durchläufe entschieden.
