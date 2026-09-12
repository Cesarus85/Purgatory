# Quest Demon MR – Roadmap nach V16

> **V19.20-Startkorrektur installiert und bestätigt:** Nach dem V19.19-Startabsturz wurden widersprüchliche Unity-Typdaten bereinigt und Startszene/Spieler neu gebaut. Nutzerstart und zwei kontrollierte Quest-Kaltstarts bestanden, vier Raumdateien erhalten. [Liefer- und Gerätenachweis](V19-20-DELIVERY.md). V19-D-Spiel-/Raum-/Balance-/Langzeitabnahme bleibt offen; der folgende V19-Implementierungsumfang bleibt erhalten.

> **Aktueller Gesamtplan (8. September 2026):** [Implementierungskonzept V19–V24](IMPLEMENTIERUNGSKONZEPT-V19-V23.md). **V19.19 implementiert Schnellneustart, hintere Raumverteilung/Feinnavigation, Kampfregisseur und Rollen/Präzision.** [Umsetzung und Geräteabnahme](V19-19-COMPLETION.md). V19-D bleibt bis zu aktuellen Quest-Raum-/Balance-/Leistungsmessungen offen. Danach: V20 Katana und Gegnervarianten/neue Typen; V21 Bestiarium, Inszenierung/Boss; V22 die vollständige Bewährungsprobe mit 20–25 Wellen, göttlichen Gnadenwaffen und Himmelsversprechen/Höllenurteil; V23 Alpha-Komfort; **V24 Koop mit maximal vier Spielern insgesamt**. Gemeinsamer/getrennter Raum wird vor dem Netzwerkprototyp entschieden. Diese späteren Inhalte sind geplant, noch nicht implementiert. Die folgenden älteren Statusmeldungen und „nächsten Schritte“ sind historische Zwischenstände.

Stand: 7. September 2026. **Zielplanung mit separat ausgewiesenem Implementierungsfortschritt.**

**V19.9 gebaut, Installation offen – Kaltstart bleibt Priorität:** V19.8 durch Nutzer als weiterhin verschoben gemeldet; tatsächlicher Log-Mitschnitt belegt zu schwache Freigabe. Nativer expliziter Trackingraum-Ankerpfad, Dreiflächenprüfung und sitzungsübergreifende Diagnose ersetzt/ergänzt. 2.483 Checks und APK bestanden, aber kein weiterer Hauptblock vor zwei getragenen Kaltstart-Ladeversuchen. [Befund/Abnahme](IMPLEMENTATION-V19-9-COLD-START.md), [Status](BUILD-REPORT.md).

**V19.8 Raum-Zuverlässigkeit – installiert, physische Abnahme offen:** Eigener Kartenrahmen statt Kamera-Rig-Verschiebung, stabile Meta-Ankerpose, getrennte abbrechbare Tiefenprüfung ohne zweiten Scan, ausdrückliche Freigabe und Wiederverwendung der gespeicherten Karte. Sauberer Rückweg bei Recenter, Fokus-/Ankerverlust und Ladefehlern. 2.457 native CHECK-Meldungen plus fokussierter Wiederholungslauf, APK-/Geräteversion geprüft; vorhandene Raumdatei unverändert. Nicht automatisch gestartet. Nächster Gate ist Save/Quit/Load mit veränderter Startpose im Headset, nicht ein neuer Gameplay-Block. [Umfang und Testfolge](IMPLEMENTATION-V19-8-ROOM-RELIABILITY.md), [Liefernachweis](BUILD-REPORT.md). Ältere Stände folgen.

**V19.7 Purgatory – gebaut, Installation offen:** Neuer sichtbarer Spielname, klarer Startbildschirm und direkter Raum-Speichern-Knopf am Schrein. Exklusive Einrichtungstexte, ausdrückliche Rückfragen statt fummeliger Halte-Auswahl, früherer Schwellenhub für Bodensprünge. 2.287 vollständige native CHECK-Meldungen plus acht Profilchecks, Menübilder und APK geprüft. Quest nur gelesen: V19.6, keine Raumprofildatei im konfigurierten App-Dateiverzeichnis; Hardware-Speicher-/Ladeabnahme offen. [Umsetzung und Bedienung](IMPLEMENTATION-V19-7-SETUP.md), [Lieferstatus](BUILD-REPORT.md). Hauptplan unverändert, ältere Stände historisch.

**Nutzerkorrektur V19.6 gebaut, nicht installiert:** Klare Raumwahl ohne Hintergrundscan, überprüftes Speichern und vollständige goldene Referenzkarte; aktuelle Sicherheitsprüfung bleibt. Sichtbare Landung/Rückkehr statt sofortigem Abbruch von Portalsprüngen. Handnaher Sternvorrat, freie Grip-Aufnahme und leichte Würfe. 2.234 native CHECK-Meldungen, drei UI-/Shaderansichten und APK geprüft; Quest nicht verbunden. [Bedienung und getragene Abnahme](REVIEW-V19-6.md), [Lieferstatus](BUILD-REPORT.md). Hauptplan unverändert; nachfolgende Stände historisch.

