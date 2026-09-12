# Purgatory V19.19 — Schnellneustart und Kampfentscheidungen

> **Nachträglicher Gerätebefund, 8. September 2026:** V19.19 scheitert auf der Quest beim nativen Laden der Startszene (`CachedReader::OutOfBoundsError`, SIGTRAP), noch vor der Spielinitialisierung. Die folgenden Editor-/Paketprüfungen haben diesen Fehler nicht erkannt und sind keine Startfreigabe. Nicht mehr als Testfassung installieren. Korrektur und erneuter Geräte-Kaltstart werden unter [V19.20](V19-20-STARTUP-FIX.md) dokumentiert.

Stand: 8. September 2026. **V19.19 gebaut und geprüft, nicht installiert.** Umsetzung des V19-0/A/B/C-Blocks und Vorbereitung der V19-D-Geräteabnahme. Keine Erweiterung auf Katana, neue Monsterfamilien, Boss, vollständige Kampagne oder Multiplayer.

APK: [Purgatory-v19.19-combat-director.apk](../Builds/Purgatory-v19.19-combat-director.apk), 134.829.218 Bytes, Version 0.19.19/code58, ARM64. SHA-256 `776db13751b2109e8e1bd301005b52366d87aa7dc41827e6d51ddfd80cc03059`. ZIP, Paketmetadaten und bestehende APK-v2-Entwicklungssignatur geprüft. [Liefernachweis](../Verification/V19Completion/delivery.json).

## Bedienung und Änderungen

- Nach Tod am Schrein **Erneut spielen** oder die dort angezeigte Fortsetzen-Taste der freien Hand benutzen. Im Raummenü ist **Neue Prüfung – dieser Raum** verfügbar, solange Raum und Schrein gültig sind. Nach neutralen Eingaben läuft eine kurze Startfrist. Gesundheit, Munition, Sterne, Gegner, Projektile, Blut und Begegnungszustand werden zurückgesetzt; Raumkarte und Schreinpose nicht.
- **Raum ändern** bleibt eine ausdrückliche Aktion. Der bisherige lange Stickdruck nach abgeschlossener Einrichtung öffnet die Auswahl, statt die Karte sofort zu löschen. Echte Tracking-/Ausrichtungsverluste bleiben Sperrgründe. Ein App-Kaltstart ist kein Schnellneustart.
- Bodenportal-Kandidaten entstehen zusätzlich aus bekannten freien Raumregionen. Ein begrenzter Wandkandidatenspeicher und raumfeste Sektoren reduzieren die Abhängigkeit vom momentanen Blickpunkt. Die vorhandene rückwärtige Vorwarnung und sichere Ausweichwahl bleiben bestehen.
- Feinere körperbreitenabhängige Wege mit 22-cm-Zellen prüfen kleine Durchgänge. Diagonalen dürfen keine Hindernisecken abschneiden; unbekannte und belegte Bereiche bleiben gesperrt. Wegsuche verteilt sich über Frames; auch die Verfolgung nutzt diese lokale Prüfung.
- Vor Öffnung und Eintritt wird der Platz erneut geprüft. Ohne sicheren Platz bleiben Gegnerquote und Welle erhalten. Nach längerem erfolglosem Suchen ohne lebende Gegner wird eine auflösbare Pause angeboten, nicht eine falsche Wellenbelohnung.
- Der Kampfregisseur wechselt zwischen Aufbau, Angriff und kurzer Entlastung. Er verändert Nachschubintervalle und Rollenwahl, nicht heimlich Lebenspunkte. Zeitlich versetzte Angriffsfreigaben berücksichtigen Nahkampf, Sprung, Fledermaussturz, Wurf und bereits fliegende Projektile. Optionale Siegel bleiben optional.
- Emberfiends bevorzugen freien Wurfabstand, Ash Stalker binden im Nahkampf; Brutes erhalten eine zeitweise exponierte Oberflächen-Schwachstelle. Ein Treffer dort zählt offen 2,5-fach, geschlossen halb; normale Körperstellen unverändert. Die Markierung folgt dem tatsächlich animierten Mesh, ohne zusätzlichen Treffer-Collider.
- Ein kurz vor Einschlag abgefangener Feuerball gibt einmal **50 Punkte und eine Patrone**, höchstens viermal je Welle. Ein kurzes Signal und heller Kern kündigen das begrenzte Fenster an. Mehrere Shotgun-Pellets können denselben Bonus nicht mehrfach auslösen. Panzerung/Schwachstelle/Abfangen ergänzen die vorhandenen Trefferklänge und Haptik.

## Architektur und Grenzen

`EncounterDirector` und `AttackCoordinator` enthalten testbare Zustände; die Spielintegration liegt in den Game-/Actor-Partials. `BodyRouteSearch` hat ein festes Expansionslimit und ein Budget pro Tick. `DemonWeakPoint` bindet drei echte Mesh-Vertices baryzentrisch und verwendet das vorhandene Sparse-Skinning. Keine neuen hochauflösenden Texturen, Portal-Kameras oder Schattenlichter für diese Erweiterung.

