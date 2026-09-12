# V19.2 – Siegelverteilung und verständliches Zeitfenster

## Korrektur

Die V19.1-Auswahl `(Welle + Gegnerindex) % 3` kollidierte mit den Zweiergruppen und Fledermausplätzen: Wellen 3–5 boten dadurch überhaupt keine Siegel an. Die Auswahl zählt jetzt ausschließlich erfolgreich eröffnete Nachschubportale mit erreichbaren Siegelplätzen. Das erste geeignete Portal jeder Welle und danach jedes zweite geeignete Portal bieten Siegel an. Einzelgegner, Fledermausplätze und ungeeignete Siegelpositionen verbrauchen diesen Zähler nicht; abgebrochene erste Austritte ebenso wenig. Der Zähler wird beim Wellenbeginn und Reset zurückgesetzt.

Die Gegnerquote, Portal-/Crowd-Limits und Raumprüfungen bleiben unverändert. Wenn es im tatsächlichen Raum keinen geeigneten erreichbaren Siegelplatz gibt, bleibt das Portal ohne Siegel – die Regel erzwingt keine unsicheren Schussziele.

## Rückmeldung

- Einheitliches Drei-Sekunden-Fenster nach dem ersten Austritt, anstelle der bisherigen variablen drei bis sechs Sekunden. Die mögliche Wartezeit auf einen freien Austritt verlängert das Siegelzeitfenster nicht mehr.
- Kleiner, dem Spieler zugewandter `3 s / 2 s / 1 s`-Countdown direkt am Portal. Warme Schrift wird zum Ende rötlicher; das vorhandene modellierte Glutrelief verliert sichtbar Energie. Keine neuen Partikel oder zusätzlichen Portal-Kameras.
- Nach Ablauf: `ZEIT ABGELAUFEN / NACHSCHUB FOLGT`. Erst nach tatsächlichem erfolgreichem Nachschubaustritt: `NACHSCHUB / DURCHGELASSEN`.
- Rechtzeitiges Zerstören aller Siegel: `NACHSCHUB / VERHINDERT`. Der Countdown verschwindet sofort beim letzten gültigen Treffer, nicht erst mit dem Portal.
- Pause friert Anzeige und Zeitfenster. Ignorieren blockiert keine Welle. Portale schließen weiterhin nach ihrem endlichen Nachschub; bereits ausgetretene lebende Gegner bleiben bestehen.

## Prüfung

Neue Regression prüft die Verteilung über 100 Wellen und echte Produktionsbegegnungen in den betroffenen Wellen 3, 4 und 5. Echte Revolverschüsse prüfen Erfolg, ignorierte Siegel prüfen Nachschub und beide Meldungszeitpunkte, abgebrochener Nachschub darf keine falsche Erfolgsmeldung erzeugen. Countdown, Billboard-Ausrichtung, kompakte Abmessung, Glut-Zeitwert, Pause und Ablauf werden geprüft. Die gesamte bestehende Scan-/Portal-/Kampf-/Mimikregression bleibt Teil des Builds.

Finaler nativer Lauf: **2.106 CHECK-Meldungen bestanden**, davon 523 im erweiterten RhythmLife-Paket (168 mehr als V19.1) und zwei neue gerenderte Countdown-Ansichten. Beide Ansichten wurden angesehen. Ein früherer vollständiger Lauf wurde vor dem Export bewusst beendet, um das sofortige Ausblenden nach erfolgreichem letztem Schuss noch zu ergänzen; dessen Log bleibt als `interrupted-countdown-review.log` erhalten. Der finale Lauf enthält diese Korrektur und ihre Prüfung.

Native Ansichten: [drei Sekunden](../Verification/SealClarity/countdown-3s.png), [letzte Sekunde](../Verification/SealClarity/countdown-1s.png). Verbindlicher Build-/Installationsstatus im [Buildbericht](BUILD-REPORT.md).

Getragene Headset-Abnahme für Lesbarkeit und Spielrhythmus bleibt offen. Kurzer Test: in Wellen 3–5 auf das erste geeignete Nachschubportal achten, einmal alle Siegel ignorieren und einmal rechtzeitig zerstören. Es darf weiterhin keine Pflichtpause und keinen verschwundenen lebenden Gegner geben.
