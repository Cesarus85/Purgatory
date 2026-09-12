# V19.4 – Waffenhand und konsistente Steuerung

**Ergänzung V19.6:** Die Handrollen bleiben gültig; der unten historisch beschriebene Hüfthalter sitzt jetzt am Handgelenk der freien Hand. [Aktuelle Aufnahme und Wurfunterstützung](REVIEW-V19-6.md).

Stand: 7. September 2026. Zusatzauftrag vor dem nächsten Headsettest; keine Änderung der Raumrekonstruktion oder Portalplatzierung.

## Bedienung

Beim ersten Start vor dem Scan: beide Abzüge zunächst loslassen, dann den Abzug der gewünschten Waffenhand zwei Sekunden halten. Gleichzeitiges Drücken wählt keine Hand. Die Auswahl wird lokal gespeichert; vorhandene App-Daten bleiben erhalten. Später am platzierten Pause-Schrein „WAFFENHAND WECHSELN“ anvisieren und den Abzug drücken. Bei laufendem Kampf, Diagnose-Benchmark oder laufendem Nachladen ist ein Wechsel gesperrt.

| Aktion | Waffenhand rechts | Waffenhand links |
| --- | --- | --- |
| Revolver / Schrein-Zielstrahl / Abzug / Haptik | rechts | links |
| Nachladen / Schreinplatzierung beginnen | A | X |
| Waffenjustierung / Platzierung abbrechen | B + rechter Stick / B | Y + linker Stick / Y |
| Schrein drehen | rechter Stick | linker Stick |
| Pause / Fortsetzen | Y kurz loslassen | B kurz loslassen |
| Wurfsterne und Hüfthalter | links | rechts |
| Greifen / Werfen / Zweihandstütze | linker Grip | rechter Grip |
| Scan nach ausreichender Fläche bestätigen | X zwei Sekunden | A zwei Sekunden |
| Scannetz umschalten | linker Stick-Klick | rechter Stick-Klick |
| Neu scannen außerhalb des Kampfes | rechter Stick zwei Sekunden | linker Stick zwei Sekunden |
| Diagnose / Diagnose-Testkombination | X / X+Y halten | A / A+B halten |

Kontextpriorität bleibt: Scanbestätigung vor Diagnose, Schreinplatzierung vor Waffenjustierung/Schuss, belegte freie Hand vor Zweihandstütze. Textanzeigen werden aus der bisherigen rechtsseitigen Belegung genau einmal beim Darstellen umgerechnet.

## Umsetzung und Erhaltung

- `HandRoles` ist die zentrale Grenze zwischen physischer Seite und Spielrolle. Waffen- und freie Hand sind stets verschieden; Controlleranker, Geräte und Haptik nutzen dieselbe Zuordnung.
- Revolver wird umgehängt, nicht neu erzeugt. Munition bleibt erhalten. Eigene Justierungswerte für links, vorhandene rechte Werte bleiben unverändert. Positive Modellskalierung; keine Spiegelung der gesamten Revolvergeometrie.
- Wurfsternhalter wechselt zur anderen Hüfte; gehaltene Sterne werden zurückgelegt, geworfene bleiben verbraucht. Regenerationszeit wird nicht zurückgesetzt. Gehaltene Sternorientierung wird seitengerecht gedreht; keine negative Skalierung.
- Beim Wechsel werden Bewegungsverlauf, Eingabeflanken und Scan-Haltebestätigung zurückgesetzt. Beide Controller müssen getrackt und alle Abzüge, Grips, Gesichtstasten und Sticks neutral sein, bevor Bedienung wieder freigegeben wird. Das verhindert einen versehentlichen Wurf, Schuss, Start oder Rescan aus vorgehaltenen Tasten.
- Kein Aufruf zum Zurücksetzen der Runde oder Raumkarte beim Handwechsel. Scan-Geometrie, erfasste Flächen, Hindernisse, Gegner und Portalverteilung bleiben unverändert.
- Hüftposition bleibt HMD-basierte Schätzung, kein Körpertracking. Revolvermechanik bleibt das bestehende asymmetrische Modell; ein komplett linkshändiger Revolver-Neuentwurf ist nicht Teil dieses Blocks.

## Prüfung und offene Abnahme

Native funktionale Regression einschließlich Wurfsternen sowie zusätzliche Handrollen-Tests. Build-/Installationsstand im [Buildbericht](BUILD-REPORT.md). Physische Abnahme bleibt nötig: Erstauswahl, linke Waffenpose/Justierung, rechte Hüfterreichbarkeit, Wurfbewegung, Scan per A, Pause per B, Platzierung/Diagnose und Wechsel mit vorgehaltenen Tasten. Automatische Editorprüfungen sind kein getragener Controller- oder Quest-Performancetest.

Ergebnis: 2.191 native CHECK-Meldungen bestanden, darunter 30 neue Handrollen-Checks. APK 0.19.4/code43 gebaut und Signatur/Metadaten bestätigt. Noch nicht installiert, da die Quest bei Lieferprüfung nicht per ADB verbunden war.
