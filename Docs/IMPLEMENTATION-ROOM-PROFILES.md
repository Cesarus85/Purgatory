# V19.5 – lokale Raumprofile

**Aktuelle Bedienung ab V19.7:** [Purgatory-Einrichtung](IMPLEMENTATION-V19-7-SETUP.md): direkt am platzierten Schrein `RAUM SPEICHERN` anklicken; der Profilmenü-Umweg unten ist historisch. Raum laden oder neu scannen erfolgt im übersichtlichen Startpanel mit Ziel-/Abzug-Schaltflächen. Sicherheits- und Dateiformatgrenzen bleiben erhalten.

**Ergänzung V19.6:** [Raumwahl und Referenzvorschau](REVIEW-V19-6.md) unterscheiden gespeicherte Profile von Menüaktionen, unterbrechen den Scan während der Auswahl und behalten die vollständige goldene gespeicherte Oberflächenkarte neben aktuellen grünen Flächen. Speichern wird durch erneutes Lesen geprüft. Bedienung und Sicherheitsgrenzen unten bleiben gültig; der Neuscan heißt jetzt ausdrücklich `NEU SCANNEN / KEIN PROFIL LADEN`.

Auftrag: eigenen Live-Scan über Spielstarts erhalten, mehrere Räume anlegen und weiter live aktualisieren. Kein Laden der manuell angelegten Meta-Szenen als Ersatz.

## Bedienung

Nach der Waffenhandwahl erscheint die kompakte Raumauswahl. Mit dem Stick der Waffenhand blättern; deren primäre Gesichtstaste bestätigt (rechts A, links X), die sekundäre geht zurück (rechts B, links Y). Zunächst Tasten loslassen. Neue Räume und das Laden eines anderen Raums setzen die aktuelle Runde zurück; bei vorhandener Karte ist dafür eine Zwei-Sekunden-Bestätigung vorgesehen. Aktualisieren und Löschen erfordern ebenfalls zwei Sekunden.

1. **Erstes Mal:** „Neuen Raum erfassen“, normal scannen und die ausreichende Fläche wie bisher bestätigen. Schrein platzieren.
2. **Speichern:** Vor Spielbeginn oder in Pause am Schrein „Raumprofile“ öffnen, „Als Raum N speichern“ wählen. Fünf feste Plätze, automatische Namen „Raum 1“ bis „Raum 5“; freie Texteingabe/Umbenennen ist nicht Teil dieser Fassung.
3. **Nächster Start:** „Letzten Raum laden“ oder einen anderen gespeicherten Platz wählen. Langsam umschauen, damit Meta den Raumanker lokalisiert; danach Boden und mehrere Wandrichtungen ansehen. Nach Anker-/Tiefenabgleich und ausreichend aktuell bestätigter lokaler Spielfläche erfolgt die Freigabe automatisch, ohne erneutes langes X/A-Halten.
4. **Schrein:** Seine gespeicherte Position wird nur nach aktueller Auflagen-/Freiraumprüfung übernommen. Sonst erscheint die normale Platzierung.
5. **Änderungen:** Live-Scan läuft weiter. Zum dauerhaften Übernehmen im Raummenü „Raum N aktualisieren“ wählen. Kein ungefragtes Überschreiben einer alten Karte.
6. **Andere Räume:** „Neuen Raum erfassen“ anlegen, anschließend freien Speicherplatz wählen. Bei belegten fünf Plätzen ist ein bewusstes Aktualisieren/Entfernen nötig.
7. **Fehler:** Ein nicht gefundener Anker, beschädigte Datei oder nicht bestätigte Ausrichtung führt ins Menü zurück. „Neu scannen“ bleibt verfügbar. Kein endloser unbeschrifteter Ladezustand und kein Start auf einer unbestätigten gespeicherten Karte.

## Architektur

- Eigene TSDF-Chunks, Metadaten, Anker-UUID/-Pose, Bodenhöhe und optionale Schreinpose. Keine Passthrough-Bilder gespeichert. Formatversion 1 bindet das vorhandene 8-cm-Raster und 17³ Samples je Chunk; maximal 768 Chunks, fünf aktive Profile.
- Lokaler Meta-`OVRSpatialAnchor`: erstellen/lokalisieren, `SaveAnchorAsync`, später per UUID `LoadUnboundAnchorsAsync`, `LocalizeAsync`, `BindTo`. Anchor-API im Projekt aktiviert. Keine Freigabe an andere Nutzer und kein eigener Cloud-Upload.
- Beim Laden wird der `OVRCameraRig` mit Translation und Yaw in das gespeicherte Kartenkoordinatensystem gebracht. Die installierte Meta-Depth-Implementierung berechnet ihre Reprojektionsmatrizen mit der aktuellen Tracking-Space-Transformation. So bleiben neue Messungen, Collider, Abfragen und Karte im gleichen Koordinatensystem. Mehr als drei Grad Neigungsabweichung wird abgelehnt, nicht durch Kippen der Schwerkraft ausgeglichen.
- Dateien im privaten App-Verzeichnis `room-profiles/room-N.qroom`: SHA-256, GZip und explizite Format-/Größen-/Werteprüfungen. Atomarer Austausch; vorherige Version als `.previous`. Dateiarbeit außerhalb des XR-Hauptthreads, Snapshot und Mesh-Import schrittweise. Menüliste liest nur vorhandene Slots, nicht alle Raumnetze.
- Neuer Raum oder Import leert die laufende Raumkarte/Runde bewusst. Die gespeicherten Profile, Highscore und Waffenhand bleiben erhalten. Unterbrechungen/Recenter während Import/Snapshot werden über die Scan-Epoche erkannt; bestehender sicherer Rescan-Fallback nach Tracking-Neuausrichtung bleibt.