**Zusatzblock V19.5 gebaut, noch nicht installiert – Raumprofile:** Fünf lokale Speicherplätze für eigenen Live-Scan plus Meta-Anker, letzte/andere Karte laden, neu erfassen, aktualisieren und wiederherstellbar entfernen. Aktueller Tiefenabgleich und lokale Freiraumprüfung vor automatischer Freigabe; gespeicherte Schreinpose nur nach aktueller Auflagenprüfung. 2.215 vollständige native CHECK-Meldungen plus acht gezielte Zusatzprüfungen; APK/Signatur/Anchor-Berechtigung geprüft. Meta-Anker-Hardwareabnahme und Performance offen, Quest nicht verbunden. [Umfang/Bedienung/Datengrenzen](IMPLEMENTATION-ROOM-PROFILES.md), [Lieferstatus](BUILD-REPORT.md). Hauptplan unverändert.

**Zusatzblock V19.4 gebaut, noch nicht installiert – Waffenhand:** Gespeicherte Links-/Rechtswahl vor dem ersten Scan, späterer Wechsel am Pause-Schrein. Handrollen für Waffe, Sterne, Zweihandstütze, Haptik, Scan, Platzierung, Diagnose und Hinweise. Raumkarte, Munition und Regeneration bleiben beim Wechsel erhalten; neutrale Eingaben vor Wiederfreigabe. 2.191 native CHECK-Meldungen einschließlich 30 Handrollen-Prüfungen, Release-Metadaten und APK-v2-Signatur bestätigt. Quest nicht verbunden; physische Abnahme offen. [Bedienung und Grenzen](IMPLEMENTATION-HAND-ROLES.md), [Lieferstatus](BUILD-REPORT.md). Hauptplan unverändert.

**Zusatzblock V19.3 gebaut, noch nicht installiert – Wurfsterne:** Geliefertes Blender-Modell übernommen, drei Sterne an geschätzter linker Hüfte, Grip-Aufnahme und bewegungsbasierter Wurf beim Loslassen. Gemeinsame Regeneration nach 150 Sekunden ab letztem Wurf, echte Monsteroberflächen-/Raumkontakte, Pause-/Tracking-/Reset-Schutz. 2.160 native CHECK-Meldungen, darunter 51 Wurfstern-Checks; Android-Metadaten und APK-v2-Signatur geprüft. Quest aktuell nicht per ADB verbunden. [Zusatzplan und Bedienung](IMPLEMENTATION-THROWING-STARS.md), [Lieferstatus](BUILD-REPORT.md). Hüftposition, Wurfgefühl und Performance müssen getragen geprüft werden; übriger Hauptplan unverändert.

**V19.2 installiert – Siegelkorrektur:** Eigenständiger Zähler geeigneter Nachschubportale beseitigt den unbeabsichtigten Siegel-Ausfall in Wellen 3–5. Erstes geeignetes Portal je Welle, danach jedes zweite; Raumprüfung und Quoten unverändert. Kleiner Drei-Sekunden-Countdown, auslaufende Glut und getrennte Meldungen für Zeitablauf, durchgelassenen und verhinderten Nachschub. 2.106 native CHECK-Meldungen bestanden. [Review](REVIEW-V19-2.md), [Lieferstatus](BUILD-REPORT.md). Physische Abnahme offen; übriger Hauptplan unverändert. Ältere Einträge dokumentieren historische Stände.

**V19.1 installiert – Korrektur nach Nutzerfeedback:** Pflichtpause durch optionale zeitlich begrenzte Nachschub-Siegel während laufender Kämpfe ersetzt; endliche Quoten und maximal zwei parallele Portale. Neues originales Blender-Kiefer-/Lidrig, additive Blick-/Atem-/Mimikbewegungen; Fledermaus-Gleiten, Kopfstabilisierung, Kurvenflügel und nachführende Beine aus bestehendem Rig. 1.936 native CHECK-Meldungen bestanden. [Review und physische Abnahmegrenzen](REVIEW-V19-1.md), [Lieferstatus](BUILD-REPORT.md). Kein weiterer Hauptblock begonnen; ältere Einträge unten sind historische Stände, insbesondere das Pflicht-Siegelgate von V19.0 gilt nicht mehr.

**V19.0 implementiert, nativ geprüft:** A10-Art um originale Blender-Kathedrale und ortsbezogene Bodenrahmen ergänzt. Erster V19-Spielblock: begrenzte Gegnergruppen, anschließend zwei bzw. drei beschießbare Siegel; einmalige Siegelmunition, Abschluss erst nach Versiegelung. 1.737 CHECK-Meldungen bestanden, darunter 181 neue Siegelprüfungen. Verbindlicher Lieferstatus im [Buildbericht](BUILD-REPORT.md), [Umfang und offene Abnahme](REVIEW-V19-0.md). A10-Headset-Qualitätsgate und übriger V19-Ausbau bleiben offen. Folgende Einträge sind historische Stände.

**V18.20 installiert – Scan-Komfortkorrektur:** Isolierte fehlende Freiraumwerte und kleine rundum belegte Boden-/Wand-Netzlücken werden begrenzt für Abfragen ergänzt. Keine Rückschreibung oder rekursive Freigabe; größere Unbekanntbereiche und Hindernisse bleiben gesperrt. Konkrete Einrichtungs-/Portalhinweise, X weiterhin zwei Sekunden. 1.550 native CHECK-Meldungen, Signatur sowie installierte Version/Hash bestätigt, Daten erhalten, nicht gestartet. [V18.20](REVIEW-V18-20.md). Nutzerraum-/Performanceabnahme und übrige A10-Art noch offen. Folgende Einträge historisch.

