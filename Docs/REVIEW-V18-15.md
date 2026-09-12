# A10a / V18.15 – Portalaustritt, Versorgung und Startlast

Stand: 6. September 2026. **0.18.15 / Code 33 implementiert, 1.291 native CHECK-Meldungen bestanden, Release-APK signaturgeprüft und um 16:05:48 auf Quest 3 installiert.** Geräteversion und APK-Hash bestätigt; App-Daten/Berechtigungen erhalten, nicht automatisch gestartet. [Build-/Prüfnachweis](BUILD-REPORT.md). Getragene Abnahme und anschließend A10b bleiben offen.

## Relikte

Es gibt weiterhin genau **Munition und Leben**, maximal +10/+25, Reserve-Cap 96 und Lebens-Cap 100. Der A9-Code erlaubte Lebensdrops erst unter 55 Leben und unterdrückte sie bei priorisierter Munition. Das erklärt, weshalb der Nutzer oft nur Munition gesehen hat; es ist keine durch einen Gerätetrace bestätigte Einzelfalldiagnose.

Jetzt kann jede tatsächliche Verletzung einen Lebensdrop erhalten. Nach vier Kills mit fehlendem Leben wird ein Lebensdrop fällig, unabhängig von gleichzeitig fälliger Munition; nötigenfalls erscheinen beide. Bei voller Gesundheit wird weiterhin kein Lebensdrop erzeugt. Die Zählung wird nur bei erfolgreicher sicherer Platzierung zurückgesetzt. Unbekannte/geblockte Auflage erzeugt kein schwebendes Ersatzobjekt. Normale Zufallschance 48 %, Lebenswahl 62 % in ihrem zulässigen Zweig; die höhere Lebensberechtigung ist eine bewusste Versorgungsänderung, nicht unveränderte Gesamtbalance.

Gameplay-Drops bevorzugen den erfassten, erreichbaren Fußboden neben dem Gegner beziehungsweise Möbel. Bleibt dort keine sichere Auflage, darf die vorherige sichere Möbelauflage verwendet werden. Kein Versprechen, dass jeder Drop immer am Fußboden liegt. Leichtes Schweben, 14 Spielsekunden Lebensdauer, kostenlose Fernaufnahme und tatsächliche Bonusmengen bleiben erhalten. Tod im Portal verwendet den geprüften Raumaustritt als Drop-Ursprung statt einer Position hinter der echten Wand.

## Sichtbarer Eintritt und erste Schmiede

- Ein einziger Gameplay-Gegner bewegt sich kontinuierlich von hinter der Portalfläche zum geprüften Austritt. Ein entfernter render-only Knochenzwilling übernimmt dieselbe Pose; er hat weder KI, Health noch Collider/Audio. Beide Darstellungen werden exakt an der Portalfläche geschnitten. Keine zweite Spielfigur, kein Teleport an den Fensterabschluss.
- Eigene Blender-Takes `PortalStep` und `Peek`, vorhandene Animationen erhalten. Weggekoppelte Schritte mit angehobenen Füßen über die erhöhte Öffnung; gelegentlich kurze Kopf-/Oberkörper-Erkundung, kein Schwenk des ganzen Portals. Peek nur an regulären Bodenportalen, nicht im schmalen Durchlass oder bei Fledermäusen. Fledermäuse fliegen kontinuierlich durch das nach unten gerichtete Deckenfenster.
- Kein Angriff während des Eintritts. Treffer können die Bewegung unterbrechen; Tod in der Schwelle blendet den Körper dort aus, statt ein Ragdoll hinter der realen Wand abzulegen. Das offene Fenster erlaubt exakte Mesh-Treffer in die entfernte Darstellung; eine vorgelagerte reale Wand/Möbelkante oder ein Strahl außerhalb des Fensters erlaubt das nicht. Portal bleibt während des vollständigen Eintritts offen. Ein blockierter Austritt wird abgebrochen und derselbe Wellenplatz erneut versucht, ohne Killbelohnung oder rekursive Coroutine-Kette.
- Originale Blender-Schmiede mit Steinplatten, seitlichen Schlackenrinnen, Pfeilern/Arkaden, aufgehängten Ketten, Hornamboss, mehrschichtigem Ofen und ferner Zitadellensilhouette. **15.184 Dreiecke, vier Materialslots, 2048²-Atlas mit gebackener Spaltenverschattung.** Bewegte Glut im Shader; vorhandener Stereo-Pro-Auge-Renderer und kompakte Portalgrößen bleiben. Der vorhandene Obsidianrahmen ist in A10a noch nicht durch drei ortsspezifische Rahmen ersetzt.
- A10a zeigt bewusst zunächst diesen einen Schauplatz. Brückenzugang, Kathedralen-Seitenhalle und zugehörige Rahmen/Variantenverteilung sind **nicht geliefert**, sondern der nach visueller Qualitätsabnahme folgende A10b-Teil. Portale sind nicht begehbar.