## Vertrauen in gespeicherte Geometrie

Gespeicherte Felder erhalten beim Import negative Gewichte. Sie gelten für Freiraum-/Wegabfragen als **unbekannt**. Gespeicherte Oberflächen dienen bis zur Aktualisierung nur als konservative Hindernisse; sie können vorübergehend noch ein inzwischen verschobenes Möbel blockieren. Sie autorisieren keine freien Austritte.

Eine passende neue Tiefenmessung bestätigt einen gespeicherten Sample direkt; eine abweichende Messung ersetzt ihn zunächst mit schwacher aktueller Evidenz und benötigt eine weitere aktuelle Beobachtung. Verdeckte Bereiche bleiben unbestätigt. Support-/Bodenabfragen gespeicherter Flächen brauchen aktuelle Oberflächenevidenz. Unbeobachtete Raumteile können somit nach dem Laden zunächst noch keine Portale tragen.

Zusätzlich zum lokalisierten Anker: mindestens 48 räumlich verschiedene bestätigte Tiefenpunkte, mindestens 80 Prozent Übereinstimmung der aufgenommenen Vergleichspunkte, drei Chunks und drei Blick-/Höhenbereiche. Pro Bereich begrenzte Punktzahl verhindert, dass allein langes Betrachten derselben Fläche den Test erfüllt. Die lokale Mindestfläche mit drei verbundenen Sektoren bleibt Voraussetzung. Erst nach einer stabilen Sekunde wird das geladene Profil freigegeben. Nach 45 Sekunden ohne Freigabe erscheint die Auswahl mit Hinweis erneut.

Ankerverlust oder Drift über acht Zentimeter/drei Grad für mehr als eine halbe Sekunde setzt die laufende Karte zurück. Es gibt keine Zusage millimetergenauer Wiedererkennung oder sofortiger Nutzbarkeit des gesamten alten Raums. Die ersten praktischen Schwellen müssen auf der Quest geprüft werden.

## Datenschutz und Wiederherstellung

Raumdateien sind räumlich sensible Daten und bleiben im privaten App-Speicher. Deinstallation/App-Daten-Löschen kann sie entfernen; sie werden nicht in der APK mitgeliefert. Ein normales Update erhält sie.

„Löschen“ entfernt den aktiven Profileintrag durch Umbenennen in eine `.deleted-…`-Datei. Diese lokale Kopie und ihr persistierter Anker bleiben für Wiederherstellung erhalten; es ist **keine sichere endgültige Löschung**. Aktualisierungen halten eine vorige Dateiversion vor. Wiederherstellung ist vorerst über die Dateien/Entwicklerwerkzeuge möglich, nicht über einen Papierkorb-Knopf im Spiel. Ältere Anker werden in dieser Fassung nicht automatisch bereinigt.

## Prüfungen und Abnahme

Automatisch: Dateirundlauf, atomarer Austausch/Backup, Pfad-/Format-/Gewichts-/Rotations-/Checksum-Prüfungen, vier unterschiedliche Anchor-Yaws, Neigungsablehnung, unbekannte geladene Samples, neue Hindernisevidenz, Produktionsimport/-abfragen und Reset. Bestehende funktionale Regression inklusive Waffenhand und Wurfsternen bleibt enthalten. Ein älterer zufälliger Siegel-Schusstest nutzt nun reproduzierbare Platzierung und den echten stabilisierten Zweihand-Schuss, um zufällige Randfehlschüsse nicht als Funktionsfehler zu werten; Produktionswaffe und Siegel bleiben unverändert.

Unbedingt auf der Quest prüfen: Raum 1 speichern → App vollständig beenden → an anderem Standort/Blickwinkel starten → Raum 1 laden. Wand-/Bodennetz auf Deckung prüfen, Schrein und Portal-/Wurfkollisionen prüfen. Danach Stuhl versetzen und Änderung beobachten; zweiten Raum getrennt speichern/laden; falschen Raum und fehlgeschlagene Lokalisierung testen. Load/Save unterbrechen, beide Waffenhände, Startzeit und Framezeiten prüfen. Native Tests ersetzen weder Meta-Anker-Hardwaretests noch diese getragene Abnahme.

Build- und Installationsnachweis: [Buildbericht](BUILD-REPORT.md).

Ergebnis vom 7. September: V19.5/code44 gebaut, Metadaten/Anchor-Berechtigung/Signatur bestätigt, noch nicht installiert. 2.215 native CHECK-Meldungen im vollständigen Lauf plus acht gezielte Zusatzchecks bestanden; [native Linkshänder-Menüansicht](../Verification/RoomProfiles/menu-left.png) angesehen. Noch kein echter Meta-Anker auf der Quest gespeichert oder erneut lokalisiert.