**V18.19 installiert – Nutzerkorrektur:** Zwei Sekunden X ohne Loslass-Geste und mit Haltefortschritt; Blender-Kniehebung mit schwellenfreien Krallen; senkrechter Kopfvoran-Sturz aus Deckenportalen mit geprüftem Ausleiten statt Längsrolle. 1.517 native CHECK-Meldungen, APK-Signatur und installierte Version/Hash bestätigt, Daten erhalten, kein automatischer Start. Vollständige V18.18-Raumerfassung vom Nutzer positiv bestätigt, Messung/Streaming unverändert. [V18.19](REVIEW-V18-19.md). Getragene V18.19-Abnahme sowie übrige Kathedrale/ortsbezogene Rahmen bleiben offen. Folgende Einträge sind historische Stände.

**V18.18 installiert – Nutzerkorrektur:** Echter angepasster Schritt über den erhöhten Portalrand, unmögliche Fledermaus-Auswahlkombination durch unabhängigen Varianten-Zähler ersetzt, bewusster Einrichtungsabschluss per X-Halten nach verbundenem Mindestscan statt automatischem Wechsel. Erfassung läuft weiter; keine Lockerung unbekannter Raumteile. 1.465 native CHECK-Meldungen, Signatur sowie installierte Version/Hash bestätigt; Daten erhalten, nicht automatisch gestartet. [V18.18](REVIEW-V18-18.md). Getragene Qualität/Performance noch offen. Kathedrale und ortsspezifische Bodenrahmen weiterhin nicht erledigt; folgende Einträge historisch.

**V18.17 – Nutzerkorrektur nach A10b-Teil:** Bei Diagnose lag noch V18.15 auf der Quest. Neue Lieferung vereint V18.16 mit fortgesetzter speicherbegrenzter Erfassung (768 CPU-/128 GPU-Blöcke, geschützte Regionen), eigenem vertikalem Blender-Fledermausschacht, unabhängig alternierenden Boden-Kulissen und schnelleren/flexibleren Austritten. 1.405 native CHECK-Meldungen, davon 32 neue Expansion-Prüfungen. [Umfang und physische Testgates](REVIEW-V18-17.md); verbindlicher APK-/Installationsnachweis im [Buildbericht](BUILD-REPORT.md). Keine behauptete physische Scan-/Performanceabnahme. Kathedrale und spezifische Bodenrahmen bleiben offen; folgende Einträge dokumentieren Vorgängerstände.

**Aktuell gebaut – V18.16 / A10b teilweise:** Nach positivem V18.15-Nutzerfeedback zweiter Blender-Ort (gebrochene Lavabrücke), abwechselnd mit Schmiede. Auf neuen Wunsch geprüfter Portal-/Angriffssprung und kopfüber beschleunigter Fledermausaustritt mit Rumpfrolle; normale Auftritte bleiben. 1.371 native CHECK-Meldungen, APK-Metadaten/Signatur bestätigt. Noch nicht installiert oder physisch abgenommen. [V18.16](REVIEW-V18-16.md). Offen in A10b: Kathedralen-Seitenhalle, ortsspezifische Rahmen und gemeinsame Qualitäts-/Leistungsabnahme. Die folgenden Einträge sind frühere Lieferstände.

**Neu installiert – A10a / V18.15:** Erste Blender-Schmiede, kontinuierlicher animierter Portalübertritt mit gelegentlichem Umschauen, unabhängige Lebensversorgung und bevorzugte Bodenauflagen. Auf Ruckel-Nachtrag Start in Vorbereitung/Scan/Schreinplatzierung getrennt, Scanarbeit begrenzt und Messpunkte ergänzt. 1.291 native CHECK-Meldungen, APK-Metadaten/Signatur bestätigt; tatsächliche Headset-Performance nicht gemessen. Am 6. September um 16:05:48 installiert, Geräteversion/Hash bestätigt, Daten/Berechtigungen erhalten; nicht automatisch gestartet. [A10a](REVIEW-V18-15.md), [Plan und Qualitätsgate](A10-IMPLEMENTATION.md). Danach A10b: Brücke/Kathedralen-Seitenhalle plus ortsspezifische Rahmen; nicht vor der Referenzabnahme als abgeschlossen behandeln.

**Vorher geliefert – A9 / V18.14 installiert:** Zwei originale Blender-Relikte, geformte Glut-/Seeleneffekte, passende Aufnahmegeräusche/Haptik und tatsächlich gewährte Mengen. Volle Ressourcen verbrauchen kein Relikt; direkte Fernaufnahme ohne Patrone, auch während Nachladen. Echte Auflagefläche und vollständige bewegte Hülle gegen Möbel geprüft. 1.217 native CHECK-Meldungen, Signatur und Gerätehash bestätigt; getragene Abnahme offen. [A9-Umsetzung](REVIEW-V18-14.md). A8-Schrein enthalten, hintere Bodenportalverteilung weiterhin zurückgestellt. Nächster Block **A10 Portal-Schauplätze**, zunächst eine Qualitätsreferenz.

**Neueste Nutzerkorrektur – V18.12 installiert:** Hintere Bodenportale ab Welle 3 zunehmend bevorzugen, schmale Gänge gezielter bis 7,5 m durchsuchen und unabhängig passende kleinere Austritte prüfen. Kompakter 1,06-m-Rahmen nur für den schlanken Gegner; Hindernisse und unbekannte Bereiche bleiben gesperrt, sichere Ausweichplätze erlaubt. Zwei Sekunden räumlicher Vorlauf hinten. 1.074 native CHECK-Meldungen, signiertes APK und Gerätehash bestätigt; tatsächlicher Nutzerraum und Such-Frametimes noch nicht abgenommen. [V18.12](REVIEW-V18-12.md). Danach weiterhin A8 → A9 → A10.

