# V18 — Live-Raum statt gespeicherter Möbelboxen

Stand: 5. September 2026. **Implementiert, gebaut, signaturgeprüft und auf Quest 3 installiert.** 220 native Unity-Prüfungen bestanden (179 bestehende und 41 neue Scan-Prüfungen). APK 0.18.0 / Code 18, installierter SHA-256 identisch bestätigt; Spiel nicht automatisch gestartet. Reale Scan-Qualität noch nicht bestätigt. V17-Langzeitprüfungen sind auf Nutzerwunsch zurückgestellt, nicht bestanden. Gleichwertigkeit mit Laser Tag ist ein Ziel für die Geräteprüfung, kein behauptetes Ergebnis.

## Geänderte Raumgrundlage

Die neue Standardszene erzeugt keinen MRUK-Raumlader und keinen gespeicherten `EffectMesh`. Die Spielinstanz verwendet im Live-Modus keinen `MRUKRoom`; alte Wand-/Möbelboxen können weder Kollisionen noch Spawnentscheidungen erzwingen. Die frühere Hybrid-Implementierung bleibt als Quellcode für historische Prüfungen, nicht als stiller Runtime-Fallback. Die V17.2-APK bleibt erhalten.

Der eigene Scanner liest Metas Depth-Textur und die dazugehörigen Reprojektionsmatrizen gemeinsam nach dem SDK-Update. Ein kleiner GPU-Readback erschließt beobachtete Raumabschnitte. Darin integriert ein Compute-Shader wiederholte Tiefenmessungen zu einem vorzeichenbehafteten Abstandsfeld. Positive Evidenz bedeutet beobachtet frei, negative Evidenz liegt nahe hinter der erkannten Oberfläche, Gewicht null bedeutet unbekannt. Keine gültige Messung bedeutet ausdrücklich nicht freien Raum. Tiefendiskontinuitäten und der unmittelbare Körperbereich des Trägers werden gefiltert.

Aus bestätigten Feldwerten entstehen interpolierte Dreiecksoberflächen (Marching Tetrahedra), keine gestapelten sichtbaren Würfel. Meshaufbau erfolgt in höchstens zwei Hintergrundaufträgen; pro Frame wird höchstens ein Mesh/Physik-Collider ersetzt. Eine Oberfläche außerhalb der aktuellen Blickrichtung bleibt erhalten. Wiederholte Messungen eines weiter entfernten Hintergrunds können ein verschobenes Möbelstück aus der Karte entfernen. Reaktionszeit und Qualität dieses Verhaltens auf Quest sind noch nicht gemessen.

## Anschluss ans Spiel

- Bodenhöhe und Standflächen werden aus der rekonstruierten Geometrie abgeleitet. Der alte Bezug auf manuell platzierte Boden-/Deckenanker entfällt für den Live-Spielmodus.
- Boden-A* und lokale Bewegung prüfen bestätigte Standflächen und Körperfreiheit. Kleinere Bodenhöhenunterschiede werden nachgeführt. Kein universelles Treppen-/Klettersystem.
- Bodenportale benötigen eine zusammenhängend beobachtete Wandfläche, freien Austritt und einen Weg in die Nähe des Spielers. Gespeicherte Wände hinter einer realen Wand werden nicht mehr zur Platzierung verwendet.
- Deckenportale werden aus gemessenen, nach unten gerichteten Flächen gewählt. Eintritt und erster Flugabschnitt müssen frei sein. Flug verwendet die persistente Karte; bisherige lokale Ausweichsteuerung bleibt, kein neuer globaler 3D-A*-Flugplaner.
- Waffen, Feuerbälle und Ragdolls treffen dieselben Physik-Meshes. Fledermaus-Leichen fallen auf gemessene Möbel/Boden; der bekannte gemessene Boden bleibt der letzte Fall-Fallback. Künstliche temporäre Ragdoll-Bodenplatten werden im Live-Modus nicht erzeugt.
- Möbelüberquerung benötigt jetzt eine gemessene niedrige Oberseite und einen geprüften Bewegungsbogen statt nur das semantische Label COUCH/TABLE. Die vorhandene Vault-Animation wird wiederverwendet.
- Keine ausgedachte Fallback-Arena, wenn die Live-Erfassung noch fehlt. Der Start ist erst nach erkannter Bodenfläche und freien Bereichen um den Spieler möglich.
- Das Startpult wird während der Vorbereitung/Pause auf eine bestätigte, erreichbare freie Fläche gesetzt. Collideränderungen werden ausdrücklich auch bei `timeScale=0` synchronisiert. Ein Recenter während der Inhaltsvorbereitung erzeugt keine zweite Wellen-Schleife.

## Nachtrag: erster echter Geräte-Start

V18.0 brach auf der Quest am 5. September um 17:50:36 bereits bei der Waffeninitialisierung wegen eines fehlenden Shaders ab, noch vor der Erstellung des Live-Scanners. Die erfolgreiche Paketierung und 220 Editor-Checks waren deshalb kein Beleg für einen funktionierenden App-Start. Fehler und Korrekturen: [V18.1](REVIEW-V18-1.md).

## Bedienung

1. Starten und in Ruhe Boden, Wände, Möbel sowie für Deckenportale die Decke ansehen. Das Netz zeigt die tatsächlich erzeugten Kollisionsflächen. Nichts manuell ausmessen; kein gespeichertes Raumsetup erforderlich.
2. Bei „LIVE-RAUM BEREIT“ wie bisher auf das Startpult schießen. Die Netzvorschau wird dabei ausgeblendet; Erfassung läuft im Hintergrund weiter.
3. Linken Stick klicken: Scan-Netz ein-/ausblenden, auch im Spiel.
4. In Pause rechten Stick zwei Sekunden halten: Karte neu erfassen. **Das setzt die aktuelle Runde zurück**, löscht aber keinen gespeicherten Quest-Raum und keinen Highscore.