## Nutzernachtrag: Ruckeln beim Raumscan/Schrank

Quellbefund: Asset-/Audio-/Modellvorbereitung startete bei bereits aktiver Live-Rekonstruktion. Danach begann sofort die Schrankvorschau. Der Scanner konnte pro Frame ein Kollisionsmesh erzeugen/übergeben und auf dem Hauptthread mehrere komplette TSDF-Felder auf Änderungen prüfen. Diese überlappenden Arbeitslasten sind plausible Ursachen; ohne aktuellen Quest-Trace ist weder der Hauptverursacher noch eine erreichte Bildratenverbesserung bewiesen.

Änderungen:

1. **Vorbereitung → Scan → Platzierung:** Assets und zunächst unsichtbarer Schrein werden vorbereitet, bevor die Rekonstruktion startet. Erst nach 0,6 Sekunden stabiler Bereitschaft beginnt die Vorschau. Eingaben lösen vorher weder Bestätigung noch Start aus. Nach Fokus-/Trackingreset wieder neue Erfassung vor Platzierung.
2. TSDF-Änderungsvergleich ebenfalls auf dem Worker; höchstens ein neuer Job pro Frame und zwei ausstehende Jobs. Kollisionsnetz-Übernahmen maximal 25 Hz, während der Platzierung 10 Hz. Readbacks während Platzierung maximal 12,5 Hz. Das sind Frequenzgrenzen, **keine garantierte Millisekunden-Obergrenze** für einen einzelnen nativen Collider-Cook.
3. Scangitter während Platzierung standardmäßig aus; ein einzelner Depth-Pass erhält die echte Verdeckung. Linker Stick kann die Vorschau weiterhin ein-/ausschalten. Keine Abschaltung der Sensorintegration, Kollisionsabfragen oder Anforderung an bekannte Freiräume. Bei Fokus-/App-Pause auch CPU-Scanarbeit ausgesetzt.
4. `QDMR_START_PHASE`, `QDMR_SCAN_COST` und Profiler-Marker `QDMR.Scan.MeshColliderCommit` / `QDMR.Scan.GeometryWorker`: Phase, Anzahl/Mean/Max der Übernahmekosten, Worker-/Chunkwarteschlange, Overlay und letzte Unity-Frametime. Keine Kameraaufnahmen oder zusätzlichen Raumkoordinaten. Das Log ist kein GPU-/Compositor-Benchmark und wird nur alle zehn Sekunden zusammengefasst.

## Offene getragene Abnahme

Installation erfolgt, jetzt Kaltstart bis zur platzierbaren Vorschau, gleicher Raum mit/ohne Scangitter, Umsehen/Gehen und Umplatzieren prüfen; Messlog für Initialscan und Platzierung vergleichen. Danach Auftreten/Schritte/Peek frontal und seitlich in beiden Augen, Treffer/Tod während Eintritt, Deckenfledermaus und Pause mitten im Durchtritt. Lebensversorgung nach tatsächlichem Schaden sowie Auflagen neben Sofa testen. Unveränderte Sicherheitsabfragen und erfolgreiche Desktopchecks ersetzen diese Prüfung nicht; Langzeit-/Thermikbudget bleibt offen.