**Vorgezogener Nutzerauftrag nach V18.3:** Vor dem nächsten Hauptpunkt den [Qualitätssprint Bewegung, Kampf und Klang](IMPLEMENTATION-COMBAT-POLISH.md) A1–A10 einschließlich A6b umsetzen. A1/A2 (V18.4) und A3 (V18.5 Feuer/Plasma) wurden vom Nutzer positiv bestätigt. A4 (V18.6) ersetzt auf zusätzlichen Nutzerwunsch die Pistole durch einen urigen Blender-Revolver mit Laufrauch und passender Mechanik. A5 (V18.7) liefert Revolver-/Treffer-/Mechanikklänge; auf jüngsten Nutzerwunsch wurde auch die 10-prozentige Größenkorrektur von A6 in A5 vorgezogen. Installiert und nativ geprüft, getragene Hör-/Griffabnahme offen. A6 (V18.8) ist implementiert: lautere Mischung, Gegnerstimmen, kontaktgebundene Schritte/Flügel und korrekter Portalaustritt; 748 native Prüfungen bestanden, auf Quest installiert; Schusslautstärke und Klangzuordnung laut neuer Nutzerrückmeldung weiterhin unzureichend. Neue Reihenfolge: **A6b Schuss-/Trefferkorrektur → A7 Dynamik → A8 platzierbarer Schrein → A9 Relikte/Aufnahmeeffekte → A10 unterschiedliche Schauplätze derselben Dämonenwelt**. Inzwischen sind A8/V18.13 und A9/V18.14 implementiert und installiert; A10 bleibt geplant. Diese Blöcke ziehen begrenzte Art-/Komfortanteile aus V20/V22/V23 vor. Das ersetzt keine vollständige Abnahme der Hauptroadmap.

**Aktuelle Priorität auf Nutzerwunsch:** Weitere V17-Gerätetests bleiben zurückgestellt. V18.1 behebt den realen Startabbruch von V18.0; Live-Tiefendaten, Oberflächen und Boden wurden im Geräteprotokoll bestätigt, der Nutzer meldet das Spiel als funktionierend. Laser Tags unmittelbare Erfassung und Raumwechsel bleiben unerreichte Vergleichspunkte; Scan-Architektur vorerst unverändert lassen. V18.2 überarbeitet nun Portalgrößen, Platzprüfung und den Blender-Rahmen ([Details](REVIEW-V18-2.md)). Die übrigen V18-Punkte unten sind nicht automatisch abgeschlossen.

Fortschritt: Der Nutzer hat V16 als abgeschlossen bestätigt. V17.1 ist installiert; drei tatsächliche Quest-Läufe sind gesichert. Exakte Teilhaut-Berechnung für Wunden reduziert im Vorher-/Nachher-Test Vollhaut-Bakes von 1705 auf 90 bei unveränderter Zahl exakter Dreieckstests; beide Nicht-Development-Läufe liegen mit zwei sichtbaren Portalen und drei Gegnern im Mittel bei rund 72 Hz. Alle 160 nativen Unity-Regressionstests und fünf Auswertungstests sind bestanden. [Messbefunde und Grenzen](V17-FIRST-DEVICE-PROFILE.md), [APK-/Installationsnachweise](BUILD-REPORT.md). Erzeugungsspitzen, zusätzliche Raum-/Abbruchtests und Langzeittest bleiben offen. Details: [REVIEW-V17.md](REVIEW-V17.md). V17 ist deshalb noch nicht vollständig abgenommen; die folgenden Stufen bleiben Zielplanung.

## Leitidee und Ausgangslage

**V17.2-Nachtrag:** Vorladen vor dem Startpult und gestaffelte Testgegner-Erzeugung implementiert, mit 179 nativen Prüfungen gebaut und installiert. Erster 60-Sekunden-Quest-Test vollständig: erstes Portal maximal 19,469 ms, Gegnerphase 18,219 ms, Kampfmittel 13,886 ms. Allerdings nur zwei statt drei sicher platzierbare Gegner; kein gleich belasteter A/B-Nachweis. Nächster Prüfschritt: Kaltstart mit mitlaufender Protokollierung und drei sicheren Gegnerplätzen, danach Pause/Fokus/Abbruch und Langzeitlauf. [Prüfbericht](REVIEW-V17-2.md).

Aus dem MR-Wellenprototyp wird eine **Rissjagd im eigenen Zimmer**: Eine 8–12-minütige Runde verbindet bedrohliche Portalauftritte, taktisch unterscheidbare Gegner, befriedigende Waffen und kleine Entscheidungen. Der reale Raum beeinflusst die Begegnungen, ohne den Spieler zu riskanten Bewegungen zu drängen.

Dokumentierte Basis: V16 ist gebaut, signaturgeprüft und auf Quest 3 installiert; 67 Editor-Assertions bestanden. Nach dem zunächst dokumentierten Controller-Systemdialog hat der Nutzer V16 als abgeschlossen bestätigt. Ein detailliertes Performance-/Thermikprofil liegt damit noch nicht vor. Die Roadmap beruht auf Quellcode, Berichten und dieser Nutzerabnahme, nicht auf einer selbst durchgeführten getragenen Hardwareprüfung.