Die Karte wird nur für die laufende Sitzung gehalten. Kein Upload, keine Kamerabilder, kein auf Platte gespeicherter Raumscan. Headset-Pause/Resume und Recenter behandeln wir vorläufig konservativ: bei Resume nach App-Pause, Recenter oder erkanntem Positionssprung wird die Karte verworfen und die Runde zurückgesetzt, um veraltete Weltkoordinaten nicht weiterzubenutzen. Ein nahtloser räumlicher Wiederanlauf ist damit noch kein abgenommenes Feature. Bei fehlenden Tiefendaten pausiert das Spiel.

## Begrenzte Ressourcen und bekannte Grenzen

- 8 cm Rasterabstand, 1,28 m je Raumabschnitt, höchstens 256 Abschnitte. Feldpuffer: 4913 × 8 Bytes je Abschnitt, zusammen maximal 10.061.824 Bytes GPU-Felddaten; zusätzliche CPU-Snapshots, Meshes und Physikstrukturen kommen hinzu. Keine Aussage zum gesamten App-Speicher.
- Höchstens zwei neue Feldpuffer pro Frame; beschränkte Entdeckungswarteschlange; höchstens zwei Integrations-Dispatches und ein neu angeforderter Feld-Readback pro Renderframe, jeweils nur ein ausstehender Readback pro Abschnitt. Normale Geometrie bleibt beim Erreichen der Kapazität erhalten, neue Bereiche werden nicht stillschweigend als frei behandelt. UI meldet Kapazitätsgrenze; Neuscan möglich.
- Das ist kein vollständiger, fehlerfreier Raum aus einer einzelnen Blickrichtung. Erst beobachtete Bereiche können zuverlässig einbezogen werden. Glas, Spiegel, dünne Möbelbeine und bewegte Personen bleiben schwierig. Ein Mesh außerhalb des Sichtbereichs kann eine dort unbemerkt erfolgte Änderung nicht kennen.
- Vorhandene Sensor-Händefilterung plus eine eigene kleine Trägermaske, keine allgemeine Personensegmentierung.
- Keine Texturierung realer Möbel, keine RGB-Kamera-Berechtigung, kein Spatial-SDK-Wechsel, kein Laser-Tag-Multiplayer übernommen, keine Änderung der OS-Sicherheitsfunktionen.
- Die volle V18-Roadmap (z. B. Engstellenreservierungen, globale Flugwegplanung und umfassende Animationsüberarbeitung) ist hiermit nicht insgesamt erledigt.

## Ausgeführte Prüfungen

41 neue native Prüfungen: unbekannte Daten, wiederholte Bestätigung, begrenztes Gewicht und Entfernen alter Hindernisse, negative Weltkoordinaten, drei interpolierte Ebenen samt Kollisionsnormalen und tatsächlichen MeshCollider-Treffern. Dazu echte Compute-Ausführung auf dem Mac-Grafikgerät: synthetische Meta-konforme Depth-Textur, Weltrekonstruktion, freie/oberflächennahe/verdeckte Proben und Sichtbereichsausschluss. Gemeinsame Gameplay-Abfragen prüfen Bodenfreiheit mit drei Körperradien, unbekannten Spawnraum, gespeicherten freien Raum, Wandblockade, Collider-Entfernung und Aufräumen. Das ersetzt keine Quest-Sensormessung.

Die 179 bisherigen nativen Regressionen liefen ebenfalls erfolgreich. Neuester vollständiger Prüflauf: `work/unity-v18-livescan-final.log` im übergeordneten Workspace. Beim ersten Paketierungsanlauf fiel ein reservierter Shader-Bezeichner auf; der Lauf wurde vor Lieferung gestoppt. Vorschau-Shader korrigiert und Fehlerprüfung ergänzt. Außerdem wurde eine durch Voxelrundung zu knappe Bodenfreiheit großer Körper korrigiert und explizit geprüft. Der zweite Paketbau war erfolgreich; danach kamen Startpult-Platzierung, explizite Physiksynchronisation in Pause und der Initialisierungs-Guard hinzu und wurden im abschließenden Lauf erneut gebaut. Kein Zwischenstand installiert.

Die bekannten TypeDB-Doppelregistrierungen erscheinen weiterhin. Der zweite und der abschließende Editor-Lauf meldeten beim Beenden außerdem zwei persistente Allokationen ohne Herkunftsstack; daraus wird weder ein nachgewiesenes Scanner-Runtime-Leck noch eine leckfreie Sitzung abgeleitet. Langzeit-Speicherprüfung bleibt offen.

Reproduktion: `bash Tools/build-v18.sh`. APK-/Installationsdaten werden im [Buildbericht](BUILD-REPORT.md) festgehalten. Ein späterer Raumtest soll zuerst Netzlage, Startbarkeit, Portalflächen, Gegnerwege und geänderte Möbel prüfen. Lange Benchmark-/Thermikserien bleiben vorerst zurückgestellt.

## Herkunft

Eigenständiger Quellcode, kein Laser-Tag-Code importiert. Meta-SDK unter seinen vorhandenen Bedingungen. Konzeptreferenz: https://github.com/anaglyphs/lasertag (aktueller Stand PolyForm Noncommercial). API-/Projektionsgrundlage: installierter Meta SDK v205 und https://developers.meta.com/horizon/documentation/unity/unity-depthapi-api-reference/ .