Die lokale Diagnose ergänzt maximal 32 Kandidatenereignisse plus Auswahl je Suche (innerhalb der bestehenden globalen Ereignisgrenze), ohne Raumkoordinaten oder Kamerabilder zu speichern. Freifläche/Austritt, Wand-Patch, Abstand, Bewertung und Anschlussweg sind getrennt nachvollziehbar. Ein neuer `QDMR.BodyRouteTick`-Marker dient der CPU-Ursachenprüfung. CSV und Auswerter enthalten jetzt P99 neben P95; die Auswertung bezeichnet es ausdrücklich als schlechtestes **Fenster**-Perzentil, nicht als globales Perzentil. Nicht verfügbare Provider-/Thermikwerte werden nicht als null Millisekunden ausgegeben.

Die Raumprüfung für die maximale Gegnerzahl wird von Regisseur, Welle und parallelen Nachschubportalen gemeinsam verwendet und höchstens alle 0,8 Gameplay-Sekunden erneuert. Sie läuft nicht pro Portal pro Frame. Die bisherige Untergrenze von drei Gegnern bleibt für die Shotgun-Bedingung erhalten; die strengere Nahkampf-Freigabe ist davon unabhängig.

Der ursprüngliche Todespfad lud den Raum nicht selbst neu. Zusätzliche Scan-Reset-Eingaben konnten jedoch die gültige Einrichtung verwerfen; die neue klare Menüführung trennt beides. Der genaue historische Ablauf beim Nutzer ist ohne damalige Geräteaufzeichnung nicht bewiesen.

## Prüfstatus

Der finale vollständige Editorlauf enthält **4.128 CHECK-Meldungen, darunter 228 neue V19-Checks**. Zusätzlich sind 36 eigenständige Statistik-/Logprüfungen und fünf Python-Auswertungstests grün. Die neuen Tests prüfen unter anderem zehn Todes-/Rundenresets bei unveränderter Raum- und Schreinpose, den wirklichen Neustart-Einstieg und die Eingabesicherung, positive und negative schmale Wege, Angriffsfreigaben/Projektilübergabe, echte Mesh-Schwachstellen in mehreren Posen und die produktive Portal-Suchroutine. Der gesamte bisherige Editor-Prüfpfad ist ebenfalls durchgelaufen. Finale C#-/Shader-/Exception-Suche sauber; bestehende Unity-/Gradle-Warnungen bleiben. Native Bilder der geschlossenen/offenen Bruststelle wurden angesehen. Ein anfänglich falscher Dreieckindex wurde vor dem finalen Export behoben und um einen geometrischen Bindepunkt-Test ergänzt.

Nicht mit diesen Tests nachgewiesen: getragene Controller-Neutralisierung, gefühlte Kampfhitzigkeit, reale hintere Nische, wiederholte App-Relokalisierung, 72-Hz-Frametimes und Thermik. Editorbilder belegen nur die native Darstellung, nicht MR-Wahrnehmung.

## V19-D: nächste Geräteabnahme vor breitem V20-Ausbau

1. Gültigen Raum einmal verwenden. Zehnmal Tod → Erneut spielen, ohne Raumwahl oder neue zwei Punkte. Wiederholen mit linker Waffenhand und Shotgun beim Tod. Raum/Schrein bleiben exakt am Ort; keine Altgegner, Projektile oder Sternreste.
2. Absichtlich **Raum ändern**, Trackingverlust und App-Neustart als Gegenfälle. Keine Freigabe einer unpassenden Karte.
3. Zwei Raumgrößen, jeweils mindestens fünf Wellen. Hinteren Gang freihalten und anschließend gezielt ein Hindernis in/außerhalb des Laufwegs testen. Keine erzwungenen Portale durch Möbel; bei mehreren passenden Regionen muss deren Nutzung nachvollziehbar sein.
4. Brute während und nach Angriffen an der exponierten Stelle treffen; Körper und Panzerung vergleichen. Feuerball früh, im Präzisionsfenster und mit Shotgun abfangen; Bonusobergrenze prüfen. Lebens-/Munitionsrelikte und Shotgun-Hilfe mitprüfen.
5. Bestehende lokale Diagnose nutzen: 0/1/2 Portale, mehrere Gegner, Blut/Sterne, Shotgun/Himmel sowie Scan/Schrein getrennt aufzeichnen. CPU/GPU, P95/P99, verpasste Frames, Speicher/GC und mindestens 20 Minuten thermischen Verlauf auswerten. Keine alten V17-Werte als aktuellen Leistungsnachweis verwenden.

V19-D bleibt bis zu diesen realen Messungen und Spieltests offen. Die APK ist eine separate Testlieferung, kein Store-Release.