Schon vorhanden, deshalb nicht als neue Features verkaufen: freie physische Bewegung im gescannten Raum, MRUK plus Live-Tiefe, Boden- und Fluggegner, Feuerbälle, Nachladen, Notmunition, Pickups, Pause, räumliche Sounds, animierte Modelle und pro Auge gerenderte 3D-Portale. Die vier Portalvarianten verwenden aktuell dieselbe Grundlandschaft mit anderen Blickwinkeln und Farbwelten.

Die Versionsnummern unten sind Vorschläge für in sich testbare Ausbaustufen, keine Terminzusagen. Aufwand: **S** = begrenztes Arbeitspaket; **M** = mehrere Implementierungs-/Testschleifen; **L** = eigener Meilenstein mit Art- oder Systemarbeit. Raum- und Headsettests brauchen Mitarbeit des Spielers.

## Priorisierte Reihenfolge

| Stufe | Schwerpunkt | Sichtbares Ergebnis | Aufwand | Voraussetzung |
| --- | --- | --- | --- | --- |
| V17 · P0 | Belastbare Basis | Verlässlicher V16-Spieltest, messbare Bildrate, nachvollziehbare Raumfehler | M | V16 testen |
| V18 · P0 | Raum und Bewegung | Gegner umgehen Möbel und reagieren glaubhaft auf Engstellen | L | V17 |
| V19 · P1 | Kampf und Begegnungen | Portal schließen statt nur eine Schlange Gegner abarbeiten | M–L | V18 |
| V20 · P1 | Grafik-Qualitätsschnitt | Ein überzeugendes Waffen-/Gegnerensemble und verschiedene Schauplätze derselben Dämonenwelt (Art-Teil vorgezogen in A10) | L | Leistungsbudget V17; Kampfzustände V19 |
| V21 · P1 | MR-Inszenierung und Boss | Die Hölle greift sichtbar in den realen Raum hinein | L | V18–V20 |
| V22 · P2 | Runde und Wiederspielwert | 8–12 Minuten mit Auswahl, Höhepunkt und Abschluss | M–L | V19, V21 |
| V23 · P2 | Spielbare Alpha | Komfort, Einstellungen, Onboarding und robuste Testpakete | M | Vorherige Gates bestanden |

Art-Recherche und ein einzelner Qualitätsprototyp aus V20 können bereits nach V17 beginnen. Der Ausbau auf mehrere Assets erfolgt erst nach Freigabe dieses Beispiels. Kein großes Assetpaket einkaufen, bevor Stil, Import und Leistung geprüft sind.

## V17 – Basis messen und absichern

**Priorität: Fehler sichtbar machen, bevor weitere Systeme darauf aufbauen.**

- Getragener V16-Test: Waffenpose, beide Portalaugen, seitliches Hineinlehnen, Deckenportal, Fledermaus über Sofa/freiem Boden, schnelle Schussfolgen und Pause/Resume.
- Separater Diagnosemodus zeigt Raumanker, echte Kollisionsflächen, Live-Tiefentreffer, unbekannte Bereiche, Flugkandidaten und Blockadeursache. Im normalen Spiel vollständig aus.
- Lokales Ereignisprotokoll: Spawnentscheidung, blockierter Weg, Recovery, Landung, Tod; reproduzierbare Zufalls-Seeds. Standardmäßig keine Kamerabilder oder Raumdaten in eine Cloud hochladen.
- Quest-Profiling in Debug und einem separaten nicht-development Testbuild: CPU, GPU, verpasste Frames, Speicher, Garbage Collection, Wärme; 0/1/2 sichtbare Portale, mehrere Gegner, gleichzeitige Effekte.
- Gemessene Engpässe bearbeiten: Portalauflösung nach sichtbarer Größe mit stabilen Qualitätsstufen; Effekte/Projektile wiederverwenden; Raumabfragen zeitlich verteilen; Mesh-Treffer zunächst grob eingrenzen, dann exakt prüfen. Die Oberfläche darf dabei nicht wieder von Trefferpunkten abweichen.
- Unbenutzte Ressourcen, doppelte Imports und SDK-Beispielshader-Warnungen getrennt bereinigen; funktionsfähigen Rückfall-Build erhalten.

**Gate:** Drei Spieltests in zwei verschieden eingerichteten Räumen; mindestens ein 20-Minuten-Lauf. Primäres Projektziel: stabile 72 Hz, etwa 13,9 ms Framebudget, mit messbarer Reserve und ohne wiederkehrende Frametime-Spitzen. CPU-/GPU-Zeit nicht einfach addieren. 90 Hz erst als optionale Qualitätsstufe, wenn Messwerte das erlauben. Keine Freigabe allein aufgrund eines hohen FPS-Mittelwerts.

