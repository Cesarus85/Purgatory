# V18.20 – begrenzte Messlückentoleranz und konkrete Scan-Hinweise

Stand: 6. September 2026. Nutzerauftrag nach V18.19: Kleine Messlücken sollen nicht wie ein ganzer unbekannter Raum behandelt werden. **0.18.20 / code 38 um 20:07:23 auf Quest 3 installiert**, Signatur, Geräteversion und APK-Hash bestätigt; Daten/Berechtigungen erhalten, nicht gestartet. [Buildbericht](BUILD-REPORT.md), [Liefernachweis](../Verification/ScanGaps/delivery-v18.20.txt). Getragene Abnahme noch offen.

## Umgesetzter Korrekturblock

1. **Einzelne fehlende Freiraumpunkte tolerieren.** Ein unbekannter 8-cm-Rasterpunkt darf für eine Abfrage als gestützt frei gelten, wenn alle sechs direkt benachbarten Rohmessungen ausreichend frei sind. Abstandsmarge berücksichtigt zusätzlich ein halbes Rastermaß für die Quantisierung. Schwache Hindernishinweise im Zentrum oder an diagonalen Nachbarpunkten verwerfen die Ergänzung. Bekannte Werte werden nie überschrieben. Pro siebenpunktiger Körperprüfung höchstens zwei ergänzte Punkte; echte Collider und gemessene Hindernisse bleiben wirksam.
2. **Kleine Boden-/Wand-Netzlöcher überbrücken.** Nur kurze Boden- und Portalflächen-Probes (höchstens 55 cm) dürfen bei fehlendem direkten Mesh-Treffer ergänzen. Dafür müssen acht direkte Treffer rund um das Loch auf einem 10-cm-Ring vorliegen, nahezu gleiche Normalen und höchstens 2,5 cm Abstandsunterschied haben. Zusätzlich sind echte positive Tiefenwerte vor und negative hinter der Fläche notwendig; widersprüchliche Zentralwerte blockieren. Ein Portal darf höchstens zwei seiner 13 Flächenprüfungen so ergänzen. Bodenauflage und Portalpassung nutzen diesen Weg; lange Sicht-/Schussstrahlen bleiben unverändert.
3. **Gezielt sagen, was fehlt.** Freigabehinweise unterscheiden fehlende Bodenauflage, ungemessenen körperhohen Freiraum, fehlende Verbindung und überwiegend blockierte Bereiche. Die verzögerten Portal-Suchhinweise unterscheiden fehlende Wände, unbestätigte Austritte, unvollständige/passungswidrige Flächen und Portalwege. Kein Hinweis bei erfolgreicher Platzierung; die bestehende Verzögerung gegen Meldungsspam bleibt erhalten.

## Bewusste Grenzen

- Ergänzungen gelten nur für die jeweilige Abfrage. Sie werden weder in die Rohkarte geschrieben noch zur Stützung weiterer Ergänzungen verwendet. Zwei angrenzende unbekannte Rasterpunkte können sich nicht gegenseitig freigeben.
- Das sichtbare Netz zeigt weiterhin die rekonstruierte Messung und kann kleine Löcher behalten. Die Änderung macht die Spiellogik toleranter, nicht die Anzeige kosmetisch lückenlos. Kein Anspruch, jede kleine optische Lücke automatisch schließen zu können.
- Tatsächlich unbekannte größere Flächen, erkannte Hindernisse, größere Höhenwechsel und fehlende Tiefenbelege bleiben gesperrt. Keine pauschale Freigabe von Türöffnungen, Möbeln oder Absturzkanten.
- Mindestfreigabe weiterhin gemessener Boden und drei verbundene nutzbare Bereiche; danach **X zwei Sekunden halten**. Kein automatisches Beenden, keine Loslass-Geste. Raumkarte und Tiefenerfassung laufen nach Bestätigung weiter.
- Messsensor, TSDF-Fusion, 768 CPU-/128 GPU-Regionen, Arbeitsbudgets, Spielerkörper-Ausnahme, Guardian und Scan-Reichweite unverändert. Keine neue Gleichwertigkeitsbehauptung gegenüber Laser Tag.
- Zusätzliche Nachbar-/Ringprüfungen sind begrenzt, nur bei fehlenden Rohmessungen aktiv und ohne neue Listen/Arrays pro Abfrage. Bekannte freie Abfragen behalten die bestehenden Kollisionsprüfungen. Reale Quest-Framezeiten bleiben zu messen.

## Nachweise

Neue native `ScanGapsValidation` mit Kette zur gesamten V18.19-Regression. Fälle: Rohdaten unverändert, einzeln eingeschlossene Lücke, echter Körper-/Wegaufruf, benachbarte Lücken ohne Rekursion, schwaches Hindernis im Zentrum/diagonal, zu wenig Abstand, physischer Collider trotz Freiraumdaten, völlig unbekannter Raum, Körper-Reparaturbudget, Chunk-Grenze; künstliches Boden-/Wandloch mit echten Ring-Collidern und TSDF-Signaturen; echter Freiraum bzw. unbekannter Bereich hinter dem Loch, größeres Loch, inkonsistente Höhen; Produktions-Bodenauflage, Portalfläche und konkrete Freigabehinweise.

**1.550 native CHECK-Meldungen bestanden, davon 33 neue ScanGaps-Prüfungen.** Unity-Export erfolgreich. Das bestätigt synthetische positive/negative Geometriefälle und die bisherige Regression; die tatsächliche Verbesserung des Erfassungsaufwands im Nutzerraum und Quest-Framezeiten sind noch offen.

`QDMR_SCAN_GAPS` ergänzt alle zehn Sekunden kumulative Zähler für erfolgreich gestützte Abfragen und den aktuellen Hinweis. Die Zähler zählen **Abfragen**, nicht eindeutige reparierte Löcher; Reset bei Kartenreset. Kein Wachstum der Rohkarte durch diese Zähler.

## Kurze Headset-Abnahme

1. Nach gewöhnlichem Umschauen auf Freigabe und konkreten Hinweis achten. Es soll nicht nötig sein, jede kleine Netzaussparung nachzuscannen.
2. X zwei Sekunden halten; Portalverteilung und Wege an zuvor lückenhaften Stellen beobachten.
3. Möbel, schmale Durchgänge und größere noch ungescannte Bereiche müssen Gegner weiterhin begrenzen. Nicht absichtlich reale gefährliche Kanten zum Test aufsuchen.
4. Auf neue Ruckler bei der Erfassung und Platzsuche achten. Das ist eine funktionelle Korrektur, noch kein physisch belegter Performancegewinn.

Animationen, Waffen, Audio und übrige A10-Art bleiben unverändert. Kathedrale und ortsbezogene Rahmen weiterhin offen.