Metas Werkzeuge dafür: [OVR Metrics Tool](https://developers.meta.com/horizon/documentation/unity/ts-ovrmetricstool/) und [Testing and performance analysis](https://developers.meta.com/horizon/documentation/unity/unity-perf/).

## V18 – Ein gemeinsames Raummodell, bessere Bewegung

**Nutzen:** Möbel und verlorene Sichtdaten erzeugen keine leicht abschießbaren Standbilder mehr.

- Raumabfragen in einen gemeinsamen Dienst überführen: `frei`, `blockiert`, `unbekannt`, jeweils mit Quelle und Alter. Spawn, Bodenbewegung, Flug und Landung sollen dieselben Begriffe benutzen. Die übrigen `CheckBox`-Verwendungen in `LiveRoomGrid` und Boden-Recovery gezielt auf diese Semantik prüfen; nicht pauschal jeden Aufruf entfernen.
- Stabile Scene-Geometrie und kurzlebige Live-Beobachtungen getrennt halten; widersprüchliche Einzelmessungen zeitlich glätten. Ein verdeckter Bereich ist weder eine bewiesene Wand noch bewiesen frei.
- Boden: erreichbare Ziele im selben zusammenhängenden Laufbereich, Engstellen-Reservierungen und weniger gegenseitiges Drängeln. Bestehendes Grid zuerst messen; NavMesh nur bei nachgewiesenem Vorteil in einem Vergleichsprototyp.
- Flug: kleine lokale 3D-Wegsuche um Möbel mit Körperabstand, geglättetem Kurs und stabiler Ausweichentscheidung. Ein Boden-NavMesh allein löst keinen Flugweg.
- Recovery sichtbar plausibel: abbremsen, wenden, Höhe ändern oder neu ansetzen. Kein Teleport zum Spieler und kein Durchschieben durch eine Wand. Ohne sicheren Weg Begegnung kontrolliert abbrechen.
- Animation folgt tatsächlicher Geschwindigkeit: Anlauf, Bremsen, Kurvenneigung, Angriff und Trefferreaktion ohne ständiges Neustarten derselben Clips. Für Sofa-Abstieg zuerst eine geprüfte, kurze Übergangsanimation mit validiertem Landepunkt statt universelles Klettern auf beliebiger Geometrie.

**Gate:** Testparcours mit Sofa, Tisch, enger Ecke und zeitweise verdecktem Weg. Pro Raum mindestens 30 Anflüge und 30 Bodenannäherungen; kein sichtbares Durchdringen oder Teleportieren. Blockade über zwei Sekunden löst eine protokollierte, kontrollierte Reaktion aus. Unlösbare Platzierungen werden übersprungen. Dynamisches Umstellen realer Möbel bleibt ein Grund für Pause und erneute Raumprüfung.

## V19 – Kampf mit Entscheidungen

**Nutzen:** Nicht nur schneller schießen, sondern Ziele priorisieren.

**V19.0-Teilstand:** Portalversiegelung einschließlich begrenztem Nachschub ist implementiert und nativ geprüft; [Details](REVIEW-V19-0.md). Danach Begegnungsrhythmus/klare Gegnerrollen, anschließend Abfangen/Schwachstellen und Trefferfeedback. Das Drei-Personen-Gate unten steht weiter aus.

- Begegnungssteuerung statt bloß steigender Stückzahlen: kurze Anspannung, Angriff, Entlastung. Belastung berücksichtigt freien Raum, aktive Angriffe und sichtbare Bedrohungen. Keine heimliche Anpassung von Gegnertrefferpunkten während des Schusses.
- Vorhandene Rollen stärker trennen: Nahkämpfer bindet, Feuerballwerfer hält Abstand, Fledermaus kündigt einen schnellen Angriff an, schwerer Gegner zeigt eine präzise angreifbare Schwachstelle.
- Feuerbälle sind bereits vorhanden: Aufladegeste, räumliche Warnung, Flugbahn und Abschussfeedback verbessern. Timing-Bonus für sauberes Abfangen; keine Pflicht zum körperlichen Ausweichen in Richtung Möbel.
- **Portalversiegelung als neues Rundenziel:** Gegner treten nur begrenzt nach; nach einer klaren Angriffsphase werden zwei bis drei Siegel am Portal verwundbar. Wer sie zerstört, schließt den Riss und beendet die Begegnung. Alle Ziele müssen von einer sicheren Position erreichbar sein.
- Trefferfeedback vereinheitlichen: Fleisch, Panzerung, Schwachstelle und Kill sind an Ton, kurzer Materialreaktion und Haptik unterscheidbar. Kein Kamerawackeln in MR.
- Verständliche Munitionsökonomie: lesbare Anzeige, hörbare Reservewarnung, klar erkennbare Pickup-Wirkung; bestehende Notmunition bleibt als Schutz vor einer ausweglosen Situation.

**Gate:** Drei Testpersonen verstehen ohne mündliche Erklärung, welches Ziel gefährlich ist und wie ein Portal geschlossen wird. Ein Durchgang muss auch ohne Raumwechsel oder hektische große Ausweichschritte spielbar sein. Keine unvermeidbare Kombination mehrerer gleichzeitiger Angriffe.

## V20 – Ein verbindlicher Grafik-Qualitätsschnitt

**Art-Richtung:** düsteres, materiell glaubhaftes, bewusst stilisiertes Horror-Fantasy. Zusammengehörige Formen, Metall, Haut, Knochen und Glut. Keine gemischte Sammlung beliebiger Gratis-Assets; kein Versprechen von PCVR-Photorealismus auf Standalone-Quest.

### Verschiedene Orte derselben Dämonenwelt – Art-Teil vorgezogen in A10

1. **Schmiede-Innenhof:** nahe Werkarchitektur, Kettenzüge, Glut und ferne Schmiedewerke.
2. **Zerbrochener Brückenzugang:** beschädigte Schwelle, Steg über dem Abgrund und Blick auf dieselbe Zitadelle.
3. **Kathedralen-Seitenhalle:** Rippengewölbe, Ritualnischen, tiefe Sichtachse und bewegte Seelenlichter.

Auf neue Nutzeranweisung keine getrennten Biome: gemeinsame Materialien, Architekturmerkmale und Landmarken verbinden diese Orte. Jeder erhält eigene Geometrie und eine lesbare Nah-, Mittel- und Fernebene; bloßer Farbwechsel oder anderer Blickwinkel reicht nicht. Zunächst eine Referenz in A10 abnehmen, danach zwei weitere Schauplätze ausbauen. Varianten und zugehörige Rahmen ohne unmittelbare Wiederholung auswählen, bei Bedarf auch für schmale Wand- und Deckenöffnungen. Den bestehenden Stereo-Renderer zunächst behalten; eine alternative direkte/stencil-basierte Portaltechnik nur dann prototypisieren, wenn ein konkret gemessenes Problem mit Stereo, Schärfe oder Leistung damit lösbar ist. V20 integriert und prüft die in A10 geleistete Arbeit, statt dieselben Assets erneut zu beauftragen.

### Modelle und Animation

- Den in A4/A5 eingeführten Ashwarden-Revolver als Waffenreferenz weiterführen: Griff-/Controllerbezug, Materialtrennung und funktionale Hahn-/Trommelmechanik. Schuss-/Trefferabstimmung wird in A6b nachgebessert, nicht auf die alte Pistole zurückgebaut. Kein zweites vollständiges Waffenarsenal im selben Schritt.
- Ein Bodenmonster und eine Fledermaus als Qualitätsreferenz: gute Silhouette, Gelenkdeformation, Gewichtsverlagerung, Flügel-/Handbewegung, verschiedene Treffer- und Todesreaktionen.
- Blender für Silhouetten, Retopologie, UVs, Rig-Anpassung, Animationsübergänge und Baking. Externe Assets dort, wo ein geprüftes Modell oder ein guter Animationssatz deutlich mehr bringt. Vor Einsatz Lizenz, Herkunft, Rig, Deformation, Quest-Import und Änderungsrechte dokumentieren; kostenpflichtige Käufe separat freigeben lassen.
- Statt flächigem Leuchten: modellierte Tiefe, Normal-/AO-Details, gezielte Emission. Für die abgeschlossene virtuelle Portalwelt gebackene Beleuchtung/Reflexionen als Performance-Option prüfen.

**Gate:** Ein vollständiger Qualitätsausschnitt – Waffe, beide Gegner, Portalreferenz, Schrein und Relikte – wird am getragenen Headset abgenommen. Danach die weiteren Portal-Schauplätze im gleichen Stil prüfen. Vergleich bei gleichem Abstand und Licht, nicht nur schöne Blender-Bilder. A8/A9/A10 liefern die vorgezogenen Art-Anteile; das V17-Leistungsbudget bleibt verbindlich.

## V21 – Der Raum wird Teil der Inszenierung

**Die drei stärksten Show-Momente:**

- Ein Riss zeichnet sich hörbar an einer echten Wand ab, bricht mit Tiefe auf, Ketten bewegen sich und dahinter setzt eine ferne Kreatur zum Angriff an.
- Die Fledermaus kündigt sich an der Decke an, löst sich mit zusammengelegten Flügeln aus dem Portal und entfaltet sie im freien Raum.
- Ein großer **Torwächter bleibt überwiegend hinter dem Portal**: Kopf, Hand und Angriffe kommen durch die Öffnung. Das erzeugt Größenwirkung, ohne einen riesigen Gegner durch das Wohnzimmer navigieren zu müssen.

Ergänzungen: oberflächengebundene Risse/Asche, kurze Kontaktwolken bei Landungen, sichtbare Portal-Schließreaktion, geschichtetes räumliches Ambiente und dezent ansteigende Musik. Effekte dürfen reale Hindernisse und den sicheren Bodenbereich nicht großflächig verdecken.

Wichtig: Ein Unity-Licht beleuchtet nicht physisch das im Passthrough sichtbare Sofa. „Glutschein auf der Wand“ wäre eine kontrollierte virtuelle Auflage auf erkannter Geometrie, keine echte Lichtänderung des Kamerabildes. Dynamische Verdeckung ist ebenfalls nicht pixelperfekt; Meta beschreibt diese Grenzen ausdrücklich in der [Occlusions Overview](https://developers.meta.com/horizon/documentation/unity/unity-depthapi-occlusions/).

**Gate:** Ein kompletter Portal-/Decken-/Bossablauf ohne Stereo-Artefakte, verdeckte reale Hindernisse oder Verlust des Leistungsbudgets. Für kleine Räume schrumpft die Begegnung oder entfällt; sie drängt den Spieler nicht aus der sicheren Fläche.

## V22 – Rundenstruktur und Wiederspielwert

- **Rissjagd:** drei unterschiedlich inszenierte Risse, kurze Zwischenphase, Torwächter-Finale; Zielbereich 8–12 Minuten nach tatsächlichen Spielmessungen.
- Nach einem geschlossenen Riss genau eine von drei Verbesserungen: z. B. stärkere Schwachstellentreffer, besseres Feuerball-Abfangen oder kurzzeitiger Schutz. Zuerst sechs klar unterschiedliche Upgrades; Synergien erst nach Balance-Tests.
- Bereits vorhandene Kristalle bekommen einen klaren Zweck: sichtbares Symbol, passende Farbe, Feedback und Anziehung aus sicherer Nähe. Keine Belohnung, die zum Greifen hinter das Sofa lockt.
  Die vorhandenen Lebens-/Munitionsobjekte und ihre Aufnahmeeffekte werden bereits in A9 als unterscheidbare Relikte neu gestaltet; V22 ergänzt danach neue Upgrade-Entscheidungen und Balance, nicht nochmals denselben Assetersatz.
- Präzision und kontrolliertes Spiel belohnen; Geschwindigkeit darf nicht der einzige Punktefaktor sein. Rundenende zeigt Trefferquote, abgefangene Angriffe und geschlossene Risse.
- Separater Endlosmodus, Schwierigkeit und lokale Highscores. Freischaltungen zunächst überwiegend kosmetisch; kein Grind nötig, um eine faire Runde zu bestehen.

**Gate:** Fünf vollständige Durchgänge ohne Fortschrittsblockade; alle Upgrades korrekt mit Pause/Neustart und Notmunition. Kein dominanter Upgrade-Pfad, keine Pflicht zu unsicherer Bewegung.

## V23 – Komfort und spielbare Alpha

- Kurze Ersteinrichtung: Raumdatenstatus erklären, Waffenpose kalibrieren, Testschuss, Start/Pause zeigen. Optional Links-/Rechtshänderbetrieb und Einhand-Komfort.
  Der frei platzierbare Start-/Pause-Schrein inklusive Platzierungsvorschau, Bestätigung und Umplatzieren wird in A8 vorgezogen. Controller-Pause bleibt unabhängig vom Schrein erreichbar; V23 übernimmt den Ablauf in das vollständige Onboarding.
- Regler für Lautstärkegruppen, Haptik, Effektdichte, Kontrast, Horrorintensität und grafische Qualität. Farben nie als einziges wichtiges Signal.
  Waffen-/Trefferlautstärke wird wegen der erneuten Nutzerrückmeldung bereits in A6b separat regelbar geplant; die übrigen Komfortregler bleiben hier.
- Sichere Pause bei Trackingverlust, Raumdatenwechsel und Unterbrechung. Laufende System-Sicherheitsgrenzen nicht umgehen.
- Saubere Szenen-/Speicherlebensdauer, versionierte Konfigurationen und reproduzierbare Testbuilds. Die großen Klassen schrittweise entlang getesteter Verantwortlichkeiten aufteilen: Raumabfrage, Bewegung, Kampf, Darstellung und Rundendirektor.
- Tutorial, Credits/Lizenzen und verständliche Fehlermeldungen. Eine Store-Veröffentlichung ist ein eigener späterer Auftrag, kein automatischer Teil dieser Roadmap.

**Gate:** Fünf Kaltstarts, wiederholtes Home/Resume, 30-Minuten-Sitzung, zwei Raumgrößen und verständliche Behandlung fehlender Berechtigungen. Keine Regression der bisherigen Treffer-/Stereo-/Pausentests.

## Bewusst später

- **Koop im selben Raum:** attraktiv, aber eigener technischer Prototyp für gemeinsame Koordinaten, Ankersynchronisation, Netzwerkzustand und gegenseitige Sicherheit. Nicht beiläufig an die lokale Spiellogik hängen.
- **Mehrraum-Spiel:** separate Prüfung von Raumwechseln, Grenzen, Tracking und sicheren Begegnungen; kein „Grenzen ausschalten“.
- **Begehbare Portale/Voll-VR-Übergänge:** nicht nötig für überzeugende Einblicke; erhöhen Komfort-, Kollisions- und Sicherheitsaufwand erheblich.
- **Handtracking-Waffen, zerstörbare reale Möbel, freie Kletter-KI, riesige Gegnerhorden:** derzeit ungünstiges Verhältnis von Nutzen, Risiko und Aufwand.
- Kein Enginewechsel und keine Render-Pipeline-Migration ohne konkreten, belegten Nutzen.

## Konkreter nächster Auftrag

**A6b/V18.9 wurde vom Nutzer vorläufig positiv bestätigt. A7 ist als V18.10 implementiert und installiert:** Blender-Gang mit streckengebundener Standphase, begrenztes Anlaufen/Bremsen und Flugkurven, Treffer-/Erholungsübergänge sowie auf Nutzerwunsch kompakte HUD-, Scan- und Hilfstexte. 895 native CHECK-Meldungen im finalen Validierungs-/Exportlauf (davon 74 A7), APK-Signatur und Gerätehash bestätigt; neue getragene Bewegungs-/Lesbarkeitsabnahme offen. [Änderungen und Grenzen](REVIEW-V18-10.md). A8 (Schrein) und A9 (Relikte) sind inzwischen installiert. Als Nächstes A10 Portal-Schauplätze gemäß [Qualitätssprint](IMPLEMENTATION-COMBAT-POLISH.md), anschließend offene Hauptmeilensteine. Die kompakte Textgestaltung ist auch als A8-Abnahmekriterium aufgenommen.

**Nutzerkorrektur nach A7 – V18.11 installiert:** häufige/irreführende Suchmeldung, wiederholte Portalplätze und weiterhin zu große Waffe bearbeitet. Live-Platzierung sucht systematischer und bewertet Wiederholungen statt sie vorübergehend zu verbieten; reale Flächen-/Weg-/Abstandsprüfungen bleiben Pflicht. Revolver nochmals 20 % kleiner, gleicher Griffpunkt. 954 native CHECK-Meldungen, APK-Signatur und Gerätehash bestätigt; reale Raum-/Größenabnahme offen. [V18.11](REVIEW-V18-11.md). Danach weiterhin A8 → A9 → A10.

Diese Roadmap ändert keinen Spielcode und löst weder Installation, Assetkauf noch Veröffentlichung aus. Der ursprüngliche `IMPLEMENTATION-PLAN.md` bleibt als historischer MVP-Plan erhalten; für die Weiterentwicklung nach V16 gilt dieses Dokument.
